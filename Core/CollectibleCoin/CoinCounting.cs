using System;
using System.Collections.Generic;
using System.Linq;
using Celeste;
using CorreWithCare.Utils;

namespace CorreWithCare.Core.CollectibleCoin;

/// <summary>
/// 金币计数：负责把收集到的金币面值累加进存档，并把结果镜像到关卡的会话计数器。
/// 存档中的 CoinCounts 是唯一数据源，会话计数器仅由它单向刷新。
/// </summary>
public static class CoinCounting
{
    /// <summary>
    /// 结算一次金币收集：先按地图集与标签累加存档计数，再刷新该地图集的会话计数器，
    /// 并通知显示层弹出对应的计数器。
    /// </summary>
    public static void Collect(Player player, string tag, int value, vec2 position)
    {
        Level level = player?.SceneAs<Level>();
        if (level is null || string.IsNullOrEmpty(tag))
            return;

        string levelSet = level.GetCurrentLevelSet();
        if (string.IsNullOrEmpty(levelSet))
            return;

        // 收集前的数值作为计数条的起始显示值，让数字从旧值滚动到新值
        int previousCount = CollectibleCoinUtils.GetCoinCount(levelSet, tag);

        CollectibleCoinUtils.CollectCoin(level, levelSet, tag, value);
        CoinDisplay.NotifyCoinChanged(level, levelSet, tag, previousCount);
    }

    /// <summary>
    /// 关卡加载完毕后把当前地图集已知的金币计数镜像到会话计数器，
    /// 使中途进入或重载关卡时也能显示正确数值。
    /// </summary>
    public static void RefreshOnLevelLoaded(Level level)
    {
        if (level?.Session is null)
            return;

        string levelSet = level.GetCurrentLevelSet();
        if (string.IsNullOrEmpty(levelSet))
            return;

        CollectibleCoinUtils.MirrorToSession(level, levelSet);
    }
}
