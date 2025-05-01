using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CelesteChess;

public class GhostPieceEntity
{
    public void SpawnGhostPiece(Piece type)
    {
        ChessPieceEntity ghostPiece = new ChessPieceEntity(new EntityData(), Vector2.Zero);
        ghostPiece.pieceType = type;
    }
}