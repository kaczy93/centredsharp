namespace CentrED.Server.Map;

enum TileType
{
    Water = 0,
    Land = 1,
}

class DirtTiles
{
    private static Random _random = new Random();
    
    public static int Dirt = 0x0095;
    
    public static int GetDirtBottomGrassTop()
    {
        int i = _random.Next(0, 2);
        return new[] { 0x0093, 0x009A }[i];
    }

    public static int GetDirtLeftGrassRight()
    {
        int i = _random.Next(0, 2);
        return new[] { 0x0096, 0x008F }[i];       
    }

    public static int GetDirtRightGrassLeft()
    {
        int i = _random.Next(0, 2);
        return new[] { 0x009B, 0x008E }[i];              
    }

    public static int GetDirtTopGrassBottom()
    {
        int i = _random.Next(0, 2);
        return new[] { 0x0092, 0x0099 }[i];                     
    }

    public static int GetDirtGrassCornerTopLeft()
    {
        int i = _random.Next(0, 2);
        return new[] { 0x00A3, 0x0098 }[i];                            
    }

    public static int GetDirtGrassCornerTopRight()
    {
        // int i = _random.Next(0, 2);
        // return new[] { 0x00A2 }[i];                                   
        return 0x00A2;
    }

    public static int GetDirtGrassCornerBottomLeft()
    {
        // int i = _random.Next(0, 2);
        // return new[] { 0x00A1 }[i];
        return 0x00A1;
    }

    public static int GetDirtGrassCornerBottomRight()
    {
        // int i = _random.Next(0, 2);
        // return new[] { 0x0097 }[i];                                                 
        return 0x0097;
    }
}


class WaterTiles
{
    private static Random _random = new Random();
    
    public static int NoName = 0x0066; // we consider this tile as a water tile for the purpose of this algorithm

    // Tip relative a single tile
    public static int TipBottom = 0x17A6;
    public static int TipLeft = 0x17A5;
    public static int TipRight = 0x17A8;
    public static int TipTop = 0x17A7;
    
    public static int VTop = 0x17AC;
    public static int VLeft = 0x17AB;
    public static int VRight = 0x17A9;
    public static int VBottom = 0x17AA;

    private static int[] _waterLandTiles = { 0xA8, 0xA9, 0xAA, 0xAB, 0x136, 0x137 };
    
    public static int GetTopMid()
    {
        int i = _random.Next(0, 2);
        return new[] { 0x17A0, 0x179F }[i];
    }
    
    public static int GetLeftMid()
    {
        int i = _random.Next(0, 2);
        return new[] { 0x17A3, 0x17A4 }[i];
    }
    public static int GetBottomMid()
    {
        int i = _random.Next(0, 2);
        return new[] { 0x17A1, 0x17A2 }[i];
    }
    public static int GetRightMid()
    {
        int i = _random.Next(0, 2);
        return new[] { 0x179D, 0x179E }[i];
    }

    public static int GetMid()
    {
        int i = _random.Next(0, 2);
        return new[] { 0x1797, 0x1798, 0x1799, 0x179A }[i];
    }

    public static bool IsWaterLandTile(int tileId)
    {
        return _waterLandTiles.Contains(tileId) || tileId == NoName;
    }
}


enum BitMask
{
    None = 0,

    // horizontal patterns
    HorizontalTop = 0b_111_000_000,
    HorizontalBottom = 0b_000_000_111,

    // vertical patterns
    VerticalLeft = 0b_100_100_100,
    VerticalRight = 0b_001_001_001,

    // Single Corners patterns
    // SingleCornerA = 0b_100_000_000,
    // SingleCornerB = 0b_001_000_000,
    // SingleCornerC = 0b_000_000_100,
    // SingleCornerD = 0b_000_000_001,

    // diagonal patterns
    DiagonalA = 0b_110_100_000,
    DiagonalB = 0b_011_001_000,
    DiagonalC = 0b_000_100_110,
    DiagonalD = 0b_000_001_011,

    // TwoFullSides pattern
    TwoFullSidesTopRight = 0b_111_001_001,
    TwoFullSidesTopLeft = 0b_111_100_100,
    TwoFullSidesBottomLeft = 0b_100_100_111,
    TwoFullSidesBottomRight = 0b_001_001_111,

