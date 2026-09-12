using CorreWithCare.Core;
using CorreWithCare.Utils;
using static CorreWithCare.Core.ExtendedAttributes;

namespace CorreWithCare.Entities.EasyProgressBar;

[Tracked]
[WorkInProgress]
public class ProgressBar : BaseEntity
{
    [Load]
    public static void Load()
    {
        On.Celeste.Level.LoadLevel += OnLevelLoad;
    }
    [Unload]
    public static void Unload()
    {
        On.Celeste.Level.LoadLevel -= OnLevelLoad;
    }
    public static void OnLevelLoad(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes intro, bool loader)
    {
        orig(self, intro, loader);

        foreach(var i in md.Session.ActiveProgressBars)
        {
            Log.Info(i.Name, i.State, i.Counter, i.MovementTimer, i.Y);
            ProgressBar bar = new ProgressBar(i);
            self.Add(bar);
        }
    }
    public ProgressBar(ses.ProgressBarData data)
    {
        bar = data;

        TryGetData();

        Tag = Tags.HUD | Tags.TransitionUpdate;
    }
    public ProgressBar(ses.ProgressBarSettings setting)
    {
        data = setting;

        TryGetSetting();

        Tag = Tags.HUD | Tags.TransitionUpdate;
    }
    public ses.ProgressBarData bar;
    public ses.ProgressBarSettings data;

    private void TryGetData()
    {
        // fetch data
        data = md.Session.ProgressBars.Find(set => 
        set.Name == bar.Name &&
        set.IsCounter == bar.Counter);
    }

    public void TryGetSetting()
    {
        // fetch setting
        bar = md.Session.ActiveProgressBars.Find(set =>
            set.Name == data.Name &&
            set.Counter == data.IsCounter);
    }

    private void Measure(out string number, out vec2 size)
    {
        // assume the names and session values are matched, start calculating font size
        number = bar.Counter ?
            bar.Name.GetCounter().ToString() :
            bar.Name.GetSlider().ToString("0.0");
        size = ActiveFont.Measure(number) * data.FontScale;
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);

        if(data == null)
        {
            TryGetData();
        }

        if(bar == null)
        {
            TryGetSetting();
        }
    }

    public void Appear()
    {
        if(data == null || bar == null || routine != null) { return; }

        routine = new(AppearRoutine());
    }

    public void Disappear()
    {
        if (data == null || bar == null || routine != null) { return; }

        routine = new(DisappearRoutine());
    }

    public void AppearFor(float time)
    {
        if (data == null || bar == null || routine != null) { return; }

        routine = new(AppearForRoutine(time));
    }

    private ien AppearRoutine()
    {
        bar.State = ses.ProgressBarStates.Appearing;

        float start = bar.Y;
        float target = data.TargetOffsetY;

        bar.MovementTimer = 0f;
        float lerp = 0f;

        while(bar.MovementTimer < data.MoveDuration)
        {
            bar.MovementTimer = bar.MovementTimer.Approach(data.MoveDuration, Engine.DeltaTime);
            lerp = bar.MovementTimer / data.MoveDuration;

            bar.Y = RenderUtils.Lerp(start, target, lerp);

            yield return null;
        }

        bar.State = ses.ProgressBarStates.Display;
    }

    private ien DisappearRoutine()
    {
        bar.State = ses.ProgressBarStates.Disappearing;

        Measure(out var number, out var size);

        float start = bar.Y;
        float target = data.FromTop ?
                -size.Y / 2f - data.GapSize.Y :
                size.Y / 2f + data.GapSize.Y;

        bar.MovementTimer = 0f;
        float lerp = 0f;

        while (bar.MovementTimer < data.MoveDuration)
        {
            bar.MovementTimer = bar.MovementTimer.Approach(data.MoveDuration, Engine.DeltaTime);
            lerp = bar.MovementTimer / data.MoveDuration;

            bar.Y = RenderUtils.Lerp(start, target, lerp);

            yield return null;
        }

        bar.State = ses.ProgressBarStates.Hidden;
    }
    
    private ien AppearForRoutine(float time)
    {
        yield return AppearRoutine();
        
        yield return time;

        yield return DisappearRoutine();
    }

    private Coroutine routine;
    public override void Update()
    {
        base.Update();

        routine?.Update();
    }

    public override void Render()
    {
        base.Render();

        if (data == null || bar == null) { return; }

        Measure(out var number, out var size);

        if (bar.State == ses.ProgressBarStates.Hidden)
        {
            float target = data.FromTop ?
                -size.Y / 2f - data.GapSize.Y :
                size.Y / 2f + data.GapSize.Y;
            bar.Y = target;
        }
        else if (bar.State == ses.ProgressBarStates.Display)
        {
            bar.Y = data.TargetOffsetY;
        }

        // drawing calculations
        float lerp = bar.Counter ? 
            data.GetLerp(bar.Name.GetCounter()) : 
            data.GetLerp(bar.Name.GetSlider());
        float barLeft = cons.ScreenWidth / 2f - data.SizeX / 2f,
            barRight = cons.ScreenWidth / 2f + data.SizeX / 2f;
        float posX = barLeft + data.SizeX * lerp;
        float left = posX - size.X / 2f - data.GapSize.X,
            right = posX + size.X / 2f + data.GapSize.X;

        // start drawing basics
        vec2 shift = data.ShadowShift;
        if (barLeft < left)
        {
            vec2 leftLineStart = new(barLeft, bar.Y);
            vec2 leftLineEnd = new(left, bar.Y);

            // the upper one is the shadow
            Draw.Line(leftLineStart, leftLineEnd, Color.Gray * data.BarColor.alpha, data.BarThickness);
            Draw.Line(leftLineStart - shift, leftLineEnd - shift, data.BarColor.Parsed(), data.BarThickness);
        }

        // the upper one is the shadow
        ActiveFont.Draw(number, new(posX, bar.Y), vec2.One * 0.5f, data.FontScale, Color.Gray * data.FontColor.alpha);
        ActiveFont.Draw(number, new vec2(posX, bar.Y) - shift, vec2.One * 0.5f, data.FontScale, data.FontColor.Parsed());
        
        if(right < barRight)
        {
            vec2 rightLineStart = new(right, bar.Y);
            vec2 rightLineEnd = new(barRight, bar.Y);

            // the upper one is the shadow
            Draw.Line(rightLineStart, rightLineEnd, Color.Gray * data.BarColor.alpha, data.BarThickness);
            Draw.Line(rightLineStart - shift, rightLineEnd - shift, data.BarColor.Parsed(), data.BarThickness);
        }

        // draw title
        if (data.TitleName.HasValidContent())
        {
            string title = Dialog.Clean(data.TitleName);
            vec2 titleSize = ActiveFont.Measure(title) * data.TitleScale;
            ActiveFont.DrawOutline(title, 
                new vec2(cons.ScreenWidth / 2f, bar.Y - size.Y / 2f - data.GapSize.Y) - shift, 
                vec2.One * 0.5f, data.TitleScale, data.TitleColor.Parsed(), 4f, Color.Black);
        }
    }
}
