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
        public vec2 FontScale = vec2.One * 2f;
        public float BarThickness = 2f;
        public ccolor FontColor = ccolor.White;
        public ccolor BarColor = ccolor.White;
        public vec2 GapSize = vec2.One * 10f;
        public class Counter : ProgressBarSettings
        {
            public (int, int) Border = (0, 100);

            public override bool Equals(object obj)
            {
                return obj is Counter counter &&
                       Name == counter.Name;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Name);
            }

            public float GetLerp(int value)
            {
                int min = RenderUtils.Min(Border.Item1, Border.Item2);
                int max = RenderUtils.Max(Border.Item1, Border.Item2);

                if(min == max || value > max) { return 1f; }
                if(value < min) { return 0f; }

                return (float)(value - min) / (max - min);
            }
        }
        public class Slider : ProgressBarSettings
        {
            public (float, float) Border = (0, 100);

            public override bool Equals(object obj)
            {
                return obj is Slider slider &&
                       Name == slider.Name;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Name);
            }

            public float GetLerp(float value)
            {
                float min = RenderUtils.Min(Border.Item1, Border.Item2);
                float max = RenderUtils.Max(Border.Item1, Border.Item2);

                if (min == max || value > max) { return 1f; }
                if(value < min) { return 0f; }

                return (value - min) / (max - min);
            }
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
