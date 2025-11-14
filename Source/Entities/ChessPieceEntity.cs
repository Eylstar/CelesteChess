using System;
using Celeste.Mod.CelesteNet;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.CoopHelper.IO;
using Celeste.Mod.CoopHelper.Data;
using Celeste.Mod.CoopHelper;

namespace Celeste.Mod.CelesteChess;

[CustomEntity("CelesteChess/ChessPiece")]
public class ChessPieceEntity : TheoCrystal
{
    public Piece pieceType;
    public ChessPieceColor ChessPieceColor;
    
    Player player;
    Holdable holdable;
    public ChessTileEntity tile;
    
    public bool movedOnce = false;
    
    Image pieceSprite;
    Color pieceTint = new(0.75f, 0.75f, 0.75f);
    
    
    //Networking variables
    public int constructorIndex;
    public EntityID id;
    
    public int originToNetwork;
    public int destinationToNetwork;
    public bool castleMove = false;
    public bool pawnJustMovedTwice = false;
    public bool AteWithEnPassant = false;

    public ChessPieceEntity(EntityData data, Vector2 offset) : base(data, offset) { }
    
    public override void Added(Scene scene)
    {
        base.Added(scene);
        Setup();
        
        //Create a new ID for the entity using a set constructor index by the board manager
        id = new EntityID(scene.ToString(), constructorIndex);
        
        ModInterop.AddListener(this, id, CoopHelperHeader, false);
    }

    void Setup()
    {
        Remove(Get<Sprite>());
        AssignSprite(out pieceSprite, pieceType, ChessPieceColor);
        Add(pieceSprite);
        
        //Adjust the collider to the piece size
        holdable = Get<Holdable>();
        holdable.PickupCollider = new Hitbox(16, 16, -8, -8);
        Collider = new Hitbox(16, 16, -8, -8);
    }
    
    void AssignSprite(out Image s, Piece type, ChessPieceColor color)
    {
        //Set the sprite based on the piece type and color
        s = new Image(GFX.Game[$"CelesteChess/pieces/{type + (color == ChessPieceColor.White ? "" : "1")}"]);
        s.Origin = new Vector2((pieceSprite.Width / 2), (pieceSprite.Height / 2) + 3f);
    }
    
    public void PromoteToQueen()
    {
        pieceType = Piece.Queen;
        AssignSprite(out pieceSprite, pieceType, ChessPieceColor);
    }
    
    public override void Update()
    {
        base.Update();
        
        //Update the alpha of the piece based on the overlapping state with the player
        if (ChessHelpers.TryGetPlayer(out player) && player.CollideCheck(this) && player.Holding == null && BoardManagerEntity.Instance.whiteTurn == IsWhite() && BoardManagerEntity.Instance.canMoveThisTurn)
            pieceSprite.Color = pieceTint * 0.75f;
        else
            pieceSprite.Color = Color.White;
    }
    
    public bool IsWhite()
    {
        return ChessPieceColor == ChessPieceColor.White;
    }
    
    //Used by the Clone board manager to create a new identical piece
    public ChessPieceEntity Clone()
    {
        ChessPieceEntity clone = new(new EntityData(), Position);
        clone.pieceType = pieceType;
        clone.ChessPieceColor = ChessPieceColor;
        clone.movedOnce = movedOnce;
        return clone;
    }

    public override void Removed(Scene scene)
    {
        base.Removed(scene);
        ModInterop.RemoveListener(id);
    }

    public override void SceneEnd(Scene scene)
    {
        base.SceneEnd(scene);
        ModInterop.RemoveListener(id);
    }

    
#region Coop Helper Integration

    //TODO CHANGER LE RANDOM ICI C'EST POUR DU DEBUG !!!!!!!!!!!!
    public static readonly int CoopHelperHeader = 21120208;
    //public static readonly int CoopHelperHeader = (int)Random.Shared.NextInt64(180000, 185000);

    internal static object Parse(CelesteNetBinaryReader reader)
    {
        return new PieceState()
        {
            origin = reader.ReadInt32(),
            destination = reader.ReadInt32(),
            castleMove = reader.ReadBoolean(),
            pawnJustMovedTwice = reader.ReadBoolean(),
            AteWithEnPassant = reader.ReadBoolean(),
        };
    }
    
    public void ApplyState(object stateobj)
    {
        if (stateobj is PieceState state)
        {
            BoardManagerEntity.Instance.MoveInducedByAction(state.origin, state.destination, !state.castleMove, state.pawnJustMovedTwice, state.AteWithEnPassant);
        }
    }

    public void WriteState(CelesteNetBinaryWriter writer)
    {
        writer.Write(originToNetwork);
        writer.Write(destinationToNetwork);
        writer.Write(castleMove);
        writer.Write(pawnJustMovedTwice);
        writer.Write(AteWithEnPassant);
    }
    
    public class PieceState
    {
        public int origin;
        public int destination;
        public bool castleMove;
        public bool pawnJustMovedTwice;
        public bool AteWithEnPassant;
    }

#endregion
}