using CentrED.Network;
using CentrED.Server.Config;

namespace CentrED.Server;

public static class ServerNetState
{
    public static Account Account(this NetState<CEDServer> ns)
    {
        return ns.Parent.GetAccount(ns)!;
    }

    public static AccessLevel AccessLevel(this NetState<CEDServer> ns)
    {
        return ns.Account().AccessLevel;
    }

    public static DateTime LastLogon(this NetState<CEDServer> ns)
    {
        return ns.Account().LastLogon;
    }
}