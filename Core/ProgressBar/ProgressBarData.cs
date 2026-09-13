using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorreWithCare.Core.ProgressBar;

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
