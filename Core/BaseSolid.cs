using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorreWithCare.Core;

public class BaseSolid : Solid
{
    public BaseSolid(EntityData data, Vector2 offset)
        : base(data.Position + offset, data.Width, data.Height, true)
    {
        Nodes = data.NodesWithPosition(offset);
    }
    public Vector2[] Nodes;
}
