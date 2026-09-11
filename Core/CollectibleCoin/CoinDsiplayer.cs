using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Celeste;
using CorreWithCare.Utils;
using Microsoft.Xna.Framework;
using static CorreWithCare.Core.ExtendedAttributes;

namespace CorreWithCare.Core.CollectibleCoin;

/// <summary>
/// 金币计数器显示层：管理多条金币计数条的槽位分配与出入场。
///
/// 槽位规则：上方优先——新标签总是优先占用最靠上的空闲槽位，
/// 空闲槽位用尽时依次向下延伸；达到设置的上限后排队等待。
/// 每条计数条独立计时、独立出入场，释放的槽位立即可被排队者占用。
/// </summary>
[Tracked]
public class CoinDisplay : Entity
{
    /// <summary>速度跑计时器显示时计数条需要额外下移的距离。</summary>
    public const float ChapterTimerOffset = 58f;

    /// <summary>文件计时器显示时计数条需要额外下移的距离。</summary>
    public const float FileTimerOffset = 78f;

    private static CoinDisplay _instance;

    private readonly List<CoinSlot> _slots = new();
    private readonly Queue<PendingCoin> _waiting = new();

    /// <summary>显示层所在的关卡，用于在实体尚未进入场景时也能添加计数条。</summary>
    private Level _level;

    internal MTexture Background;

    /// <summary>
    /// 一个计数条槽位：记录占用它的标签与正在运行的计数条。
    /// </summary>
    internal class CoinSlot
    {
        public string Tag = "";
        internal CoinTimer Timer;
        public bool Released = true;
    }

    /// <summary>
    /// 一个等待槽位的标签：记住开始排队时的数值，
    /// 上场时从该值滚动到最新值，使排队期间的累积一次性体现。
    /// </summary>
    private class PendingCoin
    {
        public string Tag = "";
        public int StartAmount;
    }

    public CoinDisplay(Level level)
    {
        _level = level;
        Depth = -101;
        Tag = Tags.HUD | Tags.PauseUpdate | Tags.TransitionUpdate | Tags.Global | Tags.Persistent;
        Background = GFX.Gui["strawberryCountBG"];
    }

    // ==================== 生命周期 ====================

    [Load]
    public static void Load()
    {
        On.Celeste.Level.LoadLevel += OnLoadLevel;
        On.Celeste.Level.End += OnLevelEnd;
    }

    [Unload]
    public static void Unload()
    {
        On.Celeste.Level.LoadLevel -= OnLoadLevel;
        On.Celeste.Level.End -= OnLevelEnd;
    }

