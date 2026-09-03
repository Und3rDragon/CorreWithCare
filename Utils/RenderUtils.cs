using System.Globalization;
using System.Numerics;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Vector4 = Microsoft.Xna.Framework.Vector4;

namespace CorreWithCare.Utils;

public static class RenderUtils
{
    #region 常量
    public const float Pi = MathHelper.Pi;
    public const float TwoPi = MathHelper.TwoPi;
    public const float PiOver2 = MathHelper.PiOver2;
    public const float PiOver4 = MathHelper.PiOver4;
    public const float E = MathF.E;
    #endregion

    #region INumber 通用扩展（float/int/long/double/short/byte/decimal）
    /// <summary>绝对值</summary>
    public static T Abs<T>(this T value) where T : INumber<T> => T.Abs(value);

    /// <summary>取符号 (-1, 0, 1)</summary>
    public static int Sign<T>(this T value) where T : INumber<T> => int.CreateChecked(T.Sign(value));

    /// <summary>约束到 [min, max] 范围</summary>
    public static T Clamp<T>(this T value, T min, T max) where T : INumber<T>
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    /// <summary>约束到 [0, 1] 范围</summary>
    public static T Saturate<T>(this T value) where T : INumber<T> => value.Clamp(T.Zero, T.One);

    /// <summary>最小值</summary>
    public static T Min<T>(this T value, T other) where T : INumber<T> => T.Min(value, other);

    /// <summary>最大值</summary>
    public static T Max<T>(this T value, T other) where T : INumber<T> => T.Max(value, other);

    /// <summary>平方根</summary>
    public static T Sqrt<T>(this T value) where T : IFloatingPointIeee754<T> => T.Sqrt(value);

    /// <summary>幂运算</summary>
    public static T Pow<T>(this T value, T exponent) where T : IFloatingPointIeee754<T> => T.Pow(value, exponent);

    /// <summary>e 的幂</summary>
    public static T Exp<T>(this T value) where T : IFloatingPointIeee754<T> => T.Exp(value);

    /// <summary>自然对数</summary>
    public static T Log<T>(this T value) where T : IFloatingPointIeee754<T> => T.Log(value);

    /// <summary>常用对数 (base 10)</summary>
    public static T Log10<T>(this T value) where T : IFloatingPointIeee754<T> => T.Log10(value);

    /// <summary>向下取整</summary>
    public static T Floor<T>(this T value) where T : IFloatingPointIeee754<T> => T.Floor(value);

    /// <summary>向上取整</summary>
    public static T Ceiling<T>(this T value) where T : IFloatingPointIeee754<T> => T.Ceiling(value);

    /// <summary>四舍五入</summary>
    public static T Round<T>(this T value) where T : IFloatingPointIeee754<T> => T.Round(value);

    /// <summary>正弦</summary>
    public static T Sin<T>(this T value) where T : IFloatingPointIeee754<T> => T.Sin(value);

    /// <summary>余弦</summary>
    public static T Cos<T>(this T value) where T : IFloatingPointIeee754<T> => T.Cos(value);

    /// <summary>正切</summary>
    public static T Tan<T>(this T value) where T : IFloatingPointIeee754<T> => T.Tan(value);

    /// <summary>反正弦</summary>
    public static T Asin<T>(this T value) where T : IFloatingPointIeee754<T> => T.Asin(value);

    /// <summary>反余弦</summary>
    public static T Acos<T>(this T value) where T : IFloatingPointIeee754<T> => T.Acos(value);

    /// <summary>反正切 (y/x)</summary>
    public static T Atan2<T>(this T y, T x) where T : IFloatingPointIeee754<T> => T.Atan2(y, x);

    /// <summary>角度转弧度</summary>
    public static T ToRadians<T>(this T degrees) where T : IFloatingPointIeee754<T> => degrees * (T.CreateChecked(MathHelper.Pi) / T.CreateChecked(180));

    /// <summary>弧度转角度</summary>
    public static T ToDegrees<T>(this T radians) where T : IFloatingPointIeee754<T> => radians * (T.CreateChecked(180) / T.CreateChecked(MathHelper.Pi));

    /// <summary>线性插值</summary>
    public static T Lerp<T>(this T from, T to, T amount) where T : IFloatingPointIeee754<T>
        => from + (to - from) * amount;

    /// <summary>逼近目标值（不超过最大增量）</summary>
    public static T Approach<T>(this T value, T target, T maxDelta) where T : INumber<T>
    {
        T diff = target - value;
        if (T.Abs(diff) <= maxDelta) return target;

        T sign = diff > T.Zero ? T.One : -T.One;
        return value + sign * maxDelta;
    }

