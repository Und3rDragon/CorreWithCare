using CorreWithCare.Core;
using CorreWithCare.Utils;
using CorreWithCare.Core.ProgressBar;
using static CorreWithCare.Core.ExtendedAttributes;
using Mono.Cecil.Cil;

namespace CorreWithCare.Entities.EasyProgressBar;

[Tracked]
[WorkInProgress]
public class VerticalProgressBar : BaseEntity
{
    public VerticalProgressBar(VerticalProgressBarData data)
    {
        Data = data;

        EnsureData();

        Tag = Tags.HUD | Tags.TransitionUpdate;
    }
    public VerticalProgressBar(VerticalProgressBarSettings setting)
    {
        Setting = setting;

        EnsureSetting();

        Tag = Tags.HUD | Tags.TransitionUpdate;
    }
    public VerticalProgressBarData Data;
    public VerticalProgressBarSettings Setting;

    private void EnsureData()
    {
        // fetch data
        Setting = md.Session.VerticalProgressBars.Find(set =>
        set.Name == Data.Name &&
        set.IsCounter == Data.Counter);

        if(Setting == null)
        {
            Setting = new()
            {
                Name = Data.Name,
                IsCounter = Data.Counter
            };
        }
    }

    public void EnsureSetting()
    {
        // fetch setting
        Data = md.Session.ActiveVerticalProgressBars.Find(set =>
            set.Name == Setting.Name &&
            set.Counter == Setting.IsCounter);

        if(Data == null)
        {
            Data = new()
            {
                Name = Setting.Name,
                Counter = Setting.IsCounter,
            };
        }
    }

    private void MeasureNumberFont(out string number, out vec2 size)
    {
        // assume the names and session values are matched, start calculating font size
        number = Data.Counter ?
            Data.Name.GetCounter().ToString() :
            Data.Name.GetSlider().ToString("0.0");
        size = ActiveFont.Measure(number) * Setting.FontScale;
    }

    private bool TryMeasureTitleFont(out string title, out vec2 size)
    {
        if (!Setting.TitleName.HasValidContent())
        {
            title = string.Empty;
            size = vec2.Zero;
            return false;
        }

        title = Dialog.Clean(Setting.TitleName);
        size = ActiveFont.Measure(title) * Setting.TitleScale;
        return true;
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);

        if (Setting == null)
        {
            EnsureData();
        }

