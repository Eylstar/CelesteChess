using System;
using System.Collections.Generic;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.CelesteChess;

public class ChessHooks
{
    public static void OnDrop(On.Celeste.Player.orig_Drop orig, Player self)
    {
        if (self.Holding?.Entity is ChessPieceEntity && Input.GrabCheck)
            return;
        orig(self);
    }
    
    public static void OnRelease(On.Celeste.TheoCrystal.orig_OnRelease orig, TheoCrystal self, Vector2 force)
    {
        if (self is ChessPieceEntity piece)
        {
            orig(self, new Vector2(0, 0));

            bool validMove = true;
            
            ChessTileEntity tile = TileOverlapDetection(piece);
            if (tile == null || !tile.isValidForActualMove)
            {
                tile = piece.tile;
                validMove = false;
            }
            
            Holdable holdable = piece.Get<Holdable>();
            holdable.Active = false;
            
            MoveHelper.MoveSmoothlyEntity(piece, tile.Position + new Vector2(0, 6), 0.25f, () => { 
                holdable.Active = true;
            });
            BoardManagerEntity.Instance.ComputeMove(piece, tile, validMove);
            return;
        }
        orig(self, force);
    }
    
    public static bool OnPickup(On.Celeste.Player.orig_Pickup orig, Player self, Holdable hold)
    {
        if (!hold.Active)
        {
            Logger.Log(LogLevel.Warn, "Mod", "Holdable is not active");
            return false;
        }
        
        if (hold.Entity is ChessPieceEntity piece)
        {
            if (BoardManagerEntity.Instance.whiteTurn != piece.IsWhite())
                return false;

            ChessTileEntity tile = TileOverlapDetection(piece);
            BoardManagerEntity.Instance.hoveredTile = tile;
            BoardManagerEntity boardManager = piece.Scene.Tracker.GetEntity<BoardManagerEntity>();
            if (boardManager == null) return false;
            Logger.Log(LogLevel.Warn, "Mod", $"I start to grab and I'm in check: {(boardManager.whiteTurn ? boardManager.whiteCheck : boardManager.blackCheck)}");
            boardManager.CalculateValidMoves(tile, piece);
        }
        
        return orig(self, hold);
    }
    
    
    public static void OnCarry(On.Celeste.Holdable.orig_Carry orig, Holdable self, Vector2 pos)
    {
        if (self.Entity is ChessPieceEntity piece)
        {
            ChessTileEntity tile = TileOverlapDetection(piece);
            if (tile != null)
            {
                tile.colorTarget = tile.isValidForActualMove ? ColorTarget.WalkGrabValid : ColorTarget.None;
                ChessTileEntity storedHoveredTile = BoardManagerEntity.Instance.hoveredTile;
                if (storedHoveredTile!= null && tile != storedHoveredTile)
                {
                    storedHoveredTile.colorTarget = storedHoveredTile.isValidForActualMove ? ColorTarget.PossibleDestination : ColorTarget.None;
                    BoardManagerEntity.Instance.hoveredTile = tile;
                }
            }
        }
        orig(self, pos);
    }

    static List<ChessTileEntity> getAllTiles()
    {
        List<ChessTileEntity> tiles = new();
        foreach (Entity entity in Engine.Scene.Tracker.GetEntities<ChessTileEntity>())
        {
            if (entity is ChessTileEntity tile)
                tiles.Add(tile);
        }
        return tiles;
    }

    static ChessTileEntity TileOverlapDetection(ChessPieceEntity piece)
    {
        ChessTileEntity correctTile = null;
        List<ChessTileEntity> allTiles = getAllTiles();
        List<ChessTileEntity> tilesInside = new();
        
        foreach (ChessTileEntity tile in allTiles)
        {
            if (tile.CollideCheck(piece))
                tilesInside.Add(tile);
        }
        
        Player player = piece.Scene.Tracker.GetEntity<Player>();

        switch (tilesInside.Count)
        {
            case 0:
                Logger.Log(LogLevel.Warn, "Chess", $"{piece.ChessPieceColor} {piece.pieceType} Dropped in: No Tile");
                return null;
            case 1:
                correctTile = tilesInside[0];
                break;
            case > 1:
                ChessTileEntity closestTile = null;
                float closestDistance = 1000;
                foreach (ChessTileEntity tile in tilesInside)
                {
                    float distance = Vector2.Distance(player.Position, tile.Position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestTile = tile;
                    }
                }
                correctTile = closestTile;
                break;
        }
        return correctTile;
    }

    
    public static void IL_DropStopping(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);
        
        if (cursor.TryGotoNextBestFit(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchCallvirt<Player>("Drop")
            ))
        {
            ILLabel label = cursor.DefineLabel();
            cursor.Emit(OpCodes.Ldc_I4_0);
            cursor.Emit(OpCodes.Brfalse, label);
            
            if(cursor.TryGotoNextBestFit(MoveType.After,
                       x => x.MatchLdarg(0),
                       x => x.MatchLdfld<Player>("Sprite"),
                       x => x.MatchLdcR4(1.4f),
                       x => x.MatchLdcR4(0.6f)))
            {
                
                if (cursor.TryGotoNext(MoveType.After, 
                        x => x.MatchStfld<GraphicsComponent>("Scale")))
                {
                    cursor.MarkLabel(label);
                }
            }
        }
    }
}