    public static T Mod<T>(this T x, T m) where T : INumber<T>
    {
        return ((x % m) + m) % m;
    }

    public static T ClampMin<T>(this T x, T m) where T : INumber<T>
    {
        return x < m ? m : x;
    }

    public static T ClampMax<T>(this T x, T m) where T : INumber<T>
    {
        return x > m ? m : x;
    }
    #endregion

    #region float 专用扩展（需要 MathHelper/Calc/MathF 支持）
    /// <summary>角度逼近（仅 float，因为 Calc.AngleApproach 是 float）</summary>
    public static float AngleApproach(this float current, float target, float maxDelta)
        => Calc.AngleApproach(current, target, maxDelta);

    /// <summary>角度包装到 [-PI, PI]（仅 float）</summary>
    public static float WrapAngle(this float angle) => MathHelper.WrapAngle(angle);

    /// <summary>对齐到指定步长（仅 float）</summary>
    public static float Snap(this float value, float snap) => Calc.Snap(value, snap);
    #endregion

    #region Vector2 扩展
    /// <summary>获取向量角度（弧度）</summary>
    public static float Angle(this Vector2 vector) => MathF.Atan2(vector.Y, vector.X);

    /// <summary>向量乘以标量</summary>
    public static Vector2 Mul(this Vector2 vector, float scalar) => vector * scalar;

    /// <summary>向量除以标量</summary>
    public static Vector2 Div(this Vector2 vector, float scalar) => vector / scalar;

    /// <summary>向量相加</summary>
    public static Vector2 Add(this Vector2 vector, Vector2 other) => vector + other;

    /// <summary>向量相减</summary>
    public static Vector2 Sub(this Vector2 vector, Vector2 other) => vector - other;

    /// <summary>向量点积</summary>
    public static float Dot(this Vector2 vector, Vector2 other) => Vector2.Dot(vector, other);

    /// <summary>向量叉积（2D标量）</summary>
    public static float Cross(this Vector2 vector, Vector2 other) => vector.X * other.Y - vector.Y * other.X;

    /// <summary>向量距离</summary>
    public static float DistanceTo(this Vector2 from, Vector2 to) => Vector2.Distance(from, to);

    /// <summary>向量平方距离</summary>
    public static float DistanceSqTo(this Vector2 from, Vector2 to) => Vector2.DistanceSquared(from, to);

    /// <summary>向量方向到目标</summary>
    public static Vector2 DirectionTo(this Vector2 from, Vector2 to)
    {
        Vector2 dir = to - from;
        if (dir.LengthSquared() < 0.0001f) return Vector2.Zero;
        return Vector2.Normalize(dir);
    }

    /// <summary>旋转向量</summary>
    public static Vector2 Rotate(this Vector2 vector, float angle)
    {
        float cos = MathF.Cos(angle);
        float sin = MathF.Sin(angle);
        return new Vector2(
            vector.X * cos - vector.Y * sin,
            vector.X * sin + vector.Y * cos
        );
    }

    /// <summary>向量转向目标方向（平滑转向）</summary>
    public static Vector2 TurnTo(this Vector2 current, Vector2 target, float maxAngle)
    {
        float currentAngle = current.Angle();
        float targetAngle = target.Angle();
        float delta = targetAngle - currentAngle;
        while (delta > MathHelper.Pi) delta -= MathHelper.TwoPi;
        while (delta < -MathHelper.Pi) delta += MathHelper.TwoPi;
        float newAngle = currentAngle + MathHelper.Clamp(delta, -maxAngle, maxAngle);
        return new Vector2(MathF.Cos(newAngle), MathF.Sin(newAngle));
    }

    /// <summary>判断向量是否为零</summary>
    public static bool IsZero(this Vector2 vector) => vector.LengthSquared() < 0.0001f;

    /// <summary>安全归一化（零向量返回零）</summary>
    public static Vector2 SafeNormalize(this Vector2 vector)
    {
        float len = vector.Length();
        if (len < 0.0001f) return Vector2.Zero;
        return vector / len;
    }
    #endregion

    #region Color 扩展
    /// <summary>颜色乘以标量（变亮/变暗）</summary>
    public static Color Mul(this Color color, float scalar) =>
        new Color(
            (int)(color.R * scalar),
            (int)(color.G * scalar),
            (int)(color.B * scalar),
            color.A
        );