    // LShape patterns
    LShapeA = 0b_011_001_001,
    LShapeC = 0b_110_100_100,
    LShapeD = 0b_111_100_000,
    LShapeE = 0b_100_100_110,
    LShapeF = 0b_000_100_111,
    LShapeG = 0b_001_001_011,
    LShapeH = 0b_000_001_111,
    LShapeI = 0b_111_001_000,

    // two blocks pattern
    TwoBlocksTopLeft = 0b_110_000_000,
    TwoBlocksTopRight = 0b_011_000_000,
    TwoBlocksLeftTop = 0b_100_100_000,
    TwoBlocksLeftBottom = 0b_000_100_100,
    TwoBlocksBottomLeft = 0b_000_000_110,
    TwoBlocksBottomRight = 0b_000_000_011,
    TwoBlocksRightTop = 0b_001_001_000,
    TwoBlocksRightBottom = 0b_000_001_001,
    
    // U Shape
    UShapeTop = 0b_111_101_00,
    UShapeLeft = 0b_110_100_110,
    UShapeRight = 0b_011_001_011,
    UShapeBottom = 0b_000_101_111,
    
}


class TemplateInfo
{

    public BitMask TileTemplate {get; set;}
    public MatrixTile[] Tiles { get; set; }

    private Random _random = new Random();

    private int CenterX { get; set; }
    
    private int CenterY { get; set; }
    
    
    public static BitMask[] _templates = Enum.GetValues(typeof(BitMask)) as BitMask[];

    public TemplateInfo(BitMask tileTemplate, int x, int y, MatrixTile[] tiles)
    {
        CenterX = x;
        CenterY = y;
        TileTemplate = tileTemplate;
        Tiles = tiles;
    }

    public MatrixTile GetCenterTile()
    {
        return Tiles[4];
    }
    
    
    /**
     * We check tiles in a 3x3 Matrix with point x,y in the center.
     * A0 = row 0, col 0
     * C2 = row 2, col 2
     */
    public static TemplateInfo CheckMatrix(ServerLandscape landscape, int x, int y)
    {
        MatrixTile tile_A0 = GetMatrixTile(landscape.GetLandTile((ushort) (x-1), (ushort) (y-1)).Id);
        MatrixTile tile_A1 = GetMatrixTile(landscape.GetLandTile((ushort) x, (ushort) (y-1)).Id);
        MatrixTile tile_A2 = GetMatrixTile(landscape.GetLandTile((ushort) (x + 1), (ushort) (y - 1)).Id);
        
        MatrixTile tile_B0 = GetMatrixTile(landscape.GetLandTile((ushort) (x-1), (ushort) y).Id);
        MatrixTile tile_B1 = GetMatrixTile(landscape.GetLandTile((ushort) x, (ushort) y).Id);
        MatrixTile tile_B2 = GetMatrixTile(landscape.GetLandTile((ushort) (x + 1), (ushort) y).Id);
        
        MatrixTile tile_C0 = GetMatrixTile(landscape.GetLandTile((ushort) (x-1), (ushort) (y+1)).Id);
        MatrixTile tile_C1 = GetMatrixTile(landscape.GetLandTile((ushort) x, (ushort) (y+1)).Id);
        MatrixTile tile_C2 = GetMatrixTile(landscape.GetLandTile((ushort) (x+1), (ushort) (y+1)).Id);
        
        int tiles_format = ( (int) tile_A0.TileType ) << 8 | 
                           ( (int) tile_A1.TileType ) << 7 | 
                           ( (int) tile_A2.TileType ) << 6 | 
                           ( (int) tile_B0.TileType ) << 5 | 
                           ( (int) tile_B1.TileType ) << 4 | 
                           ( (int) tile_B2.TileType ) << 3 | 
                           ( (int) tile_C0.TileType ) << 2 | 
                           ( (int) tile_C1.TileType ) << 1 | 
                           ( (int) tile_C2.TileType );
       
        BitMask match = BitMask.None;
        foreach (var template in _templates)
        {
            if (tiles_format == (int) template)
            {
                match = template;
            }
        }

        if (match != BitMask.None)
        {
            return new TemplateInfo (match, x, y, new[] { tile_A0, tile_A1, tile_A2, tile_B0, tile_B1, tile_B2, tile_C0, tile_C1, tile_C2 });
        }

        return null;
    }

    private static MatrixTile GetMatrixTile(int tileId)
    {
        if (WaterTiles.IsWaterLandTile(tileId) || tileId == WaterTiles.NoName)
        {
            return new MatrixTile(TileType.Water, tileId);
        } 
        
        // default is land
        return new MatrixTile(TileType.Land, tileId);
    }

