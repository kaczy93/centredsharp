namespace CentrED.UI.Windows.Multi;

/// <summary>
/// for my needs i wanted this in json so i went into uofiddler
/// and modified the save method to spit out a json file with all the multi data and the names referenced from the xml file uofiddler had names in for them.
/// </summary>
public class MultiData
{
    public int Id { get; set; }
    public string Name { get; set; }
    public List<MultiItem> Tiles { get; set; }
    
    public int Width { get; private set; }
    public int Height { get; private set; }
    
    public void CalculateDimensions()
    {
        if (Tiles == null || Tiles.Count == 0)
        {
            Width = 0;
            Height = 0;
            return;
        }
        
        var minX = Tiles.Min(t => t.OffsetX);
        var maxX = Tiles.Max(t => t.OffsetX);
        var minY = Tiles.Min(t => t.OffsetY);
        var maxY = Tiles.Max(t => t.OffsetY);
        
        Width = Math.Abs(maxX - minX) + 1;
        Height = Math.Abs(maxY - minY) + 1;
    }

}

public class MultiItem
{
    public ushort ItemId { get; set; }
    public short OffsetX { get; set; }
    public short OffsetY { get; set; }
    public short OffsetZ { get; set; }
    public int Flags { get; set; }
    public int Unk1 { get; set; }
   

}



public class TileRotationMapping
{
    public Dictionary<ushort, TileRotation> Mappings { get; set; } = new();
}

public class TileRotation
{
    public ushort Rotation0 { get; set; }   // 0 degrees (original)
    public ushort Rotation90 { get; set; }  // 90 degrees clockwise
    public ushort Rotation180 { get; set; } // 180 degrees
    public ushort Rotation270 { get; set; } // 270 degrees clockwise
    
    public ushort GetRotatedId(int rotation)
    {
        return rotation switch
        {
            0 => Rotation0,
            1 => Rotation90,
            2 => Rotation180,
            3 => Rotation270,
            _ => Rotation0
        };
    }
}
