using System;
using System.Collections;
using System.Collections.Generic;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.CelesteChess;

public class ChessEngine
{
    static readonly Vector2[] directions = 
    {
        new Vector2(1, 0), // Right
        new Vector2(-1, 0), // Left
        new Vector2(0, 1), // Down
        new Vector2(0, -1), // Up
        new Vector2(1, 1), // Down-Right
        new Vector2(-1, -1), // Up-Left
        new Vector2(1, -1), // Up-Right
        new Vector2(-1, 1) // Down-Left
    };
    
    static readonly Vector2[] knightMoves = 
    {
        new Vector2(2, 1),
        new Vector2(2, -1),
        new Vector2(-2, 1),
        new Vector2(-2, -1),
        new Vector2(1, 2),
        new Vector2(1, -2),
        new Vector2(-1, 2),
        new Vector2(-1, -2)
    };
    
    public static List<ChessTileEntity> CalculateValidMoves(ChessPieceEntity piece, ChessTileEntity tile, BoardManagerEntity board = null)
    {
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
            case Piece.Bishop:
            case Piece.Rook:
            case Piece.Queen:
                validMoves = CalculateLongRangeMoves(piece, tile, board);
                break;
            case Piece.King:
                validMoves = CalculateKingMoves(piece, tile, board);
                break;
        }
        
        return validMoves;
    }
    
    public static List<ChessTileEntity> CalculateLongRangeMoves(ChessPieceEntity piece, ChessTileEntity tile, BoardManagerEntity board)
    {
        int startIndex = piece.pieceType == Piece.Bishop ? 4 : 0;
        int endIndex = piece.pieceType == Piece.Rook ? 4 : 8;
        
        List<ChessTileEntity> validMoves = new();
        
        for (int i = startIndex; i < endIndex; i++)
        {
            
            Vector2 position = tile.gridCoordinates;
            while (true)
            {
                position += directions[i];
                if (!IsInsideBoard(position))
                    break;

                if (TryToAddTileToList(position, piece, validMoves, board))
                    break;
            }
        }
        return validMoves;
    }
    
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
                TryToAddTileToList(position, piece, validMoves);
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
                    TryToAddTileToList(newPos, piece, validMoves);
            }
        }
        
        //Move Forward twice
        if (piece.movedOnce || hasPieceInFront) return validMoves;
        position.Y += piece.IsWhite() ? -1 : 1;

        if (IsInsideBoard(position))
        {
            ChessPieceEntity otherPiece = board.GetTileByGridCoordinates(position).piece;
            if (otherPiece == null)
                TryToAddTileToList(position, piece, validMoves);
        }
        
        //En Passant
        
        return validMoves;
    }

    public static List<ChessTileEntity> CalculateKingMoves(ChessPieceEntity piece, ChessTileEntity tile, BoardManagerEntity board)
    {
        List<ChessTileEntity> validMoves = new();
        for (int i = 0; i < directions.Length; i++)
        {
            Vector2 position = tile.gridCoordinates + directions[i];
            if (IsInsideBoard(position))
                TryToAddTileToList(position, piece, validMoves, board);
        }

        return validMoves;
    }
    
    
    static bool IsInsideBoard(Vector2 position)
    {
        return position.X >= 0 && position.X < 8 && position.Y >= 0 && position.Y < 8;
    }
    
    static bool TryToAddTileToList(Vector2 otherTilePosition, ChessPieceEntity piece, List<ChessTileEntity> validMoves, BoardManagerEntity board = null)
    {
        board ??= BoardManagerEntity.Instance;
        ChessTileEntity otherTile = board.GetTileByGridCoordinates(otherTilePosition);
        if (otherTile == null) return true;

        if (otherTile.piece == null)
            validMoves.Add(otherTile);
        
        else
        {
            if (otherTile.piece.ChessPieceColor != piece.ChessPieceColor)
                validMoves.Add(otherTile);
            return true;
        }
        return false;
    }
}