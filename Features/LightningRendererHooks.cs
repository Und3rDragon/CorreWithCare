using System;
using CorreWithCare.Core.Lightnings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static CorreWithCare.Core.ExtendedAttributes;

namespace CorreWithCare.Features;

/// <summary>
/// 闪电渲染接管：让每块闪电按各自的配置绘制。
///
/// 没有放置任何 Lightning 外观控制器时直接走原版实现，行为与原版完全一致。
/// </summary>
public static class LightningRendererHooks
{
    private const int BodyBufferSize = 160;

    private static LightningRenderConfig appliedBoltConfig;

    [Load]
    public static void Load()
    {
        On.Celeste.LightningRenderer.Update += OnRendererUpdate;
        On.Celeste.LightningRenderer.Render += OnRendererRender;
        On.Celeste.LightningRenderer.OnRenderBloom += OnRendererRenderBloom;
        On.Celeste.LightningRenderer.OnBeforeRender += OnRendererBeforeRender;
        On.Celeste.LightningRenderer.StartAmbience += OnRendererStartAmbience;
        On.Celeste.Lightning.SetBreakValue += OnSetBreakValue;
    }

    [Unload]
    public static void Unload()
    {
        On.Celeste.LightningRenderer.Update -= OnRendererUpdate;
        On.Celeste.LightningRenderer.Render -= OnRendererRender;
        On.Celeste.LightningRenderer.OnRenderBloom -= OnRendererRenderBloom;
        On.Celeste.LightningRenderer.OnBeforeRender -= OnRendererBeforeRender;
        On.Celeste.LightningRenderer.StartAmbience -= OnRendererStartAmbience;
        On.Celeste.Lightning.SetBreakValue -= OnSetBreakValue;

        appliedBoltConfig = null;
    }

    /// <summary>
    /// 换关卡时同步登记表，避免上一关的配置残留。
    /// </summary>
    private static void SyncLevel(Scene scene)
    {
        if (scene is Level level)
            LightningControllerRegistry.EnsureLevel(level);
    }

    /// <summary>
    /// 把渲染器级参数立即应用到渲染器上。
    /// </summary>
    public static void ApplyGlobal(Level level)
    {
        LightningRenderer renderer = level?.Tracker.GetEntity<LightningRenderer>();

        if (renderer == null)
            return;

        renderer.Depth = (LightningControllerRegistry.Global ?? LightningRenderConfig.Default).Depth;
    }

    private static void OnRendererUpdate(On.Celeste.LightningRenderer.orig_Update orig, LightningRenderer self)
    {
        SyncLevel(self.Scene);

        if (!LightningControllerRegistry.HasAny)
        {
            orig(self);
            return;
        }

        if (self.dirty)
        {
            self.RebuildEdges();
        }

        self.ToggleEdges();

        if (self.list.Count <= 0)
            return;

        foreach (LightningRenderer.Bolt bolt in self.bolts)
        {
            bolt.Update(self.Scene);
        }

        if (!self.UpdateSeeds)
            return;

        LightningRenderConfig config = LightningControllerRegistry.Global ?? LightningRenderConfig.Default;

        if (self.Scene.OnInterval(config.SeedInterval))
        {
            self.edgeSeed = (uint)Calc.Random.Next();
        }

        if (self.Scene.OnInterval(0.7f))
        {
            self.leapSeed = (uint)Calc.Random.Next();
        }
    }

    private static void OnRendererRender(On.Celeste.LightningRenderer.orig_Render orig, LightningRenderer self)
    {
        SyncLevel(self.Scene);

        if (!LightningControllerRegistry.HasAny || self.list.Count <= 0)
        {
            orig(self);
            return;
        }

        foreach (Lightning item in self.list)
        {
            if (!item.Visible)
                continue;

            LightningRenderConfig bodyConfig = LightningControllerRegistry.Resolve(item) ?? LightningRenderConfig.Default;

            if (!bodyConfig.DrawBody)
                continue;

            Draw.SpriteBatch.Draw((RenderTarget2D)GameplayBuffers.Lightning, item.Position,
                new Rectangle((int)item.X, (int)item.Y, item.VisualWidth, item.VisualHeight), Color.White);
        }

        if (self.edges.Count <= 0 || !self.DrawEdges)
            return;

        int index = 0;
        uint seed = self.leapSeed;

        foreach (LightningRenderer.Edge edge in self.edges)
        {
            if (!edge.Visible)
                continue;

            LightningRenderConfig config = LightningControllerRegistry.Resolve(edge.Parent) ?? LightningRenderConfig.Default;

            if (!config.DrawEdges)
                continue;

            Color colorA = Color.Lerp(config.ColorA.Parsed(), Color.White, self.Fade);
            Color colorB = Color.Lerp(config.ColorB.Parsed(), Color.White, self.Fade);
            float thickness = config.EdgeThickness * (1f + self.Fade * 3f);

            LightningRenderer.DrawSimpleLightning(ref index, ref self.edgeVerts, self.edgeSeed,
                edge.Parent.Position, edge.A, edge.B, colorA, thickness);
            LightningRenderer.DrawSimpleLightning(ref index, ref self.edgeVerts, self.edgeSeed + 1,
                edge.Parent.Position, edge.A, edge.B, colorB, thickness);

            if (LightningRenderer.PseudoRand(ref seed) % 30 == 0)
            {
                LightningRenderer.DrawBezierLightning(ref index, ref self.edgeVerts, self.edgeSeed,
                    edge.Parent.Position, edge.A, edge.B, 24f, 10, colorB);
            }
        }

        if (index <= 0)
            return;

        GameplayRenderer.End();
        GFX.DrawVertices((self.Scene as Level).Camera.Matrix, self.edgeVerts, index);
        GameplayRenderer.Begin();
    }

