using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorreWithCare.Core;

public class BaseEntity : Entity
{
    public BaseEntity() : base() { }
    public BaseEntity(EntityData data, vec2 offset)
        : base(data.Position + offset)
    {
        Nodes = data.NodesWithPosition(offset);
    }
    public vec2[] Nodes;
}
