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
    public bool isWhite = true;
    public float xPosition;
    public float yPosition;
    public bool enabled;
    int storedPieceType;
    bool storedIsWhite = true;
    
    Image pieceSprite;
    Color pieceTint = new(0.75f, 0.75f, 0.75f, 0.5f);
    
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

    public void ChangePieceSprite(int type, bool white)
    {
        if (pieceSprite != null)
            Remove(pieceSprite);
        
        pieceSprite = new Image(GFX.Game[$"CelesteChess/pieces/{(Piece)type}{(white ? "" : "1")}"]);
        pieceSprite.Origin = new Vector2((pieceSprite.Width / 2), (pieceSprite.Height / 2) + 3f);
        pieceSprite.Color = pieceTint;
        Add(pieceSprite);
        storedPieceType = type;
        storedIsWhite = white;
    }
    
    public void HideFromSelf(bool hide)
    {
        pieceSprite.Visible = !hide;
    }
    public void SetPosition(float xpos, float ypos)
    {
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
    //public static readonly int CoopHelperHeader = (int)Random.Shared.NextInt64(180000, 18500000000);

    internal static object Parse(CelesteNetBinaryReader reader)
    {
        return new GhostState()
        {
            pieceType = reader.ReadInt32(),
            isWhite = reader.ReadBoolean(),
            xPosition = reader.ReadSingle(),
            yPosition = reader.ReadSingle(),
            enabled = reader.ReadBoolean()
        };
    }
    
    public void ApplyState(object stateobj)
    {
        if (stateobj is GhostState state)
        {
            GhostPieceEntity ghost = BoardManagerEntity.Instance.ghostPiece;
            if (ghost.storedPieceType != state.pieceType || ghost.storedIsWhite != state.isWhite)
                ghost.ChangePieceSprite(state.pieceType, state.isWhite);
            
            
            ghost.enabled = state.enabled;
            ghost.HideFromSelf(!state.enabled); 
            ghost.SetPosition(state.xPosition, state.yPosition);
        }
    }

    public void WriteState(CelesteNetBinaryWriter writer)
    {
        writer.Write(pieceType);
        writer.Write(isWhite);
        writer.Write(xPosition);
        writer.Write(yPosition);
        writer.Write(enabled);
    }
    
    public class GhostState
    {
        public int pieceType;
        public bool isWhite;
        public float xPosition;
        public float yPosition;
        public bool enabled;
    }

#endregion
}