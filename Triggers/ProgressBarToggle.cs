using Celeste.Mod.Entities;
using CorreWithCare.Core;
using CorreWithCare.Utils;
using CorreWithCare.Entities;
using static CorreWithCare.Core.ExtendedAttributes;
using CorreWithCare.Entities.EasyProgressBar;

namespace CorreWithCare.Triggers;

[WorkInProgress]
[CustomEntity("CorreWithCare/ProgressBarToggle")]
public class ProgressBarToggle : BaseTrigger
{
    public ProgressBarToggle(EntityData data, vec2 offset) : base(data, offset)
    {
        Toggle = data.Int("mode", Modes.Appear);
        Name = data.Attr("target", "default");
        IsCounter = data.Bool("isCounter", true);
    }
    public int Toggle;
    public string Name;
    public bool IsCounter;
    public float OffsetY;
    public struct Modes 
    {
        public const int Show = 0;
        public const int Appear = 1;
        public const int Disappear = 2;
    }

    public override void OnEnter(Player player)
    {
        base.OnEnter(player);

        // try find if there are active ones first
        int n = md.Session.ActiveProgressBars.FindIndex(set =>
            set.Counter == IsCounter && set.Name == Name);

        ProgressBar target = null;

        if(n >= 0)
        {
            ses.ProgressBarData data = md.Session.ActiveProgressBars[n];

            var bars = SceneAs<Level>().Tracker.GetEntities<ProgressBar>();

            target = bars.Find(e =>
                (e as ProgressBar).bar == data) as ProgressBar;
        }
        else
        {
            ses.ProgressBarData data = new()
            {
                Name = Name,
                Counter = IsCounter,
                State = ses.ProgressBarStates.Hidden
            };

            ProgressBar bar = new(data);

            SceneAs<Level>().Add(bar);

            md.Session.ActiveProgressBars.Add(data);

            target = bar;
        }

        if(target != null)
        {
            if(Toggle == Modes.Appear)
            {
                target.Appear();
            }
            else
            {
                target.Disappear();
            }
        }
    }
}
