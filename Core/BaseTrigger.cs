using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorreWithCare.Core;

public class BaseTrigger : Trigger
{
    public BaseTrigger(EntityData data, vec2 offset)
        : base(data, offset)
    {
        Nodes = data.NodesWithPosition(offset);
    }
    public vec2[] Nodes;
}
