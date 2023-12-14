namespace Frouros.Proxy.Workers;

public class BackgroundServiceWrapper<T>(T singleton) : IHostedService where T : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return singleton.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return singleton.StopAsync(cancellationToken);
    }
}