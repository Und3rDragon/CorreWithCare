using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using Celeste;
using Celeste.Mod;
using CorreWithCare.Core;
using CorreWithCare.Utils;
using Microsoft.Xna.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using static CorreWithCare.Core.ExtendedAttributes;

namespace CorreWithCare.Features;

/// <summary>
/// 对话分支选择：{corre_choice display target}
///   display = 选项显示文本对应的 Dialog ID
///   target  = 选择该选项后跳转的 Dialog ID
///
/// 功能：对话中的分支指令全部收集，对话结束后弹出选项（玩家锁定），
/// 选择后跳转到目标对话（仍锁定），播完恢复操作；对话中无分支指令则正常结束。
///
/// 通过 DialogCommands 框架的 CustomParseHandler 扩展点接入。
/// 使用：在 Dialog 文件中写 {corre_choice display target} 即可，无需 C# 侧额外注册。
/// </summary>
public static class DialogChoices
{
    /// <summary>分支选择指令名：{corre_choice display target}</summary>
    public const string ChoiceCmd = "choice";

    /// <summary>已收集但尚未消费的分支选项：dialogID → 选项列表。</summary>
    private static readonly Dictionary<string, List<CorreChoiceNode>> PendingChoices = new();

    private static Func<string, List<string>, List<FancyText.Node>, bool> _prevParseHandler;

    /// <summary>IL 钩子句柄（DialogCutscene.Cutscene 状态机 MoveNext）。</summary>
    private static ILHook _cutsceneIlHook;

    // ==================== 节点 ====================

    /// <summary>对话分支选择节点：{corre_choice display target}</summary>
    public class CorreChoiceNode : FancyText.Node
    {
        /// <summary>选项显示文本的 Dialog ID。</summary>
        public readonly string Display = "";

        /// <summary>选择后跳转的 Dialog ID。</summary>
        public readonly string Target = "";

        public CorreChoiceNode(List<string> rawParams)
        {
            if (rawParams.Count < 2)
            {
                Prt.Warn($"Found malformed {DialogCommands.Prefix}_{ChoiceCmd}! Expected: {{{DialogCommands.Prefix}_{ChoiceCmd} display target}}");
                return;
            }

            Display = rawParams[0];
            Target = rawParams[1];
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
            if (cmd == ChoiceCmd)
            {
                nodes.Add(new CorreChoiceNode(vals));
                return true;
            }

            return _prevParseHandler?.Invoke(cmd, vals, nodes) ?? false;
        };

        On.Celeste.Textbox.ctor_string_Language_Func1Array += CollectChoices;
        On.Celeste.Level.SkipCutscene += ClearChoicesOnSkip;

        // IL 钩 DialogCutscene.Cutscene 状态机 MoveNext（在 EndCutscene 调用前打断）
        // 写法参考 ChroniaHelper CustomBooster：GetStateMachineTarget() + ILHook
        _cutsceneIlHook = HookCutsceneMoveNext();
        if (_cutsceneIlHook == null)
            Prt.Warn($"[{DialogCommands.Prefix}_{ChoiceCmd}] 未找到 DialogCutscene.Cutscene 状态机，打断功能不可用");
    }

    [Unload]
    public static void Unload()
    {
        DialogCommands.CustomParseHandler = _prevParseHandler;
        _prevParseHandler = null;

        On.Celeste.Textbox.ctor_string_Language_Func1Array -= CollectChoices;
        On.Celeste.Level.SkipCutscene -= ClearChoicesOnSkip;

        _cutsceneIlHook?.Dispose();
        _cutsceneIlHook = null;
        PendingChoices.Clear();
    }

    /// <summary>创建 DialogCutscene.Cutscene 状态机的 IL 钩子（用于打断过场结束）。</summary>
    private static ILHook HookCutsceneMoveNext()
    {
        var cutscene = typeof(Celeste.Mod.Entities.DialogCutscene)
            .GetMethod("Cutscene", BindingFlags.NonPublic | BindingFlags.Instance);
        if (cutscene == null)
            return null;

        var moveNext = cutscene.GetStateMachineTarget();
        if (moveNext == null)
            return null;

        return new ILHook(moveNext, InterceptCutsceneIL);
    }

