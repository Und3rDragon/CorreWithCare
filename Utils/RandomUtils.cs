
namespace CorreWithCare.Utils;

/// <summary>
/// 随机数工具。
/// </summary>
public static class RandomUtils
{
    public static int RandomSeed = new Random().Next();
    public static Random Random { get; set; } = new(RandomSeed);
    public static void RefreshRandom()
    {
        Random = new(RandomSeed);
    }
    public static Random Rand(object overrideValue = null)
    {
        if (overrideValue is null)
        {
            return Random;
        }

        if(overrideValue is int)
        {
            return new Random((int)overrideValue);
        }

        if(overrideValue is float || overrideValue is double || overrideValue is byte)
        {
            return new Random((int)overrideValue);
        }

        return new Random(overrideValue.ToString().GetHashCode());
    }

    public static void Rand(out Random random, object overrideValue = null)
    {
        random = Rand(overrideValue);
    }

    public static T[] RandomPick<T>(this T[] source, int count, int? seed = null)
    {
        return Rand(seed).GetItems(source, count < 1 ? 1 : count);
    }

    public static void RandomPick<T>(this T[] source, int count, out T[] picked, int? seed = null)
    {
        picked = source.RandomPick(count, seed);
    }

    public static T[] RandomPick<T>(this ReadOnlySpan<T> source, int count, int? seed = null)
    {
        return Rand(seed).GetItems(source, count < 1 ? 1 : count);
    }

    public static void RandomPick<T>(this ReadOnlySpan<T> source, int count, out T[] picked, int? seed = null)
    {
        picked = source.RandomPick(count, seed);
    }

    public static int RandomInt(int? seed = null)
    {
        return Rand(seed).Next();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="max">Exclusive max</param>
    /// <param name="seed"></param>
    /// <returns></returns>
    public static int RandomInt(int max, int? seed = null)
    {
        return Rand(seed).Next(max);
    }

    public static int RandomInt(int min, int max, int? seed = null)
    {
        return int.Min(min, max) + RandomInt(int.Max(min, max) - int.Min(min, max));
    }

    public static void RandomInt(out int N, int? min = null, int? max = null, int? seed = null)
    {
        if(min == null && max == null)
        {
            N = RandomInt(seed);
            return;
        }

        if (min == null)
        {
            N = RandomInt(max ?? 1, seed);
            return;
        }

        if (max == null)
        {
            N = RandomInt(min ?? 1, seed);
            return;
        }

        N = RandomInt(min ?? 0, max ?? 1, seed);
    }

    /// <summary>
    /// A random value 0 <= x < 1
    /// </summary>
    /// <param name="seed"></param>
    /// <returns></returns>
    public static float RandomFloat(int? seed = null)
    {
        return Rand(seed).NextFloat();
    }

    /// <summary>
    /// A random value 0 <= x < max
    /// </summary>
    /// <param name="max">Exclusive max</param>
    /// <param name="seed"></param>
    /// <returns></returns>
    public static float RandomFloat(float max, int? seed = null)
    {
        return RandomFloat(seed) * max;
    }

    public static float RandomFloat(float min, float max, int? seed = null)
    {
        return Calc.Min(min, max) + (Calc.Max(min, max) - Calc.Min(max, min)) * RandomFloat(seed);
    }

    public static void RandomFloat(out float N, float? min = null, float? max = null, int? seed = null)
    {
        if (min == null && max == null)
        {
            N = RandomFloat(seed);
            return;
        }

        if (min == null)
        {
            N = RandomFloat(max ?? 1f, seed);
            return;
        }

        if (max == null)
        {
            N = RandomFloat(min ?? 1f, seed);
        }

        N = RandomFloat(min ?? 0, max ?? 1f, seed);
    }
    
    /// <summary>
    /// A random value 0 <= x < 1
    /// </summary>
    /// <param name="seed"></param>
    /// <returns></returns>
    public static double RandomDouble(int? seed = null)
    {
        return Rand(seed).NextDouble();
    }

    /// <summary>
    /// A random value 0 <= x < max
    /// </summary>
    /// <param name="max">Exclusive max</param>
    /// <param name="seed"></param>
    /// <returns></returns>
    public static double RandomDouble(double max, int? seed = null)
    {
        return RandomDouble(seed) * max;
    }

    public static double RandomDouble(double min, double max, int? seed = null)
    {
        return double.Min(min, max) + (double.Max(min, max) - double.Min(max, min)) * RandomDouble(seed);
    }

    public static void RandomDouble(out double N, double? min = null, double? max = null, int? seed = null)
    {
        if (min == null && max == null)
        {
            N = RandomDouble(seed);
            return;
        }

        if (min == null)
        {
            N = RandomDouble(max ?? 1f, seed);
            return;
        }

        if (max == null)
        {
            N = RandomDouble(min ?? 1f, seed);
        }

        N = RandomDouble(min ?? 0, max ?? 1f, seed);
    }

    public static Vector2[] GetRandomPoints(Vector2 a, Vector2 b, int count, int? seed = null)
    {
        if((a - b).LengthSquared() < 0.01f) 
        { 
            return RandomPick(new Vector2[] { a, b }, count, seed);
        }

        Vector2 p1 = new Vector2(float.Min(a.X, b.X), float.Min(a.Y, b.Y)),
            p2 = new Vector2(float.Max(a.X, b.X), float.Max(a.Y, b.Y));

        Vector2[] points = new Vector2[count];
        for(int i = 0; i < count; i++)
        {
            Vector2 p = Vector2.Zero;
            p.X = RandomFloat(p1.X, p2.X, seed);
            p.Y = RandomFloat(p1.Y, p2.Y, seed);

            points[i] = p;
        }
        return points;
    }

    public static void GetRandomPoints(Vector2 a, Vector2 b, int count, out Vector2[] points, int? seed = null)
    {
        points = GetRandomPoints(a, b, count, seed);
    }
}
