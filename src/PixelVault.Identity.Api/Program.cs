using Microsoft.EntityFrameworkCore;
using PixelVault.Identity.Infrastructure;
using PixelVault.Identity.Repositories;
using PixelVault.Identity.Services;
using PixelVault.Shared.TestSupport;
using PixelVault.Shared.Web;

var builder = WebApplication.CreateBuilder(args);

builder.AddPixelVaultApi("Identity");

Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "App_Data"));
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("IdentityDb")));

builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddSingleton<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<IDataSeeder, IdentitySeeder>();

var app = builder.Build();

app.UsePixelVaultApi("Identity");
await app.InitializeDatabaseAsync();

app.Run();

public partial class Program;