        if (Data == null)
        {
            EnsureSetting();
        }
    }

    public void Appear()
    {
        if (Setting == null || Data == null || routine != null) { return; }

        routine = new(AppearRoutine());
    }

    public void Disappear()
    {
        if (Setting == null || Data == null || routine != null) { return; }

        routine = new(DisappearRoutine());
    }

    public void AppearFor(float time)
    {
        if (Setting == null || Data == null || routine != null) { return; }

        routine = new(AppearForRoutine(time));
    }

    private ien AppearRoutine()
    {
        Data.State = ProgressBarStates.Appearing;

        float start = Data.X;
        float target = Setting.TargetOffsetX;

        Data.MovementTimer = 0f;
        float lerp = 0f;

        while (Data.MovementTimer < Setting.MoveDuration)
        {
            Data.MovementTimer = Data.MovementTimer.Approach(Setting.MoveDuration, Engine.DeltaTime);
            lerp = Data.MovementTimer / Setting.MoveDuration;

            Data.X = Ease.SineInOut(lerp).Lerp(start, target);

            yield return null;
        }

        Data.State = ProgressBarStates.Display;

        routine = null;
    }

    private ien DisappearRoutine()
    {
        Data.State = ProgressBarStates.Disappearing;

        MeasureNumberFont(out var number, out var size);

        bool getTitle = TryMeasureTitleFont(out string title, out vec2 titleSize);

        float maxSizeX = getTitle ? NumberUtils.Max(size.X / 2f, titleSize.X / 2f) : size.X / 2f;

        float start = Data.X;
        float target = Setting.FromLeft ?
                -maxSizeX - Setting.GapSize.X :
                cons.ScreenWidth + maxSizeX + Setting.GapSize.X;

        Data.MovementTimer = 0f;
        float lerp = 0f;

        while (Data.MovementTimer < Setting.MoveDuration)
        {
            Data.MovementTimer = Data.MovementTimer.Approach(Setting.MoveDuration, Engine.DeltaTime);
            lerp = Data.MovementTimer / Setting.MoveDuration;

            Data.X = Ease.SineInOut(lerp).Lerp(start, target);

            yield return null;
        }

        Data.State = ProgressBarStates.Hidden;

        routine = null;
    }

    private ien AppearForRoutine(float time)
    {
        yield return AppearRoutine();

        yield return time;

        yield return DisappearRoutine();

        routine = null;
    }

    private Coroutine routine;
    public override void Update()
    {
        base.Update();

        routine?.Update();

        if (Setting == null || Data == null) { return; }

        if (Setting.Flag.HasValidContent())
        {
            if(Setting.Flag.GetFlag() && Data.State == ProgressBarStates.Hidden)
            {
                Appear();
            }
            if(!Setting.Flag.GetFlag() && Data.State == ProgressBarStates.Display)
            {
                Disappear();
            }
        }
    }

    public override void Render()
    {
        base.Render();

        if (Setting == null || Data == null) { return; }

        MeasureNumberFont(out var number, out var size);

        bool getTitle = TryMeasureTitleFont(out string title, out vec2 titleSize);

        // ensure state continues
        if (Data.State == ProgressBarStates.Display)
        {
            Data.X = Setting.TargetOffsetX;
        }
        else if(Data.State == ProgressBarStates.Appearing)
        {
            Appear();
        }
        else if(Data.State == ProgressBarStates.Disappearing)
        {
            Disappear();
        }
        else
        {
            // by default the bar should be hidden
            float maxSizeX = getTitle ? NumberUtils.Max(size.X / 2f, titleSize.X / 2f) : size.X / 2f;

            float target = Setting.FromLeft ?
                -maxSizeX - Setting.GapSize.X :
                cons.ScreenWidth + maxSizeX + Setting.GapSize.X;
            Data.X = target;
        }

        // drawing calculations
        float lerp = Data.Counter ? 
            Setting.GetLerp(Data.Name.GetCounter()) : 
            Setting.GetLerp(Data.Name.GetSlider());
        float barBottom = cons.ScreenHeight / 2f + Setting.SizeY / 2f,
            barTop = cons.ScreenHeight / 2f - Setting.SizeY / 2f;
        float posY = barBottom - Setting.SizeY * lerp;
        float top = posY - size.Y / 2f - Setting.GapSize.Y,
            bottom = posY + size.Y / 2f + Setting.GapSize.Y;

        // start drawing basics
        vec2 shift = Setting.ShadowShift;
        if (barBottom > bottom)
        {
            vec2 bottomLineStart = new(Data.X, barBottom);
            vec2 bottomLineEnd = new(Data.X, bottom);

            // the upper one is the shadow
            Draw.Line(bottomLineStart, bottomLineEnd, Color.Gray * Setting.BarColor.alpha, Setting.BarThickness);
            Draw.Line(bottomLineStart - shift, bottomLineEnd - shift, Setting.BarColor.Parsed(), Setting.BarThickness);
        }

        // the upper one is the shadow
        ActiveFont.Draw(number, new(Data.X, posY), vec2.One * 0.5f, Setting.FontScale, Color.Gray * Setting.FontColor.alpha);
        ActiveFont.Draw(number, new vec2(Data.X, posY) - shift, vec2.One * 0.5f, Setting.FontScale, Setting.FontColor.Parsed());
        
        if(top > barTop)
        {
            vec2 rightLineStart = new(Data.X, top);
            vec2 rightLineEnd = new(Data.X, barTop);

            // the upper one is the shadow
            Draw.Line(rightLineStart, rightLineEnd, Color.Gray * Setting.BarColor.alpha, Setting.BarThickness);
            Draw.Line(rightLineStart - shift, rightLineEnd - shift, Setting.BarColor.Parsed(), Setting.BarThickness);
        }

        // draw title
        if (getTitle)
        {
            float titlePosY = Setting.TitleOnBottom ?
                cons.ScreenHeight / 2f + Setting.SizeY / 2f + size.Y / 2f + Setting.GapSize.Y :
                cons.ScreenHeight / 2f - Setting.SizeY / 2f - size.Y / 2f  - Setting.GapSize.Y;
            ActiveFont.DrawOutline(title, 
                new vec2(Data.X, titlePosY), 
                vec2.One * 0.5f, Setting.TitleScale, Setting.TitleColor.Parsed(), 4f, Color.Black);
        }
    }
}
