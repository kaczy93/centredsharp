using System.Net.Sockets;
using CentrED;
using CentrED.Network;

namespace Shared.Tests;

public class DummyLogging : ILogging
{
    public void LogInfo(string message)
    {
    }

    public void LogWarn(string message)
    {
    }

    public void LogError(string message)
    {
    }

    public void LogDebug(string message)
    {
    }
}

public class DummyNetstate : NetState<DummyLogging>
{
    public DummyNetstate() : base(new DummyLogging(), null, 1024, 1024)
    {
    }

    public Pipe GetRecvPipe => RecvPipe;
    public Pipe GetSendPipe => SendPipe;

    public void InvokeProcessBuffer()
    {
        ProcessBuffer();   
    }
}