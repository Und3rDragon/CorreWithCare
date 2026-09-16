using System;
using System.Collections.Generic;
using Celeste;
using CorreWithCare.Core;
using CorreWithCare.Utils;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using static CorreWithCare.Core.ExtendedAttributes;

namespace CorreWithCare.Entities;

/// <summary>
/// 单向屏障板的碰撞扩展：在对象移动过程中把屏障板纳入碰撞检测，
/// 使其仅在对象完全位于板体一侧且将与该侧接触时阻挡该方向的移动。
/// </summary>
public static class HoldableBarrierJumpthruHooks
{
    [Load]
    public static void Load()
    {
        IL.Celeste.Actor.MoveVExact += InjectBarriersInMoveVExact;
        IL.Celeste.Actor.MoveHExact += InjectBarriersInMoveHExact;
        On.Celeste.Actor.OnGround_int += OnActorOnGround;
    }

    [Unload]
    public static void Unload()
    {
        IL.Celeste.Actor.MoveVExact -= InjectBarriersInMoveVExact;
        IL.Celeste.Actor.MoveHExact -= InjectBarriersInMoveHExact;
        On.Celeste.Actor.OnGround_int -= OnActorOnGround;
    }

    /// <summary>
    /// 停在屏障板上的对象需要被判定为落地，否则会持续施加重力并反复触发碰撞。
    /// </summary>
    private static bool OnActorOnGround(On.Celeste.Actor.orig_OnGround_int orig, Actor self, int d)
    {
        if (orig(self, d))
            return true;

        if (self.IgnoreJumpThrus || d == 0)
            return false;

        return FindBarrier(self, Vector2.UnitY * Math.Sign(d), vertical: true) != null;
    }

    /// <summary>
    /// 取得符合条件的屏障板：对象未被举起、移动方向与板子阻挡方向一致，
    /// 且按该方向推进一像素后将与板子接触、接触时完全位于板体一侧。
    /// </summary>
    internal static HoldableJumpthru FindBarrier(Entity entity, Vector2 direction, bool vertical)
    {
        if (entity?.Scene == null)
            return null;

        // 绝大多数 Actor 没有 Holdable 组件，先行排除以避免无谓开销
        Holdable holdable = entity.Get<Holdable>();

        if (holdable == null || holdable.IsHeld)
            return null;

        // 直接取 Tracker 内部已维护的列表，避免每次查询都构造新集合
        if (!entity.Scene.Tracker.Entities.TryGetValue(typeof(HoldableJumpthru), out List<Entity> tracked)
            || tracked.Count == 0)
            return null;

        bool movingNegative = vertical ? direction.Y < 0f : direction.X < 0f;

        foreach (Entity trackedEntity in tracked)
        {
            if (trackedEntity is not HoldableJumpthru barrier || barrier.Scene == null)
                continue;

            if (barrier.IsHorizontal != vertical)
                continue;

            // 板子阻挡的是从自身所在侧压过来的移动，方向不符则穿过
            if (barrier.BlocksNegativeMovement != movingNegative)
                continue;

            if (barrier.WillTouch(entity.Collider, entity.Position, direction))
                return barrier;
        }

        return null;
    }

    private static void InjectBarriersInMoveVExact(ILContext il)
    {
        ILCursor cursor = new(il);

        VariableDefinition localPlatform = FindPlatformLocal(il);

        if (localPlatform == null)
        {
            Log.Warn("[HoldableBarrierJumpthru] MoveVExact 未找到 Platform 局部变量，屏障板在该轴上不生效");
            return;
        }

        if (!cursor.TryGotoNext(MoveType.AfterLabel, instr => instr.MatchLdarg(1), instr => instr.MatchLdcI4(0)))
        {
            Log.Warn("[HoldableBarrierJumpthru] MoveVExact 未匹配到移动量比较锚点，屏障板在该轴上不生效");
            return;
        }

        ILCursor jumpTarget = cursor.Clone();

        if (!jumpTarget.TryGotoNext(MoveType.AfterLabel,
                instr => instr.MatchLdarg(0), instr => instr.MatchLdflda<Actor>("movementCounter")))
        {
            Log.Warn("[HoldableBarrierJumpthru] MoveVExact 未匹配到 movementCounter 锚点，屏障板在该轴上不生效");
            return;
        }

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldarg_1);
        cursor.EmitDelegate<Func<Actor, int, HoldableJumpthru>>((self, moveV) =>
        {
            if (moveV == 0 || self.IgnoreJumpThrus)
                return null;

            return FindBarrier(self, Vector2.UnitY * Math.Sign(moveV), vertical: true);
        });

        cursor.Emit(OpCodes.Stloc, localPlatform);
        cursor.Emit(OpCodes.Ldloc, localPlatform);
        cursor.Emit(OpCodes.Brtrue, jumpTarget.Next);
    }

    private static void InjectBarriersInMoveHExact(ILContext il)
    {
        ILCursor cursor = new(il);

        if (!cursor.TryGotoNext(MoveType.After,
                instr => instr.MatchCall<Entity>("CollideFirst") &&
                         instr.Operand is Mono.Cecil.MethodReference method &&
                         method.Name == "CollideFirst" &&
                         method.DeclaringType?.Name == "Entity" &&
                         method.ToString().Contains("Solid")))
        {
            Log.Warn("[HoldableBarrierJumpthru] MoveHExact 未匹配到碰撞查询锚点，屏障板在该轴上不生效");
            return;
        }

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldarg_1);
        cursor.EmitDelegate<Func<Solid, Entity, int, Solid>>((orig, self, moveH) =>
        {
            if (orig != null || moveH == 0)
                return orig;

            HoldableJumpthru barrier = FindBarrier(self, Vector2.UnitX * Math.Sign(moveH), vertical: false);

            // 以无碰撞体积的替身返回，让引擎按原生碰撞流程处理并回调
            return barrier == null ? null : new DummySolid();
        });
    }

    /// <summary>
    /// 用于向引擎回报碰撞结果的替身，本身不带碰撞体积。
    /// </summary>
    private sealed class DummySolid : Solid
    {
        public DummySolid()
            : base(Vector2.Zero, 0f, 0f, safe: false)
        {
        }
    }

    private static VariableDefinition FindPlatformLocal(ILContext il)
    {
        foreach (VariableDefinition variable in il.Method.Body.Variables)
        {
            if (variable.VariableType.FullName == "Celeste.Platform" || variable.VariableType.Name == "Platform")
                return variable;
        }

        return null;
    }
}
