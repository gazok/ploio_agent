using System.Net;
using System.Runtime;
using Frouros.Proxy.Bridges;
using Frouros.Proxy.Bridges.Abstract;
using Frouros.Proxy.Repositories;
using Frouros.Proxy.Repositories.Abstract;
using Frouros.Proxy.Services;
using Frouros.Proxy.Services.Abstract;
using Frouros.Proxy.Workers;
using Frouros.Proxy.Workers.Routing;
using Microsoft.AspNetCore;

GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices(services =>
    services
       .AddSingleton<HttpClient>(_ => new HttpClient(new SocketsHttpHandler
        {
            AllowAutoRedirect              = true,
            AutomaticDecompression         = DecompressionMethods.All,
            EnableMultipleHttp2Connections = true
        }, true))
       .AddSingleton<IMembrane, Membrane>()
       .AddSingleton<IModuleRepository, ModuleRepository>()
       .AddSingleton<IPodAuthRepository, PodAuthRepository>()
       .AddSingleton<IGrpcServiceProvider, GrpcServiceProvider>()
       .AddSingleton<LogRouting>()
       .AddSingleton<ModuleRouting>()
       .AddSingleton<PacketRouting>()
       .AddSingleton<PodRouting>()
       .AddHostedService<BackgroundServiceWrapper<LogRouting>>()
       .AddHostedService<BackgroundServiceWrapper<ModuleRouting>>()
       .AddHostedService<BackgroundServiceWrapper<PacketRouting>>()
       .AddHostedService<BackgroundServiceWrapper<PodRouting>>()
       .AddHostedService<CRIWorker>()
       .AddHostedService<PVIWorker>()
       .AddHostedService<VMAWorker>());

var app = builder.Build();

app.Run();