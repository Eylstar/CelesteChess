using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CelesteChess;

[Tracked]
[CustomEntity("CelesteChess/ChessTile")]
public class ChessTileEntity : Entity
{
    public ChessPieceEntity piece = null;
    
    public bool isValidForActualMove = false;
    
    public int gridPosition;
    public Vector2 gridCoordinates;
    
    public ColorTarget colorTarget;
    
    Image tile;
    TileColor color;

    Color isValidMove = new(225, 255, 202);
    Color hoverBegin = new(225, 158, 3);
    Color hoveredValid = new(193, 255, 115);
    Color hoveredInvalid = new(213, 93, 11);
    
    public ChessTileEntity(EntityData data, Vector2 offset) : base(data.Position + offset)
    {
        color = data.Enum("color", TileColor.White);
        Depth = 1000000;
    }
    
    public ChessTileEntity(Vector2 position)
    {
        gridCoordinates = position;
    }
    
    public override void Added(Scene scene)
    {
        base.Added(scene);
        Setup();
    }

    void Setup()
    {
        int res = 32;
        Collider = new Hitbox(res, res, -res / 2, -res / 2);
        Visible = true;
        tile = new Image(GFX.Game[$"CelesteChess/{(color == TileColor.Black ? "Black" : "White")}Tile"]);
        tile.CenterOrigin();
        Add(tile);
    }
    
    public override void Update()
    {
        base.Update();

        switch (colorTarget)
        {
            case ColorTarget.WalkCanGrab:
                tile.Color = hoverBegin;
                break;
            case ColorTarget.WalkGrabValid:
                tile.Color = hoveredValid;
                break;
            case ColorTarget.WalkGrabInvalid:
                tile.Color = hoveredInvalid;
                break;
            case ColorTarget.PossibleDestination:
                tile.Color = isValidMove;
                break;
            default:
                tile.Color = Color.White;
                break;
        }
    }
    
    public void SetValidMove()
    {
        isValidForActualMove = true;
        colorTarget = ColorTarget.PossibleDestination;
    }
}


public enum ColorTarget
{
    None,
    WalkCanGrab,
    WalkGrabValid,
    WalkGrabInvalid,
    PossibleDestination,
}

public enum TileColor
{
    White,
    Black
}