using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorreWithCare;

public class CWCSaveData : EverestModuleSaveData
{
    public HashSet<string> flags;
    public Dictionary<string, int> counters;
    public Dictionary<string, float> sliders;
}
