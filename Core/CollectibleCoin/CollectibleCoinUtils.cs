using System;
using System.Collections.Generic;
using System.Linq;

namespace CorreWithCare.Core.CollectibleCoin;

/// <summary>
/// 金币系统的数据访问工具：读写存档中的标签计数与标签图标，
/// 并把存档计数镜像到关卡的会话计数器中。
/// </summary>
public static class CollectibleCoinUtils
{
    /// <summary>
    /// 标签计数镜像到会话计数器时使用的前缀，完整键名为 Corre_Coins_{标签}。
    /// </summary>
    public const string CounterPrefix = "Corre_Coins_";

    /// <summary>
    /// 单一地图集下所有标签金币总数对应的会话计数器键名。
    /// </summary>
    public const string TotalCounterName = CounterPrefix + "Total";

    /// <summary>
    /// 标签未定义图标时使用的默认计数器图标路径。
    /// </summary>
    public const string DefaultIcon = "CorreWithCare/coin";

    /// <summary>
    /// 金币实体未指定贴图时使用的默认贴图目录。
    /// </summary>
    public const string DefaultCoinSprite = "CorreWithCare/entities/collectibleCoin/idle";

    /// <summary>
    /// 金币实体未指定音效时使用的默认收集音效。
    /// </summary>
    public const string DefaultCoinSfx = "event:/gddcoin/key_get";

    /// <summary>
    /// 取当前关卡所属的地图集名称，用于隔离各图集的金币数据。
    /// </summary>
    public static string GetCurrentLevelSet(this Level level)
        => level?.Session?.Area.GetLevelSet();

    /// <summary>
    /// 读取指定地图集下某标签的金币数量，不存在时返回 0。
    /// </summary>
    public static int GetCoinCount(string levelSet, string tag)
    {
        if (string.IsNullOrEmpty(levelSet) || string.IsNullOrEmpty(tag))
            return 0;

        if (CWCModule.SaveData.CoinCounts.TryGetValue(levelSet, out var tags)
            && tags.TryGetValue(tag, out int count))
            return count;

        return 0;
    }

    /// <summary>
    /// 写入指定地图集下某标签的金币数量。
    /// </summary>
    public static void SetCoinCount(string levelSet, string tag, int count)
    {
        if (string.IsNullOrEmpty(levelSet) || string.IsNullOrEmpty(tag))
            return;

        if (!CWCModule.SaveData.CoinCounts.TryGetValue(levelSet, out var tags))
        {
            tags = new Dictionary<string, int>();
            CWCModule.SaveData.CoinCounts[levelSet] = tags;
        }

        tags[tag] = count;
    }

    /// <summary>
    /// 在指定地图集下某标签的原数量上追加面值，并返回追加后的结果。
    /// </summary>
    public static int AddCoinCount(string levelSet, string tag, int value)
    {
        int result = GetCoinCount(levelSet, tag) + value;
        SetCoinCount(levelSet, tag, result);
        return result;
    }

    /// <summary>
    /// 求指定地图集下所有标签的金币总数，无数据时返回 0。
    /// </summary>
    public static int GetTotalCoinCount(string levelSet)
    {
        if (string.IsNullOrEmpty(levelSet))
            return 0;

        if (!CWCModule.SaveData.CoinCounts.TryGetValue(levelSet, out var tags))
            return 0;

        int total = 0;
        foreach (int count in tags.Values)
            total += count;

        return total;
    }

    /// <summary>
    /// 读取指定地图集下某标签的计数器图标路径，未定义时返回默认图标。
    /// </summary>
    public static string GetCoinIcon(string levelSet, string tag)
    {
        if (!string.IsNullOrEmpty(levelSet) && !string.IsNullOrEmpty(tag)
            && CWCModule.SaveData.CorreCoinTexture.TryGetValue(levelSet, out var tags)
            && tags.TryGetValue(tag, out string icon)
            && !string.IsNullOrWhiteSpace(icon))
            return icon;

        return DefaultIcon;
    }

    /// <summary>
    /// 写入指定地图集下某标签的计数器图标路径。
    /// </summary>
    public static void SetCoinIcon(string levelSet, string tag, string icon)
    {
        if (string.IsNullOrEmpty(levelSet) || string.IsNullOrEmpty(tag))
            return;

        if (!CWCModule.SaveData.CorreCoinTexture.TryGetValue(levelSet, out var tags))
        {
            tags = new Dictionary<string, string>();
            CWCModule.SaveData.CorreCoinTexture[levelSet] = tags;
        }

        tags[tag] = icon;
    }

    /// <summary>
    /// 把指定地图集下所有标签的金币数量镜像到当前关卡的会话计数器，
    /// 同时写入该地图集的金币总数。存档是唯一数据源，会话计数器只读不写回。
    /// </summary>
    public static void MirrorToSession(Level level, string levelSet)
    {
        if (level?.Session is null || string.IsNullOrEmpty(levelSet))
            return;

        if (CWCModule.SaveData.CoinCounts.TryGetValue(levelSet, out var tags))
        {
            foreach (var pair in tags)
                level.Session.SetCounter(CounterPrefix + pair.Key, pair.Value);
        }

        level.Session.SetCounter(TotalCounterName, GetTotalCoinCount(levelSet));
    }

    /// <summary>
    /// 收集金币后统一处理：先累加存档中的计数，再把该地图集的全部标签镜像到会话计数器。
    /// </summary>
    public static void CollectCoin(Level level, string levelSet, string tag, int value)
    {
        if (level is null || string.IsNullOrEmpty(levelSet) || string.IsNullOrEmpty(tag))
            return;

        AddCoinCount(levelSet, tag, value);
        MirrorToSession(level, levelSet);
    }
}
