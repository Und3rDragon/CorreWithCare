using System;
using System.Collections.Generic;
using CorreWithCare.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace CorreWithCare.Utils.MaskEffect;

/// <summary>
/// 以一张或多张贴图为画布、另一张贴图为内容，按逐像素透明度做剪切蒙版。<br/>
/// 画布决定输出尺寸与每个像素的保留比例：画布完全不透明的像素保留内容原样，<br/>
/// 完全透明的像素裁掉内容，半透明的像素让内容也变为同等透明。<br/>
/// 测试可用，但是还是别用吧，太卡了
/// </summary>
public static class ObjectMask
{
    /// <summary>
    /// 记录单画布裁剪的结果贴图，键为内容与画布的组合。
    /// </summary>
    private static readonly Dictionary<(MTexture data, MTexture reference), MTexture> cache = new();

    /// <summary>
    /// 记录多画布裁剪的结果贴图，键由内容、画布集合与偏移集合共同决定。
    /// </summary>
    private static readonly Dictionary<(MTexture data, string key), MTexture> multiCache = new();

    /// <summary>
    /// 以 reference 为画布裁剪 data，返回一张新贴图。data 按 offset 摆放在画布坐标系中，
    /// 落在画布之外的像素被裁掉，画布内没有 data 覆盖的像素保持透明。
    /// </summary>
    public static MTexture Create(MTexture data, MTexture reference, Point offset)
    {
        return Create(data, reference, new Vector2(offset.X, offset.Y));
    }

    /// <summary>
    /// 以 reference 为画布裁剪 data，返回一张新贴图。data 按 offset 摆放在画布坐标系中，
    /// 落在画布之外的像素被裁掉，画布内没有 data 覆盖的像素保持透明。
    /// </summary>
    public static MTexture Create(MTexture data, MTexture reference, Vector2 offset = default)
    {
        if (data == null || reference == null)
        {
            return null;
        }

        if (!cache.TryGetValue((data, reference), out MTexture result))
        {
            Canvas canvas = new(reference.Width, reference.Height);
            Blend(canvas, data, reference, offset);
            result = Build(canvas);
            cache[(data, reference)] = result;
        }

        return result;
    }

    /// <summary>
    /// 以多张 reference 的共同画布裁剪 data，返回一张新贴图。offsets 逐项给出 data 相对
    /// 每张画布的位置，data 因此以原点为基准被裁切多次，各次结果按 alpha 取大值合并。
    /// </summary>
    public static MTexture Create(this MTexture data, MTexture[] references, Point[] offsets)
    {
        if (offsets == null)
        {
            return Create(data, references, Array.Empty<Vector2>());
        }

        Vector2[] converted = new Vector2[offsets.Length];
        for (int i = 0; i < offsets.Length; i++)
        {
            converted[i] = new Vector2(offsets[i].X, offsets[i].Y);
        }

        return Create(data, references, converted);
    }

    /// <summary>
    /// 以多张 reference 的共同画布裁剪 data，返回一张新贴图。offsets 逐项给出 data 相对
    /// 每张画布的位置，data 因此以原点为基准被裁切多次，各次结果按 alpha 取大值合并。
    /// </summary>
    public static MTexture Create(this MTexture data, MTexture[] references, Vector2[] offsets)
    {
        if (data == null || references == null || offsets == null)
        {
            return null;
        }

        int count = Math.Min(references.Length, offsets.Length);
        if (count <= 0)
        {
            return null;
        }

        if (references.Length != offsets.Length)
        {
            Log.Warn($"{nameof(ObjectMask)}: references 与 offsets 数量不一致，仅处理前 {count} 项。");
        }

        string key = BuildKey(references, offsets, count);
        if (!multiCache.TryGetValue((data, key), out MTexture result))
        {
            result = Build(CompositeMultiple(data, references, offsets, count));
            multiCache[(data, key)] = result;
        }

        return result;
    }

    /// <summary>
    /// 清空缓存。贴图被卸载或替换后调用，避免持有失效引用。
    /// </summary>
    public static void ClearCache()
    {
        cache.Clear();
        multiCache.Clear();
    }

