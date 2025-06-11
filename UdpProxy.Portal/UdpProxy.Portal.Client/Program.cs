using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using UdpProxy.Portal.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Register services
builder.Services.AddSingleton<MessageStatisticsService>();

await builder.Build().RunAsync();
