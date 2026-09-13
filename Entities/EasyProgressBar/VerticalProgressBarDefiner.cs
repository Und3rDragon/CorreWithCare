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

[WorkInProgress]
[CustomEntity("CorreWithCare/VerticalProgressBarDefiner")]
public class VerticalProgressBarDefiner : BaseEntity
{
    public VerticalProgressBarDefiner(EntityData data, vec2 offset) : base(data, offset)
    {
        VerticalProgressBarSettings setting = new()
        {
            Name = data.Attr("sessionName", "default"),
            FromLeft = data.Bool("fromLeft", true),
            MoveDuration = data.Float("moveDuration", 1f).Abs(),
            TargetOffsetX = data.Float("targetOffsetX", 40),
            Border = (data.Float("sessionBorder1", 0),
                    data.Float("sessionBorder2", 100)),
            SizeY = data.Float("sizeY", cons.ScreenHeight * 0.8f),
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

        int n = md.Session.VerticalProgressBars.FindIndex(s =>
            s.Name == setting.Name &&
            s is VerticalProgressBarSettings);
        if (n == -1)
        {
            CWCModule.Session.VerticalProgressBars.Add(setting);
        }
        else
        {
            CWCModule.Session.VerticalProgressBars[n] = setting;
        }
    }
}