    /// <summary>
    /// 逐张画布裁切 data 并合并到同一画布上。data 固定位于原点，各画布按其偏移的相反数落位，
    /// 画布共同覆盖的范围即为输出范围。
    /// </summary>
    private static Canvas CompositeMultiple(MTexture data, MTexture[] references, Vector2[] offsets, int count)
    {
        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;

        for (int i = 0; i < count; i++)
        {
            int originX = -(int)Math.Round(offsets[i].X);
            int originY = -(int)Math.Round(offsets[i].Y);
            minX = Math.Min(minX, originX);
            minY = Math.Min(minY, originY);
            maxX = Math.Max(maxX, originX + references[i].Width);
            maxY = Math.Max(maxY, originY + references[i].Height);
        }

        Canvas canvas = new(maxX - minX, maxY - minY);
        for (int i = 0; i < count; i++)
        {
            int originX = -(int)Math.Round(offsets[i].X) - minX;
            int originY = -(int)Math.Round(offsets[i].Y) - minY;
            Blend(canvas, data, references[i], offsets[i], originX, originY);
        }

        return canvas;
    }

    /// <summary>
    /// 用一张画布裁切 data 并叠到目标画布上。alpha 更大的一侧保留其内容像素。
    /// </summary>
    private static void Blend(Canvas canvas, MTexture data, MTexture reference, Vector2 offset, int pasteX = 0, int pasteY = 0)
    {
        Color[] referencePixels = ReadPixels(reference);
        Color[] dataPixels = ReadPixels(data);

        int shiftX = (int)Math.Round(offset.X);
        int shiftY = (int)Math.Round(offset.Y);
        int dataWidth = data.Width;
        int dataHeight = data.Height;

        for (int y = 0; y < reference.Height; y++)
        {
            int canvasY = pasteY + y;
            if (canvasY < 0 || canvasY >= canvas.Height)
            {
                continue;
            }

            for (int x = 0; x < reference.Width; x++)
            {
                int canvasX = pasteX + x;
                if (canvasX < 0 || canvasX >= canvas.Width)
                {
                    continue;
                }

                Color referencePixel = referencePixels[GetPixelIndex(reference, x, y)];
                ColorUtils.CorreColor mask = new(referencePixel.R, referencePixel.G, referencePixel.B, referencePixel.A);
                if (mask.alpha <= 0f)
                {
                    continue;
                }

                int sourceX = x - shiftX;
                int sourceY = y - shiftY;
                if (sourceX < 0 || sourceY < 0 || sourceX >= dataWidth || sourceY >= dataHeight)
                {
                    continue;
                }

                Color source = dataPixels[GetPixelIndex(data, sourceX, sourceY)];
                byte alpha = (byte)(source.A * mask.alpha);
                if (alpha <= canvas.Pixels[canvasY * canvas.Width + canvasX].A)
                {
                    continue;
                }

                source.A = alpha;
                canvas.Pixels[canvasY * canvas.Width + canvasX] = source;
            }
        }
    }

    /// <summary>
    /// 读取贴图的完整颜色数据。子贴图与画布共享同一张底层纹理，因此按底层纹理尺寸读取。
    /// </summary>
    private static Color[] ReadPixels(MTexture texture)
    {
        Texture2D source = texture.Texture.Texture_Safe;
        Color[] pixels = new Color[source.Width * source.Height];
        source.GetData(pixels);
        return pixels;
    }

    /// <summary>
    /// 将贴图坐标系中的像素位置换算为底层纹理的数据下标。
    /// </summary>
    private static int GetPixelIndex(MTexture texture, int x, int y)
    {
        Texture2D source = texture.Texture.Texture_Safe;
        return (texture.ClipRect.Y + y) * source.Width + texture.ClipRect.X + x;
    }

    /// <summary>
    /// 由画布集合与偏移集合生成缓存标识。
    /// </summary>
    private static string BuildKey(MTexture[] references, Vector2[] offsets, int count)
    {
        System.Text.StringBuilder builder = new();
        for (int i = 0; i < count; i++)
        {
            builder.Append(references[i]).Append('@').Append(offsets[i].X).Append(',').Append(offsets[i].Y).Append(';');
        }

        return builder.ToString();
    }

    /// <summary>
    /// 用画布颜色数据构造一张结果贴图。
    /// </summary>
    private static MTexture Build(Canvas canvas)
    {
        Texture2D texture = new(Engine.Instance.GraphicsDevice, canvas.Width, canvas.Height, false, SurfaceFormat.Color);
        texture.SetData(canvas.Pixels);
        VirtualTexture result = new($"{nameof(ObjectMask)}", canvas.Width, canvas.Height, Color.Transparent);
        result.Texture = texture;
        return new MTexture(result);
    }

    /// <summary>
    /// 一块待合成的画布，承载输出像素与其尺寸。
    /// </summary>
    private class Canvas
    {
        public int Width;
        public int Height;
        public Color[] Pixels;

        public Canvas(int width, int height)
        {
            Width = width;
            Height = height;
            Pixels = new Color[width * height];
        }
    }
}
