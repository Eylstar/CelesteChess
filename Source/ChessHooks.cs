using System.Collections.Generic;
using Celeste.Mod.CoopHelper.IO;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.CelesteChess;

public class ChessHooks
{
    //Prevents from dropping the piece if the player is holding grab
    public static void OnDrop(On.Celeste.Player.orig_Drop orig, Player self)
    {
        if (self.Holding?.Entity is ChessPieceEntity && Input.GrabCheck)
            return;
        orig(self);
    }
    
    //Calculate the valid moves of a piece when the player picks it up
    public static bool OnPickup(On.Celeste.Player.orig_Pickup orig, Player self, Holdable hold)
    {
        if (hold.Entity is ChessPieceEntity piece && hold.Active)
        {
            //If the piece is not the player's turn, return
            if (BoardManagerEntity.Instance.whiteTurn != piece.IsWhite())
                return false;

            BoardManagerEntity.Instance.CalculateValidMoves(TileOverlapDetection(piece), piece);
        }
        return orig(self, hold);
    }
    
    //Compute the move of a piece based on the tile it's dropped on
    public static void OnRelease(On.Celeste.TheoCrystal.orig_OnRelease orig, TheoCrystal self, Vector2 force)
    {
        if (self is ChessPieceEntity piece)
        {
            //Force the drop force being 0
            orig(self, new Vector2(0, 0));
            
            BoardManagerEntity.Instance.ghostPiece.enabled = false;

            bool validMove = true;
            ChessTileEntity tile = TileOverlapDetection(piece);
            if (tile == null || !tile.isValidForActualMove)
            {
                //If the tile is not valid, set the destination to the tile the piece was on
                tile = piece.tile;
                validMove = false;
            }
            
            //Disable the holdable to prevent the player from picking it up again
            Holdable holdable = piece.Get<Holdable>();
            holdable.Active = false;
            
            //Move the piece to the tile and compute the move
            ChessHelpers.MoveSmoothlyEntity(piece, tile.Position + new Vector2(0, 6), 0.25f, () => { 
                holdable.Active = true;
            });
            
            BoardManagerEntity.Instance.ComputeMove(piece, tile, validMove);
        }
        else orig(self, force);
    }
    
    //Sets the color of the tile currently overlapping with the piece
    public static void OnCarry(On.Celeste.Holdable.orig_Carry orig, Holdable self, Vector2 pos)
    {
        if (self.Entity is ChessPieceEntity piece)
        {
            ChessTileEntity tile = TileOverlapDetection(piece);
            if (tile != null)
            {
                //If the tile is valid and overlapped, set the color to the walk color
                if (tile.isValidForActualMove)
                    tile.colorTarget = ColorTarget.WalkGrabValid;
            
                //Detects the tile that was previously hovered and sets its color to none
                ChessTileEntity storedHoveredTile = BoardManagerEntity.Instance.hoveredTile;
                if (storedHoveredTile!= null && storedHoveredTile != tile)
                    storedHoveredTile.colorTarget = storedHoveredTile.isValidForActualMove ? ColorTarget.PossibleDestination : ColorTarget.None;
            
                BoardManagerEntity.Instance.hoveredTile = tile;
            }
            
            GhostPieceEntity ghostPiece = BoardManagerEntity.Instance.ghostPiece;
            if (ghostPiece != null)
            {
                ghostPiece.enabled = true;
                ghostPiece.xPosition = pos.X;
                ghostPiece.yPosition = pos.Y;
                ghostPiece.pieceType = (int)piece.pieceType;
                ghostPiece.isWhite = piece.IsWhite();
            }
            Logger.Log("CelesteChess", $"Calling PostUpdate for ghost piece with ID: {BoardManagerEntity.Instance.ghostPiece.id}");
            ModInterop.PostUpdate(BoardManagerEntity.Instance.ghostPiece.id);
        }
        orig(self, pos);
    }

    
    //Detection of the tile that the piece is currently overlapping with
    static ChessTileEntity TileOverlapDetection(ChessPieceEntity piece)
    {
        ChessTileEntity correctTile = null;
        List<ChessTileEntity> allTiles = GetAllTiles();
        List<ChessTileEntity> tilesOverlapping = new();
        
        //Create a list of all the tiles that are overlapping with the piece
        foreach (ChessTileEntity tile in allTiles)
        {
            if (tile.CollideCheck(piece))
                tilesOverlapping.Add(tile);
        }
        
        Player player = piece.Scene.Tracker.GetEntity<Player>();

        switch (tilesOverlapping.Count)
        {
            case 0:
                return null;
            case 1:
                correctTile = tilesOverlapping[0];
                break;
            case > 1:
                //If there are multiple tiles overlapping, find the closest one to the player
                ChessTileEntity closestTile = null;
                float closestDistance = 1000;
                foreach (ChessTileEntity tile in tilesOverlapping)
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
    
    //Returns a list of all the tiles in the scene
    static List<ChessTileEntity> GetAllTiles()
    {
        List<ChessTileEntity> tiles = new();
        foreach (Entity entity in Engine.Scene.Tracker.GetEntities<ChessTileEntity>())
        {
            if (entity is ChessTileEntity tile)
                tiles.Add(tile);
        }
        return tiles;
    }
    
    public static void IL_DropStopping(ILContext il)
    {
        //Black magic to prevent the player from crouching when pressing down while holding a Crystal
        ILCursor cursor = new ILCursor(il);
        
        if (cursor.TryGotoNextBestFit(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchCallvirt<Player>("Drop")
            ))
        {
            ILLabel label = cursor.DefineLabel();
            
            cursor.EmitDelegate(IsMyLevel);
            cursor.EmitBrtrue(label);
            
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

    static bool IsMyLevel() => BoardManagerEntity.Instance?.Scene is Level;
}