    /// <summary>
    /// 在 EndCutscene 调用前插入打断逻辑：有分支则跳过过场结束（由选择流程接管），无分支放行。
    /// </summary>
    private static void InterceptCutsceneIL(ILContext il)
    {
        var cursor = new ILCursor(il);

        // 1. 先定位 EndCutscene 调用
        if (!cursor.TryGotoNext(MoveType.Before, instr => instr.MatchCallvirt("Celeste.CutsceneEntity", "EndCutscene")))
        {
            Prt.Warn($"[{DialogCommands.Prefix}_{ChoiceCmd}] IL 钩子未找到 EndCutscene 调用");
            return;
        }
        var callvirt = cursor.Next;
        var afterEndCutscene = callvirt.Next;

        // 2. 往回找参数序列起点：ldc.i4.1 → ldfld level → ldarg.0 → ldloc.1
        if (!cursor.TryGotoPrev(MoveType.Before, instr => instr.MatchLdcI4(1)))
        {
            Prt.Warn($"[{DialogCommands.Prefix}_{ChoiceCmd}] IL 钩子未找到 EndCutscene 参数 ldc.i4.1");
            return;
        }
        if (!cursor.TryGotoPrev(MoveType.Before, instr => instr.Operand is FieldReference f && f.Name == "level"))
        {
            Prt.Warn($"[{DialogCommands.Prefix}_{ChoiceCmd}] IL 钩子未找到 EndCutscene 参数 level");
            return;
        }
        if (!cursor.TryGotoPrev(MoveType.Before, instr => instr.OpCode == OpCodes.Ldarg_0))
        {
            Prt.Warn($"[{DialogCommands.Prefix}_{ChoiceCmd}] IL 钩子未找到 EndCutscene 参数 ldarg.0");
            return;
        }
        if (!cursor.TryGotoPrev(MoveType.Before, instr => instr.OpCode == OpCodes.Ldloc_1))
        {
            Prt.Warn($"[{DialogCommands.Prefix}_{ChoiceCmd}] IL 钩子未找到 EndCutscene 参数 ldloc.1");
            return;
        }

        // 3. 插入点栈为空：EmitDelegate 压 bool → Brtrue 消费并跳转
        cursor.EmitDelegate<Func<bool>>(FindCutsceneAndIntercept);
        cursor.Emit(OpCodes.Brtrue, afterEndCutscene);
    }

    // ==================== 收集：Textbox 构造时 ====================

    /// <summary>
    /// Textbox 构造时收集该对话中所有分支选项（按 dialogID 存），
    /// 指令在对话开头/中间/结尾都会被收集，等对话结束统一使用。
    /// </summary>
    private static void CollectChoices(On.Celeste.Textbox.orig_ctor_string_Language_Func1Array orig,
        Textbox self, string dialog, Language language, Func<IEnumerator>[] events)
    {
        orig(self, dialog, language, events);

        var selfData = new DynamicData(self);
        var text = selfData.Get<FancyText.Text>("text");

        var list = new List<CorreChoiceNode>();
        foreach (var node in text.Nodes)
        {
            if (node is CorreChoiceNode ch)
                list.Add(ch);
        }

        if (list.Count == 0)
            return;

        PendingChoices[dialog] = list;
        // Prt.Info($"[{DialogCommands.Prefix}_{ChoiceCmd}] 对话 '{dialog}' 收集到 {list.Count} 个分支选项");
    }

    // ==================== 打断：过场结束前 ====================

    /// <summary>从场景中找到当前正在收尾的 DialogCutscene 并执行打断判断。</summary>
    private static bool FindCutsceneAndIntercept()
    {
        Celeste.Mod.Entities.DialogCutscene dc = null;
        if (Engine.Scene != null)
        {
            foreach (var e in Engine.Scene.Entities)
            {
                if (e is Celeste.Mod.Entities.DialogCutscene d)
                {
                    dc = d;
                    break;
                }
            }
        }

        return dc != null && InterceptCutscene(dc);
    }

    /// <summary>
    /// 过场结束前检查该对话是否收集到分支：有则打断过场（玩家保持锁定），
    /// 挂选择流程接管；返回 false 时过场正常结束。
    /// </summary>
    private static bool InterceptCutscene(Celeste.Mod.Entities.DialogCutscene dc)
    {
        if (PendingChoices.Count == 0)
            return false;

        string dialogID = new DynamicData(dc).Get<string>("dialogID");
        if (!PendingChoices.TryGetValue(dialogID, out var choices) || choices.Count == 0)
            return false;

        PendingChoices.Remove(dialogID);
        // Prt.Info($"[{DialogCommands.Prefix}_{ChoiceCmd}] 对话 '{dialogID}' 结束，打断过场，弹出 {choices.Count} 个选项");
        dc.Add(new Coroutine(ChoiceRoutine(dc, dc.Level, choices)));
        return true;
    }

