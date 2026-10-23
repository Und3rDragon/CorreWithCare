using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace CorreWithCare.Core.Lightnings;

/// <summary>
/// 关卡级的闪电外观配置登记表。
///
/// 同一块闪电被多个配置覆盖时，mode 数值大的优先；mode 相同时实体 ID 大的优先。
/// </summary>
public static class LightningControllerRegistry
{
    public const int ModeOnMap = 0;
    public const int ModePerRoom = 1;
    public const int ModePlaced = 2;
    public const int ModeTargetIds = 3;

    private sealed class Entry
    {
        public string Key;
        public int Mode;
        public int Priority;
        public Vector2 Point;
        public HashSet<Point> TargetPositions;
        public LightningRenderConfig Config;

        public HashSet<Lightning> Targets;
        public bool Scanned;
    }

    private static readonly List<Entry> entries = new();
    private static Level currentLevel;

    public static bool HasAny => entries.Count > 0;

    /// <summary>
    /// 交由优先级最高的配置决定的渲染器级参数；没有任何配置时返回 null。
    /// </summary>
    public static LightningRenderConfig Global => entries.Count == 0 ? null : entries.Aggregate((a, b) => Higher(a, b) ? a : b).Config;

    /// <summary>
    /// 换关卡时丢弃上一关的全部配置。
    /// </summary>
    public static void EnsureLevel(Level level)
    {
        if (currentLevel == level)
            return;

        currentLevel = level;
        entries.Clear();
    }

    public static void Register(string key, int mode, int priority, Vector2 point,
        IEnumerable<Point> targetPositions, LightningRenderConfig config)
    {
        entries.RemoveAll(e => e.Key == key);

        entries.Add(new Entry
        {
            Key = key,
            Mode = mode,
            Priority = priority,
            Point = point,
            TargetPositions = targetPositions == null ? new HashSet<Point>() : new HashSet<Point>(targetPositions),
            Config = config
        });
    }

    public static void Unregister(string key)
    {
        entries.RemoveAll(e => e.Key == key);
    }

    /// <summary>
    /// 取得作用于该块闪电的配置；没有任何配置覆盖它时返回 null。
    /// </summary>
    public static LightningRenderConfig Resolve(Lightning lightning)
    {
        Entry best = null;

        foreach (Entry entry in entries)
        {
            if (!Matches(entry, lightning))
                continue;

            if (best == null || Higher(entry, best))
                best = entry;
        }

        return best?.Config;
    }

    /// <summary>
    /// 按 targetIDs 从当前房间的地图数据中取出目标位置。
    /// </summary>
    public static IEnumerable<Point> GetTargetPositions(Level level, IEnumerable<int> ids)
    {
        HashSet<int> wanted = new(ids);
        List<Point> result = new();

        if (wanted.Count == 0 || level?.Session?.LevelData == null)
            return result;

        foreach (EntityData data in level.Session.LevelData.Entities)
        {
            if (!wanted.Contains(data.ID))
                continue;

            Point raw = new((int)Math.Round(data.Position.X), (int)Math.Round(data.Position.Y));
            Point offset = new((int)Math.Round(data.Position.X + level.LevelOffset.X),
                (int)Math.Round(data.Position.Y + level.LevelOffset.Y));

            if (!result.Contains(raw))
                result.Add(raw);

            if (!result.Contains(offset))
                result.Add(offset);
        }

        return result;
    }

    private static bool Matches(Entry entry, Lightning lightning)
    {
        switch (entry.Mode)
        {
            case ModePlaced:
                return lightning.X <= entry.Point.X && entry.Point.X <= lightning.X + lightning.VisualWidth
                    && lightning.Y <= entry.Point.Y && entry.Point.Y <= lightning.Y + lightning.VisualHeight;

            case ModeTargetIds:
                if (!entry.Scanned)
                    ScanTargets(entry, lightning);

                return entry.Targets != null && entry.Targets.Contains(lightning);

            default:
                return true;
        }
    }

    private static void ScanTargets(Entry entry, Lightning lightning)
    {
        entry.Scanned = true;
        entry.Targets = new HashSet<Lightning>();

        if (entry.TargetPositions.Count == 0 || lightning.Scene is not Level level)
            return;

        foreach (Lightning candidate in level.Entities.FindAll<Lightning>())
        {
            Point position = new((int)Math.Round(candidate.X), (int)Math.Round(candidate.Y));

            if (entry.TargetPositions.Contains(position))
                entry.Targets.Add(candidate);
        }
    }

    private static bool Higher(Entry a, Entry b)
    {
        return a.Mode != b.Mode ? a.Mode > b.Mode : a.Priority > b.Priority;
    }
}