    /// <summary>
    /// 关卡加载后重置显示层并刷新会话计数器，使中途进入关卡时数值仍然正确。
    /// </summary>
    private static void OnLoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes intro, bool fromLoader)
    {
        orig(self, intro, fromLoader);
        _instance = null;
        CoinCounting.RefreshOnLevelLoaded(self);
    }

    private static void OnLevelEnd(On.Celeste.Level.orig_End orig, Level self)
    {
        _instance?.ClearAll();
        _instance = null;
        orig(self);
    }

    // ==================== 对外入口 ====================

    /// <summary>
    /// 通知显示层某个标签的金币数量发生变化：已在显示中的标签复用原计数条并刷新数值，
    /// 否则申请一个新槽位，没有空闲槽位时排队等待。
    /// startAmount 为这次收集之前的数值，作为新计数条的起始显示值。
    /// </summary>
    public static void NotifyCoinChanged(Level level, string levelSet, string tag, int startAmount)
    {
        if (level is null || string.IsNullOrEmpty(levelSet) || string.IsNullOrEmpty(tag))
            return;

        Ensure(level)?.OnCoinChanged(levelSet, tag, startAmount);
    }

    private static CoinDisplay Ensure(Level level)
    {
        if (_instance is not null && _instance._level == level)
            return _instance;

        _instance = level.Tracker.GetEntity<CoinDisplay>();

        if (_instance is null)
        {
            _instance = level.Entities.ToAdd.OfType<CoinDisplay>().FirstOrDefault();
            if (_instance is null)
            {
                _instance = new CoinDisplay(level);
                level.Add(_instance);
            }
        }

        return _instance;
    }

    // ==================== 槽位分配 ====================

    private void OnCoinChanged(string levelSet, string tag, int startAmount)
    {
        // 已在显示中：复用原计数条，数字继续滚动到最新值
        CoinSlot existing = _slots.FirstOrDefault(s => !s.Released && s.Tag == tag);
        if (existing is not null)
        {
            existing.Timer?.Refresh(CollectibleCoinUtils.GetCoinCount(levelSet, tag));
            return;
        }

        // 已排队：等待中的条目在获得槽位时读取最新值，无需重复排队
        if (_waiting.Any(p => p.Tag == tag))
            return;

        int freeIndex = FindFreeSlotIndex();
        if (freeIndex >= 0)
            OccupySlot(freeIndex, tag, startAmount);
        else if (ActiveSlotCount < CWCModule.Settings.CorreCoinMaxSlots)
            OccupySlot(_slots.Count, tag, startAmount);
        else
            _waiting.Enqueue(new PendingCoin { Tag = tag, StartAmount = startAmount });
    }

    /// <summary>
    /// 找最靠上的空闲槽位（含已释放的中间槽位）；没有空闲槽位时返回 -1。
    /// </summary>
    private int FindFreeSlotIndex()
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            if (_slots[i].Released)
                return i;
        }

        return -1;
    }

    private void OccupySlot(int index, string tag, int startAmount)
    {
        while (_slots.Count <= index)
            _slots.Add(new CoinSlot());

        CoinSlot slot = _slots[index];
        slot.Tag = tag;
        slot.Released = false;

        slot.Timer = new CoinTimer(this, index, tag, startAmount);

        // 显示层可能尚未进入场景，用关卡引用添加，保证计数条一定被调度
        (_level ?? Scene as Level)?.Add(slot.Timer);
    }

    /// <summary>
    /// 释放一个槽位，供排队中的标签立即占用。
    /// </summary>
    internal void ReleaseSlot(CoinSlot slot)
    {
        if (slot is null)
            return;

        slot.Released = true;
        slot.Tag = "";
        slot.Timer = null;

        if (_waiting.Count == 0)
            return;

        PendingCoin next = _waiting.Dequeue();

        // 排队期间累积的数量在出场时一次性体现：从开始排队时的值滚到最新值
        int freeIndex = FindFreeSlotIndex();
        OccupySlot(freeIndex >= 0 ? freeIndex : _slots.Count, next.Tag, next.StartAmount);
    }

    /// <summary>
    /// 关闭所有计数条并清空排队，用于离开关卡。
    /// </summary>
    public void ClearAll()
    {
        foreach (CoinSlot slot in _slots)
        {
            slot.Timer?.RemoveSelf();
            slot.Timer = null;
            slot.Released = true;
            slot.Tag = "";
        }

        _waiting.Clear();
    }

    /// <summary>
    /// 当前正在显示的计数条数量。
    /// </summary>
    public int ActiveSlotCount => _slots.Count(s => !s.Released);

    // ==================== 单条计数条 ====================

    /// <summary>
    /// 单条金币计数器：负责一条计数条的数字滚动与出入场动画，
    /// 数值始终从当前显示值滚动到该标签的最新金币数。
    /// </summary>
    internal class CoinTimer : Entity
    {
        /// <summary>数字变化的间隔时间。</summary>
        private const float NumberUpdateDelay = 0.4f;
        private const float ComboUpdateDelay = 0.3f;
        private const float AfterUpdateDelay = 1.7f;
        private const float LerpInSpeed = 1.2f;
        private const float LerpOutSpeed = 2f;

        private readonly CoinDisplay _owner;
        private readonly Level _level;
        private readonly int _slotIndex;
        private readonly string _tag;
        private readonly CoinSessionCounter _counter;

        private float _drawLerp;
        private int _target = -1;
        private bool _finished;

        public CoinTimer(CoinDisplay owner, int slotIndex, string tag, int startAmount)
        {
            _owner = owner;
            _level = owner._level;
            _slotIndex = slotIndex;
            _tag = tag;

            Depth = -101;
            Tag = Tags.HUD | Tags.PauseUpdate | Tags.TransitionUpdate | Tags.Global | Tags.Persistent;

            // 从收集前的数值开始，入场后滚动到最新值
            _counter = new CoinSessionCounter(tag, false, Math.Max(0, startAmount));
            Add(_counter);

            Y = SlotY();

            Add(new Coroutine(UpdateRoutine()));
        }

        /// <summary>
        /// 刷新目标数值，让数字从当前显示值继续滚动到最新值。
        /// </summary>
        public void Refresh(int target)
        {
            _target = target;
        }

        /// <summary>
        /// 该标签当前的金币数量（读自存档，经会话计数器镜像后一致）。
        /// </summary>
        private int CurrentValue()
        {
            string levelSet = (_level ?? Scene as Level).GetCurrentLevelSet();
            return CollectibleCoinUtils.GetCoinCount(levelSet, _tag);
        }

        /// <summary>
        /// 本槽位对应的屏幕 Y 坐标，保留速度跑计时器的位置补偿。
        /// </summary>
        private float SlotY()
        {
            Level level = _level ?? Scene as Level;
            float baseY = CWCModule.Settings.CorreCoinDisplayerY;

            if (level is not null && !level.TimerHidden)
            {
                baseY += Settings.Instance.SpeedrunClock switch
                {
                    SpeedrunType.Chapter => ChapterTimerOffset,
                    SpeedrunType.File => FileTimerOffset,
                    _ => 0f
                };
            }

            return baseY + _slotIndex * CWCModule.Settings.CorreCoinRowHeight;
        }

        public override void Update()
        {
            base.Update();

            if (!_finished)
            {
                _drawLerp = Math.Min(1f, _drawLerp + LerpInSpeed * Engine.RawDeltaTime);
            }
            else
            {
                _drawLerp -= LerpOutSpeed * Engine.RawDeltaTime;
                if (_drawLerp <= 0f)
                {
                    _owner.ReleaseSlot(_owner.FindSlot(this));
                    RemoveSelf();
                    return;
                }
            }

            Y = Calc.Approach(Y, SlotY(), Engine.DeltaTime * 800f);
        }

        private IEnumerator UpdateRoutine()
        {
            // 等待入场完成
            while (_drawLerp < 1f)
                yield return null;

            // 目标值实时读取：排队期间累积的数量会在出场时一次性体现
            _target = CurrentValue();

            if (_target > _counter.Amount)
            {
                yield return NumberUpdateDelay;

                while (true)
                {
                    int diff = _target - _counter.Amount;
                    if (diff <= 0)
                        break;

                    _counter.Amount += diff >= 6 ? 6 : diff >= 3 ? 3 : 1;
                    yield return ComboUpdateDelay;
                }

                yield return AfterUpdateDelay;
            }

            _finished = true;
        }

        public override void Render()
        {
            float y = Y;
            Vector2 from = new Vector2(-_owner.Background.Width, y);
            Vector2 to = new Vector2(32f, y);
            Vector2 pos = Vector2.Lerp(from, to, Ease.CubeOut(_drawLerp)).Round();

            _owner.Background.DrawJustified(pos + new Vector2(-96f, 12f), new Vector2(0f, 0.5f));
            _counter.Position = pos + new Vector2(0f, -y);
            _counter.Render();
        }
    }

    private CoinSlot FindSlot(CoinTimer timer)
        => _slots.FirstOrDefault(s => s.Timer == timer);
}
