using Shared;

namespace Server; 

public class Block {
    private readonly HashSet<NetState> _subscribers;

    public Block(LandBlock land, StaticBlock statics) {
        LandBlock = land;
        StaticBlock = statics;
        _subscribers = new HashSet<NetState>();
    }
    
    public LandBlock LandBlock { get; }
    public StaticBlock StaticBlock { get; }

    public HashSet<NetState> Subscribers {
        get {
            _subscribers.RemoveWhere(ns => !ns.IsConnected);
            return _subscribers;
        }
    }
}