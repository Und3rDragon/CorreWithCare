namespace CorreWithCare.Utils;

/// <summary>
/// 实体朝向：按板体在格子中的位置命名。
///
/// Up 表示板体位于格子上侧，Down 位于下侧，Left 位于左侧，Right 位于右侧。
/// 上下朝向沿宽度拉伸，左右朝向沿高度拉伸。
/// </summary>
public enum DirectionMode
{
    Up,
    Down,
    Left,
    Right
}

public static class DirectionModeExtensions
{
    /// <summary>
    /// 判断该朝向是否为上下朝向（沿宽度拉伸，碰撞盒为横向）。
    /// </summary>
    public static bool IsHorizontal(this DirectionMode direction)
    {
        return direction == DirectionMode.Up || direction == DirectionMode.Down;
    }

    /// <summary>
    /// 判断该朝向的板体是否位于格子起始侧（上侧或左侧）。
    /// </summary>
    public static bool IsOnStartSide(this DirectionMode direction)
    {
        return direction == DirectionMode.Up || direction == DirectionMode.Left;
    }

    /// <summary>
    /// 判断该朝向阻挡的移动方向：为 true 时阻挡负方向移动（向上或向左），
    /// 为 false 时阻挡正方向移动（向下或向右）。
    ///
    /// 板体位于哪一侧，就阻挡从该侧压过来的移动：
    /// 板体在上侧（Up）时阻挡向下移动，在下侧（Down）时阻挡向上移动。
    /// </summary>
    public static bool BlocksNegativeMovement(this DirectionMode direction)
    {
        return direction switch
        {
            // 板体在上侧：物体从上往下压被挡住，即阻挡正方向移动
            DirectionMode.Up => false,

            // 板体在下侧：物体从下往上顶被挡住，即阻挡负方向移动
            DirectionMode.Down => true,

            // 板体在左侧：物体从左往右压被挡住，即阻挡正方向移动
            DirectionMode.Left => false,

            // 板体在右侧：物体从右往左压被挡住，即阻挡负方向移动
            DirectionMode.Right => true,

            _ => false
        };
    }
}
