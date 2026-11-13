using Mono.Cecil.Cil;
using MonoMod.Cil;
using CorreWithCare.Utils;
using System.Collections;
using System.Reflection;

namespace CorreWithCare.Features;

/// <summary>
/// 让一段对话协程使用指定字库渲染文本。
/// 用法：<c>yield return CustomDialogFont.ReplaceFancyTextFontFor(Textbox.Say(dialogId), "字体名");</c>
/// 字库名为空时原样放行；字库尚未加载时会临时加载，并在离开房间时自动卸载。
/// </summary>
public static class CustomDialogFont
{
    private static readonly Dictionary<string, List<string>> fontPaths;

    static CustomDialogFont()
    {
        fontPaths = (Dictionary<string, List<string>>) typeof(Fonts)
            .GetField("paths", BindingFlags.NonPublic | BindingFlags.Static)
            .GetValue(null);
    }

    /// <summary>
    /// 包装一段对话协程，使其中的文本改用指定字库渲染。
    /// </summary>
    public static IEnumerator ReplaceFancyTextFontFor(IEnumerator coroutine, string font)
    {
        if (!string.IsNullOrEmpty(font))
        {
            ILContext.Manipulator hook = ReplaceFontInFancyTextConstructor(font);

            if (Fonts.Get(font) == null)
            {
                // 该字库需要为这次对话临时加载
                if (!fontPaths.ContainsKey(font))
                {
                    // 字库列表里没有，先重新索引一遍
                    Log.Warn($"需要重新索引字库，{font} 不存在！");
                    Fonts.Prepare();
                }

                Fonts.Load(font);
                Engine.Scene.Add(new FontHolderEntity(font));
            }

            // 字体替换只在协程第一步生效
            IL.Celeste.FancyText.ctor += hook;
            coroutine.MoveNext();
            IL.Celeste.FancyText.ctor -= hook;

            yield return coroutine.Current;
        }

        yield return new SwapImmediately(coroutine);
    }

    private static ILContext.Manipulator ReplaceFontInFancyTextConstructor(string fontName)
    {
        return il =>
        {
            ILCursor cursor = new(il);
            FontReplacer replacer = new(fontName);

            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Language>("FontFace")))
            {
                cursor.Emit(OpCodes.Pop);
                cursor.EmitDelegate<Func<string>>(replacer.GetFont);
            }
        };
    }

    /// <summary>
    /// 把固定的字库名交给绘制链路。
    /// </summary>
    private class FontReplacer
    {
        private readonly string font;

        public FontReplacer(string font)
        {
            this.font = font;
        }

        public string GetFont()
        {
            return font;
        }
    }

    /// <summary>
    /// 保证对话期间临时加载的字库不会一直驻留：离开房间或结束场景时卸载。
    /// </summary>
    private class FontHolderEntity : Entity
    {
        private readonly string font;

        public FontHolderEntity(string font)
        {
            this.font = font;
        }

        public override void Removed(Scene scene)
        {
            base.Removed(scene);
            Fonts.Unload(font);
        }

        public override void SceneEnd(Scene scene)
        {
            base.SceneEnd(scene);
            Fonts.Unload(font);
        }
    }
}
