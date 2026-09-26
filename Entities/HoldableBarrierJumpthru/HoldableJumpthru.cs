using System;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod.Entities;
using CorreWithCare.Core;
using CorreWithCare.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using static CorreWithCare.Core.ExtendedAttributes;

namespace CorreWithCare.Entities;

/// <summary>
/// 单向屏障板：阻挡未被举起的 Holdable 组件实体穿过指定方向，玩家与其他实体不受影响。
///
/// texture — 贴图目录，目录内以 inner / outer 加连续编号命名的文件构成一套材质，
///           实体初始化时随机挑取一套渲染
/// depth   — 渲染层级
///
/// 板子按朝向选择拉伸轴：上下朝向拉伸宽度，左右朝向拉伸高度。
/// 四个朝向各自作为独立实体注册，共享同一套行为。
/// </summary>
[Tracked]
[CustomEntity(
    "CorreWithCare/HoldableJumpthruUp = LoadUp",
    "CorreWithCare/HoldableJumpthruDown = LoadDown",
    "CorreWithCare/HoldableJumpthruLeft = LoadLeft",
    "CorreWithCare/HoldableJumpthruRight = LoadRight"
)]
[exa.Credits("Viv for Holdable Barrier Jumpthrough open-source code" +
    "Maddie for sideways and upside-down jump through source code reference")]
public class HoldableJumpthru : JumpThru
{
    public static Entity LoadUp(Level level, LevelData levelData, vec2 offset, EntityData entityData)
    {
        return new HoldableJumpthru(entityData, offset, DirectionMode.Up);
    }

    public static Entity LoadDown(Level level, LevelData levelData, vec2 offset, EntityData entityData)
    {
        return new HoldableJumpthru(entityData, offset, DirectionMode.Down);
    }

    public static Entity LoadLeft(Level level, LevelData levelData, vec2 offset, EntityData entityData)
    {
        return new HoldableJumpthru(entityData, offset, DirectionMode.Left);
    }

    public static Entity LoadRight(Level level, LevelData levelData, vec2 offset, EntityData entityData)
    {
        return new HoldableJumpthru(entityData, offset, DirectionMode.Right);
    }

    private const int TileSize = 8;

    /// <summary>
    /// 碰撞盒厚度：与基类跳穿板的薄板厚度一致。
    /// </summary>
    private const float Thickness = 5f;

    private readonly DirectionMode direction;

    private readonly string texture;

    private readonly ccolor innerColor;

    private readonly ccolor outerColor;

    private readonly List<(MTexture Texture, vec2 Offset, Color Tint)> tiles = new();

    public HoldableJumpthru(EntityData data, vec2 offset, DirectionMode direction)
        : base(data.Position + offset,
              direction.IsHorizontal() ? data.Width : TileSize,
              safe: false)
    {
        this.direction = direction;

        texture = data.Attr("texture", "CorreWithCare/HoldableBarrierJumpthru/");

        innerColor = data.GetCorreColor("innerColor", new ccolor("ffffff99"));
        outerColor = data.GetCorreColor("outerColor", new ccolor("ffffffff"));

        Length = direction.IsHorizontal() ? data.Width : data.Height;

        // 基类建立的是贴格子顶边的横向薄板，其余朝向按板体位置调整碰撞盒
        Collider = direction switch
        {
            // 板体在下侧：薄板贴格子底边
            DirectionMode.Down => new Hitbox(data.Width, Thickness, 0f, TileSize - Thickness),

            // 板体在左侧：竖向薄板贴格子左边
            DirectionMode.Left => new Hitbox(Thickness, data.Height),

            // 板体在右侧：竖向薄板贴格子右边
            DirectionMode.Right => new Hitbox(Thickness, data.Height, TileSize - Thickness, 0f),

            _ => Collider
        };

        // 平时不参与常规碰撞，仅由碰撞钩子按位置条件纳入检测
        Collidable = false;

        Depth = data.Int("depth", -9000);
    }

    /// <summary>
    /// 板子沿拉伸轴的长度（上下朝向为宽度，左右朝向为高度）。
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// 是否为上下朝向（沿宽度拉伸，碰撞盒为横向）。
    /// </summary>
    public bool IsHorizontal => direction.IsHorizontal();

    /// <summary>
    /// 板体是否位于格子起始侧（上侧或左侧）。
    /// </summary>
    public bool IsOnStartSide => direction.IsOnStartSide();

    /// <summary>
    /// 该朝向阻挡的移动方向：为 true 时阻挡负方向移动（向上或向左）。
    /// </summary>
    public bool BlocksNegativeMovement => direction.BlocksNegativeMovement();

