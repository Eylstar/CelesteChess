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
        //High depth to call the Update method early
        Depth = 1000000;
        Visible = false;
    }

    public override void Update()
    {
        base.Update();
        if (ChessHelpers.TryGetPlayer(out player) && (player.Ducking || Input.Aim.Value.Y > 0.3f) && HasPlayerRider() && Input.Jump.Pressed)
        {
            //If pressed when crouching or having a down input, makes the jump input disappear and makes the player fall through
            Input.Jump.ConsumePress();
            Add(new Coroutine(FallThru()));
        }
    }

    private IEnumerator FallThru()
    {
        player.IgnoreJumpThrus = true;
        player.Speed = new Vector2(0f, 60f);
        yield return 0.1f;
        player.IgnoreJumpThrus = false;
    }
}