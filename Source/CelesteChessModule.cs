using System;
using Celeste.Mod.Celeste3DEngine;
using Celeste.Mod.CoopHelper.IO;
using Monocle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.ModInterop;

namespace Celeste.Mod.CelesteChess;

public class CelesteChessModule : EverestModule {
    public static CelesteChessModule Instance { get; private set; }
    
    public CelesteChessModule() {
        Instance = this;
        #if DEBUG
            Logger.SetLogLevel(nameof(CelesteChessModule), LogLevel.Verbose);
        #else
            Logger.SetLogLevel(nameof(CelesteChessModule), LogLevel.Info);
        #endif
    }

    public override void Load()
    {
        //Prevents from dropping the piece if the player is holding grab
        On.Celeste.Player.Drop += ChessHooks.OnDrop;
        
        //Compute the move of a piece based on the tile it's dropped on
        On.Celeste.TheoCrystal.OnRelease += ChessHooks.OnRelease;
        
        //Calculate the valid moves of a piece when the player picks it up
        On.Celeste.Player.Pickup += ChessHooks.OnPickup;
        
        //Sets the color of the tile currently overlapping with the piece
        On.Celeste.Holdable.Carry += ChessHooks.OnCarry;
        
        //Black magic to prevent the player from crouching when pressing down while holding a Crystal
        IL.Celeste.Player.NormalUpdate += ChessHooks.IL_DropStopping;
        
        typeof(ModInterop).ModInterop();
        ModInterop.RegisterSyncedType(ChessPieceEntity.CoopHelperHeader, false, false, true, ChessPieceEntity.Parse);
        ModInterop.RegisterSyncedType(GhostPieceEntity.CoopHelperHeader, false, false, true, GhostPieceEntity.Parse);
    }

    public override void Unload()
    {
        On.Celeste.Player.Drop -= ChessHooks.OnDrop;
        On.Celeste.TheoCrystal.OnRelease -= ChessHooks.OnRelease;
        On.Celeste.Holdable.Carry -= ChessHooks.OnCarry;
        IL.Celeste.Player.NormalUpdate -= ChessHooks.IL_DropStopping;
        On.Celeste.Player.Pickup -= ChessHooks.OnPickup;
    }
}