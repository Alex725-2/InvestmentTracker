using Blazored.LocalStorage;
using InvestmentTracker.Client;
using InvestmentTracker.Client.Handlers;
using InvestmentTracker.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// HttpClient с автоматической подстановкой токена
builder.Services.AddScoped<AuthHandler>();
builder.Services.AddHttpClient("ServerApi", client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
}).AddHttpMessageHandler<AuthHandler>();

// Все страницы получают именно этот HttpClient
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("ServerApi"));

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();