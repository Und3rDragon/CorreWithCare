using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod;
using Celeste.Mod.Entities;
using CorreWithCare.Utils;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using static CorreWithCare.Core.ExtendedAttributes;

namespace CorreWithCare.Features;

/// <summary>
/// Dialog 指令框架：允许在 Dialog 文件中插入自定义指令 {corre_xxx 参数...}，
/// 对话进行到该位置时自动触发注册表中对应的协程效果。
///
/// Dialog 文件用法：
///   {corre_xxx 参数...}              — 阻塞式：对话等待效果完成
///   {&corre_xxx 参数...}             — 静默式
///   {~corre_xxx 参数...}             — 并发式：不阻塞对话
///   {corre_on_skip xxx 参数...}      — 跳过过场时执行 xxx
///
/// C# 侧注册效果：
///   DialogCommands.Register("xxx", (player, level, param) => SomeRoutine(...));
/// </summary>
public static partial class DialogCommands
{
    /// <summary>指令前缀。Dialog 中所有 {corre_xxx} 形式的指令都会被框架捕获。</summary>
    public const string Prefix = "corre";

    /// <summary>跳过指令名（作用于任意已注册指令）。</summary>
    public const string OnSkip = "on_skip";

    /// <summary>
    /// 指令注册表：指令名（小写，不含前缀）→ 效果协程工厂。
    /// 工厂签名 (Player, Level, List&lt;string&gt; 参数) → IEnumerator。
    /// </summary>
    public static readonly Dictionary<string, Func<Player, Level, List<string>, IEnumerator>> Triggers = new();

    /// <summary>
    /// 外部模块的指令解析回调：(指令名, 参数, 节点列表) → 是否已处理。
    /// 返回 true 表示该指令已被外部模块消费。
    /// </summary>
    internal static Func<string, List<string>, List<FancyText.Node>, bool> CustomParseHandler;

    // ==================== 节点 ====================

    /// <summary>对话中遇到的自定义指令节点（承载任意 {corre_xxx} 指令）。</summary>
    public class CorreTriggerNode : FancyText.Trigger
    {
        /// <summary>指令名（不含前缀，如 "walk"、"set_flag"）。</summary>
        public readonly string ID = "";
        public readonly List<string> Params = new();
        /// <summary>是否并发执行（~ 前缀），不阻塞对话。</summary>
        public readonly bool Concurrent;

        public CorreTriggerNode(List<string> rawParams, bool silent, bool concurrent)
        {
            Silent = silent;
            Concurrent = concurrent;

            if (rawParams.Count == 0)
            {
                Prt.Warn($"Found empty {Prefix} trigger!");
            }
            else
            {
                ID = rawParams[0];
                Params = rawParams.GetRange(1, rawParams.Count - 1);
            }
        }
    }

    /// <summary>跳过过场时要执行的指令节点。</summary>
    public class CorreRunOnSkipNode : FancyText.Node
    {
        public readonly string ID = "";
        public readonly List<string> Params = new();

        public CorreRunOnSkipNode(List<string> rawParams)
        {
            if (rawParams.Count == 0)
            {
                Prt.Warn("CorreWithCare", $"Found empty {Prefix}_{OnSkip}!");
            }
            else
            {
                ID = rawParams[0];
                Params = rawParams.GetRange(1, rawParams.Count - 1);
            }
        }
    }

    // ==================== 钩子生命周期 ====================

    [Load]
    public static void Load()
    {
        IL.Celeste.FancyText.Parse += ParseCorreTriggers;
        On.Celeste.Textbox.ctor_string_Language_Func1Array += AddCorreEvents;
        On.Celeste.Level.SkipCutscene += SkipCutscene;
    }

    [Unload]
    public static void Unload()
    {
        IL.Celeste.FancyText.Parse -= ParseCorreTriggers;
        On.Celeste.Textbox.ctor_string_Language_Func1Array -= AddCorreEvents;
        On.Celeste.Level.SkipCutscene -= SkipCutscene;
    }

    // ==================== 解析层：FancyText.Parse IL 钩子 ====================

    /// <summary>在 FancyText.Parse 内部识别所有 {corre_xxx ...} 形式的指令并转为节点。</summary>
    private static void ParseCorreTriggers(ILContext il)
    {
        var cursor = new ILCursor(il);
        if (cursor.TryGotoNext(MoveType.Before, instr => instr.MatchLdstr("savedata")))
        {
            cursor.Emit(OpCodes.Ldarg_0); // this (FancyText)
            cursor.Emit(OpCodes.Ldloc_S, il.Method.Body.Variables[7]); // s
            cursor.Emit(OpCodes.Ldloc_S, il.Method.Body.Variables[8]); // stringList
            cursor.EmitDelegate<Action<FancyText, string, List<string>>>(HandleParse);
        }
    }

    /// <summary>
    /// 统一指令分发：先交给 CustomParseHandler（外部模块），未处理则按 on_skip / 普通指令处理。
    /// </summary>
    private static void HandleParse(FancyText text, string s, List<string> vals)
    {
        var parserData = new DynamicData(text);
        FancyText.Text group = parserData.Get<FancyText.Text>("group");
        List<FancyText.Node> nodes = group.Nodes;

        string baseName = s;
        bool silent = false, concurrent = false;
        if (baseName.StartsWith("&")) { silent = true; baseName = baseName[1..]; }
        else if (baseName.StartsWith("~")) { concurrent = true; baseName = baseName[1..]; }

        if (!baseName.StartsWith(Prefix + "_"))
            return;

        string cmd = baseName[(Prefix.Length + 1)..];

        // 外部模块优先（如 DialogChoices 的 corre_choice）
        if (CustomParseHandler?.Invoke(cmd, vals, nodes) == true)
            return;

        if (cmd == OnSkip)
            nodes.Add(new CorreRunOnSkipNode(vals));
        else
            nodes.Add(new CorreTriggerNode(vals, silent, concurrent));
    }

