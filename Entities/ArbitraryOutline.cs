using System.Collections;
using Celeste.Mod.Entities;
using CorreWithCare.Core;
using Microsoft.Xna.Framework.Graphics;

namespace CorreWithCare.Entities;

[CustomEntity("CorreWithCare/ArbitraryOutline")]
[Tracked]
public class ArbitraryOutline : BaseEntity
{
    public Color color;
    public Vector2[] nodes;
    public VertexPositionColor[] objectVertices;
    public List<Vector3> verticesRelative;

    private int _vertexLength;
    private string _effect;
    private float _markerMovement;
    private float _markerInterval;
    private float _leftmostX;

    internal string windingOrderString;

    public ArbitraryOutline(EntityData data, Vector2 offset) : base(data, offset)
    {
        windingOrderString = data.Attr("windingOrder", "Auto");
        _effect = data.Attr("effect");
        _markerMovement = data.Float("markerEffectPixels");
        _markerInterval = data.Float("markerInterval");
        nodes = data.NodesOffset(offset);
        color = data.HexColor("color", Color.White);
        Depth = data.Int("depth");

        objectVertices = GetFillVertsFromNodes(this, Vector2.Zero, color, _effect == "Marker" ? _markerMovement : 0f);
        verticesRelative = new List<Vector3>();
        _vertexLength = objectVertices.Length;

        for (int i = 0; i < _vertexLength; i++)
        {
            ref var vert = ref objectVertices[i];
            verticesRelative.Insert(i, new Vector3(vert.Position.X - X, vert.Position.Y - Y, vert.Position.Z));
        }
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);
        if (_effect == "Marker")
            Add(new Coroutine(MarkerRoutine()));
    }

    public IEnumerator MarkerRoutine()
    {
        objectVertices = GetFillVertsFromNodes(this, Vector2.Zero, color, _effect == "Marker" ? _markerMovement : 0f);
        verticesRelative = new List<Vector3>();
        _vertexLength = objectVertices.Length;

        for (int i = 0; i < _vertexLength; i++)
        {
            var vert = objectVertices[i];
            verticesRelative.Insert(i, new Vector3(vert.Position.X - X, vert.Position.Y - Y, 0f));
        }

        yield return _markerInterval;
        Add(new Coroutine(MarkerRoutine()));
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
    }

    public static Vector2 RandScaleModifier(float threshold)
    {
        return new Vector2(
            -threshold + Calc.Random.NextFloat(threshold * 2),
            -threshold + Calc.Random.NextFloat(threshold * 2)
        );
    }

    public static VertexPositionColor[] GetFillVertsFromNodes(ArbitraryOutline entity, Vector2 offset, Color color, float randScale)
    {
        var nodes = entity.nodes;
        var input = new Vector2[nodes.Length + 1];

        input[0] = entity.Position + offset + RandScaleModifier(randScale);
        for (int i = 1; i < input.Length; i++)
        {
            input[i] = nodes[i - 1] + RandScaleModifier(randScale);
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