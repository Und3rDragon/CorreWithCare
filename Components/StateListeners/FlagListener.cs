using CorreWithCare.Core;
using CorreWithCare.Utils;

namespace CorreWithCare.Components.StateListeners;

public class FlagListener : StateListener
{
    public FlagListener(string flag, bool fallback = true)
    {
        this.Flag = flag;
        this.Fallback = fallback;
    }
    public string Flag;
    public bool Fallback = true;

    protected override bool GetState()
    {
        if (!Flag.HasValidContent())
        {
            return Fallback;
        }

        return Flag.GetFlag();
    }
}

public class FlagsListener : StateListener
{
    public FlagsListener(string flags, string separator = ",", string invertChar = "!",
        bool fallback = true)
    {
        this.flags = flags;
        this.separator = separator;
        this.invertChar = invertChar;
        this.Fallback = fallback;
    }
    public string flags, separator, invertChar;
    public bool Fallback = true;

    protected override bool GetState()
    {
        if (!flags.HasValidContent())
        {
            return Fallback;
        }

        Dictionary<string, bool> check = new();
        foreach(var i in flags.Split(separator, StringSplitOptions.TrimEntries))
        {
            if (i.StartsWith(invertChar))
            {
                check.Add(i.TrimStart(invertChar).ToString(), true);
            }
            else
            {
                check.Add(i, false);
            }
        }
        return check.All(f => f.Key.GetFlag() != f.Value);
    }
}
