using Microsoft.EntityFrameworkCore;
using PixelVault.Orders.Clients;
using PixelVault.Orders.Infrastructure;
using PixelVault.Orders.Repositories;
using PixelVault.Orders.Services;
using PixelVault.Shared.TestSupport;
using PixelVault.Shared.Web;

var builder = WebApplication.CreateBuilder(args);

builder.AddPixelVaultApi("Orders");

Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "App_Data"));
builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("OrdersDb")));

builder.Services.AddHttpClient<IBasketClient, BasketClient>(client => client.BaseAddress = new Uri("http://basket"));
builder.Services.AddHttpClient<IShippingClient, ShippingClient>(client => client.BaseAddress = new Uri("http://shipping"));
builder.Services.AddHttpClient<ICatalogClient, CatalogClient>(client => client.BaseAddress = new Uri("http://catalog"));
builder.Services.AddHttpClient<IPaymentClient, PaymentClient>(client => client.BaseAddress = new Uri("http://payment"));

builder.Services.AddScoped<IOrderRepository, OrderRepository>();

builder.Services.AddSingleton<ISalesTaxCalculator, SalesTaxCalculator>();
builder.Services.AddSingleton<ICustomerDetailsValidator, CustomerDetailsValidator>();
builder.Services.AddSingleton<IAgeVerificationPolicy, AgeVerificationPolicy>();
builder.Services.AddSingleton<IOrderNumberGenerator, OrderNumberGenerator>();
builder.Services.AddSingleton<IOrderStateMachine, OrderStateMachine>();
builder.Services.AddSingleton<ICancellationPolicy, CancellationPolicy>();
builder.Services.AddSingleton<IOrderMapper, OrderMapper>();
builder.Services.AddScoped<IOrderCompensation, OrderCompensation>();
builder.Services.AddScoped<ICheckoutService, CheckoutService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IAdminOrderService, AdminOrderService>();

builder.Services.AddScoped<IDataSeeder, OrdersSeeder>();

var app = builder.Build();

app.UsePixelVaultApi("Orders");
await app.InitializeDatabaseAsync();

app.Run();

public partial class Program;