    private MatrixTile CreateTileInfo(int tileId, int relativeX, int relativeY, sbyte newZ, bool isStatic = false)
    {
        MatrixTile tile = new MatrixTile();
        tile.NewZ = newZ;
        tile.NewTileId = tileId;
        tile.RelativeX = relativeX;
        tile.RelativeY = relativeY;
        tile.IsNewTileStatic = isStatic;
        return tile;
    }

    public List<MatrixTile> GetTilesToChange(BaseLandscape landscape)
    {
         List<MatrixTile> tilesToChange = new List<MatrixTile>();
         
        if (TileTemplate != BitMask.UShapeTop || TileTemplate != BitMask.UShapeBottom || TileTemplate != BitMask.UShapeLeft || TileTemplate != BitMask.UShapeRight)
        {
             MatrixTile centerTile = GetCenterTile();
             centerTile.NewZ = -15;
             centerTile.RelativeX = 0;
             centerTile.RelativeY = 0;
             centerTile.NewTileId = WaterTiles.NoName;
             tilesToChange.Add(centerTile);           
        }
        
        if (TileTemplate == BitMask.HorizontalTop)
        {
            tilesToChange.Add(CreateTileInfo(DirtTiles.GetDirtBottomGrassTop(), 0, -1, (sbyte) _random.Next(0, 3)));
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetTopMid(), 0, 0, -5, true));
            return tilesToChange;
        }
        
        if (TileTemplate == BitMask.HorizontalBottom)
        {
            tilesToChange.Add(CreateTileInfo(DirtTiles.GetDirtTopGrassBottom(), 0, 1, -10));
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetMid(), 0, 0, -5, true));
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetBottomMid(), 0, 1, -5, true));
            return tilesToChange;
        }
        
        if (TileTemplate == BitMask.VerticalLeft)
        {
            tilesToChange.Add(CreateTileInfo(DirtTiles.GetDirtRightGrassLeft(), -1, 0, (sbyte) _random.Next(0, 3)));
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetLeftMid(), 0, 0, -5, true));
            return tilesToChange;
        }
        
        if (TileTemplate == BitMask.VerticalRight)
        {
            tilesToChange.Add(CreateTileInfo(DirtTiles.GetDirtLeftGrassRight(), 1, 0, -10));
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetMid(), 0, 0, -5, true));
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetRightMid(), 1, 0, -5, true));
            return tilesToChange;
        }
        
        if (TileTemplate == BitMask.DiagonalA || TileTemplate == BitMask.LShapeC)
        {
            tilesToChange.Add(CreateTileInfo(WaterTiles.NoName, 1, 0, -15));

            if (TileTemplate == BitMask.LShapeC)
            {
                tilesToChange.Add(CreateTileInfo(DirtTiles.GetDirtRightGrassLeft(), -1, 0, 0));
            }
            else
            {
                tilesToChange.Add(CreateTileInfo(DirtTiles.Dirt, -1, 0, 0));
            }
            
            tilesToChange.Add(CreateTileInfo(DirtTiles.GetDirtGrassCornerTopLeft(), -1, -1, 0));
            tilesToChange.Add(CreateTileInfo(WaterTiles.TipBottom, 0, 0, -5, true));
            tilesToChange.Add(CreateTileInfo(WaterTiles.VTop, 1, 0, -5, true));
            return tilesToChange;
        }       

        if (TileTemplate == BitMask.DiagonalB)
        {
            tilesToChange.Add(CreateTileInfo(DirtTiles.Dirt, 0, -1, -15));
            tilesToChange.Add(CreateTileInfo(DirtTiles.GetDirtGrassCornerTopRight(), 1, -1, -10));
            
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetMid(), 0, 0, -5, true));
            tilesToChange.Add(CreateTileInfo(WaterTiles.TipLeft, 1, -1, -5, true));
            
            if (!WaterTiles.IsWaterLandTile(landscape.GetLandTile((ushort) (CenterX - 1), (ushort) (CenterY -2)).Id) &&
                !WaterTiles.IsWaterLandTile(landscape.GetLandTile((ushort) (CenterX - 2), (ushort) (CenterY -2)).Id))
            {
                tilesToChange.Add(CreateTileInfo(WaterTiles.GetTopMid(), 0, -1, -5, true));
            }
            else
            {
                tilesToChange.Add(CreateTileInfo(WaterTiles.VRight, 0, -1, -5, true));
            }
            
            if (WaterTiles.IsWaterLandTile(landscape.GetLandTile((ushort) (CenterX + 2), (ushort) (CenterY + 1)).Id))
            {
                tilesToChange.Add(CreateTileInfo(WaterTiles.GetRightMid(), 1, 0, -5, true));
            }
            return tilesToChange;
        }
        
        if (TileTemplate == BitMask.DiagonalC)
        {
            tilesToChange.Add(CreateTileInfo(DirtTiles.Dirt, -1, 0, -15));
            tilesToChange.Add(CreateTileInfo(DirtTiles.GetDirtGrassCornerBottomLeft(), -1, 1, -10));
            
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetMid(), 0, 0, -5, true));
            tilesToChange.Add(CreateTileInfo(WaterTiles.TipRight, -1, 1, -5, true));
            
            if (!WaterTiles.IsWaterLandTile(landscape.GetLandTile((ushort) (CenterX - 2), (ushort) (CenterY -1)).Id) &&
                !WaterTiles.IsWaterLandTile(landscape.GetLandTile((ushort) (CenterX - 2), (ushort) (CenterY -2)).Id))
            {
                tilesToChange.Add(CreateTileInfo(WaterTiles.GetLeftMid(), -1, 0, -5, true));
            }
            else
            {
                tilesToChange.Add(CreateTileInfo(WaterTiles.VLeft, -1, 0, -5, true));
            }
            
            // if (!WaterTiles.IsWaterLandTile(landscape.GetLandTile((ushort) (CenterX + 2), (ushort) (CenterY + 1)).Id))
            // {
            //     tilesToChange.Add(CreateTileInfo(WaterTiles.GetBottomMid(), 1, 0, -5, true));
            // }
            return tilesToChange;
        }

        if (TileTemplate == BitMask.LShapeE)
        {
            tilesToChange.Add(CreateTileInfo(DirtTiles.GetDirtRightGrassLeft(), -1, 0, 0));
            tilesToChange.Add(CreateTileInfo(DirtTiles.GetDirtGrassCornerBottomLeft(), -1, 1, -10));
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetLeftMid(), 0, 0, -5, true));
            return tilesToChange;           
        }
        
        if (TileTemplate == BitMask.DiagonalD)
        {
            tilesToChange.Add(CreateTileInfo(WaterTiles.NoName, 0, -1, -15));
            tilesToChange.Add(CreateTileInfo(DirtTiles.Dirt, 1, 0, -10));
            tilesToChange.Add(CreateTileInfo(DirtTiles.GetDirtGrassCornerBottomRight(), 1, 1, -10));
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetMid(), 0, 0, -5, true));
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetMid(), 0, -1, -5, true));
            tilesToChange.Add(CreateTileInfo(WaterTiles.VBottom, 1, 0, -5, true));
            tilesToChange.Add(CreateTileInfo(WaterTiles.TipTop, 1, 1, -5, true));
            return tilesToChange;
        }       
        
        if (TileTemplate == BitMask.LShapeA)
        {
            tilesToChange.Add(CreateTileInfo(DirtTiles.Dirt, 0, -1, -10));
            tilesToChange.Add(CreateTileInfo(DirtTiles.Dirt, 1, 0, -10));
            tilesToChange.Add(CreateTileInfo(DirtTiles.GetDirtGrassCornerTopRight(), 1, -1, -10));
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetMid(), 0, 0, -5, true));
            tilesToChange.Add(CreateTileInfo(WaterTiles.TipLeft, 1, -1, -5, true));
            
            if (!WaterTiles.IsWaterLandTile(landscape.GetLandTile((ushort) (CenterX - 1), (ushort) (CenterY -2)).Id) &&
                !WaterTiles.IsWaterLandTile(landscape.GetLandTile((ushort) (CenterX - 2), (ushort) (CenterY -2)).Id))
            {
                tilesToChange.Add(CreateTileInfo(WaterTiles.GetTopMid(), 0, -1, -5, true));
            }
            else
            {
                tilesToChange.Add(CreateTileInfo(WaterTiles.VRight, 0, -1, -5, true));
            }
            
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetRightMid(), 1, 0, -5, true));
            
            return tilesToChange;
        }       
        
        if (TileTemplate == BitMask.LShapeD)
        {
            tilesToChange.Add(CreateTileInfo(WaterTiles.NoName, 1, 0, -15));
            tilesToChange.Add(CreateTileInfo(DirtTiles.Dirt, -1, 0, 0));
            tilesToChange.Add(CreateTileInfo(DirtTiles.GetDirtGrassCornerTopLeft(), -1, -1, 0));
            tilesToChange.Add(CreateTileInfo(DirtTiles.GetDirtBottomGrassTop(), 0, -1, 0));
            tilesToChange.Add(CreateTileInfo(WaterTiles.TipBottom, 0, 0, -5, true));
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetTopMid(), 1, 0, -5, true));
            return tilesToChange;
        }       
        
        if (TileTemplate == BitMask.LShapeF)
        {
            tilesToChange.Add(CreateTileInfo(DirtTiles.Dirt, -1, 0, -10));
            tilesToChange.Add(CreateTileInfo(DirtTiles.Dirt, 0, 1, -10));
            tilesToChange.Add(CreateTileInfo(DirtTiles.GetDirtGrassCornerBottomLeft(), -1, 1, -10));
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetMid(), 0, 0, -5, true));
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetBottomMid(), 0, 1, -5, true));
            tilesToChange.Add(CreateTileInfo(WaterTiles.TipRight, -1, 1, -5, true));
            
            if (!WaterTiles.IsWaterLandTile(landscape.GetLandTile((ushort) (CenterX - 2), (ushort) (CenterY -1)).Id) &&
                !WaterTiles.IsWaterLandTile(landscape.GetLandTile((ushort) (CenterX - 2), (ushort) (CenterY -2)).Id))
            {
                tilesToChange.Add(CreateTileInfo(WaterTiles.GetLeftMid(), -1, 0, -5, true));
            }
            else
            {
                tilesToChange.Add(CreateTileInfo(WaterTiles.VLeft, -1, 0, -5, true));
            }           
            
            return tilesToChange;
        }       
        
        if (TileTemplate == BitMask.LShapeG)
        {
            tilesToChange.Add(CreateTileInfo(DirtTiles.GetDirtGrassCornerBottomRight(), 1, 1, -10));
            tilesToChange.Add(CreateTileInfo(DirtTiles.GetDirtLeftGrassRight(), 1, 0, -10));
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetMid(), 0, 0, -5, true));
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetRightMid(), 1, 0, -5, true));
            tilesToChange.Add(CreateTileInfo(WaterTiles.TipTop, 1, 1, -5, true));
            return tilesToChange;
        }
        
        if (TileTemplate == BitMask.LShapeH)
        {
            tilesToChange.Add(CreateTileInfo(WaterTiles.NoName, 0, -1, -15));
            tilesToChange.Add(CreateTileInfo(DirtTiles.Dirt, 1, 0, -10));
            tilesToChange.Add(CreateTileInfo(DirtTiles.GetDirtGrassCornerBottomRight(), 1, 1, -10));
            tilesToChange.Add(CreateTileInfo(DirtTiles.GetDirtTopGrassBottom(), 0, 1, -10));
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetMid(), 0, 0, -5, true));
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetMid(), 0, -1, -5, true));
            tilesToChange.Add(CreateTileInfo(WaterTiles.VBottom, 1, 0, -5, true));
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetBottomMid(), 0, 1, -5, true));
            return tilesToChange;
        }

        if (TileTemplate == BitMask.LShapeI)
        {
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetTopMid(), 0, 0, -5, true));
            tilesToChange.Add(CreateTileInfo(WaterTiles.TipLeft, 1, 0, -5, true));
            tilesToChange.Add(CreateTileInfo(DirtTiles.GetDirtBottomGrassTop(), 0, -1, -10));
            tilesToChange.Add(CreateTileInfo(DirtTiles.GetDirtGrassCornerTopRight(), 1, -1, -10));
            return tilesToChange;
        }

        if (TileTemplate == BitMask.TwoBlocksBottomRight)
        {
            tilesToChange.Add(CreateTileInfo(WaterTiles.NoName, 0, -1, -15));
            tilesToChange.Add(CreateTileInfo(DirtTiles.Dirt, 0, 1, -10));
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetMid(), 0, 0, -5, true));
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetMid(), 0, -1, -5, true));           
            tilesToChange.Add(CreateTileInfo(WaterTiles.VBottom, 1, 0, -5, true));
            return tilesToChange;
        }

        if (TileTemplate == BitMask.TwoBlocksRightTop)
        {
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetMid(), 0, 0, -5, true));
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetRightMid(), 1, 0, -5, true));           
            tilesToChange.Add(CreateTileInfo(WaterTiles.VRight, 1, 1, -5, true));
            return tilesToChange;
        }

        if (TileTemplate == BitMask.TwoBlocksTopLeft)
        {
            tilesToChange.Add(CreateTileInfo(WaterTiles.NoName, 1, 0, -15));
            tilesToChange.Add(CreateTileInfo(DirtTiles.Dirt, 0, -1, -10));
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetTopMid(), 0, 0, -5, true));
            tilesToChange.Add(CreateTileInfo(WaterTiles.VTop, 1, 0, -5, true));           
            return tilesToChange;
        }

        if (TileTemplate == BitMask.TwoBlocksTopRight)
        {
            tilesToChange.Add(CreateTileInfo(DirtTiles.Dirt, 0, -1, -10));
            tilesToChange.Add(CreateTileInfo(WaterTiles.VRight, 0, 0, -5, true));                      
            // tilesToChange.Add(CreateTileInfo(WaterTiles.GetRightMid(), 0, -1, -5, true));                      
            return tilesToChange;
        }

        if (TileTemplate == BitMask.TwoBlocksLeftBottom)
        {
            tilesToChange.Add(CreateTileInfo(DirtTiles.Dirt, -1, 0, -10));
            tilesToChange.Add(CreateTileInfo(WaterTiles.VLeft, 0, 0, -5, true));                      
            
            if (!WaterTiles.IsWaterLandTile(landscape.GetLandTile((ushort) (CenterX - 2), (ushort) (CenterY -1)).Id) &&
                !WaterTiles.IsWaterLandTile(landscape.GetLandTile((ushort) (CenterX - 2), (ushort) (CenterY -2)).Id))
            {
                tilesToChange.Add(CreateTileInfo(WaterTiles.TipRight, -1, 0, -5, true));
            }
            else
            {
                tilesToChange.Add(CreateTileInfo(WaterTiles.GetBottomMid(), -1, 0, -5, true));                      
            }           
            return tilesToChange;
        }

        if (TileTemplate == BitMask.TwoBlocksBottomRight)
        {
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetMid(), 0, 0, -5, true));                                 
            return tilesToChange;
        }

        if (TileTemplate == BitMask.TwoBlocksBottomLeft)
        {
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetMid(), 0, 0, -5, true));
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetBottomMid(), 0, 1, -5, true));           
            return tilesToChange;
        }

        if (TileTemplate == BitMask.TwoBlocksLeftTop)
        {
            tilesToChange.Add(CreateTileInfo(DirtTiles.Dirt, -1, 0, 0));
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetLeftMid(), 0, 0, -5, true));                      
            return tilesToChange;
        }

        if (TileTemplate == BitMask.TwoBlocksRightBottom)
        {
            tilesToChange.Add(CreateTileInfo(DirtTiles.Dirt, 1, 0, -10));
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetMid(), 0, 0, -5, true));                      
            tilesToChange.Add(CreateTileInfo(WaterTiles.VBottom, 1, 0, -5, true));                      
            return tilesToChange;
        }
        
        if (TileTemplate == BitMask.TwoFullSidesTopLeft)
        {
            tilesToChange.Add(CreateTileInfo(DirtTiles.GetDirtGrassCornerTopLeft(), -1, -1, 0));
            tilesToChange.Add(CreateTileInfo(DirtTiles.GetDirtRightGrassLeft(), -1, 0, 0));
            tilesToChange.Add(CreateTileInfo(DirtTiles.GetDirtBottomGrassTop(), 0, -1, 0));
            tilesToChange.Add(CreateTileInfo(WaterTiles.TipBottom, 0, 0, -5, true));
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetLeftMid(), 0, 1, -5, true));
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetTopMid(), 1, 0, -5, true));
            return tilesToChange;
        }       
        
        if (TileTemplate == BitMask.TwoFullSidesTopRight)
        {
            tilesToChange.Add(CreateTileInfo(DirtTiles.GetDirtBottomGrassTop(), 0, -1, (sbyte) _random.Next(-7,3)));
            tilesToChange.Add(CreateTileInfo(DirtTiles.GetDirtLeftGrassRight(), 1, 0, -10));
            tilesToChange.Add(CreateTileInfo(DirtTiles.GetDirtGrassCornerTopRight(), 1, -1, -10));
            // tilesToChange.Add(CreateTileInfo(WaterTiles.GetMid(), 0, 0, -5, true));
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetTopMid(), 0, 0, -5, true));
            tilesToChange.Add(CreateTileInfo(WaterTiles.TipLeft, 1, 0, -5, true));
            return tilesToChange;
        }       
        
        if (TileTemplate == BitMask.TwoFullSidesBottomLeft)
        {
            tilesToChange.Add(CreateTileInfo(DirtTiles.GetDirtRightGrassLeft(), -1, 0, (sbyte) _random.Next(-7,3)));
            tilesToChange.Add(CreateTileInfo(DirtTiles.GetDirtTopGrassBottom(), 0, 1, -10));
            tilesToChange.Add(CreateTileInfo(DirtTiles.GetDirtGrassCornerBottomLeft(), -1, 1, -10));
            // tilesToChange.Add(CreateTileInfo(WaterTiles.GetMid(), 0, 0, -5, true));
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetLeftMid(), 0, 0, -5, true));
            tilesToChange.Add(CreateTileInfo(WaterTiles.TipRight, 0, 1, -5, true));
            return tilesToChange;
        }       
        
        if (TileTemplate == BitMask.TwoFullSidesBottomRight)
        {
            tilesToChange.Add(CreateTileInfo(DirtTiles.GetDirtTopGrassBottom(), 0, 1, -10));
            tilesToChange.Add(CreateTileInfo(DirtTiles.GetDirtLeftGrassRight(), 1, 0, -10));
            tilesToChange.Add(CreateTileInfo(DirtTiles.GetDirtGrassCornerBottomRight(), 1, 1, -10));
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetMid(), 0, 0, -5, true));
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetMid(), 0, -1, -5, true));
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetRightMid(), 1, 0, -5, true));
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetBottomMid(), 0, 1, -5, true));
            tilesToChange.Add(CreateTileInfo(WaterTiles.TipTop, 1, 1, -5, true));
            return tilesToChange;
        }

        if (TileTemplate == BitMask.UShapeTop || TileTemplate == BitMask.UShapeLeft) 
        {
            tilesToChange.Add(CreateTileInfo(DirtTiles.Dirt, 0, 0, (sbyte) _random.Next(0, 3)));
            return tilesToChange;
        }

        if (TileTemplate == BitMask.UShapeBottom) 
        {
            tilesToChange.Add(CreateTileInfo(DirtTiles.GetDirtTopGrassBottom(), 0, 0, -10)); 
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetBottomMid(), 0, 0, -5, true));
            tilesToChange.Add(CreateTileInfo(WaterTiles.NoName, 0, -1, -15));
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetMid(), 0, -1, -5, true));
            return tilesToChange;
        }

        if (TileTemplate == BitMask.UShapeRight)
        {
            tilesToChange.Add(CreateTileInfo(DirtTiles.GetDirtLeftGrassRight(), 0, 0, -10));
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetRightMid(), 0, 0, -5, true));
            tilesToChange.Add(CreateTileInfo(WaterTiles.NoName, -1, 0, -15));
            tilesToChange.Add(CreateTileInfo(WaterTiles.GetMid(), -1, 0, -5, true));
            return tilesToChange;
        }
        
        return tilesToChange;
    }
}

class MatrixTile
{
    public int OriginalTileId { get; set; }
    
    public TileType TileType { get; set; }
    
    public int NewTileId { get; set; }

    public sbyte NewZ { get; set; }
    
    public bool IsNewTileStatic = false; // defaults to land;
    
    public int RelativeX { get; set; } // relative to the center of the template matrix, that is, position 1,1 (0-based) in a 3x3 matrix.
    
    public int RelativeY { get; set; } // relative to the center of the template matrix, that is, position 1,1 (0-based) in a 3x3 matrix.
    
    public MatrixTile () {}
    
    public MatrixTile(TileType tileType, int originalTileId)
    {
        OriginalTileId = originalTileId;
        TileType = tileType;
    }
}

class LandTileToChange
{
    public LandTile LandTile { get; set; }
    public TemplateInfo TemplateInfo { get; set; }

    public LandTileToChange(LandTile landTile, TemplateInfo template)
    {
        LandTile = landTile;
        TemplateInfo = template;
    }
}

