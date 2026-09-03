using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using Celeste;
using Celeste.Mod;
using Celeste.Mod.Entities;
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
/// 对话分支与跳转：{corre_choice display [target]}
/// {corre_jumpto target [flag]}。
/// 通过 DialogCommands 框架的 CustomParseHandler 扩展点接入。
/// </summary>
public static class DialogChoices
{
    /// <summary>分支选择指令名：{corre_choice display [target]}</summary>
    public const string ChoiceCmd = "choice";

    /// <summary>跳转指令名：{corre_jumpto target [restrictFlag]}</summary>
    public const string JumpToCmd = "jumpto";

    /// <summary>已收集但尚未消费的分支选项：dialogID → 选项列表。</summary>
    private static readonly Dictionary<string, List<CorreChoiceNode>> PendingChoices = new();

    /// <summary>跨对话链累积、去重后的分支选项（最终在稳定对话统一弹出）。</summary>
    private static readonly List<CorreChoiceNode> AccumulatedChoices = new();

    /// <summary>待跳转的目标 Dialog ID（jumpto 触发时标记，由 IL 钩子无缝衔接；空=无待跳转）。</summary>
    private static string PendingTarget = "";

    private static Func<string, List<string>, List<FancyText.Node>, bool> _prevParseHandler;

    // ==================== 节点 ====================

    /// <summary>对话分支选择节点：{corre_choice display [target]}。</summary>
    public class CorreChoiceNode : FancyText.Trigger
    {
        /// <summary>选项显示文本的 Dialog ID。</summary>
        public readonly string Display = "";

        /// <summary>选择后跳转的 Dialog ID（空 = 直接结束过场）。</summary>
        public readonly string Target = "";

        public CorreChoiceNode(List<string> rawParams)
        {
            if (rawParams.Count < 1)
            {
                Log.Warn($"[{DialogCommands.Prefix}_{ChoiceCmd}] 指令缺少参数！Expected: {{{DialogCommands.Prefix}_{ChoiceCmd} display [target]}}");
                return;
            }

            Display = rawParams[0];
            if (rawParams.Count >= 2)
                Target = rawParams[1];
        }
    }

    /// <summary>跳转节点：{corre_jumpto target [restrictFlag]}。</summary>
    public class CorreJumpToNode : FancyText.Trigger
    {
        /// <summary>跳转目标的 Dialog ID。</summary>
        public readonly string Target = "";

        /// <summary>条件 flag 名（可选；非空时检测 flag 为 true 才跳转）。</summary>
        public readonly string RestrictFlag = "";

        public CorreJumpToNode(List<string> rawParams)
        {
            if (rawParams.Count < 1)
            {
                Log.Warn($"[{DialogCommands.Prefix}_{JumpToCmd}] 指令参数不足！Expected: {{{DialogCommands.Prefix}_{JumpToCmd} target [restrictFlag]}}");
                return;
            }

            Target = rawParams[0];
            if (rawParams.Count >= 2)
                RestrictFlag = rawParams[1];
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

            if (cmd == JumpToCmd)
            {
                nodes.Add(new CorreJumpToNode(vals));
                return true;
            }

            return _prevParseHandler?.Invoke(cmd, vals, nodes) ?? false;
        };

        On.Celeste.Textbox.ctor_string_Language_Func1Array += AddChoiceEvents;
        On.Celeste.Textbox.ctor_string_Language_Func1Array += AddJumpToEvents;
        On.Celeste.Level.SkipCutscene += ClearChoicesOnSkip;

        // On hook Level.EndCutscene（统一过场结束点）：DialogCutscene / CustomNPC / MoreCustomNPC 播完对话时都会调用
        On.Celeste.Level.EndCutscene += LevelEndCutscene;
    }

    [Unload]
    public static void Unload()
    {
        DialogCommands.CustomParseHandler = _prevParseHandler;
        _prevParseHandler = null;

        On.Celeste.Textbox.ctor_string_Language_Func1Array -= AddChoiceEvents;
        On.Celeste.Textbox.ctor_string_Language_Func1Array -= AddJumpToEvents;
        On.Celeste.Level.SkipCutscene -= ClearChoicesOnSkip;

        On.Celeste.Level.EndCutscene -= LevelEndCutscene;
        PendingChoices.Clear();
        AccumulatedChoices.Clear();
        PendingTarget = "";
    }