    /// <summary>分支流程：弹选项 → 玩家选择 → 跳转新对话 → 真正结束过场。</summary>
    private static IEnumerator ChoiceRoutine(CutsceneEntity self, Level level, List<CorreChoiceNode> choices)
    {
        var contents = new string[choices.Count];
        for (int i = 0; i < choices.Count; i++)
            contents[i] = choices[i].Display;

        // 玩家处于过场锁定中，弹出选项
        yield return ChoicePrompt.Prompt(contents);

        int idx = ChoicePrompt.Choice;
        // Prt.Info($"[{DialogCommands.Prefix}_{ChoiceCmd}] 玩家选择了索引 {idx}");
        if (idx >= 0 && idx < choices.Count)
        {
            string target = choices[idx].Target;
            // Prt.Info($"[{DialogCommands.Prefix}_{ChoiceCmd}] 跳转到对话 '{target}'");
            if (!string.IsNullOrEmpty(target))
            {
                // 跳转对话仍处于过场中，玩家保持锁定
                yield return Textbox.Say(target, null);
            }
        }

        // 分支流程结束，真正结束过场（原版 OnEnd 恢复玩家状态）
        // Prt.Info($"[{DialogCommands.Prefix}_{ChoiceCmd}] 分支流程结束，结束过场");
        self.EndCutscene(level, true);
    }

    // ==================== 跳过过场 ====================

    /// <summary>
    /// 跳过过场时：清理待处理的分支选项和可能残留的选择框 UI。
    /// </summary>
    private static void ClearChoicesOnSkip(On.Celeste.Level.orig_SkipCutscene orig, Level self)
    {
        PendingChoices.Clear();
        orig(self);

        foreach (var ui in self.Tracker.GetEntities<ChoicePrompt>())
            ui.RemoveSelf();
    }
}

// ==================== 选择框 UI（移植自 sln lua LuaCutscenes 的 ChoicePrompt） ====================

/// <summary>对话分支选择框。Prompt() 静态协程创建选择框并等待玩家确认，Choice 保存结果。</summary>
[Tracked]
public class ChoicePrompt : BaseEntity
{
    /// <summary>玩家最终选择的索引（Prompt 返回后有效）。</summary>
    public static int Choice;

    private Vector2 renderOffset = new Vector2(260f, 120f);
    private int textboxScreenLimit;
    private int scroll;

    /// <summary>
    /// 弹出选项并等待玩家确认。
    /// options 是选项显示文本的 Dialog ID（FancyText，支持头像 [madeline happy] 等）。
    /// </summary>
    public static IEnumerator Prompt(params string[] options)
    {
        var obj = new ChoicePrompt();
        Engine.Scene.Add(obj);
        foreach (var opt in options)
        {
            obj.Add(new Option(opt));
        }

        Audio.Play("event:/ui/game/chatoptions_appear");
        while (obj.Alive)
        {
            yield return null;
        }

        Choice = obj.currentOptionIndex;
        obj.RemoveSelf();
    }

    private List<Option> options = new List<Option>();
    private bool Alive, Confirmed;
    private int currentOptionIndex;

    public ChoicePrompt()
    {
        this.Tag = (int)Tags.HUD;
        this.Alive = true;
        this.textboxScreenLimit = (int)Math.Floor((Engine.Height - (int)renderOffset.Y) / 160f);
    }

    public void Add(Option option)
    {
        int idx = this.options.Count;
        this.options.Add(option);
        Engine.Scene.Add(option);

        option.Position = new Vector2(260f, 120f + 160f * idx);
        option.Ease = 0f;
    }

    public override void Removed(Scene scene)
    {
        base.Removed(scene);
        foreach (var opt in this.options)
        {
            opt.RemoveSelf();
        }
    }

    public static Vector2 textboxPosition(int index, int scroll)
    {
        return new Vector2(0f, 160f * index - 160f * scroll);
    }

    public override void Render()
    {
        for (int index = scroll; index < this.options.Count && index - scroll < textboxScreenLimit; ++index)
        {
            if (index != currentOptionIndex)
            {
                options[index].Render(renderOffset + textboxPosition(index, scroll));
            }
        }

        // 当前选中项最后渲染（盖在最上层，避免弹出动画被遮挡）
        options[currentOptionIndex].Render(renderOffset + textboxPosition(currentOptionIndex, scroll));

        base.Render();
    }

