using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CorreWithCare.Core.ExtendedAttributes;

namespace CorreWithCare.Entities.EasyProgressBar;

/// <summary>
/// 关卡加载时为会话中登记过的每个进度条创建一个显示实体。
/// </summary>
public static class ProgressBarHook
{
    [Load]
    public static void Load()
    {
        //On.Celeste.Level.LoadLevel += OnLevelLoad;
        On.Celeste.Level.Begin += OnLevelBegin;
        On.Celeste.Level.End += OnLevelEnd;
    }
    [Unload]
    public static void Unload()
    {
        //On.Celeste.Level.LoadLevel -= OnLevelLoad;
        On.Celeste.Level.Begin -= OnLevelBegin;
        On.Celeste.Level.End -= OnLevelEnd;
    }

    [Note("Unused")]
    public static void OnLevelLoad(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes intro, bool loader)
    {
        orig(self, intro, loader);

        foreach (var i in md.Session.ProgressBars)
        {
            ProgressBar bar = new ProgressBar(i);
            self.Add(bar);
        }
    }

    public static void OnLevelBegin(On.Celeste.Level.orig_Begin orig, Level self)
    {
        orig(self);

        if(md.Session == null) { return; }

        foreach (var i in md.Session.ProgressBars)
        {
            ProgressBar bar = new ProgressBar(i);
            self.Add(bar);
        }
    }

    public static void OnLevelEnd(On.Celeste.Level.orig_End orig, Level self)
    {
        if (md.Session == null)
        {
            orig(self);
            return;
        }

        var bars = self.Tracker.GetEntities<ProgressBar>();

        foreach(var e in bars)
        {
            e.RemoveSelf();
        }

        orig(self);
    }
}
