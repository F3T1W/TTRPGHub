using BlazorBootstrap;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Refit;
using TTRPGHub.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<TTRPGHub.App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBase = new Uri(builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5014");

builder.Services.AddScoped<TokenStorage>();
builder.Services.AddScoped<AuthHeaderHandler>();
builder.Services.AddScoped<AppAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(
    sp => sp.GetRequiredService<AppAuthStateProvider>());

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

builder.Services
    .AddRefitClient<IApiClient>()
    .ConfigureHttpClient(c => c.BaseAddress = apiBase)
    .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddBlazorBootstrap();

await builder.Build().RunAsync();
