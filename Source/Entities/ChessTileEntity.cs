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
    public ChessPieceEntity piece = null;
    
    Image tileImage;
    TileColor color;
    public ColorTarget colorTarget;
    
    Color hoveredColor = new(193, 255, 115);
    Color possibleMoveColor = new(225, 255, 202);
    
    public bool isValidForActualMove;
    
    public bool isDoubleMoveDestination = false;
    public ChessPieceEntity enPassantCapturePiece = null;
    
    public bool isCastleDestination = false;
    public ChessPieceEntity castlePiece = null;
    public ChessTileEntity rookDestinationTile = null;
    
    
    public ChessTileEntity(EntityData data, Vector2 offset) : base(data.Position + offset)
    {
        color = data.Enum("color", TileColor.White);
        Depth = 1000000;
    }
    
    //Secondary constructor for cloned BoardManagers
    public ChessTileEntity(Vector2 position)
    {
        gridCoordinates = position;
    }
    
    public override void Added(Scene scene)
    {
        base.Added(scene);
        
        Collider = new Hitbox(32, 32, -16, -16);
            
        //Add the tile image to the entity
        tileImage = new Image(GFX.Game[$"CelesteChess/{(color == TileColor.Black ? "Black" : "White")}Tile"]);
        tileImage.CenterOrigin();
        Add(tileImage);
    }
    
    public override void Update()
    {
        base.Update();

        //Change tile color based on the context
        switch (colorTarget)
        {
            case ColorTarget.WalkGrabValid:
                tileImage.Color = hoveredColor;
                break;
            case ColorTarget.PossibleDestination:
                tileImage.Color = possibleMoveColor;
                break;
            case ColorTarget.None:
                tileImage.Color = Color.White;
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
    WalkGrabValid,
    PossibleDestination
}

public enum TileColor
{
    White,
    Black
}