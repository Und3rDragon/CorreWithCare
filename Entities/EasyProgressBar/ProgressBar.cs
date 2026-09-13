using CorreWithCare.Core;
using CorreWithCare.Utils;
using CorreWithCare.Core.ProgressBar;
using static CorreWithCare.Core.ExtendedAttributes;

namespace CorreWithCare.Entities.EasyProgressBar;

/// <summary>
/// 进度条：在屏幕上显示某个会话来计数器的当前数值，并以进度线标出它在取值区间中的位置。
/// 走向与出没方向由设置中的 Direction 决定。
/// </summary>
[Tracked]
[WorkInProgress]
public class ProgressBar : BaseEntity
{
    public ProgressBar(ProgressBarData data)
    {
        Data = data;

        EnsureData();

        Tag = Tags.HUD | Tags.TransitionUpdate;
    }
    public ProgressBar(ProgressBarSettings setting)
    {
        Setting = setting;

        EnsureSetting();

        Tag = Tags.HUD | Tags.TransitionUpdate;
    }
    public ProgressBarData Data;
    public ProgressBarSettings Setting;

    // ==================== 轴向访问 ====================

    private bool IsVertical => BarDirection.IsVertical(Setting.Direction);
    private bool FromStart => BarDirection.IsFromStart(Setting.Direction);

    /// <summary>
    /// 进度条当前的位置偏移：水平进度条为 Y，垂直进度条为 X。
    /// </summary>
    private float Offset
    {
        get => Data.Offset;
        set => Data.Offset = value;
    }

    private void EnsureData()
    {
        // fetch data
        Setting = md.Session.ProgressBars.Find(set =>
        set.Name == Data.Name &&
        set.IsCounter == Data.Counter);

        if(Setting == null)
        {
            Setting = new()
            {
                Name = Data.Name,
                IsCounter = Data.Counter,
            };
        }
    }

    public void EnsureSetting()
    {
        // fetch setting
        Data = md.Session.ActiveProgressBars.Find(set =>
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

        float start = Offset;
        float target = Setting.TargetOffset;

        Data.MovementTimer = 0f;
        float lerp = 0f;

        while (Data.MovementTimer < Setting.MoveDuration)
        {
            Data.MovementTimer = Data.MovementTimer.Approach(Setting.MoveDuration, Engine.DeltaTime);
            lerp = Data.MovementTimer / Setting.MoveDuration;

            Offset = Ease.SineInOut(lerp).Lerp(start, target);

            yield return null;
        }

        Data.State = ProgressBarStates.Display;

        routine = null;
    }

    private ien DisappearRoutine()
    {
        Data.State = ProgressBarStates.Disappearing;

        float start = Offset;
        float target = GetHiddenOffset();

        Data.MovementTimer = 0f;
        float lerp = 0f;

        while (Data.MovementTimer < Setting.MoveDuration)
        {
            Data.MovementTimer = Data.MovementTimer.Approach(Setting.MoveDuration, Engine.DeltaTime);
            lerp = Data.MovementTimer / Setting.MoveDuration;

            Offset = Ease.SineInOut(lerp).Lerp(start, target);

            yield return null;
        }

        Data.State = ProgressBarStates.Hidden;

        routine = null;
    }

    /// <summary>
    /// 进度条完全隐藏时所在的位置，即屏幕外一侧。
    /// </summary>
    private float GetHiddenOffset()
    {
        MeasureNumberFont(out var number, out var size);

        bool getTitle = TryMeasureTitleFont(out string title, out vec2 titleSize);

        if (IsVertical)
        {
            // 垂直进度条从左右两侧进出，隐藏位置取决于数字与标题中较宽的那个
            float maxSizeX = getTitle ?
                NumberUtils.Max(size.X / 2f, titleSize.X / 2f) :
                size.X / 2f;

            return FromStart ?
                -maxSizeX - Setting.GapSize.X :
                cons.ScreenWidth + maxSizeX + Setting.GapSize.X;
        }
        else
        {
            // 水平进度条从上下两侧进出，隐藏位置取决于数字与标题的高度
            return FromStart ?
                -size.Y / 2f - Setting.GapSize.Y :
                cons.ScreenHeight + size.Y / 2f + Setting.GapSize.Y + (getTitle ? titleSize.Y : 0);
        }
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
            Offset = Setting.TargetOffset;
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
            Offset = GetHiddenOffset();
        }

        if (IsVertical)
        {
            RenderVertical(number, size, title, titleSize, getTitle);
        }
        else
        {
            RenderHorizontal(number, size, title, titleSize, getTitle);
        }
    }

