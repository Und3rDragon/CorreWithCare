using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorreWithCare;

public class CWCSettings : EverestModuleSettings
{
    /// <summary>
    /// 金币计数器同时显示的最大条数，超出的标签排队等待空位。
    /// </summary>
    public int CorreCoinMaxSlots { get; set; } = 10;

    /// <summary>
    /// 金币计数器最上一条的屏幕 Y 坐标，其余各条按行高向下排列。
    /// 默认值沿用原实现：装有 DeathTracker 时下移以避免与其 HUD 重叠。
    /// </summary>
    public int CorreCoinDisplayerY { get; set; } =
        CWCModule.CheckDependency("DeathTracker", "1.0.0") ? 277 : 202;
}
