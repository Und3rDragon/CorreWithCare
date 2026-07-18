using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorreWithCare;

public class CorreWithCareSession : EverestModuleSession
{
    public HashSet<string> flagsPerRoom;
    public HashSet<string> flagsPerDeath;
    /// <summary>
    /// Counters and its reset value
    /// </summary>
    public Dictionary<string, int> countersPerRoom;
    /// <summary>
    /// Counters and its reset value
    /// </summary>
    public Dictionary<string, int> countersPerDeath;
    /// <summary>
    /// Sliders and its reset value
    /// </summary>
    public Dictionary<string, float> slidersPerRoom;
    /// <summary>
    /// Sliders and its reset value
    /// </summary>
    public Dictionary<string, float> slidersPerDeath;
}
