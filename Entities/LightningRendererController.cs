using System;
using System.Collections.Generic;
using Celeste.Mod.Entities;
using CorreWithCare.Core;
using CorreWithCare.Core.Lightnings;
using CorreWithCare.Features;

namespace CorreWithCare.Entities;

/// <summary>
/// 闪电外观控制器：改写原版 Lightning 的外观参数。
///
/// mode   — 作用范围：0 整张地图，1 当前房间，2 放置点落在其范围内的闪电，3 targetIDs 指定的闪电
/// targetIDs — 逗号分隔的闪电实体 ID，仅 mode 为 3 时生效
///
/// 同一块闪电被多个控制器覆盖时，mode 数值大的优先；mode 相同时实体 ID 大的优先。
/// 受渲染器整体限制的参数（抖动间隔、整屏白闪、Glitch、Bloom、底色、环境音、深度）
/// 在整张地图上取优先级最高的控制器。
/// </summary>
[CustomEntity("CorreWithCare/LightningRendererController")]
public class LightningRendererController : BaseEntity
{
    private readonly int mode;
    private readonly int priority;
    private readonly List<int> targetIds;
    private readonly LightningRenderConfig config;

    private string key;
    private bool registered;

    public LightningRendererController(EntityData data, vec2 offset)
        : base(data, offset)
    {
        mode = Math.Clamp(data.Int("mode"), LightningControllerRegistry.ModeOnMap, LightningControllerRegistry.ModeTargetIds);
        priority = data.ID;
        targetIds = ParseIds(data.Attr("targetIDs"));
        config = LightningRenderConfig.FromData(data);
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);

        if (scene is not Level level)
            return;

        key = $"{level.Session.Level}#{priority}";

        LightningControllerRegistry.EnsureLevel(level);
        LightningControllerRegistry.Register(key, mode, priority, Position,
            mode == LightningControllerRegistry.ModeTargetIds ? LightningControllerRegistry.GetTargetPositions(level, targetIds) : null,
            config);

        registered = true;
        LightningRendererHooks.ApplyGlobal(level);
    }

    public override void Removed(Scene scene)
    {
        base.Removed(scene);

        // 整张地图的配置在离开房间后依然生效，由换关卡时的清理统一回收
        if (!registered || mode == LightningControllerRegistry.ModeOnMap)
            return;

        LightningControllerRegistry.Unregister(key);
        registered = false;
    }

    private static List<int> ParseIds(string raw)
    {
        List<int> ids = new();

        if (string.IsNullOrWhiteSpace(raw))
            return ids;

        foreach (string part in raw.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (int.TryParse(part.Trim(), out int id))
                ids.Add(id);
        }

        return ids;
    }
}
