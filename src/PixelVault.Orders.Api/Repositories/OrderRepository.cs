using Microsoft.EntityFrameworkCore;
using PixelVault.Orders.Domain;
using PixelVault.Orders.Infrastructure;

namespace PixelVault.Orders.Repositories;

/// <summary>Filter for the admin order list. <c>To</c> is exclusive.</summary>
public record OrderFilter(OrderStatus? Status, DateTimeOffset? From, DateTimeOffset? To, string? Search);

public interface IOrderRepository
{
    /// <summary>Gets an order with its lines and history, or null.</summary>
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string orderNumber, CancellationToken cancellationToken = default);

    /// <summary>Orders matching the filter, newest first, plus the total number of matches.</summary>
    Task<(IReadOnlyList<Order> Items, int TotalCount)> FindAsync(OrderFilter filter, int skip, int take, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken = default);

    void Add(Order order);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class OrderRepository(OrdersDbContext db) : IOrderRepository
{
    public Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default) =>
        db.Orders
            .Include(o => o.Lines)
            .Include(o => o.History)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);

    public Task<bool> ExistsAsync(string orderNumber, CancellationToken cancellationToken = default) =>
        db.Orders.AnyAsync(o => o.OrderNumber == orderNumber, cancellationToken);

    public async Task<(IReadOnlyList<Order> Items, int TotalCount)> FindAsync(OrderFilter filter, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = db.Orders.Include(o => o.Lines).AsQueryable();

        if (filter.Status is { } status)
        {
            query = query.Where(o => o.Status == status);
        }

        if (filter.From is { } from)
        {
            query = query.Where(o => o.CreatedAt >= from);
        }

        if (filter.To is { } to)
        {
            query = query.Where(o => o.CreatedAt < to);
        }

        if (filter.Search is { } search)
        {
            var pattern = $"%{search}%";
            query = query.Where(o =>
                EF.Functions.Like(o.OrderNumber, pattern)
                || EF.Functions.Like(o.CustomerEmail, pattern)
                || EF.Functions.Like(o.CustomerName, pattern));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .ThenByDescending(o => o.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await db.Orders.ToListAsync(cancellationToken);

    public void Add(Order order) => db.Orders.Add(order);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => db.SaveChangesAsync(cancellationToken);
}
