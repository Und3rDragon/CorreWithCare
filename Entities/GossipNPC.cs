using Celeste.Mod.Entities;
using CorreWithCare.Core;
using CorreWithCare.Features;
using CorreWithCare.Utils;
using MonoMod.Utils;
using System.Collections;

namespace CorreWithCare.Entities;

/// <summary>
/// 会闲聊的自定义 NPC。对话分主对话与闲聊两组：每次交谈按顺序推进一段主对话，
/// 主对话播完后每次随机抽一段闲聊；勾选 endLevel 则在主对话播完时结束关卡并跳过闲聊。
/// 外观用 SpriteBank 的 Sprite，可用两个节点围出交谈范围。
/// </summary>
[CustomEntity("CorreWithCare/GossipNPC")]
[Tracked]
public class GossipNPC : BaseEntity
{
    private readonly string[] mainDialogs;
    private readonly string[] gossipDialogs;
    private readonly bool hasGossip;
    private readonly bool endLevel;
    private readonly string mainDialogCounter;

    private readonly bool approachWhenTalking;
    private readonly int approachDistance;

    private readonly float indicatorOffsetX;
    private readonly float indicatorOffsetY;

    private readonly string onlyIfFlag;
    private readonly bool onlyIfFlagInverted;
    private readonly string setFlag;
    private readonly bool setFlagInverted;

    private readonly string customFont;
    private readonly Rectangle? talkerZone;

    private readonly string spriteName;
    private readonly Sprite sprite;

    private TalkComponent talker;
    private Coroutine talkRoutine;

    // 本次对话播出的是主对话的第几条，-1 表示播的是闲聊
    private int playedMainIndex = -1;
    // 主对话播完且勾了 endLevel：等过场收尾后再结束关卡
    private bool pendingCompleteLevel;

    public GossipNPC(EntityData data, Vector2 offset, EntityID id) : base(data, offset)
    {
        mainDialogs = SplitDialogIds(data.Attr("mainDialogIds"));
        gossipDialogs = SplitDialogIds(data.Attr("gossipDialogIds"));
        hasGossip = gossipDialogs.Length > 0;
        endLevel = data.Bool("endLevel");
        mainDialogCounter = id.ToString() + "MainDialogCounter";

        approachWhenTalking = data.Bool("approachWhenTalking");
        approachDistance = data.Int("approachDistance", 16);

        indicatorOffsetX = data.Float("indicatorOffsetX");
        indicatorOffsetY = data.Float("indicatorOffsetY");

        onlyIfFlag = data.Attr("onlyIfFlag");
        onlyIfFlagInverted = data.Bool("onlyIfFlagInverted");
        setFlag = data.Attr("setFlag");
        setFlagInverted = data.Bool("setFlagInverted");

        customFont = data.Attr("customFont");

        spriteName = data.Attr("spriteName", "");
        string[] spriteParts = spriteName.Split('/', StringSplitOptions.TrimEntries);
        if (!string.IsNullOrEmpty(spriteName) && spriteParts.Length >= 1)
        {
            sprite = GFX.SpriteBank.Create(spriteParts[0]);
            sprite.Scale = new Vector2(data.Bool("flipX") ? -1f : 1f, data.Bool("flipY") ? -1f : 1f);
            Add(sprite);

            if (spriteParts.Length >= 2)
            {
                sprite.Play(spriteParts[1]);
            }
        }

        if (Nodes.Length >= 2)
        {
            // 两个节点围出交谈范围，分别取其左上与右下。节点按 8x8 绘制，所以要补上右下各 8
            float left = Math.Min(Nodes[0].X, Nodes[1].X);
            float right = Math.Max(Nodes[0].X, Nodes[1].X) + 8;
            float top = Math.Min(Nodes[0].Y, Nodes[1].Y);
            float bottom = Math.Max(Nodes[0].Y, Nodes[1].Y) + 8;

            talkerZone = new Rectangle((int) (left - Position.X), (int) (top - Position.Y), (int) (right - left), (int) (bottom - top));
        }
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);

        if (mainDialogs.Length == 0 && gossipDialogs.Length == 0)
        {
            // 主对话与闲聊都没配，只当摆设
            return;
        }

