using Celeste.Mod.Entities;
using CorreWithCare.Core;
using CorreWithCare.Utils;

namespace CorreWithCare.Entities;

/// <summary>
/// 自动装饰填充：用放置点与节点围出的多边形，在多边形有效面积内铺放选定的 Decal。
///
/// paths          — 待填充的 Decal 路径列表，每次随机取一条
/// density        — 随机排布时有效面积内的取点密度：每 8×8 面积对应的点数
/// scatterness    — 随机排布的分散程度，0 为等距三角阵、1 为完全随机
/// gridX          — 落点的横向对齐间距，为正时按网格对齐（优先于密度）
/// gridY          — 落点的纵向对齐间距，为正时按网格对齐（优先于密度）
/// randomizeFrame — 是否随机 Decal 的起始动画帧
/// depth          — Decal 的深度
///
/// 随机结果由实体 ID 与位置派生，换房间往返后位置与贴图保持一致。
/// </summary>
[CustomEntity("CorreWithCare/AutoDecaler")]
public class AutoDecaler : BaseEntity
{
    // 计算多边形有效面积时的扫描条带高度
    private const float FreeStep = 8f;

    // 三角阵行距与点距的比例
    private static readonly float TriangleRowRatio = (float)Math.Sqrt(3) / 2f;

    private readonly string[] paths;
    private readonly float density;
    private readonly float scatterness;
    private readonly float gridX;
    private readonly float gridY;
    private readonly bool randomizeFrame;
    private readonly int decalDepth;

    private readonly Random rng;
    private readonly List<Decal> decals = new();

