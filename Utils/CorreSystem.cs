using System.Collections;
using System.Runtime.InteropServices.Marshalling;
using CorreWithCare.Core;
using static CorreWithCare.Core.ExtendedAttributes;

namespace CorreWithCare.Utils;

public class CorreSystem
{
    [Load]
    public static void Onload()
    {
        On.Celeste.Level.Reload += OnLevelReload;
        On.Celeste.Level.LoadLevel += OnLoadLevel;
        On.Monocle.Scene.Update += GlobalUpdate;
        On.Celeste.Level.TransitionRoutine += OnLevelTransition;
        On.Celeste.Player.Die += OnPlayerDeath;
        On.Celeste.Level.Begin += OnLevelBegin;
        On.Celeste.Level.End += OnLevelEnd;
        On.Celeste.Level.Update += OnLevelUpdate;
    }

    [Unload]
    public static void Unload()
    {
        On.Celeste.Level.Reload -= OnLevelReload;
        On.Celeste.Level.LoadLevel -= OnLoadLevel;
        On.Monocle.Scene.Update -= GlobalUpdate;
        On.Celeste.Level.TransitionRoutine -= OnLevelTransition;
        On.Celeste.Player.Die -= OnPlayerDeath;
        On.Celeste.Level.Begin -= OnLevelBegin;
        On.Celeste.Level.End -= OnLevelEnd;
        On.Celeste.Level.Update -= OnLevelUpdate;
    }

    public static IEnumerator OnLevelTransition(On.Celeste.Level.orig_TransitionRoutine orig, 
        Level self, LevelData levelData, Vector2 dir)
    {
        ResetPerRoom(self);
        ApplyGlobals(self);

        yield return new SwapImmediately(orig(self, levelData, dir)); //On transition
    }

    public static void OnLevelReload(On.Celeste.Level.orig_Reload orig, Level self)
    {
        orig(self); // Once per reload, not on first enter, not on F5
        // After LoadLevel

    }

    public static void OnLoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes intro, bool fromLoader)
    {
        orig(self, intro, fromLoader); // Once per level load, also reload

        ResetPerRoom(self);
        ApplyGlobals(self);
    }

    public static void GlobalUpdate(On.Monocle.Scene.orig_Update orig, Scene self)
    {
        orig(self);
    }

    public static void OnLevelUpdate(On.Celeste.Level.orig_Update orig, Level self)
    {
        orig(self);

        if (self.Session is null) { return; }

    }
    
    public static PlayerDeadBody OnPlayerDeath(On.Celeste.Player.orig_Die orig, 
        Player self, Vector2 dir, bool eii, bool reg)
    {
        ResetPerDeath(self.SceneAs<Level>());

        return orig(self, dir, eii, reg);
    }

    public static void OnLevelBegin(On.Celeste.Level.orig_Begin orig, Level self)
    {
        orig(self);
    }

    public static void OnLevelEnd(On.Celeste.Level.orig_End orig, Level self)
    {

        orig(self);
    }

    public static void ResetPerRoom(Level self)
    {
        foreach (var item in CWCModule.Session.flagsPerRoom)
        {
            self.Session.SetFlag(item, false);
        }

        CWCModule.Session.flagsPerRoom.Clear();

        foreach (var item in CWCModule.Session.countersPerRoom)
        {
            self.Session.SetCounter(item.Key, item.Value);
        }

        CWCModule.Session.countersPerRoom.Clear();

        foreach (var item in CWCModule.Session.slidersPerRoom)
        {
            self.Session.SetSlider(item.Key, item.Value);
        }

        CWCModule.Session.slidersPerRoom.Clear();
    }

    public static void ResetPerDeath(Level self)
    {
        foreach (var item in CWCModule.Session.flagsPerDeath)
        {
            self.Session.SetFlag(item, false);
        }

        CWCModule.Session.flagsPerDeath.Clear();

        foreach (var item in CWCModule.Session.countersPerDeath)
        {
            self.Session.SetCounter(item.Key, item.Value);
        }

        CWCModule.Session.countersPerDeath.Clear();

        foreach (var item in CWCModule.Session.slidersPerDeath)
        {
            self.Session.SetSlider(item.Key, item.Value);
        }

        CWCModule.Session.slidersPerDeath.Clear();
    }

    public static void ApplyGlobals(Level self)
    {
        foreach (var item in CWCModule.SaveData.flags)
        {
            self.Session.SetFlag(item, true);
        }

        foreach (var item in CWCModule.SaveData.counters)
        {
            self.Session.SetCounter(item.Key, item.Value);
        }

        foreach (var item in CWCModule.SaveData.sliders)
        {
            self.Session.SetSlider(item.Key, item.Value);
        }
    }
}
