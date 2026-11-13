using System;
using System.Collections.Generic;
using CorreWithCare.Utils;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using static CorreWithCare.Core.ExtendedAttributes;

namespace CorreWithCare.Features;

/// <summary>
/// 地图图标尺寸：读取地图 meta 的 UnlockIconSize，为 true 时章节选择界面的图标保持贴图原始尺寸。
/// 没有声明该字段的地图仍然按原版规则缩放图标宽度。
/// </summary>
public static class MapIconScaleHooks
{
    /// <summary>
    /// 各地图是否解锁图标尺寸的缓存，避免每帧重新读取 meta。
    /// </summary>
    private static readonly Dictionary<int, bool> UnlockIconSizeCache = new();

    [Load]
    public static void Load()
    {
        IL.Celeste.OuiChapterSelectIcon.Render += ResolveUnlockIconSize;
    }

    [Unload]
    public static void Unload()
    {
        IL.Celeste.OuiChapterSelectIcon.Render -= ResolveUnlockIconSize;

        UnlockIconSizeCache.Clear();
    }

    /// <summary>
    /// 把图标缩放后的宽度倍率替换为按地图设置取值。
    /// </summary>
    private static void ResolveUnlockIconSize(ILContext il)
    {
        ILCursor cursor = new(il);

        if (!cursor.TryGotoNext(ins => ins.MatchLdcR4(44f))
            || !cursor.TryGotoNext(MoveType.After, ins => ins.OpCode == OpCodes.Div))
        {
            Log.Warn("MapIconScale: 图标缩放的注入点未找到，UnlockIconSize 不会生效。");
            return;
        }

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Func<float, OuiChapterSelectIcon, float>>(GetIconScale);
    }

    /// <summary>
    /// 按地图设置给出图标宽度倍率。
    /// </summary>
    private static float GetIconScale(float scaleFactor, OuiChapterSelectIcon icon)
    {
        if (icon != null && ShouldUnlockIconSize(icon.Area))
            return 1f;

        return scaleFactor;
    }

    /// <summary>
    /// 读取地图 meta 的 UnlockIconSize，未声明时按 false 处理。
    /// </summary>
    private static bool ShouldUnlockIconSize(int areaId)
    {
        if (UnlockIconSizeCache.TryGetValue(areaId, out bool cached))
            return cached;

        bool result = ReadUnlockIconSize(areaId);
        UnlockIconSizeCache[areaId] = result;
        return result;
    }

    private static bool ReadUnlockIconSize(int areaId)
    {
        AreaData area = AreaData.Get(areaId);

        if (area == null || string.IsNullOrEmpty(area.SID))
            return false;

        if (!Everest.Content.TryGet($"Maps/{area.SID}.meta", out ModAsset asset))
            return false;

        if (asset == null || !(asset.PathVirtual?.StartsWith("Maps") ?? false))
            return false;

        if (!asset.TryDeserialize(out CWCMapMeta meta))
            return false;

        return meta != null && meta.UnlockIconSize;
    }
}

/// <summary>
/// 地图 meta 中本模组读取的字段。
/// </summary>
public class CWCMapMeta
{
    /// <summary>
    /// 为 true 时章节选择界面的地图图标不再被缩放，保持贴图原始尺寸。默认为 false。
    /// </summary>
    public bool UnlockIconSize { get; set; } = false;
}
