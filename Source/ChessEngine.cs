using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.CelesteChess;

public class ChessEngine
{
    static readonly Vector2[] directions = 
    {
        new (1, 0), // Right
        new (-1, 0), // Left
        new (0, 1), // Down
        new (0, -1), // Up
        new (1, 1), // Down-Right
        new (-1, -1), // Up-Left
        new (1, -1), // Up-Right
        new (-1, 1) // Down-Left
    };

    static readonly Vector2[] knightMoves =
    {
        new(2, 1),
        new(2, -1),
        new(-2, 1),
        new(-2, -1),
        new(1, 2),
        new(1, -2),
        new(-1, 2),
        new(-1, -2)
    };
    
    static readonly Vector2[] castle =
    {
        new(1, 0),
        new(2, 0)
    };
    
    //Main function to calculate the valid moves of any piece
    public static List<ChessTileEntity> CalculateValidMoves(ChessPieceEntity piece, ChessTileEntity tile, BoardManagerEntity board = null)
    {
        //If the board is null, get the instance of the board instead of referencing it (clone context)
        board ??= BoardManagerEntity.Instance;
        List<ChessTileEntity> validMoves = new();
        
        switch (piece.pieceType)
        {
            case Piece.Pawn:
                validMoves = CalculatePawnMoves(piece, tile, board);
                break;
            case Piece.Knight:
                validMoves = CalculateKnightMoves(piece, tile, board);
                break;
            case Piece.Bishop or Piece.Rook or Piece.Queen:
                validMoves = CalculateLongRangeMoves(piece, tile, board);
                break;
            case Piece.King:
                validMoves = CalculateKingMoves(piece, tile, board);
                break;
        }
        
        return validMoves;
    }
    
    //Calculate the valid moves of a long range piece (Bishop, Rook, Queen)
    public static List<ChessTileEntity> CalculateLongRangeMoves(ChessPieceEntity piece, ChessTileEntity tile, BoardManagerEntity board)
    {
        //Change the indexes of the Direction array based on the piece type
        int startIndex = piece.pieceType == Piece.Bishop ? 4 : 0;
        int endIndex = piece.pieceType == Piece.Rook ? 4 : 8;
        
        List<ChessTileEntity> validMoves = new();
        
        for (int i = startIndex; i < endIndex; i++)
        {
            Vector2 position = tile.gridCoordinates;
            //For each direction, move until the end of the board or until a piece is found
            while (true)
            {
                position += directions[i];
                //If the position is outside the board, break the loop
                if (!IsInsideBoard(position)) 
                    break;
                //If the position is valid, add it to the list, if not break the loop
                if (!TryToAddTileToList(position, piece, validMoves, board)) 
                    break;
            }
        }
        return validMoves;
    }
    
    //Calculate the valid moves of a knight
    public static List<ChessTileEntity> CalculateKnightMoves(ChessPieceEntity piece, ChessTileEntity tile, BoardManagerEntity board)
    {
        List<ChessTileEntity> validMoves = new();
        
        foreach (Vector2 move in knightMoves)
        {
            Vector2 position = tile.gridCoordinates + move;
            if (IsInsideBoard(position))
                TryToAddTileToList(position, piece, validMoves, board);
        }
        return validMoves;
    }
    
    //Calculate the valid moves of a king
    public static List<ChessTileEntity> CalculateKingMoves(ChessPieceEntity piece, ChessTileEntity tile, BoardManagerEntity board)
    {
        List<ChessTileEntity> validMoves = new();
        
        foreach (Vector2 move in directions)
        {
            Vector2 position = tile.gridCoordinates + move;
            if (IsInsideBoard(position))
                TryToAddTileToList(position, piece, validMoves, board);
        }
        
        //Check for castling
        if (!piece.movedOnce)
        {
            Vector2 leftRookPosition = new Vector2(0, piece.IsWhite() ? 7 : 0);
            Vector2 rightRookPosition = new Vector2(7, piece.IsWhite() ? 7 : 0);
            
            TryAddCastleMovement(piece, tile, board, validMoves, leftRookPosition, true);
            TryAddCastleMovement(piece, tile, board, validMoves, rightRookPosition, false);
        }

        return validMoves;
    }

