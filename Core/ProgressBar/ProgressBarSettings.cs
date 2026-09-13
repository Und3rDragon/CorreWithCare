using CorreWithCare.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorreWithCare.Core.ProgressBar;

/// <summary>
/// 进度条的样式与行为设置，由地图上的进度条定义实体写入会话。
/// </summary>
public class ProgressBarSettings
{
    public string Name = "default";

    /// <summary>
    /// 出没方向，取值见 <see cref="BarDirection"/>。
    /// </summary>
    public int Direction = BarDirection.FromTop;

    /// <summary>
    /// 进度条主体的长度，水平进度条为宽度，垂直进度条为高度。
    /// </summary>
    public float Size = cons.ScreenWidth * 0.8f;

    /// <summary>
    /// 进度条完全显示时距离屏幕中轴的位置，水平进度条为 Y，垂直进度条为 X。
    /// </summary>
    public float TargetOffset = cons.ScreenHeight * 0.1f;

    public float MoveDuration = 1f;
    public vec2 FontScale = vec2.One;
    public float BarThickness = 10f;
    public ccolor FontColor = ccolor.White;
    public ccolor BarColor = ccolor.White;
    /// <summary>
    /// 水平进度条：X 为数字与线条的间距，Y 为进度条与屏幕边界的间距。
    /// 垂直进度条：X 为进度条与屏幕边界的间距，Y 为数字与线条的间距。
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

    /// <summary>
    /// 标题是否绘制在进度条远离屏幕中轴的一侧，仅垂直进度条有效。
    /// </summary>
    public bool TitleOnBottom = true;

    public override bool Equals(object obj)
    {
        return obj is ProgressBarSettings set && set.Name == Name && set.IsCounter == IsCounter;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, IsCounter);
    }

    public float GetLerp(float value) => value.GetLerp(Border.Item1, Border.Item2);
}