    /// <summary>
    /// 判断碰撞箱按给定方向推进一像素后是否会与本板接触，
    /// 且接触时完全位于板体所在的一侧。
    /// </summary>
    public bool WillTouch(Collider collider, vec2 entityPosition, vec2 direction)
    {
        // 推进后的碰撞箱范围
        float left = entityPosition.X + direction.X + collider.Left;
        float right = entityPosition.X + direction.X + collider.Right;
        float top = entityPosition.Y + direction.Y + collider.Top;
        float bottom = entityPosition.Y + direction.Y + collider.Bottom;

        // 板子占据的范围
        float barLeft = Left;
        float barRight = Right;
        float barTop = Top;
        float barBottom = Bottom;

        bool overlapsAlongBar = right > barLeft && left < barRight;

        // 向下推进：接触板子上表面
        if (direction.Y > 0f)
            return overlapsAlongBar && bottom > barTop && top < barTop;

        // 向上推进：接触板子下表面
        if (direction.Y < 0f)
            return overlapsAlongBar && top < barBottom && bottom > barBottom;

        bool overlapsAcrossBar = bottom > barTop && top < barBottom;

        // 向右推进：接触板子左表面
        if (direction.X > 0f)
            return overlapsAcrossBar && right > barLeft && left < barLeft;

        // 向左推进：接触板子右表面
        if (direction.X < 0f)
            return overlapsAcrossBar && left < barRight && right > barRight;

        return false;
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);

        List<MTexture> innerVariants = GFX.Game.GetAtlasSubtextures(texture + "inner");
        List<MTexture> outerVariants = GFX.Game.GetAtlasSubtextures(texture + "outer");

        // 图集配置了兜底贴图时，检索不到会返回兜底而非空列表，因此额外校验名称
        if (!IsValidVariants(innerVariants, "inner") || !IsValidVariants(outerVariants, "outer"))
        {
            Log.Warn($"[HoldableBarrierJumpthru] 贴图缺失，位置 {Position}，路径 {texture}");
            return;
        }

        // 在全部变体中随机挑一套；内层与外层数量不一致时按取模循环取用
        int setIndex = RandomUtils.Random.Next(innerVariants.Count);
        MTexture inner = innerVariants[setIndex];
        MTexture outer = outerVariants[setIndex % outerVariants.Count];

        int cells = Math.Max(1, Length / TileSize);
        int row = GetTextureRow();

        for (int i = 0; i < cells; i++)
        {
            int tileX = GetQuadIndex(i, cells) * TileSize;

            vec2 cell = GetCellPosition(i);
            tiles.Add((inner.GetSubtexture(tileX, row, TileSize, TileSize), cell, innerColor.Parsed()));
            tiles.Add((outer.GetSubtexture(tileX, row, TileSize, TileSize), cell, outerColor.Parsed()));
        }
    }

    /// <summary>
    /// 校验检索到的变体列表是否真的来自目标目录，用于排除图集兜底贴图。
    /// </summary>
    private bool IsValidVariants(List<MTexture> variants, string prefix)
    {
        if (variants.Count == 0 || variants[0]?.AtlasPath == null)
        {
            return false;
        }

        return variants[0].AtlasPath.StartsWith(texture + prefix, StringComparison.Ordinal);
    }

    /// <summary>
    /// 按邻居轴选取纵向两行之一：上下朝向检查左右邻居，左右朝向检查上下邻居。
    /// </summary>
    private int GetTextureRow()
    {
        return HasNeighbor() ? 0 : TileSize;
    }

    /// <summary>
    /// 取该格对应的素材分段：上下朝向沿 X 从左往右为 A 至 C，
    /// 左右朝向沿 Y 排布，向右朝向自上而下为 A 至 C，向左朝向则相反。
    /// </summary>
    private int GetQuadIndex(int index, int cells)
    {
        bool reversed = direction == DirectionMode.Left;

        if (index == 0)
            return reversed ? 2 : 0;

        if (index == cells - 1)
            return reversed ? 0 : 2;

        return 1;
    }

    /// <summary>
    /// 沿邻居检测轴检查板体前后相邻格是否有实体。
    /// </summary>
    private bool HasNeighbor()
    {
        int cells = Math.Max(1, Length / TileSize);
        vec2 start = Position;
        vec2 end = IsHorizontal
            ? Position + new Vector2((cells - 1) * TileSize, 0f)
            : Position + new Vector2(0f, (cells - 1) * TileSize);

        vec2 before = IsHorizontal ? start - Vector2.UnitX * TileSize : start - Vector2.UnitY * TileSize;
        vec2 after = IsHorizontal ? end + Vector2.UnitX * TileSize : end + Vector2.UnitY * TileSize;

        return CollideCheck<Solid>(before) || CollideCheck<JumpThru>(before)
            || CollideCheck<Solid>(after) || CollideCheck<JumpThru>(after);
    }

    public override void Render()
    {
        base.Render();

        // 以格中心为旋转轴心，绘制点取格中心，使旋转与翻转后内容落回同一格
        float rotation = direction switch
        {
            DirectionMode.Left => -MathHelper.PiOver2,
            DirectionMode.Right => MathHelper.PiOver2,
            _ => 0f
        };

        vec2 scale = direction == DirectionMode.Down ? new vec2(1f, -1f) : vec2.One;
        vec2 origin = new vec2(TileSize / 2f, TileSize / 2f);
        vec2 half = new vec2(TileSize / 2f, TileSize / 2f);

        foreach ((MTexture tile, vec2 offset, Color tint) in tiles)
        {
            tile.Draw(Position + offset + half, origin, tint, scale, rotation);
        }
    }

    /// <summary>
    /// 按朝向算出第 index 格在本地坐标中的位置：上下朝向沿宽度排布，左右朝向沿高度排布。
    /// </summary>
    private vec2 GetCellPosition(int index)
    {
        return direction.IsHorizontal()
            ? new Vector2(index * TileSize, 0f)
            : new Vector2(0f, index * TileSize);
    }
}
