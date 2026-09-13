namespace CorreWithCare.Utils;

public static class EaseUtils
{
    /// <summary>
    /// 存储每个 Ease.Easer 对应的整数索引（从 0 开始），便于 EntityData 等按 int 配置缓动。
    /// </summary>
    public struct EaseWrapper
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

        public EaseWrapper(int value)
        {
            Value = value;
        }

        public static implicit operator EaseWrapper(int value) => new(value);
        public static implicit operator int(EaseWrapper ease) => ease.Value;

        /// <summary>
        /// Easers → Ease.Easer：按索引取缓动函数。
        /// </summary>
        public static implicit operator Monocle.Ease.Easer(EaseWrapper ease) => EaseUtils.GetEase(ease);

        /// <summary>
        /// Ease.Easer → Easers：反向查找对应的索引；未知缓动回退 Linear。
        /// </summary>
        public static implicit operator EaseWrapper(Monocle.Ease.Easer easer)
        {
            if (easer == Monocle.Ease.Linear) return EaseWrapper.Linear;
            if (easer == Monocle.Ease.SineIn) return EaseWrapper.SineIn;
            if (easer == Monocle.Ease.SineOut) return EaseWrapper.SineOut;
            if (easer == Monocle.Ease.SineInOut) return EaseWrapper.SineInOut;
            if (easer == Monocle.Ease.QuadIn) return EaseWrapper.QuadIn;
            if (easer == Monocle.Ease.QuadOut) return EaseWrapper.QuadOut;
            if (easer == Monocle.Ease.QuadInOut) return EaseWrapper.QuadInOut;
            if (easer == Monocle.Ease.CubeIn) return EaseWrapper.CubeIn;
            if (easer == Monocle.Ease.CubeOut) return EaseWrapper.CubeOut;
            if (easer == Monocle.Ease.CubeInOut) return EaseWrapper.CubeInOut;
            if (easer == Monocle.Ease.QuintIn) return EaseWrapper.QuintIn;
            if (easer == Monocle.Ease.QuintOut) return EaseWrapper.QuintOut;
            if (easer == Monocle.Ease.QuintInOut) return EaseWrapper.QuintInOut;
            if (easer == Monocle.Ease.ExpoIn) return EaseWrapper.ExpoIn;
            if (easer == Monocle.Ease.ExpoOut) return EaseWrapper.ExpoOut;
            if (easer == Monocle.Ease.ExpoInOut) return EaseWrapper.ExpoInOut;
            if (easer == Monocle.Ease.BackIn) return EaseWrapper.BackIn;
            if (easer == Monocle.Ease.BackOut) return EaseWrapper.BackOut;
            if (easer == Monocle.Ease.BackInOut) return EaseWrapper.BackInOut;
            if (easer == Monocle.Ease.BigBackIn) return EaseWrapper.BigBackIn;
            if (easer == Monocle.Ease.BigBackOut) return EaseWrapper.BigBackOut;
            if (easer == Monocle.Ease.BigBackInOut) return EaseWrapper.BigBackInOut;
            if (easer == Monocle.Ease.ElasticIn) return EaseWrapper.ElasticIn;
            if (easer == Monocle.Ease.ElasticOut) return EaseWrapper.ElasticOut;
            if (easer == Monocle.Ease.ElasticInOut) return EaseWrapper.ElasticInOut;
            if (easer == Monocle.Ease.BounceIn) return EaseWrapper.BounceIn;
            if (easer == Monocle.Ease.BounceOut) return EaseWrapper.BounceOut;
            if (easer == Monocle.Ease.BounceInOut) return EaseWrapper.BounceInOut;
            return EaseWrapper.Linear;
        }
    }

    /// <summary>
    /// 根据 Easers 索引返回对应的 Ease.Easer；越界时回退为 Ease.Linear。
    /// </summary>
    public static Monocle.Ease.Easer GetEase(EaseWrapper ease)
    {
        return ease.Value switch
        {
            EaseWrapper.Linear => Monocle.Ease.Linear,
            EaseWrapper.SineIn => Monocle.Ease.SineIn,
            EaseWrapper.SineOut => Monocle.Ease.SineOut,
            EaseWrapper.SineInOut => Monocle.Ease.SineInOut,
            EaseWrapper.QuadIn => Monocle.Ease.QuadIn,
            EaseWrapper.QuadOut => Monocle.Ease.QuadOut,
            EaseWrapper.QuadInOut => Monocle.Ease.QuadInOut,
            EaseWrapper.CubeIn => Monocle.Ease.CubeIn,
            EaseWrapper.CubeOut => Monocle.Ease.CubeOut,
            EaseWrapper.CubeInOut => Monocle.Ease.CubeInOut,
            EaseWrapper.QuintIn => Monocle.Ease.QuintIn,
            EaseWrapper.QuintOut => Monocle.Ease.QuintOut,
            EaseWrapper.QuintInOut => Monocle.Ease.QuintInOut,
            EaseWrapper.ExpoIn => Monocle.Ease.ExpoIn,
            EaseWrapper.ExpoOut => Monocle.Ease.ExpoOut,
            EaseWrapper.ExpoInOut => Monocle.Ease.ExpoInOut,
            EaseWrapper.BackIn => Monocle.Ease.BackIn,
            EaseWrapper.BackOut => Monocle.Ease.BackOut,
            EaseWrapper.BackInOut => Monocle.Ease.BackInOut,
            EaseWrapper.BigBackIn => Monocle.Ease.BigBackIn,
            EaseWrapper.BigBackOut => Monocle.Ease.BigBackOut,
            EaseWrapper.BigBackInOut => Monocle.Ease.BigBackInOut,
            EaseWrapper.ElasticIn => Monocle.Ease.ElasticIn,
            EaseWrapper.ElasticOut => Monocle.Ease.ElasticOut,
            EaseWrapper.ElasticInOut => Monocle.Ease.ElasticInOut,
            EaseWrapper.BounceIn => Monocle.Ease.BounceIn,
            EaseWrapper.BounceOut => Monocle.Ease.BounceOut,
            EaseWrapper.BounceInOut => Monocle.Ease.BounceInOut,
            _ => Monocle.Ease.Linear,
        };
    }
}
