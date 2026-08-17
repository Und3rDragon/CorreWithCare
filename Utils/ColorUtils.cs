namespace CorreWithCare.Utils;

public static class ColorUtils
{
    public static string RGBAToHex(this Color color)
    {
        return RGBAToHex(color.R, color.G, color.B, color.A, false);
    }
    
    public static string RGBAToHex(int red, int green, int blue, int alpha, bool sign)
    {
        return $"{(sign ? "#" : "")}{red:X2}{green:X2}{blue:X2}{alpha:X2}";
    }
    
    public struct HSLColor
    {
        public float H;
        public float S;
        public float L;
        public float A;

        public HSLColor(float H = 0, float S = 0, float L = 0, float A = 1f)
        {
            this.H = Math.Clamp(H, 0f, 360f);
            this.S = Math.Clamp(S, 0f, 1f);
            this.L = Math.Clamp(L, 0f, 1f);
            this.A = Math.Clamp(A, 0f, 1f);
        }

        public HSLColor(Color color)
        {
            RGBToHSL(color);
        }

        public HSLColor(CorreColor color)
        {
            RGBToHSL(color.color);
            A = color.alpha;
        }

        public HSLColor(string hex)
        {
            RGBToHSL(Calc.HexToColorWithAlpha(hex));
        }

        private void RGBToHSL(Color color)
        {
            float r = color.R / 255f;
            float g = color.G / 255f;
            float b = color.B / 255f;

            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));
            float delta = max - min;

            float l = (max + min) / 2f;

            float s = 0f;
            if (delta != 0)
            {
                if (l < 0.5f)
                    s = delta / (max + min);
                else
                    s = delta / (2f - max - min);
            }

            float h = 0f;
            if (delta != 0)
            {
                if (max == r)
                    h = ((g - b) / delta + (g < b ? 6 : 0)) * 60;
                else if (max == g)
                    h = ((b - r) / delta + 2) * 60;
                else if (max == b)
                    h = ((r - g) / delta + 4) * 60;
            }

