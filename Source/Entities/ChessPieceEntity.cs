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
    public int constructorIndex;
    public EntityID id;
    
    public ChessTileEntity tile;
    Holdable holdable;
    
    public bool movedOnce = false;
    public bool needOnlineSync = false;
    
    public Piece pieceType;
    public ChessPieceColor ChessPieceColor;
    
    Player player;
    Image pieceSprite;
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
        /*Sprite s = Get<Sprite>();
        s.Texture = GFX.Game[$"CelesteChess/pieces/{pieceType + (ChessPieceColor == ChessPieceColor.White ? "" : "1")}"];
        s.Origin = Vector2.Zero + new Vector2(s.Width / 6, (s.Height / 6) + 2);
        s.currentAnimation = null;
        s.Animating = false;
        s.CurrentAnimationFrame = 0;
        s.CurrentAnimationID = "";
        s.animations.Clear();*/
        
        Remove(Get<Sprite>());
        pieceSprite = new Image(GFX.Game[$"CelesteChess/pieces/{pieceType + (ChessPieceColor == ChessPieceColor.White ? "" : "1")}"]);
        pieceSprite.Origin = new Vector2(pieceSprite.Width / 2, (pieceSprite.Height / 2) + 2);
        Add(pieceSprite);
        
        holdable = Get<Holdable>();
        holdable.PickupCollider = new Hitbox(16, 16, -8, -8);
        Collider = new Hitbox(16, 16, -8, -8);
    }
    
    public override void Update()
    {
        base.Update();
        player = BoardManagerEntity.Instance.GetPlayer();
        if (player != null && player.CollideCheck(this) && player.Holding == null && BoardManagerEntity.Instance.whiteTurn == IsWhite())
            pieceSprite.Color = pieceTint * 0.75f;
        else
            pieceSprite.Color = Color.White;
    }
    
    public void PromoteToQueen()
    {
        pieceType = Piece.Queen;
        pieceSprite.Texture = GFX.Game[$"CelesteChess/pieces/{pieceType + (ChessPieceColor == ChessPieceColor.White ? "" : "1")}"];
        pieceSprite.Origin = new Vector2(pieceSprite.Width / 2, (pieceSprite.Height / 2) + 2);
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