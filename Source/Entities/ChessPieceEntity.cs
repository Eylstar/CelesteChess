using Celeste.Mod.CelesteNet;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.ModInterop;
using Celeste.Mod.CoopHelper.Infrastructure;
using Celeste.Mod.CoopHelper.IO;
using Celeste.Mod.CoopHelper;

namespace Celeste.Mod.CelesteChess;

[CustomEntity("CelesteChess/ChessPiece")]
public class ChessPieceEntity : TheoCrystal
{
    public int constructorIndex;
    public EntityID id;
    
    public ChessTileEntity tile;
    Holdable holdable;
    
    public bool movedOnce = false;
    public bool needOnlineSync = false;
    
    public Piece pieceType;
    public ChessPieceColor ChessPieceColor;
    
    Player player;
    Image piece;
    Color pieceTint = new(0.75f, 0.75f, 0.75f);
    
    public int originToNetwork;
    public int destinationToNetwork;

    public ChessPieceEntity(EntityData data, Vector2 offset) : base(data, offset) { }
    
    public override void Added(Scene scene)
    {
        base.Added(scene);
        Setup();
        
        id = new EntityID(scene.ToString(), constructorIndex);
        Logger.Log(LogLevel.Error, "CelesteChess", $"ADDED PIECE [ID={id}] {pieceType}");
        ModInterop.AddListener(this, id, CoopHelperHeader, false);
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

    public static readonly int CoopHelperHeader = 7419;

    internal static object Parse(CelesteNetBinaryReader reader)
    {
        return new PieceState()
        {
            origin = reader.ReadInt32(),
            destination = reader.ReadInt32()
        };
    }
    
    public void ApplyState(object stateobj)
    {
        if (stateobj is PieceState state)
        {
            Logger.Log(LogLevel.Error, "CelesteChess", $"APPLY STATE: {state.origin} to {state.destination}");
            BoardManagerEntity.Instance.MoveReceivedByNetwork(state.origin, state.destination);
        }
    }

    public void WriteState(CelesteNetBinaryWriter writer)
    {
        Logger.Log(LogLevel.Error, "CelesteChess", $"WRITING STATE [ID={id}] {pieceType} from {originToNetwork} to {destinationToNetwork}");
        writer.Write(originToNetwork);
        writer.Write(destinationToNetwork);
    }

#endregion
}

public class PieceState
{
    public int origin;
    public int destination;
}