using Frouros.Net.Models;

var wh = new EventWaitHandle(false, EventResetMode.ManualReset);

CancellationToken tkn;

{
    var cts = new CancellationTokenSource();
    tkn = cts.Token;
    Console.CancelKeyPress += (_, _) =>
    {
        cts.Cancel();
        wh.WaitOne(1000);
        wh.Dispose();
        cts.Dispose();
    };
}

unsafe
{
    Console.WriteLine(sizeof(UInt128));
}

Console.WriteLine("hello...");

using (var client = new HttpClient())
{
    while (!tkn.IsCancellationRequested)
    {
        await Task.Delay(2000, tkn);
        
        await using var stream = await client.GetStreamAsync("http://localhost:65001/", tkn);
        if (!PacketDumpHeader.TryReadFrom(stream, out var hdr))
        {
            Console.WriteLine("E: invalid stream");
            continue;
        }

        foreach (var log in PacketLog.ReadFrom(stream, hdr.Value.Count))
            if (log.LX == LxProto.HTTP)
                Console.WriteLine(log.ToString());
    }
}

wh.Set();