    private static void OnRendererRenderBloom(On.Celeste.LightningRenderer.orig_OnRenderBloom orig, LightningRenderer self)
    {
        SyncLevel(self.Scene);

        if (!LightningControllerRegistry.HasAny)
        {
            orig(self);
            return;
        }

        Camera camera = (self.Scene as Level).Camera;
        Color color = Color.White * (0.25f + self.Fade * 0.75f);

        foreach (LightningRenderer.Edge edge in self.edges)
        {
            if (!edge.Visible)
                continue;

            LightningRenderConfig config = LightningControllerRegistry.Resolve(edge.Parent) ?? LightningRenderConfig.Default;

            if (!config.DrawEdges)
                continue;

            Draw.Line(edge.Parent.Position + edge.A, edge.Parent.Position + edge.B, color, 4f);
        }

        foreach (Lightning item in self.list)
        {
            if (!item.Visible)
                continue;

            LightningRenderConfig config = LightningControllerRegistry.Resolve(item) ?? LightningRenderConfig.Default;

            if (!config.DrawBody)
                continue;

            Draw.Rect(item.X, item.Y, item.VisualWidth, item.VisualHeight, color);
        }

        if (self.Fade > 0f)
        {
            LightningRenderConfig global = LightningControllerRegistry.Global ?? LightningRenderConfig.Default;
            Draw.Rect(camera.X, camera.Y, 320f, 180f, Color.White * (self.Fade * global.FlashStrength));
        }
    }

    private static void OnRendererBeforeRender(On.Celeste.LightningRenderer.orig_OnBeforeRender orig, LightningRenderer self)
    {
        SyncLevel(self.Scene);

        EnsureBolts(self, LightningControllerRegistry.HasAny
            ? LightningControllerRegistry.Global ?? LightningRenderConfig.Default
            : LightningRenderConfig.Default);

        if (!LightningControllerRegistry.HasAny || self.list.Count <= 0)
        {
            orig(self);
            return;
        }

        LightningRenderConfig config = LightningControllerRegistry.Global ?? LightningRenderConfig.Default;

        Engine.Graphics.GraphicsDevice.SetRenderTarget(GameplayBuffers.Lightning);
        Engine.Graphics.GraphicsDevice.Clear(Color.Lerp(config.FillColor.Parsed(), Color.White, self.Fade));
        Draw.SpriteBatch.Begin();

        foreach (LightningRenderer.Bolt bolt in self.bolts)
        {
            bolt.Render();
        }

        Draw.SpriteBatch.End();
    }

    private static void OnRendererStartAmbience(On.Celeste.LightningRenderer.orig_StartAmbience orig, LightningRenderer self)
    {
        SyncLevel(self.Scene);

        if (!LightningControllerRegistry.HasAny)
        {
            orig(self);
            return;
        }

        LightningRenderConfig config = LightningControllerRegistry.Global ?? LightningRenderConfig.Default;

        if (string.IsNullOrWhiteSpace(config.Ambience))
        {
            self.AmbientSfx.Stop();
            return;
        }

        if (!self.AmbientSfx.Playing)
        {
            self.AmbientSfx.Play(config.Ambience, null, config.AmbienceVolume);
        }
    }

    private static void OnSetBreakValue(On.Celeste.Lightning.orig_SetBreakValue orig, Level level, float t)
    {
        orig(level, t);

        if (level == null)
            return;

        LightningControllerRegistry.EnsureLevel(level);

        if (!LightningControllerRegistry.HasAny)
            return;

        LightningRenderConfig config = LightningControllerRegistry.Global ?? LightningRenderConfig.Default;

        Glitch.Value = MathHelper.Lerp(0f, config.GlitchStrength, t);
        level.Bloom.Strength = MathHelper.Lerp(1f, 1f + config.BloomStrength, t);
    }

    /// <summary>
    /// 按配置同步内部电光的颜色与尺寸。
    /// </summary>
    private static void EnsureBolts(LightningRenderer self, LightningRenderConfig config)
    {
        if (self.bolts.Count <= 0 || ReferenceEquals(appliedBoltConfig, config))
            return;

        // 从一开始就没有控制器时保持渲染器自己创建的电光
        if (appliedBoltConfig == null && ReferenceEquals(config, LightningRenderConfig.Default))
        {
            appliedBoltConfig = config;
            return;
        }

        appliedBoltConfig = config;
        self.bolts.Clear();

        for (int i = 0; i < 4; i++)
        {
            self.bolts.Add(new LightningRenderer.Bolt(config.ColorA.Parsed(), 1f, BodyBufferSize, BodyBufferSize));
            self.bolts.Add(new LightningRenderer.Bolt(config.ColorB.Parsed(), config.ScaleB, BodyBufferSize, BodyBufferSize));
        }
    }
}
