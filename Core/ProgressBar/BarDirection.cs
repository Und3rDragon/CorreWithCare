namespace CorreWithCare.Core.ProgressBar;

/// <summary>
/// 进度条的出没方向，决定进度条的走向（水平或垂直）以及从哪一侧进出。
/// </summary>
public struct BarDirection
{
    /// <summary>水平进度条，从屏幕上方进出。</summary>
    public const int FromTop = 0;
    /// <summary>水平进度条，从屏幕下方进出。</summary>
    public const int FromBottom = 1;
    /// <summary>垂直进度条，从屏幕左侧进出。</summary>
    public const int FromLeft = 2;
    /// <summary>垂直进度条，从屏幕右侧进出。</summary>
    public const int FromRight = 3;

    /// <summary>
    /// 该方向是否为垂直走向（从左/右侧进出）。
    /// </summary>
    public static bool IsVertical(int direction) => direction >= FromLeft;

    /// <summary>
    /// 该方向是否从正向（上方/左侧）进出。
    /// </summary>
    public static bool IsFromStart(int direction) => direction % 2 == FromTop;
}
