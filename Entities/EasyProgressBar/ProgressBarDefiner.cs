using Celeste.Mod.Entities;
using CorreWithCare.Core;
using CorreWithCare.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CorreWithCare.Core.ProgressBar;
using static CorreWithCare.Core.ExtendedAttributes;

namespace CorreWithCare.Entities.EasyProgressBar;

/// <summary>
/// 进度条定义：声明一个进度条的样式，并把它登记到当前会话中。
/// 同一名称与计数器类型的定义会覆盖之前的设置。
/// </summary>
[WorkInProgress]
[CustomEntity("CorreWithCare/ProgressBarDefiner")]
public class ProgressBarDefiner : BaseEntity
{
    public ProgressBarDefiner(EntityData data, vec2 offset) : base(data, offset)
    {
        int direction = data.Int("direction", BarDirection.FromTop);
        bool vertical = BarDirection.IsVertical(direction);

        ProgressBarSettings setting = new()
        {
            Name = data.Attr("sessionName", "default"),
            Direction = direction,
            MoveDuration = data.Float("moveDuration", 1f).Abs(),
            TargetOffset = data.Float("targetOffset", vertical ? 192f : cons.ScreenHeight * 0.1f),
            Border = (data.Float("sessionBorder1", 0),
                    data.Float("sessionBorder2", 100)),
            Size = data.Float("size", vertical ? cons.ScreenHeight * 0.8f : cons.ScreenWidth * 0.8f),
            FontScale = data.Vector2("fontSizeX", "fontSizeY", vec2.One).Abs(),
            BarThickness = data.Float("barSize", 10f).ClampMin(1f),
            FontColor = data.GetCorreColor("fontColor", ccolor.White),
            BarColor = data.GetCorreColor("barColor", ccolor.White),
            GapSize = data.Vector2("gapX", "gapY", vec2.One * 10f).Abs(),
            ShadowShift = data.Vector2("shadowShiftX", "shadowShiftY", vec2.UnitY * 8f),
            TitleName = data.Attr("titleName"),
            IsCounter = data.Bool("isCounter", true),
            TitleScale = data.Vector2("titleScaleX", "titleScaleY", vec2.One),
            TitleColor = data.GetCorreColor("titleColor", ccolor.White),
            Flag = data.Attr("flag"),
            TitleOnBottom = data.Bool("titleOnBottom", true)
        };

        int n = md.Session.ProgressBars.FindIndex(s =>
            s.Name == setting.Name &&
            s is ProgressBarSettings);
        if (n == -1)
        {
            CWCModule.Session.ProgressBars.Add(setting);
        }
        else
        {
            CWCModule.Session.ProgressBars[n] = setting;
        }
    }
}