    // ==================== 收集：Textbox 构造时 ====================

    // ==================== 过场结束：统一决策（On hook Level.EndCutscene） ====================

    /// <summary>
    /// On hook：Level.EndCutscene（统一过场结束点）。
    /// DialogCutscene / CustomNPC / MoreCustomNPC 播完对话时都会调用 Level.EndCutscene。
    /// 有 pending choice/jumpto → 接管（不调 orig，保持锁定）；否则正常结束。
    /// </summary>
    private static void LevelEndCutscene(On.Celeste.Level.orig_EndCutscene orig, Level self)
    {
        if (HandleEndCutscene(self))
            return; // 已接管（弹选项/无缝衔接），保持玩家锁定
        orig(self);
    }

    /// <summary>
    /// 统一过场结束决策（通用：DialogCutscene / CustomNPC / MoreCustomNPC 播完对话时都会走到这里）：
    ///   有 PendingTarget → 无缝衔接新过场，返回 true（跳过解锁）
    ///   有累积 choice → 弹选项，返回 true（跳过解锁）
    ///   否则 → 返回 false（正常解锁）
    /// </summary>
    private static bool HandleEndCutscene(Level level)
    {
        // 1. 有 PendingTarget → 无缝衔接新过场
        if (!string.IsNullOrEmpty(PendingTarget))
        {
            string target = PendingTarget;
            PendingTarget = "";
            var player = level?.Tracker.GetEntity<Player>();
            if (player != null)
            {
                // Add 新 DialogCutscene（OnBegin 锁定玩家）
                if (!Celeste.Mod.Entities.DialogCutscene.IsInProgress(target))
                    Engine.Scene.Add(new Celeste.Mod.Entities.DialogCutscene(target, player, false));
                return true;
            }
            return false;
        }

        // 2. 有累积 choice → 弹选项
        if (AccumulatedChoices.Count == 0)
            return false;

        var choices = new List<CorreChoiceNode>(AccumulatedChoices);
        AccumulatedChoices.Clear();
        // 用场景级协程承载 DisplayChoicesRoutine（不依赖 DialogCutscene实体，CustomNPC 场景也能弹出）
        Entity runner = new();
        runner.Add(new Coroutine(DisplayChoicesRoutine(level, choices)));
        Engine.Scene.Add(runner);
        return true;
    }

    /// <summary>把对话中的 choice 节点在播放到对应位置时累积为选项。</summary>

    private static void AddChoiceEvents(On.Celeste.Textbox.orig_ctor_string_Language_Func1Array orig,
        Textbox self, string dialog, Language language, Func<IEnumerator>[] events)
    {
        orig(self, dialog, language, events);

        var selfData = new DynamicData(self);
        var text = selfData.Get<FancyText.Text>("text");

        var choiceNodes = new List<CorreChoiceNode>();
        foreach (var node in text.Nodes)
        {
            if (node is CorreChoiceNode ch)
                choiceNodes.Add(ch);
        }

        if (choiceNodes.Count == 0)
            return;

        // 读取当前 events（可能已被其他钩子更新），在其基础上追加 choice 事件
        var currentEvents = selfData.Get<Func<IEnumerator>[]>("events") ?? events ?? new Func<IEnumerator>[0];
        int baseCount = currentEvents.Length;

        var choiceEvents = new List<Func<IEnumerator>>();
        for (int i = 0; i < choiceNodes.Count; i++)
        {
            var ch = choiceNodes[i];
            // 给 choice 节点设置 Index，Textbox 播放到该位置时触发累积
            ch.Index = baseCount + choiceEvents.Count;
            var copy = ch; // 避免闭包自引用
            choiceEvents.Add(() => AccumulateChoiceCoroutine(copy));
        }

        var newEvents = new Func<IEnumerator>[currentEvents.Length + choiceEvents.Count];
        Array.Copy(currentEvents, newEvents, currentEvents.Length);
        for (int i = 0; i < choiceEvents.Count; i++)
            newEvents[currentEvents.Length + i] = choiceEvents[i];

        selfData.Set("events", newEvents);
    }

