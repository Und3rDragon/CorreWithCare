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
public class CollectibleCoin : BaseEntity
{
    private readonly string coinTag;
    private readonly int value;
    private readonly string sfx;
    private EntityID entityID => SourceId;

    private bool collected;
    private Sprite sprite;
    private BloomPoint bloom;

    private float collectOffsetY, collectOffsetDuration;
    private bool shrinkX;

    private int existence;
    private struct Existence
    {
        public const int Persistent = 0;
        public const int Normal = 1;
        public const int OncePerMap = 2;
    }

    public CollectibleCoin(EntityData data, vec2 offset)
        : base(data, offset)
    {
        this.coinTag = data.Attr("tag", "default");
        this.value = data.Int("value", 1);
        this.sfx = data.Attr("sfx", CollectibleCoinUtils.DefaultCoinSfx);
        
        this.collectOffsetY = data.Float("collectOffsetY", -12f);
        this.collectOffsetDuration = data.Float("collectOffsetDuration", 1.25f);
        this.shrinkX = data.Bool("shrinkX", true);
        this.existence = data.Int("existence", Existence.Normal);

        Add(bloom = new BloomPoint(0.5f, 16f));

        Collider = new Hitbox(16f, 16f, -8f, -8f);

        string spriteName = data.Attr("sprite", CollectibleCoinUtils.DefaultCoinSpriteXML);

        sprite = GFX.SpriteBank.Create(spriteName);
        Add(sprite);

        Add(new PlayerCollider(OnPlayer));

        sprite.Play("idle");
        sprite.CenterOrigin();
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);

        string levelSet = SceneAs<Level>().GetCurrentLevelSet();
        if(md.SaveData.CollectedCoins.TryGetValue(levelSet, out var ids))
        {
            if (ids.Contains(SourceData.ID))
            {
                RemoveSelf();
            }
        }
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
        if (existence == Existence.Normal)
        { 
            level.Session.DoNotLoad.Add(entityID); 
        }
        if (existence == Existence.OncePerMap)
        {
            string levelSet = SceneAs<Level>().GetCurrentLevelSet();
            md.SaveData.CollectedCoins.Create(levelSet, new());
            md.SaveData.CollectedCoins[levelSet].Add(SourceData.ID);
        }

        CoinCounting.Collect(player, coinTag, value, Position);

        PlayCollectAnimation();

        // 节点数组含实体自身位置：nodes[0] 为放置点，nodes[1]、nodes[2] 才是两个回程节点
        if (Nodes is not null && Nodes.Length >= 3)
            player?.Add(new Coroutine(ReturnRoutine(player, Nodes[2], Nodes[1])));
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

        sprite.Play("collect");

        Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.SineOut, collectOffsetDuration.ClampMin(Engine.DeltaTime / 2f), false);
        float startY = Y;
        float targetY = Y + collectOffsetY;

        tween.OnUpdate = t =>
        {
            Y = t.Eased.Lerp(startY, targetY);

            const float bloomFadePoint = 0.2f;
            if (t.Eased >= bloomFadePoint)
            {
                float eased = (t.Eased - bloomFadePoint) / (1f - bloomFadePoint);
                bloom.Alpha = Calc.LerpClamp(1f, 0.2f, eased);
            }

            if (shrinkX)
            {
                const float shrinkPoint = 0.95f;
                if (t.Eased >= shrinkPoint)
                {
                    float eased = (t.Eased - shrinkPoint) / (1f - shrinkPoint);
                    sprite.Scale.X = Calc.LerpClamp(1f, 0f, eased);
                }
            }
        };

        tween.OnComplete = _ => RemoveSelf();
        Add(tween);
        tween.Start();
    }
}
