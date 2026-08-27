using Celeste;
using Monocle;

namespace CorreWithCare.Utils;

public static class SessionUtils
{
    public static bool GetFlag(this string name)
    {
        if (Engine.Scene is Level level)
        {
            return level?.Session?.GetFlag(name) ?? false;
        }
        return false;
    }

    public static float GetSlider(this string name)
    {
        if (Engine.Scene is Level level)
        {
            return level?.Session?.GetSlider(name) ?? 0f;
        }
        return 0f;
    }

    public static int GetCounter(this string name)
    {
        if (Engine.Scene is Level level)
        {
            return level?.Session?.GetCounter(name) ?? 0;
        }
        return 0;
    }

    public static void SetFlag(this string name, bool value = true,
        bool global = false, bool perDeath = false, bool perRoom = false)
    {
        (Engine.Scene as Level)?.Session?.SetFlag(name, value);
        
        if (perDeath)
        {
            if (value)
            {
                CWCModule.Session.flagsPerDeath.Add(name);
            }
            else
            {
                CWCModule.Session.flagsPerDeath.Remove(name);
            }
        }

        if (perRoom)
        {
            if (value)
            {
                CWCModule.Session.flagsPerRoom.Add(name);
            }
            else
            {
                CWCModule.Session.flagsPerRoom.Remove(name);
            }
        }

        if (global && !perDeath && !perRoom)
        {
            if (value)
            {
                CWCModule.SaveData.flags.Add(name);
            }
            else
            {
                CWCModule.SaveData.flags.Remove(name);
            }
        }
    }
    
    public static void SetCounter(this string name, int value = 0,
        bool global = false, bool perDeath = false, bool perRoom = false,
        int reset = 0)
    {
        (Engine.Scene as Level)?.Session?.SetCounter(name, value);
        
        if (perDeath)
        {
            CWCModule.Session.countersPerDeath[name] = reset;
        }

        if (perRoom)
        {
            CWCModule.Session.countersPerRoom[name] = reset;
        }

        if (global && !perDeath && !perRoom)
        {
            CWCModule.SaveData.counters[name] = reset;
        }
    }
    
    public static void SetSlider(this string name, float value = 0,
        bool global = false, bool perDeath = false, bool perRoom = false,
        float reset = 0)
    {
        (Engine.Scene as Level)?.Session?.SetSlider(name, value);
        
        if (perDeath)
        {
            CWCModule.Session.slidersPerDeath[name] = reset;
        }

        if (perRoom)
        {
            CWCModule.Session.slidersPerRoom[name] = reset;
        }

        if (global && !perDeath && !perRoom)
        {
            CWCModule.SaveData.sliders[name] = reset;
        }
    }
}
