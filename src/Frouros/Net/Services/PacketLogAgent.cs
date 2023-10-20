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

using System.Text;
using Frouros.Net.Abstraction;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;

namespace Frouros.Net.Services;

public class PacketLogAgent : IHostedService, IDisposable
{
    private readonly LibPcapLiveDevice _dev;
    private readonly IPacketChannel    _channel;
    private readonly IPacketParser     _parser;

    private readonly ILogger<PacketLogAgent> _logger;

    public PacketLogAgent(
        ILogger<PacketLogAgent> logger,
        IConfiguration config, 
        IPacketChannel channel, 
        IPacketParser parser)
    {
        _logger = logger;
        
        _channel = channel;
        _parser  = parser;

        try
        {
            var name = config["Device"] ?? throw new KeyNotFoundException();
            _dev = LibPcapLiveDeviceList.Instance[name];
        }
        catch (KeyNotFoundException e)
        {
            // fast-fail
            throw new ArgumentException("network interface name must be provided", nameof(config), e);
        }
        catch (IndexOutOfRangeException e)
        {
            // fast-fail
            throw new ArgumentOutOfRangeException($"network interface '{config["Device"]}' not found.", e);
        }
    }

    private void OnPacketArrival(object sender, PacketCapture args)
    {
        var ts = args.Header.Timeval.Date;
        
        var pkt = args.GetPacket().GetPacket();

        var ret = _parser.TryParse(ts, pkt, out var log);
        if (ret is false)
        {
            _logger.LogWarning("couldn't parse packet");
            return;
        }
        if (ret is null) 
            return;
            
        _channel.Write(log.Value);
        _logger.LogTrace("{}", log);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Start capturing on '{}'", _dev.Name);
        
        _dev.Open();
        _dev.OnPacketArrival += OnPacketArrival;
        _dev.StartCapture();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _dev.StopCapture();
        _dev.Close();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _dev.Dispose();
    }
}