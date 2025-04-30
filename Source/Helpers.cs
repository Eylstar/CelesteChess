using System;
using System.Collections;
using System.Collections.Generic;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;

using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.Client;
using Celeste.Mod.CelesteNet.DataTypes;

namespace Celeste.Mod.CelesteChess;

public static class MoveHelper
{
    public static void MoveSmoothlyEntity(Entity entity, Vector2 targetPosition, float duration)
    {
        if (entity == null) return;
        entity.Add(new Coroutine(EntityMove(entity, targetPosition, duration)));
    }
    
    public static void MoveSmoothlyEntity(Entity entity, Vector2 targetPosition, float duration, Action onComplete)
    {
        if (entity == null) return;
        entity.Add(new Coroutine(EntityMove(entity, targetPosition, duration, onComplete)));
    }
    
    static IEnumerator EntityMove(Entity entity, Vector2 targetPosition, float duration, Action onComplete = null)
    {
        if (entity == null) yield break;
        Vector2 startPosition = entity.Position;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Engine.DeltaTime;
            float t = MathHelper.Clamp(elapsedTime / duration, 0f, 1f);
            float ease = Ease.SineOut(t);
            entity.Position = Vector2.Lerp(startPosition, targetPosition, ease);
            yield return null;
        }
        entity.Position = targetPosition;
        onComplete?.Invoke();
    }
    
    public static void MoveSmoothlyEntityHorizontal(Entity entity, float targetX, float duration)
    {
        if (entity == null) return;
        entity.Add(new Coroutine(EntityMoveHorizontal(entity, targetX, duration)));
    }
    
    static IEnumerator EntityMoveHorizontal(Entity entity, float targetX, float duration)
    {
        if (entity == null) yield break;
        Vector2 startPosition = entity.Position;
        float elapsedTime = 0f;
        Vector2 targetPosition = new Vector2(targetX, entity.Position.Y);

        while (elapsedTime < duration)
        {
            targetPosition = new Vector2(targetX, entity.Position.Y);
            elapsedTime += Engine.DeltaTime;
            float t = MathHelper.Clamp(elapsedTime / duration, 0f, 1f);
            entity.Position = Vector2.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        entity.Position = targetPosition;
    }
}

public class NetHelper
{
    public static void SendMove(ChessPieceEntity piece, ChessTileEntity tile)
    {
        if (piece == null || tile == null) return;
        CelesteNetClient client = CelesteNetClient.Instance;
        if (client == null || !client.IsConnected) return;

        ChessMove move = new(piece, tile);
        client.Send(new NetMessage(NetMessageType.ChessMove, move));
    }
}