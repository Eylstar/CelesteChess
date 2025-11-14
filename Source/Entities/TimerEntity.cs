using System;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CelesteChess;

[Tracked]
[CustomEntity("CelesteChess/TimerText")]
public class TimerEntity : Entity
{
    public bool isBlack;
    Color displayColor = Color.White;
    
    //bool isRunning = true;
    
    public TimerEntity(EntityData data, Vector2 offset) : base(data.Position + offset)
    {
        Depth = -100;
        isBlack = data.Bool("isBlack");
    }
    
    PlutoniumText timerText;
    string timeString = "";


    public override void Added(Scene scene)
    {
        base.Added(scene);
        timerText = new PlutoniumText("CelesteChess/fonts/timerFont", "0123456789:", new Vector2(7, 12));
        Add(timerText);
        Visible = true;
    }
    
    public void UpdateTimer(string time)
    {
        timeString = time;
    }
    
    public void SetDisplayColor(Color color)
    {
        displayColor = color;
    }

    public override void Render()
    {
        base.Render();
        timerText.PrintCentered(Position, timeString, true, 8, displayColor, Color.Black);
    }
}