    /// <summary>播放到 choice 位置时累积该选项到全局池（去重）。</summary>
    private static IEnumerator AccumulateChoiceCoroutine(CorreChoiceNode choice)
    {
        AccumulateChoice(choice);
        yield break;
    }

    /// <summary>把 choice 去重累积到全局池（按 display：单参数优先，双参数取最后）。</summary>
    private static void AccumulateChoice(CorreChoiceNode choice)
    {
        int idx = AccumulatedChoices.FindIndex(c => c.Display == choice.Display);
        if (idx < 0)
        {
            AccumulatedChoices.Add(choice);
            return;
        }

        var existing = AccumulatedChoices[idx];
        bool existingSingle = string.IsNullOrEmpty(existing.Target);
        bool newSingle = string.IsNullOrEmpty(choice.Target);

        if (newSingle && !existingSingle)
        {
            // 新的是单参数（直接结束）且已有的是双参数 → 单参数优先，替换
            AccumulatedChoices[idx] = choice;
        }
        else if (!newSingle && !existingSingle)
        {
            // 都是双参数 → 保留最后一个
            AccumulatedChoices[idx] = choice;
        }
        // 其他情况（已有单参数，或都是单参数）→ 保留已有的
    }

    // ==================== 跳转：jumpto events ====================
    /// <summary>把对话中的 jumpto 节点在播放到对应位置时标记待跳转。</summary>
    private static void AddJumpToEvents(On.Celeste.Textbox.orig_ctor_string_Language_Func1Array orig,
        Textbox self, string dialog, Language language, Func<IEnumerator>[] events)
    {
        orig(self, dialog, language, events);

        var selfData = new DynamicData(self);
        var text = selfData.Get<FancyText.Text>("text");

        var jumpNodes = new List<CorreJumpToNode>();
        foreach (var node in text.Nodes)
        {
            if (node is CorreJumpToNode jumptoNode)
                jumpNodes.Add(jumptoNode);
        }

        if (jumpNodes.Count == 0)
            return;

        var currentEvents = selfData.Get<Func<IEnumerator>[]>("events") ?? events ?? new Func<IEnumerator>[0];
        int baseCount = currentEvents.Length;

        var jumpEvents = new List<Func<IEnumerator>>();
        for (int i = 0; i < jumpNodes.Count; i++)
        {
            var jump = jumpNodes[i];
            jump.Index = baseCount + jumpEvents.Count;
            var copy = jump; // 避免闭包自引用
            jumpEvents.Add(() => JumpToCoroutine(copy));
        }

        var newEvents = new Func<IEnumerator>[currentEvents.Length + jumpEvents.Count];
        Array.Copy(currentEvents, newEvents, currentEvents.Length);
        for (int i = 0; i < jumpEvents.Count; i++)
            newEvents[currentEvents.Length + i] = jumpEvents[i];

        selfData.Set("events", newEvents);
    }
    /// <summary>播放到 jumpto 时标记待跳转（检测可选 flag 后），由过场结束钩子无缝衔接。</summary>
    private static IEnumerator JumpToCoroutine(CorreJumpToNode jump)
    {

        // 有 restrictFlag：仅当 flag 为 true 才跳转
        if (!string.IsNullOrEmpty(jump.RestrictFlag))
        {
            if (!jump.RestrictFlag.GetFlag())
            {

                yield break;
            }
        }

        if (string.IsNullOrEmpty(jump.Target))
            yield break;

        PendingTarget = jump.Target;

        // 关键：调用 Textbox.Close()（设 Opened=false），让 Textbox.Say 的 IEnumerator 返回，
        // 从而旧 Cutscene 协程走完 yield Textbox.Say → 走到 EndCutscene → IL 钩子无缝衔接。
        // 不能用 RemoveSelf()（Opened 仍为 true，Textbox.Say 永不返回 → 卡死）。
        if (Engine.Scene != null)
        {
            foreach (var e in Engine.Scene.Entities)
            {
                if (e is Textbox tb)
                {
                    tb.Close();
                    break;
                }
            }
        }
        yield break;
    }

