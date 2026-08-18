using System.Collections;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod;
using CorreWithCare.Core;
using CorreWithCare.Features;
using static CorreWithCare.Core.ExtendedAttributes;

namespace CorreWithCare.Features;

/// <summary>
/// 对话分支选择的常驻调度实体。
///
/// 通过钩 Level.LoadLevel 强制在每次加载关卡时添加一个 ChoiceEntity 到场景，
/// 由它负责弹出选择框并处理跳转——因为它是场景内被正常调度 Update 的实体，
/// 协程挂在它身上不会出现"临时实体不被驱动"的问题。
///
/// 使用方式（由 DialogChoice 调用）：
///   level.Tracker.GetEntity&lt;ChoiceEntity&gt;()?.ShowChoices(choices);
/// </summary>
[Tracked]
public class ChoiceAwakeEntity : BaseEntity
{
    // ==================== 生命周期：强制常驻场景 ====================

    // [Load]
    public static void Load()
    {
        On.Celeste.Level.LoadLevel += OnLoadLevel;
    }

    // [Unload]
    public static void Unload()
    {
        On.Celeste.Level.LoadLevel -= OnLoadLevel;
    }

    /// <summary>
    /// 每次加载关卡时，如果场景里还没有 ChoiceEntity 就补一个。
    /// </summary>
    private static void OnLoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes intro, bool fromLoader)
    {
        orig(self, intro, fromLoader);

        if (self.Tracker.GetEntity<ChoiceAwakeEntity>() == null)
        {
            self.Add(new ChoiceAwakeEntity());
            Logger.Log(LogLevel.Info, "CorreWithCare", "[ChoiceEntity] 已添加到场景");
        }
    }

    // ==================== 对外接口 ====================

    /// <summary>
    /// 开始一次分支选择：弹出选项 → 等待玩家选择 → 跳转到对应 Dialog。
    /// </summary>
    public void ShowChoices(List<DialogChoice.CorreChoice> choices)
    {
        // 防止上一次协程还没结束时重复启动
        if (_active)
            return;

        _choices = choices;
        Add(new Coroutine(ChoiceRoutine()));
    }

    private bool _active;
    private List<DialogChoice.CorreChoice> _choices;

    // ==================== 内部协程 ====================

    private IEnumerator ChoiceRoutine()
    {
        _active = true;
        Level level = Scene as Level;
        Player player = level?.Tracker.GetEntity<Player>();
        bool wasLocked = false;

        try
        {
            // 锁定玩家：进入过场状态，防止选择与跳转期间玩家可移动
            if (player != null)
            {
                wasLocked = player.StateMachine.Locked;
                player.StateMachine.State = Player.StDummy;
                player.StateMachine.Locked = true;
                player.DummyAutoAnimate = true;
            }

            var contents = new string[_choices.Count];
            for (int i = 0; i < _choices.Count; i++)
                contents[i] = _choices[i].Content;
            Logger.Log(LogLevel.Info, "CorreWithCare", $"[ChoiceEntity] 弹出 {contents.Length} 个选项: {string.Join(", ", contents)}");

            // ChoicePrompt.Prompt 内部会创建选择框实体并等待玩家确认
            yield return ChoicePrompt.Prompt(contents);

            int idx = ChoicePrompt.Choice;
            Logger.Log(LogLevel.Info, "CorreWithCare", $"[ChoiceEntity] 玩家选择了索引 {idx}");
            if (idx >= 0 && idx < _choices.Count)
            {
                string target = _choices[idx].Target;
                Logger.Log(LogLevel.Info, "CorreWithCare", $"[ChoiceEntity] 跳转到对话 '{target}'");
                if (!string.IsNullOrEmpty(target))
                {
                    // 新对话期间玩家保持锁定
                    yield return Textbox.Say(target, null);
                }
            }
        }
        finally
        {
            // 恢复玩家操作（仅当原本是解锁状态才恢复，避免破坏原本就在过场中的情况）
            if (player != null)
            {
                player.StateMachine.Locked = wasLocked;
                if (!wasLocked)
                {
                    player.StateMachine.State = Player.StNormal;
                    player.DummyAutoAnimate = false;
                }
            }

            _active = false;
            _choices = null;
        }
    }
}
