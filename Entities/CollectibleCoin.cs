using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod.Entities;
using CorreWithCare.Core;
using CorreWithCare.Core.CollectibleCoin;
using CorreWithCare.Utils;
using Microsoft.Xna.Framework;
using static CorreWithCare.Core.ExtendedAttributes;

namespace CorreWithCare.Entities;

/// <summary>
/// 可收集金币：按面值累加到所属地图集下对应标签的金币数，并在收集时弹出计数器。
///
/// tag     — 金币分类标签，决定计入哪个计数器（必填）
/// value   — 面值，收集后为对应标签增加的数量
/// sprite  — 自身贴图目录（不含帧号），留空使用默认贴图
/// sfx     — 收集音效，留空使用默认音效
/// persist — 是否可重复收集；关闭时收集过的金币不再出现
///
/// 放置两个节点时，收集后玩家会沿弧线飞向第二个节点并落在那里。
/// </summary>
[Tracked]
[CustomEntity("CorreWithCare/CollectibleCoin")]
[WorkInProgress]
public class CollectibleCoin : Entity
{
    private readonly string coinTag;
    private readonly int value;
    private readonly string sfx;
    private readonly bool persist;
    private readonly vec2[] nodes;
    private readonly EntityID entityID;

    private bool collected;
    private Sprite sprite;
    private BloomPoint bloom;

    public CollectibleCoin(EntityData data, vec2 offset, EntityID entityID)
        : this(data.Position + offset, data.NodesWithPosition(offset), entityID,
              data.Attr("tag", ""),
              data.Int("value", 1),
              data.Attr("sprite", CollectibleCoinUtils.DefaultCoinSprite),
              data.Attr("sfx", CollectibleCoinUtils.DefaultCoinSfx),
              data.Bool("persist", false))
    {
    }

    public CollectibleCoin(vec2 position, vec2[] nodes, EntityID entityID,
        string tag, int value, string spriteName, string sfx, bool persist)
    {
        this.coinTag = tag ?? "";
        this.value = value;
        this.nodes = nodes;
        this.entityID = entityID;
        this.sfx = string.IsNullOrWhiteSpace(sfx) ? CollectibleCoinUtils.DefaultCoinSfx : sfx;
        this.persist = persist;

        Position = position;

        if (string.IsNullOrWhiteSpace(this.coinTag))
            Log.Warn($"[CollectibleCoin] 金币缺少标签，位置 {Position}，该金币将无法计入任何计数器");

        Add(bloom = new BloomPoint(0.5f, 16f));

        Collider = new Hitbox(16f, 16f, -8f, -8f);
        Add(sprite = new Sprite(GFX.Game, string.IsNullOrWhiteSpace(spriteName)
            ? CollectibleCoinUtils.DefaultCoinSprite
            : spriteName));
        Add(new PlayerCollider(OnPlayer));

        sprite.Add("idle", "", 0.1f, new Chooser<string>("idle", 1f), 0, 0, 1, 2, 3, 4, 5, 6, 6, 7, 8, 9, 10, 11);
        sprite.Play("idle");
        sprite.CenterOrigin();
    }

    public override void Update()
    {
        base.Update();

        float lerp = ((float)Math.Sin(Scene.TimeActive * 4f) + 1f) / 6f;
        sprite.Color = Color.Lerp(Color.White, Color.Black, lerp);
    }

    private void OnPlayer(Player player)
    {
        if (collected)
            return;

        collected = true;
        Collect(player);
    }

    /// <summary>
    /// 结算收集：写入计数、播放表现、按需标记不再出现，并在配置了节点时把玩家送走。
    /// </summary>
    private void Collect(Player player)
    {
        Audio.Play(sfx, Center);

        Level level = SceneAs<Level>();
        if (!persist)
            level.Session.DoNotLoad.Add(entityID);

        CoinCounting.Collect(player, coinTag, value, Position);

        PlayCollectAnimation();

        // 节点数组含实体自身位置：nodes[0] 为放置点，nodes[1]、nodes[2] 才是两个回程节点
        if (nodes is not null && nodes.Length >= 3)
            player?.Add(new Coroutine(ReturnRoutine(player, nodes[2], nodes[1])));
    }

    private static ien ReturnRoutine(Player player, vec2 to, vec2 from)
    {
        yield return 0.3f;

        if (!player.Dead)
        {
            Level level = player.SceneAs<Level>();
            Audio.Play("event:/game/general/cassette_bubblereturn",
                level.Camera.Position + new vec2(160f, 90f));
            player.StartCassetteFly(to, from);
        }
    }

    /// <summary>
    /// 收集表现：爆出粒子、加速闪烁并向上漂移淡出。
    /// </summary>
    private void PlayCollectAnimation()
    {
        Level level = SceneAs<Level>();

        for (int i = 0; i < 8; i++)
        {
            float angle = Calc.Random.NextFloat(MathHelper.Pi * 2f);
            level.Particles.Emit(TouchSwitch.P_FireWhite,
                Position + Calc.AngleToVector(angle, Calc.Random.NextFloat(6f)), angle);
        }

        sprite.Rate = 4f;

        Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.SineOut, 1.25f, false);
        float startY = Y;
        float targetY = Y - 12f;

        tween.OnUpdate = t =>
        {
            Y = Calc.LerpClamp(startY, targetY, t.Eased);

            const float bloomFadePoint = 0.2f;
            if (t.Eased >= bloomFadePoint)
            {
                float eased = (t.Eased - bloomFadePoint) / (1f - bloomFadePoint);
                bloom.Alpha = Calc.LerpClamp(1f, 0.2f, eased);
            }

            const float shrinkPoint = 0.95f;
            if (t.Eased >= shrinkPoint)
            {
                float eased = (t.Eased - shrinkPoint) / (1f - shrinkPoint);
                sprite.Scale.X = Calc.LerpClamp(1f, 0f, eased);
            }
        };

        tween.OnComplete = _ => RemoveSelf();
        Add(tween);
        tween.Start();
    }
}
