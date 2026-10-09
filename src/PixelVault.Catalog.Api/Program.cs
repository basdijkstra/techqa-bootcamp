using Microsoft.EntityFrameworkCore;
using PixelVault.Catalog.Infrastructure;
using PixelVault.Catalog.Repositories;
using PixelVault.Catalog.Services;
using PixelVault.Shared.TestSupport;
using PixelVault.Shared.Web;

var builder = WebApplication.CreateBuilder(args);

builder.AddPixelVaultApi("Catalog");

Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "App_Data"));
builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("CatalogDb")));

builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<IStockAdjustmentRepository, StockAdjustmentRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();

builder.Services.AddSingleton<IPricingCalculator, PricingCalculator>();
builder.Services.AddSingleton<IStockStatusResolver, StockStatusResolver>();
builder.Services.AddSingleton<IGameMapper, GameMapper>();
builder.Services.AddScoped<IGameCatalogService, GameCatalogService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IReservationService, ReservationService>();

builder.Services.AddScoped<IDataSeeder, CatalogSeeder>();

var app = builder.Build();

app.UsePixelVaultApi("Catalog");
await app.InitializeDatabaseAsync();

app.Run();

public partial class Program;
