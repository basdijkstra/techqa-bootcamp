using Microsoft.EntityFrameworkCore;
using PixelVault.Basket.Clients;
using PixelVault.Basket.Infrastructure;
using PixelVault.Basket.Repositories;
using PixelVault.Basket.Services;
using PixelVault.Shared.TestSupport;
using PixelVault.Shared.Web;

var builder = WebApplication.CreateBuilder(args);

builder.AddPixelVaultApi("Basket");

Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "App_Data"));
builder.Services.AddDbContext<BasketDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("BasketDb")));

builder.Services.AddHttpClient<ICatalogClient, CatalogClient>(client => client.BaseAddress = new Uri("http://catalog"));

builder.Services.AddScoped<IBasketRepository, BasketRepository>();
builder.Services.AddScoped<IPromoCodeRepository, PromoCodeRepository>();

builder.Services.AddSingleton<IBasketRules, BasketRules>();
builder.Services.AddSingleton<IBundleDiscountCalculator, BundleDiscountCalculator>();
builder.Services.AddSingleton<IPromoCodeEvaluator, PromoCodeEvaluator>();
builder.Services.AddSingleton<IBasketPricingCalculator, BasketPricingCalculator>();
builder.Services.AddScoped<IBasketService, BasketService>();

builder.Services.AddScoped<IDataSeeder, BasketSeeder>();

var app = builder.Build();

app.UsePixelVaultApi("Basket");
await app.InitializeDatabaseAsync();

app.Run();

public partial class Program;
