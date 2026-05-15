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
builder.Services.AddHostedService<UsbPcapSupervisor>();

builder.Logging.AddEventLog(settings =>
{
    settings.SourceName = "OpenSAABCollector";
    // Default EventLog filter is LogLevel.Warning. Drop to Information so
    // "OpenSAAB Collector starting. Watch=…" + "Uploaded …" entries land in
    // Application Event Log too — useful when contributors hit weird state
    // and we need a paper trail without redeploying for diagnostics.
    settings.Filter = (_, level) => level >= LogLevel.Information;
});

var host = builder.Build();
host.Run();