    public override void Update()
    {
        base.Update();

        if (this.Confirmed)
        {
            this.Alive = false;
            foreach (var opt in this.options)
            {
                opt.Ease = Calc.Approach(opt.Ease, 0f, Engine.DeltaTime * 4);
                if (opt.Ease != 0f)
                {
                    this.Alive = true;
                }
            }
        }
        else
        {
            if (Input.MenuConfirm.Pressed)
            {
                Audio.Play("event:/ui/game/chatoptions_select");
                this.Confirmed = true;
            }
            else if (Input.MenuUp.Pressed && this.currentOptionIndex > 0)
            {
                Audio.Play("event:/ui/game/chatoptions_roll_up");
                this.currentOptionIndex--;
                if (currentOptionIndex < scroll)
                {
                    scroll--;
                }
            }
            else if (Input.MenuDown.Pressed && this.currentOptionIndex < this.options.Count - 1)
            {
                Audio.Play("event:/ui/game/chatoptions_roll_down");
                this.currentOptionIndex++;
                if (currentOptionIndex - scroll >= textboxScreenLimit)
                {
                    scroll++;
                }
            }

            var idx = 0;
            foreach (var opt in this.options)
            {
                opt.Ease = Calc.Approach(opt.Ease, 1f, Engine.DeltaTime * 4);
                opt.Highlight = Calc.Approach(opt.Highlight, idx == this.currentOptionIndex ? 1f : 0f, Engine.DeltaTime * 4);
                opt.Portrait?.Update();
                idx++;
            }
        }
    }
}

/// <summary>单个选项，支持头像立绘。</summary>
public class Option : BaseEntity
{
    public float Ease;
    public float Highlight;

    public string Key;
    public string Textbox;
    public FancyText.Text Text;
    public Sprite Portrait;
    public Facings PortraitSide;
    public float PortraitSize;

    public Option(string key)
    {
        this.Key = key;
        this.Tag = (int)Tags.HUD;

        int maxLineWidth = 1828;
        this.Text = FancyText.Parse(Dialog.Get(this.Key), maxLineWidth, -1);
        this.Textbox = "textbox/madeline_ask";
        foreach (FancyText.Node node in this.Text.Nodes)
        {
            if (!(node is FancyText.Portrait portrait))
            {
                continue;
            }

            this.Portrait = GFX.PortraitsSpriteBank.Create(portrait.SpriteId);
            this.Portrait.Play(portrait.IdleAnimation);
            this.PortraitSide = (Facings)portrait.Side;
            this.Textbox = "textbox/" + portrait.Sprite + "_ask";

            XmlElement xml = GFX.PortraitsSpriteBank.SpriteData[portrait.SpriteId].Sources[0].XML;
            if (xml != null)
            {
                string textboxFallback = "textbox/" + xml.Attr("textbox", portrait.Sprite) + "_ask";

                this.PortraitSize = xml.AttrInt("size", 160);
                this.Textbox = xml.Attr("ask_textbox", textboxFallback);
            }

            break;
        }

        if (!GFX.Portraits.Has(this.Textbox))
        {
            this.Textbox = "textbox/madeline_ask";
        }
    }

    public void Render(Vector2 position)
    {
        if (this.Scene is Level level && level.Paused)
        {
            return;
        }

        float introEase = Monocle.Ease.CubeOut(this.Ease);
        float highlightEase = Monocle.Ease.CubeInOut(this.Highlight);

        position.Y += -32f * (1f - introEase);
        position.X += highlightEase * 32f;

        Color color1 = Color.Lerp(Color.Gray, Color.White, highlightEase) * introEase;
        float alpha = MathHelper.Lerp(0.6f, 1f, highlightEase) * introEase;

        if (this.Textbox != null)
        {
            GFX.Portraits[this.Textbox]?.Draw(position, Vector2.Zero, color1);
        }

        Facings facings = this.PortraitSide;
        if (SaveData.Instance != null && SaveData.Instance.Assists.MirrorMode)
        {
            facings = (Facings)(-(int)facings);
        }

        float num2 = 100f;

        if (this.Portrait != null)
        {
            this.Portrait.Scale = Vector2.One * (num2 / this.PortraitSize);
            if (facings == Facings.Right)
            {
                this.Portrait.Position = position + new Vector2(1380f - num2 * 0.5f, 70f);
                this.Portrait.Scale.X *= -1f;
            }
            else
            {
                this.Portrait.Position = position + new Vector2(20f + num2 * 0.5f, 70f);
            }

            this.Portrait.Color = Color.White * (0.5f + highlightEase * 0.5f) * introEase;
            this.Portrait.Render();
        }

        float num3 = (140f - ActiveFont.LineHeight * 0.7f) / 2f;
        Vector2 position1 = new Vector2(0f, position.Y + 70f);
        Vector2 justify = new Vector2(0f, 0.5f);
        if (facings == Facings.Right)
        {
            justify.X = 1f;
            position1.X = (position.X + 1400f - 20f) - num3 - num2;
        }
        else
        {
            position1.X = position.X + 20f + num3 + num2;
        }

        this.Text.Draw(position1, justify, Vector2.One * 0.7f, alpha);
    }
}