        Add(talker = new TalkComponent(TalkerBounds(), TalkerDrawAt(), OnTalk));
    }

    public override void Update()
    {
        base.Update();

        if (talker != null && !string.IsNullOrEmpty(onlyIfFlag))
        {
            talker.Enabled = (SceneAs<Level>().Session.GetFlag(onlyIfFlag) != onlyIfFlagInverted);
        }
    }

    /// <summary>
    /// 交谈范围的矩形，未设置节点时按外观尺寸围出一圈可交互区域。
    /// </summary>
    private Rectangle TalkerBounds()
    {
        if (talkerZone.HasValue)
        {
            return talkerZone.Value;
        }

        return new Rectangle(-24, -16, 48, 16);
    }

    private Vector2 TalkerDrawAt()
    {
        float height = sprite != null ? sprite.Height * sprite.Scale.Y : 0f;

        return new Vector2(-0.5f + indicatorOffsetX, (0f - height) / 2f + indicatorOffsetY);
    }

    private void OnTalk(Player player)
    {
        if (talkRoutine != null)
        {
            return;
        }

        string dialogId = SelectDialogId();

        player.StateMachine.State = 11;

        Level level = SceneAs<Level>();
        level.StartCutscene(OnTalkEnd);
        Add(talkRoutine = new Coroutine(TalkRoutine(player, dialogId)));
    }

    private IEnumerator TalkRoutine(Player player, string dialogId)
    {
        if (approachWhenTalking)
        {
            if (sprite == null || sprite.Scale.X > 0f)
            {
                yield return PlayerApproach(player, 1);
            }
            else
            {
                yield return PlayerApproach(player, -1);
            }
        }

        IEnumerator dialog = Textbox.Say(dialogId, startSkipping, stopSkipping);
        if (!string.IsNullOrEmpty(customFont))
        {
            dialog = CustomDialogFont.ReplaceFancyTextFontFor(dialog, customFont);
        }

        yield return dialog;

        Level level = SceneAs<Level>();
        level.EndCutscene();
        OnTalkEnd(level);
    }

    private void OnTalkEnd(Level level)
    {
        if (talkRoutine == null)
        {
            return;
        }

        AdvanceDialogProgress();

        Player player = Scene.Tracker.GetEntity<Player>();
        if (player != null)
        {
            player.StateMachine.Locked = false;
            player.StateMachine.State = 0;
        }

        talkRoutine.Cancel();
        talkRoutine.RemoveSelf();
        talkRoutine = null;

        if (!pendingCompleteLevel)
        {
            return;
        }

        pendingCompleteLevel = false;
        level.CompleteArea(true, false);

        if (player != null)
        {
            player.StateMachine.State = 11;
        }
    }

    /// <summary>
    /// 决定这次对话要播哪一段：主对话按顺序推进，主对话播完后随机抽闲聊。
    /// </summary>
    private string SelectDialogId()
    {
        int index = SceneAs<Level>().Session.GetCounter(mainDialogCounter);

        if (index >= 0 && index < mainDialogs.Length)
        {
            // 最后一句主对话：勾了 endLevel 就先标记区域完成
            if (endLevel && index == mainDialogs.Length - 1)
            {
                SceneAs<Level>().RegisterAreaComplete();
            }

            playedMainIndex = index;
            return mainDialogs[index];
        }

        // 主对话播完（或根本没配），转到闲聊
        playedMainIndex = -1;

        if (!hasGossip)
        {
            return mainDialogs.Length > 0 ? mainDialogs[^1] : "";
        }

        return Calc.Random.Choose(gossipDialogs);
    }

    /// <summary>
    /// 对话播完：推进主对话，最后一句时设置 flag、结束关卡或让 NPC 不能再聊。
    /// </summary>
    private void AdvanceDialogProgress()
    {
        if (playedMainIndex < 0)
        {
            return;
        }

        Level level = SceneAs<Level>();
        bool lastMain = playedMainIndex == mainDialogs.Length - 1;

        level.Session.SetCounter(mainDialogCounter, playedMainIndex + 1);
        playedMainIndex = -1;

        if (!lastMain)
        {
            return;
        }

        if (!string.IsNullOrEmpty(setFlag))
        {
            level.Session.SetFlag(setFlag, !setFlagInverted);
        }

        // 只有闲聊、没有主对话时走不到这里，endLevel 在这种情况下不生效
        if (endLevel)
        {
            pendingCompleteLevel = true;
        }
        else if (!hasGossip)
        {
            // 没有闲聊可聊了
            if (talker != null)
            {
                Remove(talker);
                talker = null;
            }
        }
    }

    /// <summary>
    /// 让玩家走到 NPC 的一侧并转向它。
    /// </summary>
    private IEnumerator PlayerApproach(Player player, int side)
    {
        player.StateMachine.State = 11;
        player.StateMachine.Locked = true;

        yield return player.DummyWalkToExact((int) (X + side * approachDistance));

        player.Facing = (Facings) (-side);
        yield return null;
    }

    /// <summary>
    /// 把「对话 ID 串」按逗号拆成数组。
    /// </summary>
    private static string[] SplitDialogIds(string ids)
    {
        if (string.IsNullOrEmpty(ids))
        {
            return Array.Empty<string>();
        }

        return ids.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    // {trigger 0} 调用
    private IEnumerator startSkipping()
    {
        new DynData<Textbox>(Scene.Tracker.GetEntity<Textbox>())["autoPressContinue"] = true;
        yield break;
    }

    // {trigger 1} 调用
    private IEnumerator stopSkipping()
    {
        new DynData<Textbox>(Scene.Tracker.GetEntity<Textbox>())["autoPressContinue"] = false;
        yield break;
    }

    /// <summary>
    /// 按 Sprite 名找到场景中的 GossipNPC。
    /// </summary>
    public static GossipNPC GetNPC(string spriteName)
    {
        foreach (GossipNPC result in Engine.Scene.Tracker.GetEntities<GossipNPC>())
        {
            if (result.spriteName == spriteName)
            {
                return result;
            }
        }

        Log.Warn($"找不到 Sprite 名为 {spriteName} 的 NPC！");
        return null;
    }

    /// <summary>
    /// 播放该 NPC 的 Sprite 动画。
    /// </summary>
    public void PlayAnimation(string name)
    {
        if (sprite == null)
        {
            Log.Warn("该 NPC 没有使用 Sprite！");
            return;
        }

        if (sprite.Has(name))
        {
            sprite.Play(name);
        }
        else
        {
            Log.Warn($"尝试播放不存在的动画 {name}！");
        }
    }

    /// <summary>
    /// 水平翻转缩放。
    /// </summary>
    public void SetHorizontalScale(float scale)
    {
        if (sprite == null)
        {
            return;
        }

        sprite.Scale = new Vector2(scale, sprite.Scale.Y);
    }

    /// <summary>
    /// 取该 NPC 的 Sprite。
    /// </summary>
    public Sprite GetSprite()
    {
        if (sprite == null)
        {
            Log.Warn("该 NPC 没有使用 Sprite！");
        }

        return sprite;
    }
}
