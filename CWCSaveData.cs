using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorreWithCare;

public class CWCSaveData : EverestModuleSaveData
{
    public HashSet<string> flags = new();
    public Dictionary<string, int> counters = new();
    public Dictionary<string, float> sliders = new();
}
