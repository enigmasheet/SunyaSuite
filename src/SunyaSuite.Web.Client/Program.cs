using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using SunyaSuite.Web.Client;
using SunyaSuite.Web.Client.Extensions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiUrl = builder.Configuration.GetValue<string>("ApiUrl") ?? builder.HostEnvironment.BaseAddress;

builder.Services.AddMudServices();
builder.Services.AddAuthServices();
builder.Services.AddHttpClients(apiUrl);
builder.Services.AddMenuService();
builder.Services.AddAppServiceClients();
builder.Services.AddSingleton(TimeProvider.System);

await builder.Build().RunAsync();
