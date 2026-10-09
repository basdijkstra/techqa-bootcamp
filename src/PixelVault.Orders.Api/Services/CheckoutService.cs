using PixelVault.Orders.Clients;
using PixelVault.Orders.Domain;
using PixelVault.Orders.Dtos;
using PixelVault.Orders.Repositories;
using PixelVault.Shared.Errors;

namespace PixelVault.Orders.Services;

public interface ICheckoutService
{
    /// <summary>Works out the order totals (shipping, tax) without placing the order.</summary>
    Task<OrderPreviewDto> PreviewAsync(PreviewOrderRequest request, CancellationToken cancellationToken = default);

    Task<OrderDto> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Places an order. The steps, in order:
/// <list type="number">
/// <item>Validate the customer details and the payment card is present.</item>
/// <item>Get the basket. It must be active, not empty and without issues.</item>
/// <item>Check the age requirement for M and AO games.</item>
/// <item>Get the shipping quote. It must exist, not be expired, offer the chosen method and match the basket.</item>
/// <item>Calculate sales tax and the total.</item>
/// <item>Generate a unique order number and reserve the stock.</item>
/// <item>Save the order as Pending and charge the card.</item>
/// <item>Declined or failed payment: release the stock and mark the order PaymentFailed.</item>
/// <item>Approved payment: mark the order Paid, commit the stock reservation and close the basket.</item>
/// </list>
/// </summary>
public class CheckoutService(
    IOrderRepository orders,
    IBasketClient basketClient,
    IShippingClient shippingClient,
    ICatalogClient catalogClient,
    IPaymentClient paymentClient,
    ICustomerDetailsValidator customerValidator,
    IAgeVerificationPolicy agePolicy,
    ISalesTaxCalculator taxCalculator,
    IOrderNumberGenerator orderNumberGenerator,
    IOrderMapper mapper,
    TimeProvider timeProvider,
    ILogger<CheckoutService> logger) : ICheckoutService
{
    public const int MaxOrderNumberAttempts = 5;

    private sealed record PricedCheckout(
        BasketSnapshot Basket,
        ShippingQuoteSnapshot Quote,
        ShippingOptionSnapshot Option,
        decimal TaxRate,
        decimal TaxAmount,
        decimal Total);

    public async Task<OrderPreviewDto> PreviewAsync(PreviewOrderRequest request, CancellationToken cancellationToken = default)
    {
        var priced = await PriceAsync(request.BasketId, request.ShippingQuoteId, request.ShippingMethod, cancellationToken);
        return new OrderPreviewDto(
            priced.Basket.Subtotal,
            priced.Basket.DiscountTotal,
            priced.Basket.DiscountDescription,
            priced.Option.Method,
            priced.Option.Cost,
            priced.TaxRate,
            priced.TaxAmount,
            priced.Total,
            priced.Option.ShipDate,
            priced.Option.EstimatedDeliveryDate,
            priced.Basket.MinimumAge,
            priced.Basket.MinimumAge >= AgeVerificationPolicy.AgeCheckThreshold);
    }

    public async Task<OrderDto> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken cancellationToken = default)
    {
        var customer = customerValidator.Validate(request.Customer);
        if (request.Payment is null)
        {
            throw new ValidationException("payment", "Payment details are required.");
        }

        var now = timeProvider.GetUtcNow();
        var priced = await PriceAsync(request.BasketId, request.ShippingQuoteId, request.ShippingMethod, cancellationToken);
        agePolicy.EnsureOldEnough(priced.Basket.MinimumAge, request.AgeVerification?.DateOfBirth, DateOnly.FromDateTime(now.UtcDateTime));

        var orderNumber = await GenerateUniqueOrderNumberAsync(now, cancellationToken);
        await catalogClient.ReserveStockAsync(
            orderNumber,
            priced.Basket.Items.Select(i => new ReservationItem(i.GameId, i.Quantity)),
            cancellationToken);

        var order = CreatePendingOrder(orderNumber, request.BasketId, customer, priced, now);
        orders.Add(order);
        await orders.SaveChangesAsync(cancellationToken);

        ChargeResult charge;
        try
        {
            charge = await paymentClient.ChargeAsync(
                orderNumber,
                new ChargeRequest(orderNumber, order.Total, customer.Email, new CardDetails(
                    request.Payment.Number, request.Payment.CardholderName, request.Payment.ExpiryMonth,
                    request.Payment.ExpiryYear, request.Payment.Cvv)),
                cancellationToken);
        }
        catch (DomainException ex)
        {
            await FailOrderAsync(order, ex.Code, cancellationToken);
            if (ex is DownstreamServiceException || ex.StatusCode >= 500)
            {
                throw new DomainStatusException(502, "PAYMENT_UNAVAILABLE",
                    "We could not reach the payment provider. No money was taken. Please try again.");
            }

            throw;
        }

        if (charge.Outcome == ChargeOutcome.Declined)
        {
            await FailOrderAsync(order, charge.DeclineCode ?? "CARD_DECLINED", cancellationToken);
            var declined = new DomainStatusException(402, "PAYMENT_DECLINED", charge.Message ?? "Your card was declined.");
            declined.Details["declineCode"] = charge.DeclineCode;
            declined.Details["orderNumber"] = orderNumber;
            throw declined;
        }

        order.Status = OrderStatus.Paid;
        order.PaidAt = timeProvider.GetUtcNow();
        order.PaymentId = charge.PaymentId;
        order.CardBrand = charge.CardBrand;
        order.CardLast4 = charge.CardLast4;
        order.History.Add(new OrderStatusChange
        {
            FromStatus = OrderStatus.Pending, ToStatus = OrderStatus.Paid, ChangedAt = order.PaidAt.Value, ChangedBy = "system",
            Note = "Payment approved"
        });
        await orders.SaveChangesAsync(cancellationToken);

        await RunAfterPaymentStepAsync(() => catalogClient.CommitReservationAsync(orderNumber, cancellationToken), "commit stock", orderNumber);
        await RunAfterPaymentStepAsync(() => basketClient.CheckoutBasketAsync(request.BasketId, orderNumber, cancellationToken), "close basket", orderNumber);

        return mapper.ToDto(order, timeProvider.GetUtcNow());
    }

