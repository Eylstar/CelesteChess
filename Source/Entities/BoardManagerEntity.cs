using System;
using System.Collections;
using System.Collections.Generic;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CelesteChess;

[Tracked]
[CustomEntity("CelesteChess/BoardManager")]
public class BoardManagerEntity : Entity
{
    List<ChessTileTrigger> tiles = new();

    public BoardManagerEntity(EntityData data, Vector2 offset) : base(data.Position + offset) { }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);

        foreach (Entity entity in scene.Tracker.GetEntities<ChessTileTrigger>())
        {
            if (entity is ChessTileTrigger tile)
            {
                tiles.Add(tile);
            }
        }
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

        foreach (ChessTileTrigger tile in tiles)
        {
            if (piecesDataToSpawn.TryGetValue(tile.gridPosition, out PieceInfo pieceInfo))
            {
                ChessPieceEntity chessPiece = new(new EntityData(), Vector2.Zero);
                chessPiece.pieceType = pieceInfo.pieceType;
                chessPiece.ChessPieceColor = pieceInfo.ChessPieceColor;
                chessPiece.Position = tile.Position + new Vector2(tile.Width / 2, tile.Height / 2);
                Scene.Add(chessPiece);
            }
        }
    }
    
    struct PieceInfo
    {
        public Piece pieceType;
        public ChessPieceColor ChessPieceColor;

        public PieceInfo(Piece pieceType, ChessPieceColor chessPieceColor)
        {
            this.pieceType = pieceType;
            this.ChessPieceColor = chessPieceColor;
        }
    }
}
