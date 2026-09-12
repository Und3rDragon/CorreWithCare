using CorreWithCare.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CorreWithCare;

public class CWCSession : EverestModuleSession
{
    public class ProgressBarSettings
    {
        public string Name = "default";
        public bool FromTop = true;
        public float SizeX = cons.ScreenWidth * 0.8f;
        public float TargetOffsetY = cons.ScreenHeight * 0.1f;
        public float MoveDuration = 1f;
        public vec2 FontScale = vec2.One;
        public float BarThickness = 10f;
        public ccolor FontColor = ccolor.White;
        public ccolor BarColor = ccolor.White;
        /// <summary>
        /// value Y is the gap between screen border and the main graphic
        /// value X is the gap between the number and the bar lines
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
        public override bool Equals(object obj)
        {
            return obj is ProgressBarSettings set && set.Name == Name && set.IsCounter == IsCounter;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(Name, IsCounter);
        }
        public float GetLerp(float value)
        {
            float min = RenderUtils.Min(Border.Item1, Border.Item2);
            float max = RenderUtils.Max(Border.Item1, Border.Item2);

            if (min == max || value > max) { return 1f; }
            if (value < min) { return 0f; }

            return (value - min) / (max - min);
        }
    }
    public List<ProgressBarSettings> ProgressBars = new();
    public struct ProgressBarStates
    {
        public const int Hidden = 0;
        public const int Appearing = 1;
        public const int Display = 2;
        public const int Disappearing = 3;
    }
    public class ProgressBarData
    {
        public string Name = "default";
        public bool Counter = false;
        public float Y = 0f;
        public int State = 0;
        public float MovementTimer = -1f;

        public override bool Equals(object obj)
        {
            return obj is ProgressBarData data &&
                   Name == data.Name &&
                   Counter == data.Counter;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Counter);
        }
    }
    public List<ProgressBarData> ActiveProgressBars = new();

    #region session values
    public HashSet<string> flagsPerRoom = new();
    public HashSet<string> flagsPerDeath = new();
    /// <summary>
    /// Counters and its reset value
    /// </summary>
    public Dictionary<string, int> countersPerRoom = new();
    /// <summary>
    /// Counters and its reset value
    /// </summary>
    public Dictionary<string, int> countersPerDeath = new();
    /// <summary>
    /// Sliders and its reset value
    /// </summary>
    public Dictionary<string, float> slidersPerRoom = new();
    /// <summary>
    /// Sliders and its reset value
    /// </summary>
    public Dictionary<string, float> slidersPerDeath = new();
    #endregion
}
