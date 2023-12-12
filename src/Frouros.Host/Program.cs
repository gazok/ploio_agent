using System.Runtime;
using System.Runtime.InteropServices;
using Frouros.Host.Bridges;
using Frouros.Host.Imports;
using Frouros.Host.Repositories;
using Frouros.Host.Repositories.Abstract;
using Frouros.Host.Services;
using Frouros.Host.Workers;
using Frouros.Shared;
using Microsoft.AspNetCore.Server.Kestrel.Core;

GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;

if (!File.Exists(Specials.ConfigPath))
{
    File.Copy(
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Properties/src.appsettings.json"),
        Specials.ConfigPath
    );
}

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile(Specials.ConfigPath);
builder.WebHost.ConfigureKestrel(options =>
{
    if (!OperatingSystem.IsWindows())
    {
        var dir = new DirectoryInfo(Specials.PipePath).Parent;
        if (!dir!.Exists) dir.Create();

        var file = new FileInfo(Specials.PipePath);
        if (file.Exists) file.Delete();
    }

    ((Action<string, Action<ListenOptions>>)(
            OperatingSystem.IsWindows()
                ? options.ListenNamedPipe
                : options.ListenUnixSocket))
       .Invoke(
            Specials.PipePath,
            opt => opt.Protocols = HttpProtocols.Http2
        );
});

builder.Services
       .AddSingleton<IApplicationInformation, ApplicationInformation>()
       .AddSingleton<IARPTable, ARPTable>()
       .AddSingleton<ICAMTable, CAMTable>()
       .AddSingleton<IPodAuthRepository, PodAuthRepository>()
       .AddSingleton<Netfilter>()
       .AddHostedService<CRIWorker>()
       .AddHostedService<PVIWorker>()
       .AddHostedService<PrivilegeWorker>()
       .AddGrpc();

var app = builder.Build();

app.MapGrpcService<CRIService>();
app.MapGrpcService<PVIService>();
app.MapGrpcService<ARPService>();

app.Run();