    private async Task<PricedCheckout> PriceAsync(Guid basketId, Guid quoteId, string? method, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(method))
        {
            throw new ValidationException("shippingMethod", "Shipping method is required.");
        }

        var basket = await basketClient.GetBasketAsync(basketId, cancellationToken);
        if (basket.Status != "Active")
        {
            throw new ConflictException("BASKET_CHECKED_OUT", "This basket has already been checked out.");
        }

        if (basket.Items.Count == 0)
        {
            throw new BusinessRuleException("BASKET_EMPTY", "Your basket is empty.");
        }

        if (basket.HasIssues)
        {
            throw new ConflictException("BASKET_HAS_ISSUES", "Some items in your basket are no longer available. Please review your basket.");
        }

        var quote = await shippingClient.GetQuoteAsync(quoteId, cancellationToken)
                    ?? throw new BusinessRuleException("SHIPPING_QUOTE_NOT_FOUND", "The shipping quote was not found. Please calculate shipping again.");

        if (quote.IsExpired)
        {
            throw new ConflictException("SHIPPING_QUOTE_EXPIRED", "Your shipping quote has expired. Please calculate shipping again.");
        }

        if (quote.ItemCount != basket.ItemCount || quote.OrderSubtotal != basket.Total)
        {
            throw new ConflictException("SHIPPING_QUOTE_OUTDATED", "Your basket changed after shipping was calculated. Please calculate shipping again.");
        }

        var option = quote.Options.FirstOrDefault(o => string.Equals(o.Method, method.Trim(), StringComparison.OrdinalIgnoreCase))
                     ?? throw new BusinessRuleException("SHIPPING_METHOD_UNAVAILABLE", $"{method} shipping is not available for this address.");

        var rule = taxCalculator.GetRule(quote.Address.Country, quote.Address.Region);
        var tax = taxCalculator.CalculateTax(quote.Address.Country, quote.Address.Region, basket.Total, option.Cost);
        var total = basket.Total + option.Cost + tax;

        return new PricedCheckout(basket, quote, option, rule.Rate, tax, total);
    }

    private async Task<string> GenerateUniqueOrderNumberAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxOrderNumberAttempts; attempt++)
        {
            var candidate = orderNumberGenerator.Generate(now);
            if (!await orders.ExistsAsync(candidate, cancellationToken))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException($"Could not generate a unique order number in {MaxOrderNumberAttempts} attempts.");
    }

    private static Order CreatePendingOrder(string orderNumber, Guid basketId, ValidCustomerDetails customer, PricedCheckout priced, DateTimeOffset now)
    {
        var address = priced.Quote.Address;
        var order = new Order
        {
            OrderNumber = orderNumber,
            BasketId = basketId,
            Status = OrderStatus.Pending,
            CustomerName = customer.FullName,
            CustomerEmail = customer.Email,
            CustomerPhone = customer.Phone,
            ShippingLine1 = address.Line1,
            ShippingLine2 = address.Line2,
            ShippingCity = address.City,
            ShippingRegion = address.Region,
            ShippingPostalCode = address.PostalCode,
            ShippingCountry = address.Country,
            ShippingMethod = priced.Option.Method,
            ShippingCost = priced.Option.Cost,
            EstimatedShipDate = priced.Option.ShipDate,
            EstimatedDeliveryDate = priced.Option.EstimatedDeliveryDate,
            Subtotal = priced.Basket.Subtotal,
            DiscountTotal = priced.Basket.DiscountTotal,
            DiscountDescription = priced.Basket.DiscountDescription,
            TaxRate = priced.TaxRate,
            TaxAmount = priced.TaxAmount,
            Total = priced.Total,
            CreatedAt = now,
            Lines = priced.Basket.Items.Select(i => new OrderLine
            {
                GameId = i.GameId,
                Title = i.Title,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                LineTotal = i.LineTotal,
                IsPreOrder = i.IsPreOrder,
                ReleaseDate = i.ReleaseDate
            }).ToList()
        };

        order.History.Add(new OrderStatusChange { FromStatus = null, ToStatus = OrderStatus.Pending, ChangedAt = now, ChangedBy = "customer", Note = "Order placed" });
        return order;
    }

    private async Task FailOrderAsync(Order order, string reason, CancellationToken cancellationToken)
    {
        try
        {
            await catalogClient.ReleaseReservationAsync(order.OrderNumber, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not release stock for failed order {OrderNumber}", order.OrderNumber);
        }

        order.Status = OrderStatus.PaymentFailed;
        order.PaymentFailureCode = reason;
        order.History.Add(new OrderStatusChange
        {
            FromStatus = OrderStatus.Pending, ToStatus = OrderStatus.PaymentFailed, ChangedAt = timeProvider.GetUtcNow(),
            ChangedBy = "system", Note = $"Payment failed: {reason}"
        });
        await orders.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Steps after a successful payment must not fail the order: the customer has paid. Problems are logged.
    /// </summary>
    private async Task RunAfterPaymentStepAsync(Func<Task> step, string description, string orderNumber)
    {
        try
        {
            await step();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not {Step} for paid order {OrderNumber}", description, orderNumber);
        }
    }
}