    /// <summary>颜色乘以颜色（混合）</summary>
    public static Color Mul(this Color color, Color other) =>
        new Color(
            (int)(color.R * other.R / 255f),
            (int)(color.G * other.G / 255f),
            (int)(color.B * other.B / 255f),
            (int)(color.A * other.A / 255f)
        );

    /// <summary>调整颜色亮度</summary>
    public static Color Brightness(this Color color, float amount) =>
        new Color(
            (int)MathHelper.Clamp(color.R * amount, 0, 255),
            (int)MathHelper.Clamp(color.G * amount, 0, 255),
            (int)MathHelper.Clamp(color.B * amount, 0, 255),
            color.A
        );

    /// <summary>调整颜色透明度</summary>
    public static Color WithAlpha(this Color color, float alpha) =>
        new Color(color.R, color.G, color.B, (int)(alpha * 255));

    /// <summary>调整颜色透明度</summary>
    public static Color WithAlpha(this Color color, int alpha) =>
        new Color(color.R, color.G, color.B, MathHelper.Clamp(alpha, 0, 255));

    /// <summary>将颜色转换为 Vector3 (RGB 0-1)</summary>
    public static Vector3 ToVector3(this Color color) =>
        new Vector3(color.R / 255f, color.G / 255f, color.B / 255f);

    /// <summary>将颜色转换为 Vector4 (RGBA 0-1)</summary>
    public static Vector4 ToVector4(this Color color) =>
        new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);

    /// <summary>从 Vector3 创建颜色</summary>
    public static Color FromVector3(this Vector3 vec) =>
        new Color(
            (int)(MathHelper.Clamp(vec.X, 0f, 1f) * 255),
            (int)(MathHelper.Clamp(vec.Y, 0f, 1f) * 255),
            (int)(MathHelper.Clamp(vec.Z, 0f, 1f) * 255)
        );

    /// <summary>从 Vector4 创建颜色</summary>
    public static Color FromVector4(this Vector4 vec) =>
        new Color(
            (int)(MathHelper.Clamp(vec.X, 0f, 1f) * 255),
            (int)(MathHelper.Clamp(vec.Y, 0f, 1f) * 255),
            (int)(MathHelper.Clamp(vec.Z, 0f, 1f) * 255),
            (int)(MathHelper.Clamp(vec.W, 0f, 1f) * 255)
        );

    /// <summary>从十六进制字符串解析颜色</summary>
    public static Color HexToColor(this string hex) => Calc.HexToColor(hex);

    /// <summary>从十六进制字符串解析颜色（含 Alpha）</summary>
    public static Color HexToColorWithAlpha(this string hex) => Calc.HexToColorWithAlpha(hex);

    /// <summary>颜色转十六进制字符串</summary>
    public static string ToHex(this Color color) => color.ToHex();

    /// <summary>颜色转十六进制字符串（含 Alpha）</summary>
    public static string ToHexWithAlpha(this Color color) => color.ToHexWithAlpha();
    public static Color Lerp(this Color a, Color b, float amount) => Color.Lerp(a, b, amount);
    public static Color Lerp(this float amount, Color a, Color b) => Color.Lerp(a, b, amount);
    #endregion

    #region 数字解析扩展
    /// <summary>
    /// 将字符串解析为数字类型（使用 INumber）
    /// 支持 int, float, double, long, short, decimal, byte 等
    /// </summary>
    public static bool TryParse<T>(this string s, out T value) where T : INumber<T>
    {
        value = T.Zero;

        if (string.IsNullOrWhiteSpace(s))
            return false;

        return T.TryParse(s.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>
    /// 将字符串解析为数字，失败时返回默认值
    /// </summary>
    public static T ParseOrDefault<T>(this string s, T defaultValue = default) where T : INumber<T>
    {
        return s.TryParse<T>(out T value) ? value : defaultValue;
    }
    #endregion

    #region 额外静态方法
    /// <summary>反正切 (y/x)</summary>
    public static float Atan2(this float y, float x) => MathF.Atan2(y, x);

    /// <summary>角度转弧度（float 版本）</summary>
    public static float ToRadians(this float degrees) => MathHelper.ToRadians(degrees);

    /// <summary>弧度转角度（float 版本）</summary>
    public static float ToDegrees(this float radians) => MathHelper.ToDegrees(radians);

    /// <summary>线性插值（float 版本）</summary>
    public static float Lerp(this float from, float to, float amount) => MathHelper.Lerp(from, to, amount);

    /// <summary>平滑插值（float 版本）</summary>
    public static float SmoothStep(this float from, float to, float amount) => MathHelper.SmoothStep(from, to, amount);
    #endregion
}