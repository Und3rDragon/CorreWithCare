using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorreWithCare.Core.ProgressBar;

/// <summary>
/// 进度条的运行时状态：记录当前出入场位置、显示状态与动画进度。
/// </summary>
public class ProgressBarData
{
    public string Name = "default";
    public bool Counter = false;

    /// <summary>
    /// 进度条当前的位置偏移。水平进度条为 Y，垂直进度条为 X。
    /// </summary>
    public float Offset = 0f;

    public int State = ProgressBarStates.Hidden;
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
