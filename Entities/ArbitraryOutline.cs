using System.Collections;
using System.Collections.Generic;
using Celeste.Mod.Entities;
using CorreWithCare.Core;
using CorreWithCare.Utils;
using Microsoft.Xna.Framework.Graphics;
using static CorreWithCare.Utils.ColorUtils;

namespace CorreWithCare.Entities;

[CustomEntity("CorreWithCare/ArbitraryOutline")]
[Tracked]
public class ArbitraryOutline : BaseEntity
{
    public CorreColor color;
    public CorreColor outlineColor;
    public float outlineWidth;
    public VertexPositionColor[] objectVertices;
    public List<Vector3> verticesRelative;

    private int _vertexLength;

    public ArbitraryOutline(EntityData data, Vector2 offset) : base(data, offset)
    {
        Nodes = data.NodesOffset(offset);
        color = data.GetCorreColor("color", Color.White);
        outlineColor = data.GetCorreColor("outlineColor", Color.White);
        outlineWidth = data.Float("outlineWidth", 2f);
        Depth = data.Int("depth");

        objectVertices = GetFillVertsFromNodes(this, Vector2.Zero, color.Parsed());
        verticesRelative = new List<Vector3>();
        _vertexLength = objectVertices.Length;

        for (int i = 0; i < _vertexLength; i++)
        {
            ref var vert = ref objectVertices[i];
            verticesRelative.Insert(i, new Vector3(vert.Position.X - X, vert.Position.Y - Y, vert.Position.Z));
        }
    }

    public override void Render()
    {
        base.Render();

        Camera camera = (Scene as Level).Camera;

        GameplayRenderer.End();
        for (int i = 0; i < _vertexLength; i++)
        {
            ref var vert = ref objectVertices[i];
            vert.Position = new Vector3(X, Y, 0f) + verticesRelative[i];
        }

        GFX.DrawVertices(camera.Matrix, objectVertices, _vertexLength, null, null);
        GameplayRenderer.Begin();

        // 多边形描边：沿 Position + Nodes 闭合轮廓绘制
        if (outlineColor.alpha > 0f && outlineWidth > 0f && Nodes.Length >= 2)
        {
            Color oc = outlineColor.Parsed();
            for (int i = 0; i < Nodes.Length; i++)
            {
                Vector2 from = (i == 0) ? Position : Nodes[i - 1];
                Vector2 to = Nodes[i];
                Draw.Line(from, to, oc, outlineWidth);
            }
            // 最后一段闭合回起点
            Draw.Line(Nodes[Nodes.Length - 1], Position, oc, outlineWidth);
        }
    }

    public static VertexPositionColor[] GetFillVertsFromNodes(ArbitraryOutline entity, Vector2 offset, Color color)
    {
        var nodes = entity.Nodes;
        var input = new Vector2[nodes.Length + 1];

        input[0] = entity.Position + offset;
        for (int i = 1; i < input.Length; i++)
        {
            input[i] = nodes[i - 1];
        }

        // using "earcut" library for triangulations
        // transforming Vector2[] into float[]
        // because earcut requires flat arrays
        var flatVertices = new double[input.Length * 2];
        for (int i = 0; i < input.Length; i++)
        {
            flatVertices[i * 2] = input[i].X;
            flatVertices[i * 2 + 1] = input[i].Y;
        }

        // triangulation
        var indices = Earcut.earcut(flatVertices, null, 2);

        // create point arrays
        var fill = new VertexPositionColor[indices.Count];
        for (int i = 0; i < indices.Count; i++)
        {
            ref var f = ref fill[i];
            int idx = indices[i];
            f.Position = new Vector3(input[idx].X, input[idx].Y, 0f);
            f.Color = color;
        }

        return fill;
    }
}
