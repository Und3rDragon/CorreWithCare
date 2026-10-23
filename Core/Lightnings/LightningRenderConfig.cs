using System;
using CorreWithCare.Utils;

namespace CorreWithCare.Core.Lightnings;

/// <summary>
/// 一套闪电外观参数。
///
/// 与单块闪电相关的参数（颜色、粗细、是否绘制边缘与内部电光）按块生效；
/// 受渲染器整体限制的参数（抖动间隔、整屏白闪、Glitch、Bloom、底色、环境音、深度）
/// 在整张地图上取优先级最高的配置。
///
/// 颜色使用 CorreColor 存储，实际取色时再解析，因此 alpha 会同时参与 RGB 的缩放。
/// </summary>
public class LightningRenderConfig
{
    public const string DefaultColorA = "fcf579ff";
    public const string DefaultColorB = "8cf7e2ff";
    public const string DefaultFillColor = "f7b2621a";

    private static LightningRenderConfig defaultConfig;

    public static LightningRenderConfig Default => defaultConfig ??= CreateDefault();

    /// <summary>主闪电线颜色</summary>
    public ccolor ColorA;

    /// <summary>细闪电线颜色</summary>
    public ccolor ColorB;

    /// <summary>细闪电的尺寸比例</summary>
    public float ScaleB;

    /// <summary>边缘闪电线粗细</summary>
    public float EdgeThickness;

    /// <summary>闪电区域内底色</summary>
    public ccolor FillColor;

    /// <summary>闪电抖动换种间隔</summary>
    public float SeedInterval;

    /// <summary>整屏白闪强度倍率</summary>
    public float FlashStrength;

    /// <summary>破箱时的 Glitch 强度</summary>
    public float GlitchStrength;

    /// <summary>破箱时的 Bloom 增强量</summary>
    public float BloomStrength;

    /// <summary>环境音路径，留空表示不播放</summary>
    public string Ambience;

    /// <summary>环境音音量</summary>
    public float AmbienceVolume;

    /// <summary>渲染深度</summary>
    public int Depth;

    /// <summary>是否绘制边缘闪电线</summary>
    public bool DrawEdges;

    /// <summary>是否绘制内部电光</summary>
    public bool DrawBody;

    private static LightningRenderConfig CreateDefault()
    {
        return new LightningRenderConfig
        {
            ColorA = new ccolor(DefaultColorA),
            ColorB = new ccolor(DefaultColorB),
            ScaleB = 0.35f,
            EdgeThickness = 1f,
            FillColor = new ccolor(DefaultFillColor),
            SeedInterval = 0.1f,
            FlashStrength = 1f,
            GlitchStrength = 0.15f,
            BloomStrength = 0.5f,
            Ambience = "event:/new_content/env/10_electricity",
            AmbienceVolume = 1f,
            Depth = -1000100,
            DrawEdges = true,
            DrawBody = true
        };
    }

    /// <summary>
    /// 从实体数据生成配置，缺省值与原版闪电一致。
    /// </summary>
    public static LightningRenderConfig FromData(EntityData data)
    {
        LightningRenderConfig fallback = Default;

        return new LightningRenderConfig
        {
            ColorA = data.PackColor("colorA", DefaultColorA),
            ColorB = data.PackColor("colorB", DefaultColorB),
            ScaleB = data.Float("scaleB", fallback.ScaleB),
            EdgeThickness = data.Float("edgeThickness", fallback.EdgeThickness),
            FillColor = data.PackColor("fillColor", DefaultFillColor),
            SeedInterval = Math.Max(0.01f, data.Float("seedInterval", fallback.SeedInterval)),
            FlashStrength = data.Float("flashStrength", fallback.FlashStrength),
            GlitchStrength = data.Float("glitchStrength", fallback.GlitchStrength),
            BloomStrength = data.Float("bloomStrength", fallback.BloomStrength),
            Ambience = data.Attr("ambience", fallback.Ambience),
            AmbienceVolume = data.Float("ambienceVolume", fallback.AmbienceVolume),
            Depth = data.Int("depth", fallback.Depth),
            DrawEdges = data.Bool("drawEdges", fallback.DrawEdges),
            DrawBody = data.Bool("drawBody", fallback.DrawBody)
        };
    }
}
