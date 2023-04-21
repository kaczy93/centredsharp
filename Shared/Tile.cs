namespace CentrED;

public abstract class Tile<TBlock> : Tile where TBlock : WorldBlock  {
    private bool _locked;
    private TBlock? _owner;
    private bool _selected;

    protected Tile(TBlock? owner) {
        Selected = false;
        Locked = false;
        Owner = owner;
    }

    public TBlock? Owner {
        get => _owner;
        set {
            if (_owner != value) {
                if (_owner != null) {
                    _owner.Changed = true;
                    if (Locked) _owner.RemoveRef();
                    if (Selected) _owner.RemoveRef();
                }

                _owner = value;
                if (_owner != null) {
                    _owner.Changed = true;
                    if (Locked) _owner.AddRef();
                    if (Selected) _owner.AddRef();
                }
            }
        }
    }

    public bool Selected {
        get => _selected;
        set {
            if (Owner != null && _selected != value) {
                if (value) Owner.AddRef();
                else Owner.RemoveRef();
            }

            _selected = value;
        }
    }

    public bool Locked {
        get => _locked;
        set {
            if (_locked != value) {
                _locked = value;
                if (Owner != null) {
                    if (_locked)
                        Owner.AddRef();
                    else
                        Owner.RemoveRef();
                }
            }
        }
    }

    protected override void DoChanged() {
        if (Owner != null) Owner.Changed = true;
    }

    public override void Delete() {
        Selected = false;
        Locked = false;
        base.Delete();
    }
}

public abstract class Tile : IComparable<Tile>  {
    protected ushort _tileId;
    protected ushort _x;
    protected ushort _y;
    protected sbyte _z;

    public virtual ushort TileId {
        get => _tileId;
        set {
            if (_tileId != value) {
                _tileId = value;
                DoChanged();
            }
        }
    }

    public virtual ushort X {
        get => _x;
        set {
            if (_x != value) {
                _x = value;
                DoChanged();
            }
        }
    }

    public virtual ushort Y {
        get => _y;
        set {
            if (_y != value) {
                _y = value;
                DoChanged();
            }
        }
    }

    public virtual sbyte Z {
        get => _z;
        set {
            if (_z != value) {
                _z = value;
                DoChanged();
            }
        }
    }

    public int Priority { get; set; }
    public sbyte PriorityBonus { get; set; }
    public int PrioritySolver { get; set; }

    public int CompareTo(Tile? other) {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;

        if (X != other.X) return X - other.X;
        if (Y != other.Y) return Y - other.Y;
        var result = Priority.CompareTo(other.Priority);
        if (result != 0) {
            if (this is LandTile && other is StaticTile) return -1;
            if (this is StaticTile && other is LandTile) return 1;
            if (this is LandTile) return -1;
            if (other is LandTile) return 1;
        }

        return PrioritySolver - other.PrioritySolver;
    }

    public void UpdatePos(ushort x, ushort y, sbyte z) {
        _x = x;
        _y = y;
        _z = z;
        DoChanged();
    }

    public virtual void Delete() {
        DoChanged();
    }

    protected abstract void DoChanged();

    public abstract void Write(BinaryWriter writer);
}