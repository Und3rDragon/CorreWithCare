namespace CorreWithCare.Utils;

public static class EaseUtils
{
    /// <summary>
    /// 存储每个 Ease.Easer 对应的整数索引（从 0 开始），便于 EntityData 等按 int 配置缓动。
    /// </summary>
    public struct Easers
    {
        public const int Linear = 0;
        public const int SineIn = 1;
        public const int SineOut = 2;
        public const int SineInOut = 3;
        public const int QuadIn = 4;
        public const int QuadOut = 5;
        public const int QuadInOut = 6;
        public const int CubeIn = 7;
        public const int CubeOut = 8;
        public const int CubeInOut = 9;
        public const int QuintIn = 10;
        public const int QuintOut = 11;
        public const int QuintInOut = 12;
        public const int ExpoIn = 13;
        public const int ExpoOut = 14;
        public const int ExpoInOut = 15;
        public const int BackIn = 16;
        public const int BackOut = 17;
        public const int BackInOut = 18;
        public const int BigBackIn = 19;
        public const int BigBackOut = 20;
        public const int BigBackInOut = 21;
        public const int ElasticIn = 22;
        public const int ElasticOut = 23;
        public const int ElasticInOut = 24;
        public const int BounceIn = 25;
        public const int BounceOut = 26;
        public const int BounceInOut = 27;

        public int Value;

        public Easers(int value)
        {
            Value = value;
        }

        public static implicit operator Easers(int value) => new(value);
        public static implicit operator int(Easers ease) => ease.Value;

        /// <summary>
        /// Easers → Ease.Easer：按索引取缓动函数。
        /// </summary>
        public static implicit operator Monocle.Ease.Easer(Easers ease) => EaseUtils.GetEase(ease);

        /// <summary>
        /// Ease.Easer → Easers：反向查找对应的索引；未知缓动回退 Linear。
        /// </summary>
        public static implicit operator Easers(Monocle.Ease.Easer easer)
        {
            if (easer == Monocle.Ease.Linear) return Easers.Linear;
            if (easer == Monocle.Ease.SineIn) return Easers.SineIn;
            if (easer == Monocle.Ease.SineOut) return Easers.SineOut;
            if (easer == Monocle.Ease.SineInOut) return Easers.SineInOut;
            if (easer == Monocle.Ease.QuadIn) return Easers.QuadIn;
            if (easer == Monocle.Ease.QuadOut) return Easers.QuadOut;
            if (easer == Monocle.Ease.QuadInOut) return Easers.QuadInOut;
            if (easer == Monocle.Ease.CubeIn) return Easers.CubeIn;
            if (easer == Monocle.Ease.CubeOut) return Easers.CubeOut;
            if (easer == Monocle.Ease.CubeInOut) return Easers.CubeInOut;
            if (easer == Monocle.Ease.QuintIn) return Easers.QuintIn;
            if (easer == Monocle.Ease.QuintOut) return Easers.QuintOut;
            if (easer == Monocle.Ease.QuintInOut) return Easers.QuintInOut;
            if (easer == Monocle.Ease.ExpoIn) return Easers.ExpoIn;
            if (easer == Monocle.Ease.ExpoOut) return Easers.ExpoOut;
            if (easer == Monocle.Ease.ExpoInOut) return Easers.ExpoInOut;
            if (easer == Monocle.Ease.BackIn) return Easers.BackIn;
            if (easer == Monocle.Ease.BackOut) return Easers.BackOut;
            if (easer == Monocle.Ease.BackInOut) return Easers.BackInOut;
            if (easer == Monocle.Ease.BigBackIn) return Easers.BigBackIn;
            if (easer == Monocle.Ease.BigBackOut) return Easers.BigBackOut;
            if (easer == Monocle.Ease.BigBackInOut) return Easers.BigBackInOut;
            if (easer == Monocle.Ease.ElasticIn) return Easers.ElasticIn;
            if (easer == Monocle.Ease.ElasticOut) return Easers.ElasticOut;
            if (easer == Monocle.Ease.ElasticInOut) return Easers.ElasticInOut;
            if (easer == Monocle.Ease.BounceIn) return Easers.BounceIn;
            if (easer == Monocle.Ease.BounceOut) return Easers.BounceOut;
            if (easer == Monocle.Ease.BounceInOut) return Easers.BounceInOut;
            return Easers.Linear;
        }
    }

    /// <summary>
    /// 根据 Easers 索引返回对应的 Ease.Easer；越界时回退为 Ease.Linear。
    /// </summary>
    public static Monocle.Ease.Easer GetEase(Easers ease)
    {
        return ease.Value switch
        {
            Easers.Linear => Monocle.Ease.Linear,
            Easers.SineIn => Monocle.Ease.SineIn,
            Easers.SineOut => Monocle.Ease.SineOut,
            Easers.SineInOut => Monocle.Ease.SineInOut,
            Easers.QuadIn => Monocle.Ease.QuadIn,
            Easers.QuadOut => Monocle.Ease.QuadOut,
            Easers.QuadInOut => Monocle.Ease.QuadInOut,
            Easers.CubeIn => Monocle.Ease.CubeIn,
            Easers.CubeOut => Monocle.Ease.CubeOut,
            Easers.CubeInOut => Monocle.Ease.CubeInOut,
            Easers.QuintIn => Monocle.Ease.QuintIn,
            Easers.QuintOut => Monocle.Ease.QuintOut,
            Easers.QuintInOut => Monocle.Ease.QuintInOut,
            Easers.ExpoIn => Monocle.Ease.ExpoIn,
            Easers.ExpoOut => Monocle.Ease.ExpoOut,
            Easers.ExpoInOut => Monocle.Ease.ExpoInOut,
            Easers.BackIn => Monocle.Ease.BackIn,
            Easers.BackOut => Monocle.Ease.BackOut,
            Easers.BackInOut => Monocle.Ease.BackInOut,
            Easers.BigBackIn => Monocle.Ease.BigBackIn,
            Easers.BigBackOut => Monocle.Ease.BigBackOut,
            Easers.BigBackInOut => Monocle.Ease.BigBackInOut,
            Easers.ElasticIn => Monocle.Ease.ElasticIn,
            Easers.ElasticOut => Monocle.Ease.ElasticOut,
            Easers.ElasticInOut => Monocle.Ease.ElasticInOut,
            Easers.BounceIn => Monocle.Ease.BounceIn,
            Easers.BounceOut => Monocle.Ease.BounceOut,
            Easers.BounceInOut => Monocle.Ease.BounceInOut,
            _ => Monocle.Ease.Linear,
        };
    }
}
