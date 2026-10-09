using Microsoft.EntityFrameworkCore;
using PixelVault.Payment.Infrastructure;
using PixelVault.Payment.Repositories;
using PixelVault.Payment.Services;
using PixelVault.Shared.TestSupport;
using PixelVault.Shared.Web;

var builder = WebApplication.CreateBuilder(args);

builder.AddPixelVaultApi("Payment");

Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "App_Data"));
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("PaymentDb")));

builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

builder.Services.AddSingleton<ICardValidator, CardValidator>();
builder.Services.AddSingleton<IFraudScorer, FraudScorer>();
builder.Services.AddSingleton<IPaymentProcessor, SimulatedPaymentProcessor>();
builder.Services.AddSingleton<IFaultInjector, FaultInjector>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Services.AddScoped<IDataSeeder, PaymentSeeder>();

var app = builder.Build();

app.UsePixelVaultApi("Payment");
await app.InitializeDatabaseAsync();

app.Run();

public partial class Program;