    /// <summary>弹选项并等待玩家选择：选 target 无缝衔接新过场，选无 target 直接结束过场。</summary>
    private static IEnumerator DisplayChoicesRoutine(Level level, List<CorreChoiceNode> choices)
    {
        var contents = new string[choices.Count];
        for (int i = 0; i < choices.Count; i++)
            contents[i] = choices[i].Display;

        // 重新锁定玩家：部分 NPC 过场在 EndCutscene 后还会调 OnTalkEnd 解锁玩家，
        // choice 弹出前必须重新锁定，保证玩家处于过场状态不可操作。
        // 注意：StateMachine.Locked=true 会阻止 State 赋值，所以必须先设 State 再 Locked。
        var lockPlayer = level?.Tracker.GetEntity<Player>();
        if (lockPlayer != null)
        {
            lockPlayer.StateMachine.State = 11;
            lockPlayer.StateMachine.Locked = true;
        }

        // 玩家处于过场锁定中，弹出选项
        yield return ChoicePrompt.Prompt(contents);

        int idx = ChoicePrompt.Choice;
        if (idx >= 0 && idx < choices.Count)
        {
            string target = choices[idx].Target;
            if (!string.IsNullOrEmpty(target))
            {
                // 选 target → 无缝衔接新过场（保持锁定），结束当前过场
                var player = (Engine.Scene as Level)?.Tracker.GetEntity<Player>();
                if (player != null)
                {
                    Engine.Scene.Add(new Celeste.Mod.Entities.DialogCutscene(target, player, false));
                    level.EndCutscene();
                    yield break;
                }
            }
        }

        // 选无 target 或 player 缺失 → 正常结束过场
        // 关键：Level.EndCutscene() 只设 InCutscene=false，不恢复玩家 StateMachine。
        // 原版 DialogCutscene 会由 OnEnd 恢复；但 CustomNPC 的 OnTalkEnd 已提前执行完，
        // 所以这里必须手动恢复玩家 State。注意：恢复（解锁）顺序与锁定时相反——
        // 必须先 Locked=false 再 State=0（否则 State 赋值会被 Locked 阻止，玩家卡在 StDummy）。
        var unlockPlayer = level?.Tracker.GetEntity<Player>();
        if (unlockPlayer != null)
        {
            unlockPlayer.StateMachine.Locked = false;
            unlockPlayer.StateMachine.State = 0;
        }
        level.EndCutscene();
    }

    // ==================== 跳过过场 ====================

    /// <summary>
    /// 跳过过场时：清理待处理的分支选项和可能残留的选择框 UI。
    /// </summary>
    private static void ClearChoicesOnSkip(On.Celeste.Level.orig_SkipCutscene orig, Level self)
    {
        PendingChoices.Clear();
        AccumulatedChoices.Clear();
        PendingTarget = "";
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
        if (this.options.Count == 0)
        {
            base.Render();
            return;
        }

        for (int index = scroll; index < this.options.Count && index - scroll < textboxScreenLimit; ++index)
        {
            if (index != currentOptionIndex)
            {
                options[index].Render(renderOffset + textboxPosition(index, scroll));
            }
        }

        // 当前选中项最后渲染（盖在最上层，避免弹出动画被遮挡）
        int safeIndex = Math.Min(currentOptionIndex, this.options.Count - 1);
        options[safeIndex].Render(renderOffset + textboxPosition(safeIndex, scroll));

        base.Render();
    }

    public override void Update()
    {
        base.Update();

        // 选择框显示期间持续锁定玩家：部分 NPC 过场（CustomNPC 等）在 EndCutscene 后
        // 还会调 OnTalkEnd 解锁玩家，这里每帧重新锁定，保证选择时玩家不可操作。
        // 注意：StateMachine.Locked=true 会阻止 State 赋值，所以必须先设 State 再 Locked。
        if (Engine.Scene is Level level)
        {
            var p = level.Tracker.GetEntity<Player>();
            if (p != null)
            {
                p.StateMachine.State = 11;
                p.StateMachine.Locked = true;
            }
        }

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

            // 防御：SpriteId 无效时跳过该头像，避免 Missing animation name 崩溃
            if (string.IsNullOrEmpty(portrait.SpriteId))
                break;
            if (!GFX.PortraitsSpriteBank.SpriteData.ContainsKey(portrait.SpriteId))
                break;

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
