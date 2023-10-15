//     Copyright 2023 Yeong-won Seo
// 
//     Licensed under the Apache License, Version 2.0 (the "License");
//     you may not use this file except in compliance with the License.
//     You may obtain a copy of the License at
// 
//         http://www.apache.org/licenses/LICENSE-2.0
// 
//     Unless required by applicable law or agreed to in writing, software
//     distributed under the License is distributed on an "AS IS" BASIS,
//     WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//     See the License for the specific language governing permissions and
//     limitations under the License.

using Frouros.Net.Abstraction;
using Frouros.Net.Impls;
using Frouros.Net.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Configuration
       .AddJsonFile("settings.json");

builder.WebHost
       .ConfigureKestrel(options =>
       {
           options.ListenAnyIP(8080, opts =>
           {
               // HTTP2 / HTTP3 require a ssl certificate
               opts.Protocols = HttpProtocols.Http1;
           });
       });

builder.Services
       .AddSingleton<IPacketParser, PacketParser>()
       .AddSingleton<IPacketChannel, PacketLogChannel>()
       .AddHostedService<PacketLogAgent>();

var app     = builder.Build();
var channel = app.Services.GetRequiredService<IPacketChannel>();

app.MapGet("/", ctx =>
{
    return Task.Factory.StartNew(() =>
    {
        var written = channel.Read(ctx.Response.Body);

        ctx.Response.Headers.CacheControl = "no-cache";
        ctx.Response.ContentType          = "application/octet-stream";
        ctx.Response.ContentLength        = written;
    });
});

app.Run();