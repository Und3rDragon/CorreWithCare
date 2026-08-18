using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod;
using CorreWithCare.Features;
using CorreWithCare.Utils;
using MonoMod.Utils;
using static CorreWithCare.Core.ExtendedAttributes;

namespace CorreWithCare.Features;

/// <summary>
/// 对话分支选择：通过 Dialog 指令 {corre_choice content target} 实现。
///
/// 在对话中插入任意数量的该指令，对话结束后统一弹出对应数量的选项：
///   {corre_choice content target}
///     content = 选项显示文本的 Dialog ID
///     target  = 选择该项后要跳转的 Dialog ID
///
/// 不插入任何 corre_choice 时，对话正常进行并结束，不受影响。
///
/// 实现基于 DialogCommands 框架 + ChoiceEntity 常驻调度实体：
///   1. 通过 CustomParseHandler 注册 corre_choice 的解析 → CorreChoice 节点
///   2. Textbox 构造时收集所有 CorreChoice 存到 DynamicData
///   3. Textbox 关闭（Removed）时调用 ChoiceEntity.ShowChoices 弹出选项并跳转
/// </summary>
public static class DialogChoice
{
    /// <summary>分支选择指令名（配合 DialogCommands.Prefix 组成 {corre_choice ...}）。</summary>
    public const string Choice = "choice";

    /// <summary>DynamicData 中存储待处理选项的键。</summary>
    private const string ChoicesKey = "CorreWithCare:choices";

    // ==================== 节点类型 ====================

    /// <summary>
    /// 对话分支选择节点：{corre_choice content target}
    /// content = 选项显示文本的 Dialog ID；target = 选择后跳转的 Dialog ID。
    /// </summary>
    public class CorreChoice : FancyText.Node
    {
        public readonly string Content = "";
        public readonly string Target = "";

        public CorreChoice(List<string> rawParams)
        {
            if (rawParams.Count < 2)
            {
                Logger.Log(LogLevel.Warn, "CorreWithCare",
                    $"Found malformed {DialogCommands.Prefix}_{Choice}! Expected: {{{DialogCommands.Prefix}_{Choice} content target}}");
                return;
            }

            Content = rawParams[0];
            Target = rawParams[1];
        }
    }

    // ==================== 钩子生命周期 ====================

    // [Load]
    public static void Load()
    {
        // 通过框架的扩展点注册 corre_choice 的解析
        DialogCommands.CustomParseHandler = (cmd, vals, nodes) =>
        {
            if (cmd == Choice)
            {
                nodes.Add(new CorreChoice(vals));
                return true;
            }

            return false;
        };

        On.Celeste.Textbox.ctor_string_Language_Func1Array += CollectChoiceEvents;
        On.Celeste.Textbox.Removed += ShowChoiceOnRemoved;
        
        ChoicePrompt.Load();
    }

    // [Unload]
    public static void Unload()
    {
        DialogCommands.CustomParseHandler = null;

        On.Celeste.Textbox.ctor_string_Language_Func1Array -= CollectChoiceEvents;
        On.Celeste.Textbox.Removed -= ShowChoiceOnRemoved;
        
        ChoicePrompt.Unload();
    }

    // ==================== 收集 ====================

    /// <summary>
    /// Textbox 构造时收集所有 CorreChoice 节点，存到 DynamicData。
    /// 不在对话播放中触发，等 Textbox 关闭时统一处理。
    /// </summary>
    private static void CollectChoiceEvents(On.Celeste.Textbox.orig_ctor_string_Language_Func1Array orig,
        Textbox self, string dialog, Language language, Func<IEnumerator>[] events)
    {
        orig(self, dialog, language, events);

        var selfData = new DynamicData(self);
        var text = selfData.Get<FancyText.Text>("text");

        var choices = new List<CorreChoice>();
        foreach (var node in text.Nodes)
        {
            if (node is CorreChoice ch)
                choices.Add(ch);
        }

        if (choices.Count == 0)
            return;

        selfData.Set(ChoicesKey, choices);
        Prt.Info($"[DialogChoice] Textbox '{dialog}' 收集到 {choices.Count} 个分支选项");
    }

    // ==================== 弹出层：Textbox 关闭钩子 ====================

    /// <summary>
    /// Textbox 被移除（对话关闭）时，若存在待处理选项，交给 ChoiceEntity 统一处理。
    /// </summary>
    private static void ShowChoiceOnRemoved(On.Celeste.Textbox.orig_Removed orig, Textbox self, Scene scene)
    {
        orig(self, scene);
        
        var selfData = new DynamicData(self);
        if (!selfData.TryGet(ChoicesKey, out List<CorreChoice> choices) || choices == null || choices.Count == 0)
            return;

        // 清理，避免重复触发
        selfData.Set(ChoicesKey, null);

        Prt.Info($"[DialogChoice] 对话关闭，交给 ChoiceEntity 处理 {choices.Count} 个选项");

        if (scene is Level level)
        {
            var entity = level.Tracker.GetEntity<ChoiceAwakeEntity>();
            if (entity != null)
                entity.ShowChoices(choices);
            else
                Prt.Warn("[DialogChoice] 场景中找不到 ChoiceEntity，无法弹出选项");
        }
    }
}
