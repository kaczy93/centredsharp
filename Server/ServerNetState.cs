using CentrED.Network;
using CentrED.Server.Config;

namespace CentrED.Server;

public static class ServerNetState
{
    public static bool ValidateAccess(this NetState<CEDServer> ns, AccessLevel accessLevel)
    {
        return ns.AccessLevel() >= accessLevel;
    }
    
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