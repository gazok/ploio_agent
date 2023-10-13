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