using System;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod;
using CorreWithCare.Utils;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using static CorreWithCare.Core.ExtendedAttributes;

namespace CorreWithCare.Core.CollectibleCoin;

/// <summary>
/// 金币计数器：把草莓计数器的图标替换为标签对应的金币图标。
/// 图标按地图集与标签从存档中读取，未定义时使用默认金币图标。
/// </summary>
public class CoinSessionCounter : StrawberriesCounter
{
    /// <summary>
    /// 本计数器显示的金币标签，用于查找对应图标。
    /// </summary>
    public string CoinTag = "";

    public CoinSessionCounter(string coinTag, bool centeredX, int amount)
        : base(centeredX, amount, 0, false)
    {
        CoinTag = coinTag ?? "";
    }

    [Load]
    public static void Load()
    {
        IL.Celeste.StrawberriesCounter.Render += ReplaceCounterIcon;
    }

    [Unload]
    public static void Unload()
    {
        IL.Celeste.StrawberriesCounter.Render -= ReplaceCounterIcon;
    }

    /// <summary>
    /// 原版计数器渲染时使用的草莓图标路径。
    /// </summary>
    private const string VanillaCounterIcon = "collectables/strawberry";

    private static void ReplaceCounterIcon(ILContext il)
    {
        ILCursor cursor = new(il);

        if (!cursor.TryGotoNext(ins => ins.MatchLdstr(VanillaCounterIcon)))
            return;

        cursor.Index++;
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Func<string, StrawberriesCounter, string>>(ResolveCounterIcon);
    }

    /// <summary>
    /// 把计数器渲染用的草莓图标路径替换为标签对应的金币图标。
    /// </summary>
    private static string ResolveCounterIcon(string original, StrawberriesCounter counter)
    {
        if (counter is not CoinSessionCounter coinCounter)
            return original;

        string levelSet = CollectibleCoinUtils.GetCurrentLevelSet(Engine.Scene as Level);
        return CollectibleCoinUtils.GetCoinIcon(levelSet, coinCounter.CoinTag);
    }
}
