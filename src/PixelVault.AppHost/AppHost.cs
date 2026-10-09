// Starts every PixelVault service and the React frontend. Run with: dotnet run --project src/PixelVault.AppHost
// The Aspire dashboard (http://localhost:15888) shows logs, traces and health for everything.

var builder = DistributedApplication.CreateBuilder(args);

var catalog = builder.AddProject<Projects.PixelVault_Catalog_Api>("catalog")
    .WithHttpHealthCheck("/health");

var basket = builder.AddProject<Projects.PixelVault_Basket_Api>("basket")
    .WithReference(catalog)
    .WaitFor(catalog)
    .WithHttpHealthCheck("/health");

var shipping = builder.AddProject<Projects.PixelVault_Shipping_Api>("shipping")
    .WithHttpHealthCheck("/health");

var payment = builder.AddProject<Projects.PixelVault_Payment_Api>("payment")
    .WithHttpHealthCheck("/health");

var identity = builder.AddProject<Projects.PixelVault_Identity_Api>("identity")
    .WithHttpHealthCheck("/health");

var orders = builder.AddProject<Projects.PixelVault_Orders_Api>("orders")
    .WithReference(catalog)
    .WithReference(basket)
    .WithReference(shipping)
    .WithReference(payment)
    .WaitFor(catalog)
    .WaitFor(basket)
    .WaitFor(shipping)
    .WaitFor(payment)
    .WithHttpHealthCheck("/health");

// The browser calls every service directly, so the frontend gets each service's URL.
builder.AddViteApp("web", "../web")
    .WithNpm(install: true)
    .WithEndpoint("http", endpoint => endpoint.Port = 5173)
    .WithReference(catalog)
    .WithReference(basket)
    .WithReference(shipping)
    .WithReference(payment)
    .WithReference(identity)
    .WithReference(orders)
    .WaitFor(catalog)
    .WaitFor(basket)
    .WaitFor(shipping)
    .WaitFor(identity)
    .WaitFor(orders);

builder.Build().Run();
