using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorreWithCare.Core;

public class BaseTrigger : Trigger
{
    public BaseTrigger(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        Nodes = data.NodesWithPosition(offset);
    }
    public Vector2[] Nodes;
}
