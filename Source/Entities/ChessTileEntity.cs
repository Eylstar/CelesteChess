using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CelesteChess;

[Tracked]
[CustomEntity("CelesteChess/ChessTile")]
public class ChessTileEntity : Entity
{
    public int gridPosition;
    public Vector2 gridCoordinates;
    
    Image tile;
    TileColor color;

    Color hoverBegin = Color.White;
    Color hoveredValid = Color.Green;
    Color hoveredInvalid = Color.Red;
    
    Trigger trigger;

    public ChessTileEntity(EntityData data, Vector2 offset) : base(data.Position + offset)
    {
        color = data.Enum("color", TileColor.White);
        Depth = 1000000;
    }
    
    public override void Added(Scene scene)
    {
        base.Added(scene);

        Visible = true;
        tile = new Image(GFX.Game[$"CelesteChess/{(color == TileColor.Black ? "Black" : "White")}Tile"]);
        tile.CenterOrigin();
        Add(tile);
    }
}

public enum TileColor
{
    White,
    Black
}