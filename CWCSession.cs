using CorreWithCare.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CorreWithCare.Core.ProgressBar;

namespace CorreWithCare;

public class CWCSession : EverestModuleSession
{
    public List<ProgressBarSettings> ProgressBars = new();
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
