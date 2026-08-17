using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.Entities;
using CorreWithCare.Core;
using static CorreWithCare.Utils.ColorUtils;

namespace CorreWithCare.Entities;

/// <summary>
/// 可自定义的滚动文字实体 - 所有参数通过 EntityData 配置
/// </summary>
[CustomEntity("CorreWithCare/CustomCredits")]
[Tracked]
public class CustomCredits : BaseEntity
{
    // ==================== 配置参数 ====================
    
    // 位置与布局
    private float _xPosition;
    private float _alignment;
    private float _scale;
    private float _spacing;
    
    // 滚动控制
    private float _scrollTime;
    private bool _scrollOffScreen;
    private bool _allowInput;
    
    // 颜色
    private CorreColor _headingColor;
    private CorreColor _subtitleColor;
    private CorreColor _textColor;
    private CorreColor _outlineColor;
    private CorreColor _edgeColor;
    
    // 字体缩放
    private float _headingScale;
    private float _subtitleScale;
    private float _textScale;
    
    // 数据源
    private string _dialogKey;
    private string _inlineText;
    
    // ==================== 内部状态 ====================
    
    private List<CreditNode> _nodes = new();
    private float _scroll;
    private float _scrollSpeed;
    private float _totalHeight;
    private float _scrollDelay;
    private float _scrollbarAlpha;
    
    // ==================== 节点定义 ====================
    
    public abstract class CreditNode
    {
        public abstract void Render(Vector2 position, float alignment, float scale);
        public abstract float Height(float scale);
    }
    
    public class HeadingNode : CreditNode
    {
        public string Text;
        public Color Color;
        public float Scale;
        
        public override void Render(Vector2 position, float alignment, float scale)
        {
            var finalScale = Scale * scale;
            ActiveFont.DrawEdgeOutline(
                Text, 
                position.Floor(), 
                new Vector2(alignment, 0f), 
                Vector2.One * finalScale, 
                Color, 
                4f, 
                Color.DarkSlateBlue, 
                2f, 
                Color.Black
            );
        }
        
        public override float Height(float scale)
        {
            return ActiveFont.LineHeight * Scale * scale + 4f * scale;
        }
    }
    
    public class SubtitleNode : CreditNode
    {
        public string Text;
        public Color Color;
        public float Scale;
        
        public override void Render(Vector2 position, float alignment, float scale)
        {
            var finalScale = Scale * scale;
            ActiveFont.DrawEdgeOutline(
                Text, 
                position.Floor(), 
                new Vector2(alignment, 0f), 
                Vector2.One * finalScale, 
                Color, 
                4f, 
                Color.DarkSlateBlue, 
                2f, 
                Color.Black
            );
        }
        
        public override float Height(float scale)
        {
            return ActiveFont.LineHeight * Scale * scale + 4f * scale;
        }
    }
    
    public class TextNode : CreditNode
    {
        public string Text;
        public Color Color;
        public float Scale;
        
        public override void Render(Vector2 position, float alignment, float scale)
        {
            var finalScale = Scale * scale;
            ActiveFont.DrawEdgeOutline(
                Text, 
                position.Floor(), 
                new Vector2(alignment, 0f), 
                Vector2.One * finalScale, 
                Color, 
                4f, 
                Color.DarkSlateBlue, 
                2f, 
                Color.Black
            );
        }
        
        public override float Height(float scale)
        {
            return ActiveFont.LineHeight * Scale * scale + 4f * scale;
        }
    }
    
    public class SpacerNode : CreditNode
    {
        public float Size;
        
        public override void Render(Vector2 position, float alignment, float scale) { }
        public override float Height(float scale) => Size * scale;
    }
    
    public class ImageNode : CreditNode
    {
        public MTexture Texture;
        public float Size;
        public float Rotation;
        
        public override void Render(Vector2 position, float alignment, float scale)
        {
            if (Texture == null) return;
            var pos = position + new Vector2(
                (Texture.Width * 0.5f - alignment * Texture.Width) * scale,
                Texture.Height * 0.5f * scale
            );
            Texture.DrawCentered(pos, Color.White, scale, Rotation);
        }
        
        public override float Height(float scale) => Texture.Height * scale + Size * scale;
    }
    
    // ==================== 构造函数 ====================
    
    public CustomCredits(EntityData data, Vector2 offset) 
        : base(data, offset)
    {
        // ===== 从 EntityData 读取所有参数 =====
        
        // 位置与布局
        _xPosition = data.Float("xPosition", 0.5f);
        _alignment = data.Float("alignment", 0.5f);
        _scale = data.Float("scale", 1f);
        _spacing = data.Float("spacing", 10f);
        
        // 滚动控制
        _scrollTime = data.Float("scrollTime", 60f);
        _scrollOffScreen = data.Bool("scrollOffScreen", true);
        _allowInput = data.Bool("allowInput", true);
        
        // 颜色
        _headingColor = data.GetCorreColor("headingColor", Color.White);
        _subtitleColor = data.GetCorreColor("subtitleColor", Color.Gray);
        _textColor = data.GetCorreColor("textColor", Color.White);
        _outlineColor = data.GetCorreColor("outlineColor", Color.Black);
        _edgeColor = data.GetCorreColor("edgeColor", Color.DarkSlateBlue);
        
        // 字体缩放
        _headingScale = data.Float("headingScale", 2.5f);
        _subtitleScale = data.Float("subtitleScale", 0.9f);
        _textScale = data.Float("textScale", 1.4f);
        
        // 数据源 - 支持两种方式
        _dialogKey = data.Attr("dialogKey", "");
        _inlineText = data.Attr("inlineText", "");
        
        // 构建节点
        BuildNodes();
        
        // 计算总高度
        RecalculateHeight();
        
        // 计算滚动速度
        _scrollSpeed = _totalHeight / _scrollTime;
        
        // 深度和标签
        Depth = data.Int("depth", -2000000);
        Tag = TagsExt.SubHUD;
    }
    
