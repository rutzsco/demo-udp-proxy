using UdpProxy.Portal.Components;
using UdpProxy.Portal.Hubs;
using UdpProxy.Portal.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// Add SignalR with conditional Azure SignalR
var signalRBuilder = builder.Services.AddSignalR();
var azureSignalRConnectionString = builder.Configuration.GetConnectionString("AzureSignalR");

if (!string.IsNullOrEmpty(azureSignalRConnectionString) && 
    azureSignalRConnectionString != "YOUR_AZURE_SIGNALR_CONNECTION_STRING")
{
    signalRBuilder.AddAzureSignalR(azureSignalRConnectionString);
    Console.WriteLine("Using Azure SignalR Service");
}
else
{
    Console.WriteLine("Using local SignalR (Azure SignalR connection string not configured)");
}

// Add the background service for Service Bus processing (only if configured)
var serviceBusConnectionString = builder.Configuration["ServiceBusConnectionString"];
var serviceBusQueueName = builder.Configuration["ServiceBusQueueName"];

if (!string.IsNullOrEmpty(serviceBusConnectionString) && 
    !string.IsNullOrEmpty(serviceBusQueueName) &&
    serviceBusConnectionString != "YOUR_SERVICE_BUS_CONNECTION_STRING" &&
    serviceBusQueueName != "YOUR_QUEUE_NAME")
{
    builder.Services.AddHostedService<ServiceBusListenerService>();
    Console.WriteLine("Service Bus Listener Service enabled");
}
else
{
    Console.WriteLine("Service Bus Listener Service disabled (connection strings not configured)");
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

// Map SignalR Hub
app.MapHub<DeviceMessageHub>("/deviceMessageHub");

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(UdpProxy.Portal.Client._Imports).Assembly);

app.Run();
