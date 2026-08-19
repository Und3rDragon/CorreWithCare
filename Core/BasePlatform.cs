using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorreWithCare.Core;

public abstract class BasePlatform : Platform
{
    public BasePlatform(EntityData data, Vector2 offset)
        : base(data.Position + offset, false)
    {
        Nodes = data.NodesWithPosition(offset);
    }
    public Vector2[] Nodes;
}
