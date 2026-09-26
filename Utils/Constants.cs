global using md = CorreWithCare.CWCModule;
global using ses = CorreWithCare.CWCSession;
global using sav = CorreWithCare.CWCSaveData;
global using exa = CorreWithCare.Core.ExtendedAttributes;
global using cons = CorreWithCare.Utils.Constants;
global using ccolor = CorreWithCare.Utils.ColorUtils.CorreColor;
global using vec2 = Microsoft.Xna.Framework.Vector2;
global using ien = System.Collections.IEnumerator;

namespace CorreWithCare.Utils;

public static class Constants
{
    public const int ScreenWidth = 1920;
    public const int ScreenHeight = 1080;
    public const int GameWidth = 320;
    public const int GameHeight = 180;
    public const int ScreenScale = 6;
    public static vec2 ScreenSize = new(ScreenWidth, ScreenHeight);
    public static vec2 GameSize = new(GameWidth, GameHeight);
}
