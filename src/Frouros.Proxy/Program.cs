using System.Runtime;
using Frouros.Proxy.Bridges;
using Frouros.Proxy.Bridges.Abstract;
using Frouros.Proxy.Repositories;
using Frouros.Proxy.Repositories.Abstract;
using Frouros.Proxy.Services;
using Frouros.Proxy.Services.Abstract;
using Frouros.Proxy.Workers;
using Frouros.Proxy.Workers.Routing;

GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;

var builder = WebApplication.CreateBuilder(args);

builder.Services
        
       .AddSingleton<IMembrane, Membrane>()
        
       .AddSingleton<IApplicationInformation, ApplicationInformation>()
       .AddSingleton<IARPTable, ARPTable>()
       .AddSingleton<ICAMTable, CAMTable>()
       .AddSingleton<IModuleRepository, ModuleRepository>()
       .AddSingleton<IPodAuthRepository, PodAuthRepository>()
        
       .AddSingleton<IGrpcServiceProvider, GrpcServiceProvider>()
        
       .AddHostedService<LogRouting>()
       .AddHostedService<ModuleRouting>()
       .AddHostedService<PacketRouting>()
       .AddHostedService<PodRouting>()
        
       .AddHostedService<CRIWorker>()
       .AddHostedService<PVIWorker>()
       .AddHostedService<VMAWorker>()
        
       .AddGrpc();

var app = builder.Build();

app.MapGrpcService<ARPService>();

app.Run();
