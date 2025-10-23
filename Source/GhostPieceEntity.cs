using System;
using Celeste.Mod.CelesteNet;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.CoopHelper.IO;
using Celeste.Mod.CoopHelper.Data;
using Celeste.Mod.CoopHelper;

namespace Celeste.Mod.CelesteChess;

[Tracked]
[CustomEntity("CelesteChess/GhostPiece")]
public class GhostPieceEntity : Entity
{
    public EntityID id;
    
    public bool enabled = false;
    
    public int pieceType;
    public bool isWhite;
    public float xPosition;
    public float yPosition;
    
    Image pieceSprite;
    Color pieceTint = new(0.75f, 0.75f, 0.75f);
    
    public GhostPieceEntity(EntityData data, Vector2 offset) : base(data.Position + offset) { }
    public override void Added(Scene scene)
    {
        base.Added(scene);
        
        // Create a new ID for the entity using a set constructor index by the board manager
        id = new EntityID(scene.ToString(), 666);
        
        ModInterop.AddListener(this, id, CoopHelperHeader, false);
        
        pieceSprite = new Image(GFX.Game[$"CelesteChess/pieces/Pawn"]);
        pieceSprite.Origin = new Vector2((pieceSprite.Width / 2), (pieceSprite.Height / 2) + 3f);
        pieceSprite.Color = pieceTint;
        
        Add(pieceSprite);
    }

    public override void Update()
    {
        pieceSprite.Visible = enabled;
    }

    public void SetPosition(float xpos, float ypos, bool white, int type)
    {
        // Set the position of the ghost piece based on the current piece type and color
        Position = new Vector2(xPosition, yPosition);
        Piece piece = (Piece)type;
        Logger.Log(LogLevel.Error, "CelesteChess", $"Setting ghost piece position to: {xPosition}, {yPosition} with type: {piece} and color: {(white ? "White" : "Black")}");
        pieceSprite = new Image(GFX.Game[$"CelesteChess/pieces/{piece + (white ? "" : "1")}"]);
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
    //public static readonly int CoopHelperHeader = 747194269;
    public static readonly int CoopHelperHeader = (int)Random.Shared.NextInt64(180000, 185000);

    internal static object Parse(CelesteNetBinaryReader reader)
    {
        return new GhostState()
        {
            pieceType = reader.ReadInt32(),
            isWhite = reader.ReadBoolean(),
            xPosition = reader.ReadSingle(),
            yPosition = reader.ReadSingle()
        };
    }
    
    public void ApplyState(object stateobj)
    {
        if (stateobj is GhostState state)
        {
            BoardManagerEntity.Instance.ghostPiece.SetPosition(xPosition = state.xPosition, yPosition = state.yPosition, isWhite = state.isWhite, pieceType = state.pieceType);
        }
    }

    public void WriteState(CelesteNetBinaryWriter writer)
    {
        writer.Write(pieceType);
        writer.Write(isWhite);
        writer.Write(xPosition);
        writer.Write(yPosition);
    }
    
    public class GhostState
    {
        public int pieceType;
        public bool isWhite;
        public float xPosition;
        public float yPosition;
    }

#endregion
}