    // ==================== 事件层：Textbox 构造钩子 ====================

    /// <summary>Textbox 构造时把 CorreTrigger 节点转换为 events 协程，对话到点自动触发。</summary>
    private static void AddCorreEvents(On.Celeste.Textbox.orig_ctor_string_Language_Func1Array orig,
        Textbox self, string dialog, Language language, Func<IEnumerator>[] events)
    {
        orig(self, dialog, language, events);

        var selfData = new DynamicData(self);
        var text = selfData.Get<FancyText.Text>("text");

        // 读取当前 events（可能已被其他钩子更新），在其基础上追加
        var currentEvents = selfData.Get<Func<IEnumerator>[]>("events") ?? events ?? new Func<IEnumerator>[0];
        int baseCount = currentEvents.Length;

        var correEvents = new List<Func<IEnumerator>>();

        foreach (var node in text.Nodes)
        {
            if (node is CorreTriggerNode trg)
            {
                trg.Index = baseCount + correEvents.Count;
                Level level = Engine.Scene as Level;
                var cutscene = Get(trg.ID, level?.Tracker.GetEntity<Player>(), level, trg.Params);
                var copy = cutscene; // 避免闭包自引用
                if (trg.Concurrent)
                    cutscene = () => WrapCoroutine(copy());
                correEvents.Add(cutscene);
            }
        }

        if (correEvents.Count == 0)
            return;

        var newEvents = new Func<IEnumerator>[currentEvents.Length + correEvents.Count];
        Array.Copy(currentEvents, newEvents, currentEvents.Length);
        for (int i = 0; i < correEvents.Count; i++)
            newEvents[currentEvents.Length + i] = correEvents[i];

        selfData.Set("events", newEvents);
    }

    // ==================== 跳过处理 ====================

    private static void SkipCutscene(On.Celeste.Level.orig_SkipCutscene orig, Level self)
    {
        var player = self.Tracker.GetEntity<Player>();
        self.Entities.With<Textbox>(textbox =>
        {
            DynamicData boxData = new(textbox);
            List<FancyText.Node> nodes = boxData.Get<FancyText.Text>("text").Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] is CorreRunOnSkipNode skip)
                {
                    var cutscene = Get(skip.ID, player, self, skip.Params)();
                    while (cutscene.MoveNext())
                        ; // 跳过时立即跑完，不做延迟
                }
            }
        });
        
        orig(self);
    }

    // ==================== 注册 ====================

    /// <summary>注册一个指令效果（无 mod 前缀）。指令名不含 corre_ 前缀。</summary>
    public static void Register(string triggerName, Func<Player, Level, List<string>, IEnumerator> effect)
    {
        Register(null, triggerName, effect);
    }

    /// <summary>注册一个指令效果，支持 "modname:triggername" 前缀区分。</summary>
    public static void Register(string modName, string triggerName, Func<Player, Level, List<string>, IEnumerator> effect)
    {
        if (!string.IsNullOrWhiteSpace(modName))
            Triggers[modName.Trim().ToLower() + ":" + triggerName.Trim().ToLower()] = effect;
        else
            Triggers[triggerName.Trim().ToLower()] = effect;
    }

    /// <summary>按指令 ID 查找效果；找不到返回空协程。</summary>
    public static Func<IEnumerator> Get(string id, Player player, Level level, List<string> p)
    {
        static IEnumerator nothing()
        {
            yield return null;
        }

        string clean = id?.Trim()?.ToLower() ?? "";
        if (Triggers.TryGetValue(clean, out var trigger))
            return () => trigger(player, level, p);
        return nothing;
    }

    // ==================== 参数解析 ====================

    public static float GetFloatParam(List<string> strings, int index, float def = 0)
    {
        return strings.Count <= index ? def : (float.TryParse(strings[index], out float amnt) ? amnt : def);
    }

    public static string GetStringParam(List<string> strings, int index, string def = "")
    {
        return strings.Count <= index ? def : strings[index];
    }

    public static bool GetBoolParam(List<string> strings, int index, bool def = false)
    {
        if (strings.Count <= index) return def;
        return bool.TryParse(strings[index], out bool b) ? b : def;
    }

    public static Vector2? GetVectorParam(List<string> strings, int index, Vector2? def = null)
    {
        if (strings.Count > index)
        {
            string[] parts = strings[index].Split(',');
            if (parts.Length == 2 && float.TryParse(parts[0], out float x) && float.TryParse(parts[1], out float y))
                return new Vector2(x, y);
        }

        return def;
    }

    // ==================== 辅助 ====================

    /// <summary>把协程包到一个临时实体上运行，实现并发（不阻塞对话）。</summary>
    public static IEnumerator WrapCoroutine(IEnumerator routine)
    {
        Entity entity = new();
        entity.Add(new Coroutine(routine));
        Engine.Scene.Add(entity);
        yield return null;
    }
}
