using System.Collections;
using CentrED.Network;

namespace CentrED;

// It iterates over a tiles in specified range, but in chunk-by-chunk order which is optimised in favor of cedserver
public class TileRange(ushort x1, ushort y1, ushort x2, ushort y2) : IEnumerable<(ushort x, ushort y)>
{
    public TileRange(AreaInfo area) : this(area.Left, area.Top, area.Right, area.Bottom)
    {
    }
    
    public IEnumerator<(ushort, ushort)> GetEnumerator()
    {
        return new TileRangeEnumerator(x1, y1, x2, y2);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public class TileRangeEnumerator(ushort x1, ushort y1, ushort x2, ushort y2) : IEnumerator<(ushort x, ushort y)>
{
    public TileRangeEnumerator(AreaInfo area) : this(area.Left, area.Top, area.Right, area.Bottom)
    {
    }
    
    private readonly int _XDelta = (sbyte)(x1 < x2 ? 1 : -1);
    private readonly int _YDelta = (sbyte)(y1 < y2 ? 1 : -1);
    private readonly int _NewChunkXMask = 0x7; 
    private readonly int _NewChunkXValue = x1 < x2 ? 0 : 7;
        
    private bool _Reset = true;
    private (ushort, ushort) _Current = (x1, y1);

    //Move tiles left-to-right then top-to-bottom
    //Move chunks top-to-bottom then left-to-right
    public bool MoveNext()
    {
        if (_Reset)
        {
            _Reset = false;
            return true;
        }
            
        if (_Current == (x2, y2))
            return false;

        var x = (int)_Current.Item1;
        var y = (int)_Current.Item2;
        if (x == x2)
        {
            x = (x & 0xFFF8) + _NewChunkXValue;
            y += _YDelta;
        }
        else
        {
            x += _XDelta;
            if ((x & _NewChunkXMask) == _NewChunkXValue)
            {
                x -= 8 * _XDelta;
                y += _YDelta;
            }
            if ((y - y2) * _YDelta > 0)
            {
                x += 8 * _XDelta;
                y = y1;
            }
        }
        if ((x - x1) * _XDelta < 0)
        {
            x = x1;
        }


        _Current = ((ushort)x, (ushort)y);
        return true;
    }

    public void Reset()
    {
        _Current = (x1,y1);
        //We use reset var to fake initial position, which is before first element
        _Reset = true;
    }

    
    public (ushort x, ushort y) Current => _Current;

    object IEnumerator.Current => _Current;

    public void Dispose()
    {
        //Nothing to do
    }
}