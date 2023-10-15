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

using System.Buffers;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using RouteHandler = Frouros.Web.RouteHandler;

var builder = WebApplication.CreateSlimBuilder(args);

builder.WebHost
       .ConfigureAppConfiguration(options =>
        {
            var basedir  = AppDomain.CurrentDomain.BaseDirectory;
            var filepath = Path.Combine(basedir, "config.json");
            options.AddJsonFile(filepath, true);
        })
       .ConfigureKestrel(options =>
        {
            options.ListenAnyIP(8080, opts =>
            {
                // HTTP2 / HTTP3 require a ssl certificate
                opts.Protocols = HttpProtocols.Http1;
            });
        });

var app = builder.Build();

app.MapGet("/", async ctx =>
{
    using var data   = await RouteHandler.RunAsync(ctx);
    var       buffer = data.GetBuffer();

    ctx.Response.Headers.CacheControl = "no-cache";

    ctx.Response.ContentType   = "application/octet-stream";
    ctx.Response.ContentLength = buffer.Length;

    ctx.Response.BodyWriter.Write(buffer);
});

app.Run();