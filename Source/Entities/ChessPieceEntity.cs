using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CelesteChess;

[CustomEntity("CelesteChess/ChessPiece")]
public class ChessPieceEntity : TheoCrystal
{
    Holdable holdable;

    public Piece pieceType;
    public ChessPieceColor ChessPieceColor;
    
    Image piece;
    Sprite crystal;
    
    Player player;
    
    public ChessPieceEntity(EntityData data, Vector2 offset) : base(data, offset)
    {
        pieceType = data.Enum("piece", Piece.Pawn);
        ChessPieceColor = data.Enum("color", ChessPieceColor.White);
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);
        
        SetupGraphic();
        
        holdable = Get<Holdable>();
        holdable.PickupCollider = new Hitbox(16, 16, -8, -8);
        Collider = new Hitbox(16, 16, -8, -8);
    }
    
    public override void Update()
    {
        base.Update();
        player = Scene.Tracker.GetEntity<Player>();
        player.carryOffset = new Vector2(0, -20);
    }

    void SetupGraphic()
    {
        crystal = Get<Sprite>();
        Remove(crystal);
        Visible = true;
        
        piece = new Image(GFX.Game[$"CelesteChess/pieces/{pieceType + (ChessPieceColor == ChessPieceColor.White ? "" : "1")}"]);
        piece.Origin = new Vector2(piece.Width / 2, (piece.Height / 2) + 2);
        Add(piece);
    }
}

public enum Piece
{
    Pawn,
    Rook,
    Knight,
    Bishop,
    Queen,
    King
}

public enum ChessPieceColor
{
    White,
    Black
}