            H = h;
            S = s;
            L = l;
            A = Math.Clamp(color.A / 255f, 0f, 1f);
        }

        public Color ToColor()
        {
            float h = H;              // 0 ~ 360
            float s = S;       // 0 ~ 1
            float l = L;       // 0 ~ 1

            float c = (1 - Math.Abs(2 * l - 1)) * s;  // Chroma
            float x = c * (1 - Math.Abs((h / 60f) % 2 - 1));
            float m = l - c / 2;

            float r1 = 0, g1 = 0, b1 = 0;
            int sector = (int)(h / 60) % 6;

            switch (sector)
            {
                case 0: r1 = c; g1 = x; b1 = 0; break;
                case 1: r1 = x; g1 = c; b1 = 0; break;
                case 2: r1 = 0; g1 = c; b1 = x; break;
                case 3: r1 = 0; g1 = x; b1 = c; break;
                case 4: r1 = x; g1 = 0; b1 = c; break;
                case 5: r1 = c; g1 = 0; b1 = x; break;
            }

            int R = Math.Clamp((int)((r1 + m) * 255), 0, 255);
            int G = Math.Clamp((int)((g1 + m) * 255), 0, 255);
            int B = Math.Clamp((int)((b1 + m) * 255), 0, 255);

            return new Color(R, G, B, A);
        }

    }

    public struct HSVColor
    {
        public float H;
        public float S;
        public float V;
        public float A;

        public HSVColor(float h = 0, float s = 0, float v = 0, float a = 1f)
        {
            this.H = Math.Clamp(h, 0f, 360f);
            this.S = Math.Clamp(s, 0f, 1f);
            this.V = Math.Clamp(v, 0f, 1f);
            this.A = Math.Clamp(a, 0f, 1f);
        }

        public HSVColor(Color color)
        {
            RGBToHSV(color);
        }

        public HSVColor(CorreColor color)
        {
            RGBToHSV(color.color);
            A = color.alpha;
        }

        public HSVColor(string hex)
        {
            RGBToHSV(Calc.HexToColorWithAlpha(hex));
        }

        private void RGBToHSV(Color color)
        {
            float r = color.R / 255f;
            float g = color.G / 255f;
            float b = color.B / 255f;

            float max = MathHelper.Max(r, MathHelper.Max(g, b));
            float min = MathHelper.Min(r, MathHelper.Min(g, b));
            float delta = max - min;

            // Value (V)
            float v = max;

            // Saturation (S)
            float s = max == 0 ? 0 : delta / max;

            // Hue (H)
            float h = 0;
            if (delta != 0)
            {
                if (max == r)
                    h = ((g - b) / delta + (g < b ? 6 : 0)) * 60;
                else if (max == g)
                    h = ((b - r) / delta + 2) * 60;
                else if (max == b)
                    h = ((r - g) / delta + 4) * 60;
            }
            // h ∈ [0, 360)

            // 映射到 byte (0~255)
            H = h;
            S = s;
            V = v;
            A = Math.Clamp(color.A / 255f, 0f, 1f);
        }

        public Color ToColor()
        {
            // 先将 byte 映射回浮点范围
            float h = H;  // 0~360
            float s = S;         // 0~1
            float v = V;         // 0~1

            // HSV → RGB 算法
            float c = v * s;           // Chroma
            float x = c * (1 - Math.Abs((h / 60f) % 2 - 1));
            float m = v - c;

            float r1 = 0, g1 = 0, b1 = 0;
            int sector = (int)(h / 60) % 6;

            switch (sector)
            {
                case 0: r1 = c; g1 = x; b1 = 0; break;
                case 1: r1 = x; g1 = c; b1 = 0; break;
                case 2: r1 = 0; g1 = c; b1 = x; break;
                case 3: r1 = 0; g1 = x; b1 = c; break;
                case 4: r1 = x; g1 = 0; b1 = c; break;
                case 5: r1 = c; g1 = 0; b1 = x; break;
            }

            int R = Math.Clamp((int)((r1 + m) * 255), 0, 255);
            int G = Math.Clamp((int)((g1 + m) * 255), 0, 255);
            int B = Math.Clamp((int)((b1 + m) * 255), 0, 255);

            return new Color(R, G, B, A);
        }
    }

    public struct CorreColor
    {
        public override string ToString()
        {
            return $"ChroniaColor[{color.R}, {color.G}, {color.B}, {alpha}]";
        }

        public static CorreColor White = new(Color.White);
        public static CorreColor Black = new(Color.Black);
        public static CorreColor Red = new(Color.Red);
        public static CorreColor Blue = new(Color.Blue);
        public static CorreColor Green = new(Color.Green);

        public Color color;
        public float alpha;

        public CorreColor(Color color, float alpha = 1f)
        {
            this.color = color;
            this.alpha = Math.Clamp(alpha, 0f, 1f);
        }

        public CorreColor(byte R, byte G, byte B, byte A)
        {
            color = new Color(R, G, B);
            alpha = A / 255f;
        }

        public CorreColor(int R = 0, int G = 0, int B = 0, float A = 0f)
        {
            color = new Color(R, G, B);
            alpha = Math.Clamp(A, 0f, 1f);
        }

        public CorreColor(string hex)
        {
            Color c = Calc.HexToColorWithAlpha(hex);
            color = new Color(c.R, c.G, c.B);
            alpha = c.A / 255f;
        }

        public CorreColor(HSLColor hsl)
        {
            alpha = hsl.A;
            Color c = hsl.ToColor();
            color = new Color(c.R, c.G, c.B);
        }

        public CorreColor(HSVColor hsv)
        {
            alpha = hsv.A;
            Color c = hsv.ToColor();
            color = new Color(c.R, c.G, c.B);
        }

        public Color Parsed()
        {
            return color * alpha;
        }

        public Color Parsed(params float[] additionalAlpha)
        {
            float value = 1f;
            for (int i = 0; i < additionalAlpha.Length; i++)
            {
                value *= additionalAlpha[i];
            }

            if (0.99999f <= value && value <= 1.00001f)
            {
                return color * alpha;
            }
            else
            {
                return color * alpha * value;
            }
        }

        public Color OverrideParse(params float[] overrideAlpha)
        {
            float value = 1f;
            for (int i = 0; i < overrideAlpha.Length; i++)
            {
                value *= overrideAlpha[i];
            }

            if (0.99999f <= value && value <= 1.00001f)
            {
                return color;
            }
            else
            {
                return color * value;
            }
        }

        public static CorreColor operator *(CorreColor c, float f)
        {
            c.alpha *= f;
            return c;
        }

        public static CorreColor operator *(float f, CorreColor c)
        {
            c.alpha *= f;
            return c;
        }

        public static CorreColor operator /(CorreColor c, float f)
        {
            c.alpha /= f;
            return c;
        }

        public static CorreColor operator /(float f, CorreColor c)
        {
            c.alpha /= f;
            return c;
        }
    }

    public static CorreColor GetCorreColor(this Color color, float alpha = 1f)
    {
        return new CorreColor(color, alpha);
    }

    public static CorreColor GetCorreColor(this string hex)
    {
        return new CorreColor(hex);
    }
    
    public static CorreColor GetCorreColor(this EntityData data, string colorAttributeName, string defaultColor = "ffffff")
    {
        return new CorreColor(data.Attr(colorAttributeName, defaultColor));
    }

    public static CorreColor GetCorreColor(this EntityData data, string colorAttributeName, Color defaultColor)
    {
        return data.GetCorreColor(colorAttributeName, defaultColor.RGBAToHex());
    }
    
    public static CorreColor GetCorreColor(this EntityData data, string colorAttributeName, CorreColor defaultColor)
    {
        return data.GetCorreColor(colorAttributeName, defaultColor.Parsed());
    }
}