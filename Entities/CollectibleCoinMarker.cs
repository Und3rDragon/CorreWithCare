using System;
using System.Collections.Generic;
using System.Linq;
using Celeste;
using Celeste.Mod.Entities;
using CorreWithCare.Core;
using CorreWithCare.Core.CollectibleCoin;
using CorreWithCare.Utils;
using Microsoft.Xna.Framework;
using static CorreWithCare.Core.ExtendedAttributes;

namespace CorreWithCare.Entities;

/// <summary>
/// 金币计数器图标定义：为某个金币标签指定计数器上显示的图标（GFX.Gui 路径）。
///
/// tag  — 金币标签，需与金币实体上的标签一致
/// icon — 计数器图标路径，留空时该标签使用默认图标
///
/// 通常放置在关卡起始房间。同一房间内存在多个相同标签的定义时，
/// 以实体 ID 最大者为准；进入新房间时新房间的定义覆盖之前的设置。
/// </summary>
[Tracked]
[CustomEntity("CorreWithCare/CollectibleCoinMarker")]
[WorkInProgress]
public class CollectibleCoinMarker : BaseEntity
{
    private readonly string coinTag;
    private readonly string icon;
    private readonly int id;

    public CollectibleCoinMarker(EntityData data, vec2 offset)
        : base(data, offset)
    {
        coinTag = data.Attr("tag", "");
        icon = data.Attr("icon", "");
        id = data.ID;
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);

        if (string.IsNullOrWhiteSpace(coinTag))
        {
            Log.Warn($"[CollectibleCoinMarker] 图标定义缺少标签，位置 {Position}");
            return;
        }

        Level level = scene as Level;
        string levelSet = level.GetCurrentLevelSet();
        if (string.IsNullOrEmpty(levelSet))
            return;

        // 同一房间内同标签的定义以 ID 最大者为准
        foreach (CollectibleCoinMarker other in scene.Tracker.GetEntities<CollectibleCoinMarker>())
        {
            if (other == this || other.coinTag != coinTag)
                continue;

            if (other.id > id)
                return;
        }

        CollectibleCoinUtils.SetCoinIcon(levelSet, coinTag,
            string.IsNullOrWhiteSpace(icon) ? CollectibleCoinUtils.DefaultIcon : icon);
    }
}