    // ==================== 节点构建 ====================
    
    private void BuildNodes()
    {
        _nodes.Clear();
        
        // 优先使用 Dialog
        if (!string.IsNullOrEmpty(_dialogKey))
        {
            var dialogText = Dialog.Clean(_dialogKey, null);
            if (!string.IsNullOrEmpty(dialogText))
            {
                ParseTextToNodes(dialogText);
                return;
            }
        }
        
        // 其次使用 inlineText
        if (!string.IsNullOrEmpty(_inlineText))
        {
            ParseTextToNodes(_inlineText);
            return;
        }
        
        // 默认内容
        _nodes.Add(new HeadingNode { Text = "Credits", Color = _headingColor.Parsed(), Scale = _headingScale });
        _nodes.Add(new TextNode { Text = "Define 'dialogKey' or 'inlineText' in EntityData.", Color = _textColor.Parsed(), Scale = _textScale });
    }
    
    private void ParseTextToNodes(string text)
    {
        var lines = text.Split(new[] { '\n' }, StringSplitOptions.None);
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line)) continue;
            
            // 解析标记
            if (line.StartsWith("h1:") || line.StartsWith("h1："))
            {
                var content = line.Substring(line.IndexOf(':') + 1).Trim();
                _nodes.Add(new HeadingNode { Text = content, Color = _headingColor.Parsed(), Scale = _headingScale });
            }
            else if (line.StartsWith("h2:") || line.StartsWith("h2："))
            {
                var content = line.Substring(line.IndexOf(':') + 1).Trim();
                _nodes.Add(new SubtitleNode { Text = content, Color = _subtitleColor.Parsed(), Scale = _subtitleScale });
            }
            else if (line.StartsWith("img:") || line.StartsWith("img："))
            {
                var path = line.Substring(line.IndexOf(':') + 1).Trim();
                var texture = GFX.Gui[path];
                if (texture != null)
                {
                    _nodes.Add(new ImageNode { Texture = texture, Size = 0f, Rotation = 0f });
                }
            }
            else if (line.StartsWith("space:") || line.StartsWith("space："))
            {
                var sizeStr = line.Substring(line.IndexOf(':') + 1).Trim();
                if (float.TryParse(sizeStr, out float size))
                {
                    _nodes.Add(new SpacerNode { Size = size });
                }
            }
            else
            {
                _nodes.Add(new TextNode { Text = line, Color = _textColor.Parsed(), Scale = _textScale });
            }
        }
    }
    
    private void RecalculateHeight()
    {
        _totalHeight = 0f;
        foreach (var node in _nodes)
        {
            _totalHeight += node.Height(_scale) + _spacing * _scale;
        }
        _totalHeight += 200f * _scale; // 底部留白
    }
    
    // ==================== 更新 ====================
    
    public override void Update()
    {
        base.Update();
        
        if (_scrollDelay > 0f)
            _scrollDelay -= Engine.DeltaTime;
        
        if (_scrollDelay <= 0f)
        {
            _scrollSpeed = Calc.Approach(_scrollSpeed, 100f * _scale, 1800f * Engine.DeltaTime);
        }
        else
        {
            _scrollSpeed = Calc.Approach(_scrollSpeed, 0f, 1800f * Engine.DeltaTime);
        }
        
        if (_allowInput)
        {
            if (Input.MenuDown.Check)
            {
                _scrollDelay = 1f;
                _scrollSpeed = Calc.Approach(_scrollSpeed, 600f, 1800f * Engine.DeltaTime);
            }
            else if (Input.MenuUp.Check)
            {
                _scrollDelay = 1f;
                _scrollSpeed = Calc.Approach(_scrollSpeed, -600f, 1800f * Engine.DeltaTime);
            }
        }
        
        _scroll += _scrollSpeed * Engine.DeltaTime;
        
        if (!_scrollOffScreen)
        {
            _scroll = Calc.Clamp(_scroll, 0f, _totalHeight);
            if (_scroll >= _totalHeight || _scroll <= 0f)
                _scrollSpeed = 0f;
        }
        else
        {
            if (_scroll < 0f)
            {
                _scroll = 0f;
                _scrollSpeed = 0f;
            }
            if (_scroll > _totalHeight)
            {
                _scrollSpeed = 0f;
            }
        }
        
        _scrollbarAlpha = Calc.Approach(_scrollbarAlpha, _scrollDelay > 0f ? 1f : 0f, Engine.DeltaTime * 2f);
    }
    
    // ==================== 渲染 ====================
    
    public override void Render()
    {
        base.Render();
        
        float x = _xPosition * Engine.Width;
        Vector2 position = new Vector2(x, Engine.Height - _scroll).Floor();
        
        foreach (var node in _nodes)
        {
            float height = node.Height(_scale);
            float yPos = position.Y;
            
            if (yPos > -height && yPos < Engine.Height)
            {
                node.Render(position, _alignment, _scale);
            }
            
            position.Y += height + _spacing * _scale;
        }
        
        // 滚动条
        if (_scrollbarAlpha > 0f)
        {
            int margin = 64;
            int barHeight = Engine.Height - margin * 2;
            float barScale = Math.Min(1f, (float)barHeight / _totalHeight);
            float barY = margin + _scroll / _totalHeight * (barHeight - barHeight * barScale);
            
            Draw.Rect(Engine.Width - 36, margin, 12, barHeight, Color.White * 0.2f * _scrollbarAlpha);
            Draw.Rect(Engine.Width - 36, barY, 12, barHeight * barScale, Color.White * 0.5f * _scrollbarAlpha);
        }
    }
}