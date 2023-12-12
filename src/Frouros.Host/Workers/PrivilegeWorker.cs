using Frouros.Host.Imports;
using Frouros.Shared;

namespace Frouros.Host.Workers;

public class PrivilegeWorker(IConfiguration configuration, ILogger<PrivilegeWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (!File.Exists(Specials.PipePath)) 
                await Task.Delay(100, token);
            
            var user  = configuration.GetValue<uint>("User");
            var group = configuration.GetValue<uint>("Group");
            if (Native.ChangeOwner(Specials.PipePath, user, group) != 0 ||
                Native.ChangeAccessControl(Specials.PipePath, 0x1B0 /* 660 */) != 0)
            {
                logger.LogCritical("Couldn't change ACL of pipe: '{}'", Native.GetLastError());
            }
        }
    }
}