    public AutoDecaler(EntityData data, vec2 offset)
        : base(data, offset)
    {
        paths = data.Attr("paths", "")
            .Split(new char[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        density = data.Float("density", 1f).Clamp(0f, 1f);
        scatterness = data.Float("scatterness", 0.7f).Clamp(0f, 1f);
        gridX = data.Float("gridX", -1f);
        gridY = data.Float("gridY", -1f);
        randomizeFrame = data.Bool("randomizeFrame", false);
        decalDepth = data.Int("depth", (int)Depths.FGDecals);

        // 随机源以实体 ID 与位置派生，保证同一实体每次重建得到相同结果
        int seed = HashCode.Combine(data.ID, X, Y);
        rng = RandomUtils.Rand(seed);

        Tag = Tags.TransitionUpdate;

        NormalizePaths();
    }

    // 归一化路径：去掉扩展名，避免与 Decal 自身的 decals/ 前缀重复
    private void NormalizePaths()
    {
        for (int i = 0; i < paths.Length; i++)
        {
            string path = paths[i].Replace('\\', '/');

            int dot = path.LastIndexOf('.');
            int slash = path.LastIndexOf('/');
            if (dot > slash)
            {
                path = path.Substring(0, dot);
            }

            if (path.StartsWith("decals/", StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring("decals/".Length);
            }

            paths[i] = path;
        }
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);

        if (paths.Length == 0 || Nodes.Length < 3)
        {
            return;
        }

        vec2 min = Nodes[0];
        vec2 max = Nodes[0];
        for (int i = 1; i < Nodes.Length; i++)
        {
            min = new vec2(Math.Min(min.X, Nodes[i].X), Math.Min(min.Y, Nodes[i].Y));
            max = new vec2(Math.Max(max.X, Nodes[i].X), Math.Max(max.Y, Nodes[i].Y));
        }

        foreach (vec2 point in CollectCandidatePoints(min, max))
        {
            Decal decal = new Decal(paths[rng.Next(paths.Length)], point, Vector2.One, decalDepth);
            if (decal.textures.Count == 0)
            {
                continue;
            }

            if (randomizeFrame)
            {
                decal.RandomizeStartingFrame();
            }

            decals.Add(decal);
            scene.Add(decal);
        }
    }

    public override void Removed(Scene scene)
    {
        base.Removed(scene);

        foreach (Decal decal in decals)
        {
            decal.RemoveSelf();
        }
        decals.Clear();
    }

    // 收集落在多边形有效区域内的放置点，网格轴优先，其余轴按密度与分散程度取点
    private List<vec2> CollectCandidatePoints(vec2 min, vec2 max)
    {
        bool gridAxisX = gridX > 0f;
        bool gridAxisY = gridY > 0f;

        if (gridAxisX && gridAxisY)
        {
            return CollectGridPoints(min, max);
        }

        if (!gridAxisX && !gridAxisY)
        {
            return CollectFreePoints(min, max);
        }

        return gridAxisX ? CollectColumnPoints(min, max) : CollectRowPoints(min, max);
    }

    // 两轴均为网格：以包围盒左上角为原点排满落点
    private List<vec2> CollectGridPoints(vec2 min, vec2 max)
    {
        List<vec2> points = new();
        for (float y = min.Y; y <= max.Y; y += gridY)
        {
            for (float x = min.X; x <= max.X; x += gridX)
            {
                vec2 point = new vec2(x, y);
                if (PointInPolygon(point))
                {
                    points.Add(point);
                }
            }
        }
        return points;
    }

    // 两轴均为自由：按密度取点，分散程度决定等距三角阵与随机之间的过渡
    private List<vec2> CollectFreePoints(vec2 min, vec2 max)
    {
        if (scatterness >= 1f)
        {
            return CollectRandomPoints(min, max);
        }

        return CollectTrianglePoints(min, max);
    }

    // 完全随机：在包围盒内按密度取点，保留落在有效区域内的
    private List<vec2> CollectRandomPoints(vec2 min, vec2 max)
    {
        int count = PointCount(PolygonArea());
        List<vec2> points = new(count);

        for (int i = 0; i < count; i++)
        {
            vec2 point = new vec2(Range(min.X, max.X), Range(min.Y, max.Y));
            if (PointInPolygon(point))
            {
                points.Add(point);
            }
        }
        return points;
    }

    // 等距三角阵：以包围盒左上角为锚点、朝向随机铺排基准点，再按分散程度抖动
    private List<vec2> CollectTrianglePoints(vec2 min, vec2 max)
    {
        float spacing = PointSpacing();
        float radius = vec2.Distance(min, max);
        float angle = Range(0f, MathF.PI * 2f);
        float cos = (float)Math.Cos(angle);
        float sin = (float)Math.Sin(angle);
        float jitter = scatterness * spacing / 2f;

        List<vec2> points = new();
        int steps = (int)Math.Ceiling(radius / spacing) + 1;
        float rowHeight = spacing * TriangleRowRatio;

        for (int row = -steps; row <= steps; row++)
        {
            float localY = row * rowHeight;
            float rowOffset = (row % 2 == 0) ? 0f : spacing / 2f;

            for (int col = -steps; col <= steps; col++)
            {
                float localX = col * spacing + rowOffset;

                vec2 point = new vec2(
                    min.X + localX * cos - localY * sin,
                    min.Y + localX * sin + localY * cos);

                if (jitter > 0f)
                {
                    point += new vec2(Range(-jitter, jitter), Range(-jitter, jitter));
                }

                if (PointInPolygon(point))
                {
                    points.Add(point);
                }
            }
        }
        return points;
    }

    // 横向为网格、纵向自由：沿每条网格线按密度在有效区间内取点
    private List<vec2> CollectColumnPoints(vec2 min, vec2 max)
    {
        float spacing = PointSpacing();
        float jitter = scatterness * spacing / 2f;

        List<vec2> points = new();
        for (float x = min.X; x <= max.X; x += gridX)
        {
            for (float y = min.Y; y <= max.Y; y += spacing)
            {
                float py = scatterness >= 1f ? Range(min.Y, max.Y) : y + Range(-jitter, jitter);
                vec2 point = new vec2(x, py);

                if (PointInPolygon(point))
                {
                    points.Add(point);
                }
            }
        }
        return points;
    }

    // 纵向为网格、横向自由：沿每条网格线按密度在有效区间内取点
    private List<vec2> CollectRowPoints(vec2 min, vec2 max)
    {
        float spacing = PointSpacing();
        float jitter = scatterness * spacing / 2f;

        List<vec2> points = new();
        for (float y = min.Y; y <= max.Y; y += gridY)
        {
            for (float x = min.X; x <= max.X; x += spacing)
            {
                float px = scatterness >= 1f ? Range(min.X, max.X) : x + Range(-jitter, jitter);
                vec2 point = new vec2(px, y);

                if (PointInPolygon(point))
                {
                    points.Add(point);
                }
            }
        }
        return points;
    }

    // 三角阵的点距：使每点的平均占有面积等于 64 / density
    private float PointSpacing()
    {
        if (density <= 0f)
        {
            return FreeStep;
        }
        return (float)Math.Sqrt(FreeStep * FreeStep / density / TriangleRowRatio);
    }

    // 按密度把面积换算成取点数：每 64 平方像素（8×8）对应 density 个点
    private int PointCount(float area)
    {
        return Math.Max(0, (int)Math.Round(area / (FreeStep * FreeStep) * density));
    }

    private float Range(float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }

    // 扫描线奇偶规则：按条带统计落在多边形有效区域内的横段总长累加为面积
    private float PolygonArea()
    {
        int n = Nodes.Length;
        float minY = Nodes[0].Y;
        float maxY = Nodes[0].Y;
        for (int i = 1; i < n; i++)
        {
            minY = Math.Min(minY, Nodes[i].Y);
            maxY = Math.Max(maxY, Nodes[i].Y);
        }

        if (maxY - minY <= 0f)
        {
            return 0f;
        }

        float area = 0f;
        List<float> crossings = new();

        for (float top = minY; top < maxY; top += FreeStep)
        {
            float bottom = Math.Min(top + FreeStep, maxY);
            float midY = (top + bottom) / 2f;

            crossings.Clear();
            for (int i = 0; i < n; i++)
            {
                vec2 a = Nodes[i];
                vec2 b = Nodes[(i + 1) % n];
                if ((a.Y > midY) != (b.Y > midY))
                {
                    crossings.Add(a.X + (midY - a.Y) / (b.Y - a.Y) * (b.X - a.X));
                }
            }

            if (crossings.Count < 2)
            {
                continue;
            }

            crossings.Sort();
            for (int k = 0; k + 1 < crossings.Count; k += 2)
            {
                area += (crossings[k + 1] - crossings[k]) * (bottom - top);
            }
        }

        return area;
    }

    // 射线法（奇偶规则）判定点是否落在多边形有效区域内，兼容自交多边形
    private bool PointInPolygon(vec2 point)
    {
        bool inside = false;
        for (int i = 0, j = Nodes.Length - 1; i < Nodes.Length; j = i++)
        {
            vec2 a = Nodes[i];
            vec2 b = Nodes[j];
            if ((a.Y > point.Y) != (b.Y > point.Y) &&
                point.X < (b.X - a.X) * (point.Y - a.Y) / (b.Y - a.Y) + a.X)
            {
                inside = !inside;
            }
        }
        return inside;
    }
}
