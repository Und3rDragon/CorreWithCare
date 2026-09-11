using Celeste.Mod.Entities;
using CorreWithCare.Core;
using CorreWithCare.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CorreWithCare.Core.ExtendedAttributes;

namespace CorreWithCare.Entities.EasyProgressBar;

[WorkInProgress]
[CustomEntity("CorreWithCare/ProgressBarDefiner")]
public class ProgressBarDefiner : BaseEntity
{
    public ProgressBarDefiner(EntityData data, vec2 offset) : base(data, offset)
    {
        bool DefineAsCounter = data.Bool("isCounter", true);

        if (DefineAsCounter)
        {
            CWCSession.ProgressBarSettings.Counter setting = new()
            {
                Name = data.Attr("sessionName", "default"),
                FromTop = data.Bool("fromTop", true),
                MoveDuration = data.Float("moveDuration", 1f).Abs(),
                TargetOffsetY = data.Float("targetOffsetY", cons.ScreenHeight * 0.1f),
                Border = (data.Int("sessionBorder1",0),
                    data.Int("sessionBorder2",100)),
                SizeX = data.Float("sizeX", cons.ScreenWidth * 0.8f),
                FontScale = data.Vector2("fontSizeX", "fontSizeY", vec2.One).Abs(),
                BarThickness = data.Float("barSize", 10f).ClampMin(1f),
                FontColor = data.GetCorreColor("fontColor", ccolor.White),
                BarColor = data.GetCorreColor("barColor", ccolor.White),
                GapSize = data.Vector2("gapX", "gapY", vec2.One * 10f).Abs(),
                ShadowShift = data.Vector2("shadowShiftX", "shadowShiftY", vec2.UnitY * 10f)
            };

            int n = md.Session.ProgressBars.FindIndex(s => 
                s.Name == setting.Name &&
                s is CWCSession.ProgressBarSettings.Counter);
            if (n == -1)
            {
                CWCModule.Session.ProgressBars.Add(setting);
            }
            else
            {
                CWCModule.Session.ProgressBars[n] = setting;
            }
        }
        else
        {
            CWCSession.ProgressBarSettings.Slider setting = new()
            {
                Name = data.Attr("sessionName", "default"),
                FromTop = data.Bool("fromTop", true),
                MoveDuration = data.Float("moveDuration", 1f).Abs(),
                TargetOffsetY = data.Float("targetOffsetY", cons.ScreenHeight * 0.1f),
                Border = (data.Float("sessionBorder1", 0),
                    data.Float("sessionBorder2", 100)),
                SizeX = data.Float("sizeX", cons.ScreenWidth * 0.8f),
                FontScale = data.Vector2("fontSizeX", "fontSizeY", vec2.One).Abs(),
                BarThickness = data.Float("barSize", 10f).ClampMin(1f),
                FontColor = data.GetCorreColor("fontColor", ccolor.White),
                BarColor = data.GetCorreColor("barColor", ccolor.White),
                GapSize = data.Vector2("gapX", "gapY", vec2.One * 10f).Abs(),
                ShadowShift = data.Vector2("shadowShiftX", "shadowShiftY", vec2.UnitY * 10f)
            };

            int n = md.Session.ProgressBars.FindIndex(s =>
                s.Name == setting.Name &&
                s is CWCSession.ProgressBarSettings.Slider);
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
}
