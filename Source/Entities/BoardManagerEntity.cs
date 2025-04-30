using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CelesteChess;

[Tracked]
[CustomEntity("CelesteChess/BoardManager")]
public class BoardManagerEntity : Entity
{
    public bool whiteTurn = true;
    public bool whiteCheck = false;
    public bool blackCheck = false;
    
    public static BoardManagerEntity Instance;
    
    Player player;
    List<ChessTileEntity> tiles = new();
    List<ChessTileEntity> validMoves = new();
    
    public ChessTileEntity hoveredTile;

    public int test1 = 1;
    public int test2 = 2;
    public int test3 = 3;
    
    public Action<int, int> onMove;

    public BoardManagerEntity(EntityData data, Vector2 offset) : base(data.Position + offset) { }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);
        
        foreach (Entity entity in scene.Tracker.GetEntities<ChessTileEntity>())
        {
            if (entity is ChessTileEntity tile)
            {
                tiles.Add(tile);
            }
        }
        Instance = this;
        
        AssignTriggerValues();
        SpawnChessPieces();
    }

    void AssignTriggerValues()
    {
        tiles.Sort((a, b) =>
        {
            if (a.Position.Y == b.Position.Y)
                return a.Position.X.CompareTo(b.Position.X);

            return a.Position.Y.CompareTo(b.Position.Y);
        });
        
        for (int i = 0; i < tiles.Count; i++)
        {
            tiles[i].gridPosition = i;
            tiles[i].gridCoordinates = new Vector2(i % 8, i / 8);
        }
    }
    
    void SpawnChessPieces()
    {
        Dictionary<int, PieceInfo> piecesDataToSpawn = new();
        Piece[] row = { Piece.Rook, Piece.Knight, Piece.Bishop, Piece.Queen, Piece.King, Piece.Bishop, Piece.Knight, Piece.Rook };
        
        for (int i = 0; i < 8; i++)
        {
            piecesDataToSpawn.Add(i, new PieceInfo(row[i], ChessPieceColor.Black));
            piecesDataToSpawn.Add(i + 8, new PieceInfo(Piece.Pawn, ChessPieceColor.Black));
            piecesDataToSpawn.Add(i + 48, new PieceInfo(Piece.Pawn, ChessPieceColor.White));
            piecesDataToSpawn.Add(i + 56, new PieceInfo(row[i], ChessPieceColor.White));
        }

        foreach (ChessTileEntity tile in tiles)
        {
            if (piecesDataToSpawn.TryGetValue(tile.gridPosition, out PieceInfo pieceInfo))
            {
                ChessPieceEntity chessPiece = new(new EntityData(), Vector2.Zero);
                chessPiece.pieceType = pieceInfo.pieceType;
                chessPiece.ChessPieceColor = pieceInfo.ChessPieceColor;
                chessPiece.Position = tile.Position;
                chessPiece.tile = tile;
                tile.piece = chessPiece; 
                Scene.Add(chessPiece);
            }
        }
    }
    
    public override void Update()
    {
        base.Update();
        
        player = Scene.Tracker.GetEntity<Player>();
        if (player == null) return;
        player.carryOffset = new Vector2(0, -20);

        if (Input.DashPressed)
        {
            //Teleport the player to the center, tile 27
            player.Position = tiles[27].Position + new Vector2(Math.Abs(tiles[27].Width / 2), 6);
        }
    }
    
    public ChessTileEntity GetTileByGridPosition(int gridPosition)
    {
        if (gridPosition < 0 || gridPosition >= tiles.Count)
            return null;
        
        return tiles[gridPosition];
    }
    
    public ChessTileEntity GetTileByGridCoordinates(Vector2 gridCoordinates)
    {
        int gridPosition = (int)gridCoordinates.Y * 8 + (int)gridCoordinates.X;
        return GetTileByGridPosition(gridPosition);
    }
    
    
    
    public void CalculateValidMoves(ChessTileEntity tile, ChessPieceEntity piece)
    {
        if (piece == null || tile == null)
            return;
        validMoves.Clear();
        validMoves.AddRange(ChessEngine.CalculateValidMoves(piece, tile));
        if (validMoves == null || validMoves.Count == 0)
        {
            Logger.Log(LogLevel.Warn, "Chess", "No valid moves");
            return;
        }
        Logger.Log(LogLevel.Warn, "Mod", $"I calculated {validMoves.Count} valid moves for {piece.pieceType} {piece.ChessPieceColor}");
        //Check for checks
        List<ChessTileEntity> safe = new();
        foreach (ChessTileEntity t in validMoves)
        {
            if (t == null) continue;
            BoardManagerEntity temporaryBoard = CloneBoard();
            ChessPieceEntity tempPiece = temporaryBoard.GetTileByGridCoordinates(piece.tile.gridCoordinates).piece;
            ChessTileEntity tempTile = temporaryBoard.GetTileByGridCoordinates(t.gridCoordinates);
            if (temporaryBoard.SimulateMove(tempPiece, tempTile))
                safe.Add(t);
            temporaryBoard.Free();
        }
        validMoves = safe;
        
        foreach (ChessTileEntity validMove in validMoves)
        {
            if (validMove != null)
                validMove.SetValidMove();
        }
    }
    
    public void ComputeMove(ChessPieceEntity piece, ChessTileEntity tile, bool valid, bool fromSource = true)
    {
        ResetValidMoves();
        
        if (tile == null || piece == null || tile == piece.tile || valid == false)
            return;

        if (fromSource)
            onMove?.Invoke(piece.tile.gridPosition, tile.gridPosition);
        
        if (tile.piece != null)
        {
            Scene.Remove(tile.piece);
            tile.piece = null;
        }
        
        piece.tile.piece = null;
        piece.tile = tile;
        tile.piece = piece;

        piece.movedOnce = true;

        if (piece.pieceType == Piece.Pawn && (piece.IsWhite() && tile.gridCoordinates.Y == 0) || (!piece.IsWhite() && tile.gridCoordinates.Y == 7))
        {
            piece.PromoteToQueen();
        }
        
        CheckChecks(false);
        
        whiteTurn = !whiteTurn;
    }
    
    public void MoveReceivedByNetwork(int origin, int target)
    {
        Logger.Log(LogLevel.Warn, "Chess", $"Move received from network: {origin} to {target}");
        ChessTileEntity originTile = GetTileByGridPosition(origin);
        ChessTileEntity targetTile = GetTileByGridPosition(target);
        ChessPieceEntity piece = originTile.piece;
        
        if (piece == null || targetTile == null)
            return;
        
        Holdable holdable = piece.Get<Holdable>();
        holdable.Active = false;
            
        MoveHelper.MoveSmoothlyEntity(piece, targetTile.Position + new Vector2(0, 6), 0.25f, () => { 
            holdable.Active = true;
        });
        ComputeMove(piece, targetTile, true, false);
    }

    public bool CheckChecks(bool simulation)
    {
        List<ChessTileEntity> attackedTiles = new();
        foreach (ChessTileEntity tile in tiles)
        {
            if (tile.piece != null && tile.piece.IsWhite() == whiteTurn)
            {
                attackedTiles.AddRange(ChessEngine.CalculateValidMoves(tile.piece, tile, (simulation ? this : BoardManagerEntity.Instance)));
            }
        }
        
        whiteCheck = blackCheck = false;
        
        foreach (ChessTileEntity tile in attackedTiles)
        {
            if (tile.piece != null)
                Logger.Log(LogLevel.Warn, "Chess", "Attacked tile: " + tile.gridCoordinates + " with " + (tile.piece != null ? tile.piece.pieceType : "nothing") + " by " + (whiteTurn ? "White" : "Black"));
            if (tile.piece?.pieceType == Piece.King)
            {
                if (!simulation)
                {
                    if (tile.piece.IsWhite())
                        whiteCheck = true;
                    else
                        blackCheck = true;
                }
                else
                {
                    Logger.Log(LogLevel.Warn, "Mod", $"SIMULATION RUNNING");
                }
                Logger.Log(LogLevel.Warn, "Chess", "Check detected for " + (tile.piece.IsWhite() ? "White" : "Black") + " King attacked by " + tile.piece.pieceType);
                return true;
            }
        }
        return false;
    }
    
    public void ResetValidMoves()
    {
        foreach (ChessTileEntity tile in tiles)
        {
            if (tile != null)
            {
                tile.colorTarget = ColorTarget.None;
                tile.isValidForActualMove = false;
                validMoves.Clear();
            }
        }
    }
    
    public Player GetPlayer()
    {
        return player;
    }
    
    struct PieceInfo
    {
        public Piece pieceType;
        public ChessPieceColor ChessPieceColor;

        public PieceInfo(Piece pieceType, ChessPieceColor chessPieceColor)
        {
            this.pieceType = pieceType;
            ChessPieceColor = chessPieceColor;
        }
    }


    private BoardManagerEntity CloneBoard()
    {
        BoardManagerEntity clone = new(new EntityData(), Vector2.Zero);
        clone.whiteTurn = !whiteTurn;
        clone.whiteCheck = whiteCheck;
        clone.blackCheck = blackCheck;
        clone.tiles = new List<ChessTileEntity>();
        foreach (ChessTileEntity tile in tiles)
        {
            ChessTileEntity newTile = new ChessTileEntity(tile.gridCoordinates);
            clone.tiles.Add(newTile);
        }

        foreach (ChessTileEntity tile in tiles)
        {
            if (tile.piece != null)
            {
                ChessPieceEntity newPiece = tile.piece.Clone();
                ChessTileEntity newTile = clone.tiles.First(t => t.gridCoordinates == tile.gridCoordinates);
                newTile.piece = newPiece;
                newPiece.tile = newTile;
            }
        }
        return clone;
    }
    
    public bool SimulateMove(ChessPieceEntity piece, ChessTileEntity destination)
    {
        ChessPieceEntity capturedPiece = destination.piece;
        if (capturedPiece != null)
            destination.piece = null;
        
        piece.tile.piece = null;
        piece.tile = destination;
        destination.piece = piece;
        piece.movedOnce = true;
        if (CheckChecks(true))
        {
            return false;
        }
        return true;
    }
    
    public void Free()
    {
        foreach (ChessTileEntity tile in tiles)
        {
            if (tile.piece != null)
                tile.piece.tile = null;
        }
        tiles.Clear();
        validMoves.Clear();
    }
}


public enum Piece
{
    Pawn = 0,
    Rook = 1, 
    Knight = 2,
    Bishop = 3,
    Queen = 4,
    King = 5
}

public enum ChessPieceColor
{
    White,
    Black
}