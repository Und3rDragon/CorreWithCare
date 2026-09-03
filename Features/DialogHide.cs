﻿using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod.Entities;
using CorreWithCare.Utils;
using static CorreWithCare.Core.ExtendedAttributes;

namespace CorreWithCare.Features;

/// <summary>
/// 对话文本隐藏：{corre_hide [flag]} 文本 {/corre_hide}。
///   flag 可选；有 flag 且 flag 为 true 时显示，否则隐藏中间文本。
/// </summary>
public static class DialogHide
{
    /// <summary>隐藏指令名（开始标签）。</summary>
    public const string HideCmd = "hide";

    private static Func<string, List<string>, List<FancyText.Node>, bool> _prevParseHandler;

    // ==================== 节点 ====================

    /// <summary>隐藏区开始标记节点（含可选条件 flag）。</summary>
    public class CorreHideNode : FancyText.Node
    {
        /// <summary>条件 flag 名（可选；空 = 无条件隐藏）。</summary>
        public readonly string Flag = "";
        public CorreHideNode(List<string> rawParams)
        {
            if (rawParams.Count >= 1)
                Flag = rawParams[0];
        }
    }

    // ==================== 钩子生命周期 ====================

    [Load]
    public static void Load()
    {
        // 链式注册解析：不覆盖框架上已有的外部模块 handler
        _prevParseHandler = DialogCommands.CustomParseHandler;
        DialogCommands.CustomParseHandler = (cmd, vals, nodes) =>
        {
            if (cmd == HideCmd)
            {
                nodes.Add(new CorreHideNode(vals));
                return true;
            }

            if (cmd == "/" + HideCmd)
            {
                CloseHide(nodes);
                return true;
            }

            return _prevParseHandler?.Invoke(cmd, vals, nodes) ?? false;
        };
    }

    [Unload]
    public static void Unload()
    {
        DialogCommands.CustomParseHandler = _prevParseHandler;
        _prevParseHandler = null;
    }

    // ==================== 逻辑 ====================

    /// <summary>
    /// 闭合 {/corre_hide}：找到最近的开始标记，若应隐藏则移除其间的文本节点。
    /// 有 flag 且 flag 为 true → 显示（不移除）；否则移除。
    /// </summary>
    private static void CloseHide(List<FancyText.Node> nodes)
    {
        // 从末尾往前找最近的没有参与隐藏的开始标记（同一对话可能嵌套/多处）
        for (int i = nodes.Count - 1; i >= 0; i--)
        {
            if (nodes[i] is CorreHideNode hide)
            {
                bool shouldHide = string.IsNullOrEmpty(hide.Flag) || !hide.Flag.GetFlag();

                if (shouldHide)
                {
                    // 移除 hide 之后到当前的所有节点（即被隐藏的中间文本）
                    nodes.RemoveRange(i + 1, nodes.Count - (i + 1));
                }
                // 移除开始标记节点本身
                nodes.RemoveAt(i);
                return;
            }
        }
        Log.Warn($"[{DialogCommands.Prefix}_{HideCmd}] 未找到匹配的 {DialogCommands.Prefix}_hide 开始标签");
    }
}
