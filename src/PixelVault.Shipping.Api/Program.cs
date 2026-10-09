using Microsoft.EntityFrameworkCore;
using PixelVault.Shipping.Infrastructure;
using PixelVault.Shipping.Repositories;
using PixelVault.Shipping.Services;
using PixelVault.Shared.TestSupport;
using PixelVault.Shared.Web;

var builder = WebApplication.CreateBuilder(args);

builder.AddPixelVaultApi("Shipping");

Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "App_Data"));
builder.Services.AddDbContext<ShippingDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("ShippingDb")));

builder.Services.AddScoped<IQuoteRepository, QuoteRepository>();

builder.Services.AddSingleton<IAddressValidator, AddressValidator>();
builder.Services.AddSingleton<IShippingZoneResolver, ShippingZoneResolver>();
builder.Services.AddSingleton<IShippingCostCalculator, ShippingCostCalculator>();
builder.Services.AddSingleton<IHolidayCalendar, UsFederalHolidayCalendar>();
builder.Services.AddSingleton<IDeliveryDateEstimator, DeliveryDateEstimator>();
builder.Services.AddScoped<IQuoteService, QuoteService>();

builder.Services.AddScoped<IDataSeeder, ShippingSeeder>();

var app = builder.Build();

app.UsePixelVaultApi("Shipping");
await app.InitializeDatabaseAsync();

app.Run();

public partial class Program;