    /// <summary>
    /// 绘制水平进度条：线条左右横贯屏幕，数字跟随进度点水平移动。
    /// </summary>
    private void RenderHorizontal(string number, vec2 size, string title, vec2 titleSize, bool getTitle)
    {
        // drawing calculations
        float lerp = Data.Counter ?
            Setting.GetLerp(Data.Name.GetCounter()) :
            Setting.GetLerp(Data.Name.GetSlider());
        float barLeft = cons.ScreenWidth / 2f - Setting.Size / 2f,
            barRight = cons.ScreenWidth / 2f + Setting.Size / 2f;
        float posX = barLeft + Setting.Size * lerp;
        float left = posX - size.X / 2f - Setting.GapSize.X,
            right = posX + size.X / 2f + Setting.GapSize.X;

        // start drawing basics
        vec2 shift = Setting.ShadowShift;
        if (barLeft < left)
        {
            vec2 leftLineStart = new(barLeft, Offset);
            vec2 leftLineEnd = new(left, Offset);

            // the upper one is the shadow
            Draw.Line(leftLineStart, leftLineEnd, Color.Gray * Setting.BarColor.alpha, Setting.BarThickness);
            Draw.Line(leftLineStart - shift, leftLineEnd - shift, Setting.BarColor.Parsed(), Setting.BarThickness);
        }

        // the upper one is the shadow
        ActiveFont.Draw(number, new(posX, Offset), vec2.One * 0.5f, Setting.FontScale, Color.Gray * Setting.FontColor.alpha);
        ActiveFont.Draw(number, new vec2(posX, Offset) - shift, vec2.One * 0.5f, Setting.FontScale, Setting.FontColor.Parsed());

        if(right < barRight)
        {
            vec2 rightLineStart = new(right, Offset);
            vec2 rightLineEnd = new(barRight, Offset);

            // the upper one is the shadow
            Draw.Line(rightLineStart, rightLineEnd, Color.Gray * Setting.BarColor.alpha, Setting.BarThickness);
            Draw.Line(rightLineStart - shift, rightLineEnd - shift, Setting.BarColor.Parsed(), Setting.BarThickness);
        }

        // draw title
        if (getTitle)
        {
            ActiveFont.DrawOutline(title,
                new vec2(cons.ScreenWidth / 2f, Offset - size.Y / 2f - Setting.GapSize.Y) - shift,
                vec2.One * 0.5f, Setting.TitleScale, Setting.TitleColor.Parsed(), 4f, Color.Black);
        }
    }

    /// <summary>
    /// 绘制垂直进度条：线条上下贯穿屏幕，数字跟随进度点垂直移动。
    /// </summary>
    private void RenderVertical(string number, vec2 size, string title, vec2 titleSize, bool getTitle)
    {
        // drawing calculations
        float lerp = Data.Counter ?
            Setting.GetLerp(Data.Name.GetCounter()) :
            Setting.GetLerp(Data.Name.GetSlider());
        float barBottom = cons.ScreenHeight / 2f + Setting.Size / 2f,
            barTop = cons.ScreenHeight / 2f - Setting.Size / 2f;
        float posY = barBottom - Setting.Size * lerp;
        float top = posY - size.Y / 2f - Setting.GapSize.Y,
            bottom = posY + size.Y / 2f + Setting.GapSize.Y;

        // start drawing basics
        vec2 shift = Setting.ShadowShift;
        if (barBottom > bottom)
        {
            vec2 bottomLineStart = new(Offset, barBottom);
            vec2 bottomLineEnd = new(Offset, bottom);

            // the upper one is the shadow
            Draw.Line(bottomLineStart, bottomLineEnd, Color.Gray * Setting.BarColor.alpha, Setting.BarThickness);
            Draw.Line(bottomLineStart - shift, bottomLineEnd - shift, Setting.BarColor.Parsed(), Setting.BarThickness);
        }

        // the upper one is the shadow
        ActiveFont.Draw(number, new(Offset, posY), vec2.One * 0.5f, Setting.FontScale, Color.Gray * Setting.FontColor.alpha);
        ActiveFont.Draw(number, new vec2(Offset, posY) - shift, vec2.One * 0.5f, Setting.FontScale, Setting.FontColor.Parsed());

        if(top > barTop)
        {
            vec2 topLineStart = new(Offset, top);
            vec2 topLineEnd = new(Offset, barTop);

            // the upper one is the shadow
            Draw.Line(topLineStart, topLineEnd, Color.Gray * Setting.BarColor.alpha, Setting.BarThickness);
            Draw.Line(topLineStart - shift, topLineEnd - shift, Setting.BarColor.Parsed(), Setting.BarThickness);
        }

        // draw title
        if (getTitle)
        {
            float titlePosY = Setting.TitleOnBottom ?
                cons.ScreenHeight / 2f + Setting.Size / 2f + size.Y / 2f + Setting.GapSize.Y :
                cons.ScreenHeight / 2f - Setting.Size / 2f - size.Y / 2f - Setting.GapSize.Y;
            ActiveFont.DrawOutline(title,
                new vec2(Offset, titlePosY),
                vec2.One * 0.5f, Setting.TitleScale, Setting.TitleColor.Parsed(), 4f, Color.Black);
        }
    }
}
