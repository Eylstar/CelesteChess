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
    
    List<Image> pieceSprites = new();
    Color pieceTint = new(0.5f, 0.5f, 0.5f, 0.5f);
    
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
        // Load all piece sprites and store them in the list
        for (int i = 0; i <= 5; i++)
        {
            string pieceName = i switch
            {
                0 => "Pawn",
                1 => "Rook",
                2 => "Knight",
                3 => "Bishop",
                4 => "Queen",
                5 => "King",
                _ => default
            };
            
            Image s = new Image(GFX.Game[$"CelesteChess/pieces/{pieceName}"]);
            s.Origin = new Vector2((s.Width / 2), (s.Height / 2) + 3f);
            s.Color = pieceTint;
            pieceSprites.Add(s);
            Add(s);
            s.Visible = false;
        }
        
        storedPieceType = 0;
        ChangePieceSprite(0);
        
        HideFromSelf();
    }

    public void ChangePieceSprite(int type)
    {
        foreach (Image i in pieceSprites)
            i.Visible = false;
        
        pieceSprites[type].Visible = true;
        storedPieceType = type;
    }
    
    public void HideFromSelf()
    {
        foreach (Image i in pieceSprites)
            i.Visible = false;
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
            if (!state.enabled)
            {
                piece.HideFromSelf();
            }
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