    static void TryAddCastleMovement(ChessPieceEntity piece, ChessTileEntity tile, BoardManagerEntity board, List<ChessTileEntity> validMoves, Vector2 rookPosition, bool isLeft)
    {
        ChessPieceEntity rook = board.GetTileByGridCoordinates(rookPosition).piece;
        if (rook != null && !rook.movedOnce)
        {
            int direction = isLeft ? -1 : 1;
            
            Vector2[] movesToCheck = 
            {
                tile.gridCoordinates,
                tile.gridCoordinates + castle[0] * direction,
                tile.gridCoordinates + castle[1] * direction
            };
            
            if (IsHorizontalPathClear(piece.tile, rook.tile, board) && IsCastlePathSafe(piece, movesToCheck, board))
            {
                Vector2 kingTarget = tile.gridCoordinates + new Vector2(2 * direction, 0);
                ChessTileEntity kingDestination = board.GetTileByGridCoordinates(kingTarget);
                if (IsInsideBoard(kingTarget) && TryToAddTileToList(kingTarget, piece, validMoves, board))
                {
                    kingDestination.isCastleDestination = true;
                    kingDestination.castlePiece = rook;
                    Vector2 rookTarget = new Vector2(isLeft ? 3 : 5, piece.IsWhite() ? 7 : 0);
                    kingDestination.rookDestinationTile = board.GetTileByGridCoordinates(rookTarget);
                }
            }
        }
    }
    
    public static bool IsHorizontalPathClear(ChessTileEntity orig, ChessTileEntity dest, BoardManagerEntity board)
    {
        if (orig.gridCoordinates.Y != dest.gridCoordinates.Y) return false;

        int startX = (int)Math.Min(orig.gridCoordinates.X, dest.gridCoordinates.X);
        int endX = (int)Math.Max(orig.gridCoordinates.X, dest.gridCoordinates.X);

        for (int x = startX + 1; x < endX; x++)
        {
            if (board.GetTileByGridCoordinates(new Vector2(x, orig.gridCoordinates.Y)).piece != null)
                return false;
        }
        
        return true;
    }
    
    public static bool IsCastlePathSafe(ChessPieceEntity king, Vector2[] positionsToCheck, BoardManagerEntity board)
    {
        foreach (Vector2 position in positionsToCheck)
        {
            BoardManagerEntity clone = board.CloneBoard();
            
            ChessPieceEntity simulatedKing = clone.GetTileByGridCoordinates(king.tile.gridCoordinates).piece;
            ChessTileEntity destination = clone.GetTileByGridCoordinates(position);

            
            bool safe = clone.SimulateMove(simulatedKing, destination);
            clone.ReleaseClone();
            
            if (!safe) return false;
        }
        return true;
    }
    
    //Calculate the valid moves of a pawn
    public static List<ChessTileEntity> CalculatePawnMoves(ChessPieceEntity piece, ChessTileEntity tile, BoardManagerEntity board)
    {
        List<ChessTileEntity> validMoves = new();
        Vector2 position = tile.gridCoordinates;
        bool hasPieceInFront = false;
        
        //Move Forward once
        position.Y += piece.IsWhite() ? -1 : 1;

        if (IsInsideBoard(position))
        {
            ChessPieceEntity otherPiece = board.GetTileByGridCoordinates(position).piece;
            if (otherPiece == null)
                TryToAddTileToList(position, piece, validMoves, board);
            else
                hasPieceInFront = true;
            
        }
        
        //Diagonal Attack
        int[] deltas = new[] {1, -1};

        foreach (int delta in deltas)
        {
            Vector2 newPos = position;
            newPos.X += delta;
            
            if (IsInsideBoard(newPos))
            {
                ChessPieceEntity otherPiece = board.GetTileByGridCoordinates(newPos).piece;
                if(otherPiece != null && otherPiece.ChessPieceColor != piece.ChessPieceColor)
                    TryToAddTileToList(newPos, piece, validMoves, board);
            }
        }
        
        //En Passant
        
        
        //Move Forward twice
        if (piece.movedOnce || hasPieceInFront) return validMoves;
        position.Y += piece.IsWhite() ? -1 : 1;

        if (IsInsideBoard(position))
        {
            ChessPieceEntity otherPiece = board.GetTileByGridCoordinates(position).piece;
            if (otherPiece == null)
                TryToAddTileToList(position, piece, validMoves, board);
        }
        
        return validMoves;
    }

    
    
    //Check if the position is inside the board to avoid out of bounds exceptions
    static bool IsInsideBoard(Vector2 position)
    {
        return position.X >= 0 && position.X < 8 && position.Y >= 0 && position.Y < 8;
    }
    
    //Check if the tile is a valid move and add it to the list
    static bool TryToAddTileToList(Vector2 otherTilePosition, ChessPieceEntity piece, List<ChessTileEntity> validMoves, BoardManagerEntity board)
    {
        ChessTileEntity otherTile = board.GetTileByGridCoordinates(otherTilePosition);
        if (otherTile == null) return false;

        //Check if the tile is empty or occupied by a piece of the other color
        if (otherTile.piece == null)
        {
            validMoves.Add(otherTile);
            return true;
        }
        else
        {
            if (otherTile.piece.ChessPieceColor != piece.ChessPieceColor)
                validMoves.Add(otherTile);
            return false;
        }
    }
}