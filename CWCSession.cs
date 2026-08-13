using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorreWithCare;

public class CWCSession : EverestModuleSession
{
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
}
