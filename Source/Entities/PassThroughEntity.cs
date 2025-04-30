using System.Collections;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CelesteChess;

[CustomEntity("CelesteChess/PassThrough")]
public class PassThroughEntity : JumpthruPlatform
{
    Player player;

    public PassThroughEntity(EntityData data, Vector2 offset) : base(data, offset)
    {
        Depth = 1000000;
        Visible = false;
    }

    public override void Update()
    {
        base.Update();
        if (TryGetPlayer(out player) && (player.Ducking || Input.Aim.Value.Y > 0.3f) && HasPlayerRider() && Input.Jump.Pressed)
        {
            Input.Jump.ConsumePress();
            player.IgnoreJumpThrus = true;
            Add(new Coroutine(FallThru(), true));
        }
    }

    private IEnumerator FallThru()
    {
        if (player == null || player.Dead) yield break;
        player.IgnoreJumpThrus = true;
        player.Speed = new Vector2(0f, 60f);
        yield return 0.1f;
        player.IgnoreJumpThrus = false;
    }
    
    static bool TryGetPlayer(out Player p)
    {
        p = Engine.Scene?.Tracker?.GetEntity<Player>();
        return p != null;
    }
}