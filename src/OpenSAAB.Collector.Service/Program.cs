using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenSAAB.Collector.Service;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "OpenSAABCollector";
});

builder.Services.AddSingleton<InstallSettings>(_ => InstallSettings.Load());
builder.Services.AddSingleton<Uploader>();
builder.Services.AddHostedService<Worker>();

builder.Logging.AddEventLog(settings =>
{
    settings.SourceName = "OpenSAABCollector";
});

var host = builder.Build();
host.Run();
