using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CelesteChess;

[CustomEntity("CelesteChess/ChessPiece")]
public class ChessPieceEntity : TheoCrystal
{
    public ChessTileEntity tile;
    Holdable holdable;
    
    public bool movedOnce = false;
    
    public Piece pieceType;
    public ChessPieceColor ChessPieceColor;
    
    Player player;
    Image piece;
    Color pieceTint = new(0.75f, 0.75f, 0.75f);
    
    public ChessPieceEntity(EntityData data, Vector2 offset) : base(data, offset) { }

    public override void Added(Scene scene)
    {
        base.Added(scene);
        Setup();
    }

    void Setup()
    {
        Remove(Get<Sprite>());
        piece = new Image(GFX.Game[$"CelesteChess/pieces/{pieceType + (ChessPieceColor == ChessPieceColor.White ? "" : "1")}"]);
        piece.Origin = new Vector2(piece.Width / 2, (piece.Height / 2) + 2);
        Add(piece);
        holdable = Get<Holdable>();
        holdable.PickupCollider = new Hitbox(16, 16, -8, -8);
        Collider = new Hitbox(16, 16, -8, -8);
    }
    
    public override void Update()
    {
        base.Update();
        player = BoardManagerEntity.Instance.GetPlayer();
        if (player != null && player.CollideCheck(this) && player.Holding == null && BoardManagerEntity.Instance.whiteTurn == IsWhite())
            piece.Color = pieceTint * 0.75f;
        else
            piece.Color = Color.White;
    }
    
    public void PromoteToQueen()
    {
        pieceType = Piece.Queen;
        piece.Texture = GFX.Game[$"CelesteChess/pieces/{pieceType + (ChessPieceColor == ChessPieceColor.White ? "" : "1")}"];
        piece.Origin = new Vector2(piece.Width / 2, (piece.Height / 2) + 2);
    }
    
    public bool IsWhite()
    {
        return ChessPieceColor == ChessPieceColor.White;
    }
    
    public ChessPieceEntity Clone()
    {
        ChessPieceEntity clone = new(new EntityData(), Position);
        clone.pieceType = pieceType;
        clone.ChessPieceColor = ChessPieceColor;
        clone.movedOnce = movedOnce;
        return clone;
    }
}