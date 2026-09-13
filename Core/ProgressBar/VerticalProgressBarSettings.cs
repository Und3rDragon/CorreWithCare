using CorreWithCare.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorreWithCare.Core.ProgressBar;

public class VerticalProgressBarSettings
{
    public string Name = "default";
    public bool FromLeft = true;
    public float SizeY = cons.ScreenHeight * 0.8f;
    public float TargetOffsetX = 40;
    public float MoveDuration = 1f;
    public vec2 FontScale = vec2.One;
    public float BarThickness = 10f;
    public ccolor FontColor = ccolor.White;
    public ccolor BarColor = ccolor.White;
    /// <summary>
    /// value X is the gap between screen border and the main graphic
    /// value Y is the gap between the number and the bar lines
    /// </summary>
    public vec2 GapSize = vec2.One * 10f;
    /// <summary>
    /// where the real pattern will be drawn above the shadows
    /// </summary>
    public vec2 ShadowShift = vec2.UnitY * 8f;
    public string TitleName = string.Empty;
    public vec2 TitleScale = vec2.One;
    public ccolor TitleColor = ccolor.White;
    public (float, float) Border = (0, 100);
    public bool IsCounter = true;
    public string Flag = string.Empty;
    public bool TitleOnBottom = true;
    public override bool Equals(object obj)
    {
        return obj is VerticalProgressBarSettings set && set.Name == Name && set.IsCounter == IsCounter;
    }
    public override int GetHashCode()
    {
        return HashCode.Combine(Name, IsCounter);
    }
    public float GetLerp(float value) => value.GetLerp(Border.Item1, Border.Item2);
}
