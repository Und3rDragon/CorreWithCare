using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorreWithCare.Core.ProgressBar;

public struct ProgressBarStates
{
    public const int Hidden = 0;
    public const int Appearing = 1;
    public const int Display = 2;
    public const int Disappearing = 3;
}
