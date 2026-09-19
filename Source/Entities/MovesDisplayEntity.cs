using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CelesteChess;

[Tracked]
[CustomEntity("CelesteChess/MovesDisplayText")]
public class MovesDisplayEntity : Entity
{
    public MovesDisplayEntity(EntityData data, Vector2 offset) : base(data.Position + offset) { }
    
    int moveCount;
    int maxMovesToShow = 22;
    List<PlutoniumText> moveTexts = new();
    Dictionary<int, string> movesDict = new();
    
    int lineHeight = 10;

    public override void Added(Scene scene)
    {
        base.Added(scene);
        Visible = true;

        for (int i = 0; i < maxMovesToShow; i++)
        {
            PlutoniumText t = new PlutoniumText("CelesteChess/fonts/movesFont", "0123456789 abcdefghx+-.KQRNB=", new Vector2(4, 6));
            moveTexts.Add(t);
            Add(t);
        }
    }

    public void AddMoveDebug()
    {
        moveCount++;
        if (moveCount < maxMovesToShow)
        {
            movesDict.Add(moveCount, "dxc4+");
        }
        else
        {
            movesDict.Remove(moveCount - maxMovesToShow);
            movesDict.Add(moveCount, "af3");
        }
    }
    
    public void AddMove(MoveToDisplay move)
    {
        moveCount++;
        string notation = MoveToNotation.ToDisplayString(move);
        movesDict[moveCount] = notation;
        
        if (moveCount > maxMovesToShow)
            movesDict.Remove(moveCount - maxMovesToShow);
    }

    public override void Update()
    {
        base.Update();
        if (Input.CrouchDashPressed)
        {
            AddMoveDebug();
        }
    }

    public override void Render()
    {
        base.Render();
        int i = 0;

        foreach (var kvp in movesDict.OrderBy(p => p.Key))
        {
            int move = kvp.Key % 2;
            moveTexts[i].Print(Position + new Vector2(0, i * lineHeight), $"{(move == 1 ? kvp.Key / 2 < 10 ? " " : "" : "  ")}{(move == 1 ? (kvp.Key / 2) + 1 : "")}{(move == 1 ? "." : " ")}  {kvp.Value}", true, 4.5f, new Color(0.8f, 0.8f, 0.8f, 0.8f), Color.Black);
            i++;
        }
    }
}

static class MoveToNotation
{
    static string Square(int file, int rank)
    {
        char fileChar = (char)('a' + file);
        char rankChar = (char)('1' + rank);
        return $"{fileChar}{rankChar}";
    }
    
    static string PieceLetter(Piece p)
    {
        return p switch
        {
            Piece.King => "K",
            Piece.Queen => "Q",
            Piece.Rook => "R",
            Piece.Bishop => "B",
            Piece.Knight => "N",
            Piece.Pawn => "",
            _ => ""
        };
    }
    
    public static string ToDisplayString(MoveToDisplay m)
    {
        // Roques
        if (m.isCastleKingSide)
            return "o-o";      // ou "0-0" si tu préfères le zéro
        if (m.isCastleQueenSide)
            return "o-o-o";
        
        string prefix;
        if (m.piece == Piece.Pawn)
        {
            if (m.isCapture)
            {
                char fromFileChar = (char)('a' + m.fromFile);
                prefix = fromFileChar + "x";
            }
            else
            {
                prefix = "";
            }
        }
        else
        {
            string pieceChar = PieceLetter(m.piece);
            string capture = m.isCapture ? "x" : "";
            prefix = pieceChar + capture;
        }
        
        string toSquare = Square(m.toFile, m.toRank);
        
        string promo = "";
        if (m.isPromotion)
        {
            string promoChar = PieceLetter(m.promotionPiece);
            promo = "=" + promoChar;
        }
        
        string suffix = "";
        if (m.isCheck)
            suffix = "+";

        return $"{prefix}{toSquare}{promo}{suffix}";
    }
}

public struct MoveToDisplay {
    public Piece piece;

    public int fromFile;
    public int fromRank;
    public int toFile;
    public int toRank;

    public bool isCapture;
    public bool isCheck;

    public bool isCastleKingSide;
    public bool isCastleQueenSide;

    public bool isPromotion;
    public Piece promotionPiece;
}

