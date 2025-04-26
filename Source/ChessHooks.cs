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
            ChessTileTrigger tile = TileCoordinatesDetection(piece);
            
            if (tile == null) return;
            
            Logger.Log(LogLevel.Warn, "Chess", $"{piece.ChessPieceColor} {piece.pieceType} Dropped in: {tile.gridPosition} ({tile.gridCoordinates})");
            Helpers.MoveSmoothlyEntityHorizontal(piece, tile.Position.X + tile.Width / 2, 0.1f);
            
            return;
        }
        orig(self, force);
    }
    
    public static void OnPickup(On.Celeste.TheoCrystal.orig_OnPickup orig, TheoCrystal self)
    {
        if (self is ChessPieceEntity piece)
        {
            ChessTileTrigger tile = TileCoordinatesDetection(piece);
            Logger.Log(LogLevel.Warn, "Chess", $"{piece.ChessPieceColor} {piece.pieceType} Grabbed in: {tile.gridPosition} ({tile.gridCoordinates})");
        }
        orig(self);
    }

    static ChessTileTrigger TileCoordinatesDetection(ChessPieceEntity piece)
    {
        ChessTileTrigger correctTile = null;
        List<ChessTileTrigger> triggersInside = new();
        
        Player player = piece.Scene.Tracker.GetEntity<Player>();
        Tuple<int, Vector2> error = new(-1, new Vector2(-1, -1));
        
        foreach (Trigger trigger in player.triggersInside)
        {
            if (trigger is not ChessTileTrigger chessTileTrigger) continue;
            triggersInside.Add(chessTileTrigger);
        }

        switch (triggersInside.Count)
        {
            case 0:
                return null;
            case 1:
                correctTile = triggersInside[0];
                break;
            case > 1:
                ChessTileTrigger closestTrigger = null;
                float closestDistance = 1000;
                foreach (ChessTileTrigger trigger in triggersInside)
                {
                    float distance = Vector2.Distance(player.Position, trigger.Position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestTrigger = trigger;
                    }
                }
                correctTile = closestTrigger;
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