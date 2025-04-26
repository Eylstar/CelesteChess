using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CelesteChess;

[Tracked]
[CustomEntity("CelesteChess/ChessTileTrigger")]
public class ChessTileTrigger : Trigger
{
    public int gridPosition;
    public Vector2 gridCoordinates;
    public ChessTileEntity tile;

    public ChessTileTrigger(EntityData data, Vector2 offset) : base(data, offset)
    {
        gridPosition = data.Int("position");
    }
}