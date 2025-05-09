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

        return validMoves;
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
        
        //Move Forward twice
        if (piece.movedOnce || hasPieceInFront) return validMoves;
        position.Y += piece.IsWhite() ? -1 : 1;

        if (IsInsideBoard(position))
        {
            ChessPieceEntity otherPiece = board.GetTileByGridCoordinates(position).piece;
            if (otherPiece == null)
                TryToAddTileToList(position, piece, validMoves, board);
        }
        
        //En Passant
        
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