using System;
using System.Collections.Generic;
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
    
    public int pieceType;
    public float xPosition;
    public float yPosition;
    public bool enabled;
    int storedPieceType;
    
    Image pieceSprite;
    Color pieceTint = new(1f, 1f, 1f, 0.5f);
    
    public GhostPieceEntity(EntityData data, Vector2 offset) : base(data.Position + offset) { }
    public override void Added(Scene scene)
    {
        base.Added(scene);
        Depth = -100;
        
        // Create a new ID for the entity using a set constructor index by the board manager
        id = new EntityID(scene.ToString(), 666);
        ModInterop.AddListener(this, id, CoopHelperHeader, false);
        
        SetupSprites();
    }

    void SetupSprites()
    {
        Image s = new Image(GFX.Game[$"CelesteChess/pieces/Pawn"]);
        s.Origin = new Vector2((s.Width / 2), (s.Height / 2) + 3f);
        s.Color = pieceTint;
        pieceSprite = s;
        Add(s);
        
        storedPieceType = 0;
        
        HideFromSelf(true);
    }

    public void ChangePieceSprite(int type)
    {
        Remove(pieceSprite);
        pieceSprite = new Image(GFX.Game[$"CelesteChess/pieces/{(Piece)type}" + (BoardManagerEntity.Instance.isWhitePlayer ? "1" : "")]);
        pieceSprite.Origin = new Vector2((pieceSprite.Width / 2), (pieceSprite.Height / 2) + 3f);
        pieceSprite.Color = pieceTint;
        Add(pieceSprite);
        storedPieceType = type;
    }
    
    public void HideFromSelf(bool hide)
    {
        pieceSprite.Visible = !hide;
    }
    public void SetPosition(float xpos, float ypos)
    {
        // Set the position of the ghost piece based on the current piece type and color
        Position = new Vector2(xpos, ypos);
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
    public static readonly int CoopHelperHeader = 747194269;
    //public static readonly int CoopHelperHeader = (int)Random.Shared.NextInt64(180000, 185000);

    internal static object Parse(CelesteNetBinaryReader reader)
    {
        return new GhostState()
        {
            pieceType = reader.ReadInt32(),
            xPosition = reader.ReadSingle(),
            yPosition = reader.ReadSingle(),
            enabled = reader.ReadBoolean()
        };
    }
    
    public void ApplyState(object stateobj)
    {
        Logger.Log(LogLevel.Error, "CelesteChess", "APPLYING STATE");
        if (stateobj is GhostState state)
        {
            GhostPieceEntity piece = BoardManagerEntity.Instance.ghostPiece;
            piece.enabled = state.enabled;
            piece.HideFromSelf(!state.enabled);
            if (piece.storedPieceType != state.pieceType)
            {
                Logger.Log(LogLevel.Error, "CelesteChess", "UPDATING SPRITE");
                piece.ChangePieceSprite(state.pieceType);
            }
            BoardManagerEntity.Instance.ghostPiece.SetPosition(state.xPosition, state.yPosition);
        }
    }

    public void WriteState(CelesteNetBinaryWriter writer)
    {
        Logger.Log(LogLevel.Error, "CelesteChess", "WRITING STATE");
        writer.Write(pieceType);
        writer.Write(xPosition);
        writer.Write(yPosition);
        writer.Write(enabled);
    }
    
    public class GhostState
    {
        public int pieceType;
        public float xPosition;
        public float yPosition;
        public bool enabled;
    }

#endregion
}