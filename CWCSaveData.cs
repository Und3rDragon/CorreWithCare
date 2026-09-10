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

    /// <summary>
    /// 各地图集下按标签统计的金币数量：CoinCounts[地图集][标签] = 数量。
    /// 是金币计数的唯一权威来源，会话内的计数器由它单向镜像而来。
    /// </summary>
    public Dictionary<string, Dictionary<string, int>> CoinCounts = new();

    /// <summary>
    /// 各地图集下金币标签对应的计数器图标（GFX.Gui 路径）：
    /// CorreCoinTexture[地图集][标签] = 贴图路径。
    /// </summary>
    public Dictionary<string, Dictionary<string, string>> CorreCoinTexture = new();
}
