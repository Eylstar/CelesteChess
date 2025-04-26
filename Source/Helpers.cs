using System;
using System.Collections;
using System.Collections.Generic;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.CelesteChess;

public static class Helpers
{
    public static void MoveSmoothlyEntity(Entity entity, Vector2 targetPosition, float speed)
    {
        if (entity == null) return;
        entity.Add(new Coroutine(EntityMove(entity, targetPosition, speed)));
    }
    
    public static void MoveSmoothlyEntityInvoke(Entity entity, Vector2 targetPosition, float speed, Action onComplete)
    {
        if (entity == null) return;
        entity.Add(new Coroutine(EntityMove(entity, targetPosition, speed)));
    }
    
    static IEnumerator EntityMove(Entity entity, Vector2 targetPosition, float speed)
    {
        if (entity == null) yield break;
        while (Vector2.Distance(entity.Position, targetPosition) > 1f)
        {
            entity.Position = Vector2.Lerp(entity.Position, targetPosition, speed * Engine.DeltaTime);
            yield return null;
        }
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