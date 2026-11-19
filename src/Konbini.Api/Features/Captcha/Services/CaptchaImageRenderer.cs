using SkiaSharp;

namespace Konbini.Api.Features.Captcha.Services;

/// <summary>把驗證碼的答案畫成 PNG。</summary>
public interface ICaptchaImageRenderer
{
    byte[] RenderPng(string code);
}

/// <summary>
/// 用 SkiaSharp 畫：字元各自旋轉、位移，加上干擾線與雜點。
/// 字形用線段自己畫（<see cref="CaptchaGlyphs"/>），不嵌入字型檔：容器映像沒有字型，
/// SkiaSharp 的 NoDependencies 原生檔也不讀系統字型；自畫字形不用管字型授權。
/// </summary>
public sealed class SkiaCaptchaImageRenderer : ICaptchaImageRenderer
{
    public const int Width = 160;
    public const int Height = 56;

    /// <summary>以 2 倍解析度輸出，高解析度螢幕上不會糊。</summary>
    private const float Scale = 2f;

    /// <summary>左右留白，旋轉後的第一個與最後一個字元才不會被切掉。</summary>
    private const float Padding = 8f;

    private static readonly SKColor Background = new(0xF4, 0xF1, 0xEE);

    public byte[] RenderPng(string code)
    {
        // 干擾的位置與顏色不需要密碼學亂數：答案已經由 CaptchaCode 決定
        var rng = Random.Shared;
        using var surface = SKSurface.Create(new SKImageInfo((int)(Width * Scale), (int)(Height * Scale)));
        var canvas = surface.Canvas;
        canvas.Scale(Scale);
        canvas.Clear(Background);

        using var noise = new SKPaint { IsAntialias = true, StrokeWidth = 1.2f, Style = SKPaintStyle.Stroke };
        for (var i = 0; i < 5; i++)
        {
            noise.Color = RandomColor(rng, 110, 200);
            canvas.DrawLine(rng.Next(Width), rng.Next(Height), rng.Next(Width), rng.Next(Height), noise);
        }

        using var ink = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
        };
        var cell = (Width - Padding * 2) / code.Length;
        for (var i = 0; i < code.Length; i++)
        {
            ink.Color = RandomColor(rng, 20, 90);
            var glyphScale = 2.8f + (float)rng.NextDouble() * 0.4f;   // 字形格子 6×10 → 約 17–19 × 28–32 px
            ink.StrokeWidth = 3.2f / glyphScale;                      // 縮放後線寬約 3.2px
            canvas.Save();
            canvas.Translate(Padding + cell * i + cell / 2 + rng.Next(-2, 3), Height / 2f + rng.Next(-4, 5));
            canvas.RotateDegrees(rng.Next(-20, 21));
            canvas.Scale(glyphScale);
            canvas.Translate(-CaptchaGlyphs.Width / 2, -CaptchaGlyphs.Height / 2);
            using var path = CaptchaGlyphs.Build(code[i]);
            canvas.DrawPath(path, ink);
            canvas.Restore();
        }

        for (var i = 0; i < 120; i++)
        {
            noise.Color = RandomColor(rng, 80, 200);
            canvas.DrawPoint(rng.Next(Width), rng.Next(Height), noise);
        }

        // 穿過所有字元的曲線，字元不容易被逐一切開辨識
        using (var builder = new SKPathBuilder())
        {
            builder.MoveTo(0, rng.Next(20, 40));
            builder.CubicTo(Width / 3f, rng.Next(Height), Width * 2 / 3f, rng.Next(Height), Width, rng.Next(20, 40));
            using var curve = builder.Detach();
            using var stroke = new SKPaint
            {
                IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.6f, Color = new SKColor(60, 45, 40, 170),
            };
            canvas.DrawPath(curve, stroke);
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static SKColor RandomColor(Random rng, int min, int max) =>
        new((byte)rng.Next(min, max), (byte)rng.Next(min, max), (byte)rng.Next(min, max));
}

/// <summary>
/// 驗證碼字元集（<see cref="Models.CaptchaCode.Alphabet"/>）每個字元的筆畫：
/// 寬 6、高 10 的格子上的折線，每兩個數字是一個點（x, y），y 向下。
/// </summary>
public static class CaptchaGlyphs
{
    public const float Width = 6;
    public const float Height = 10;

    private static readonly Dictionary<char, float[][]> Strokes = new()
    {
        ['2'] = [[0, 2, 1, 0.5f, 3, 0, 5, 0.5f, 6, 2, 5.5f, 4, 0, 10, 6, 10]],
        ['3'] = [[0, 1, 2, 0, 4, 0, 5.5f, 1, 5.5f, 3.5f, 4, 5, 2, 5], [4, 5, 6, 6.5f, 6, 8.5f, 4.5f, 10, 1.5f, 10, 0, 9]],
        ['4'] = [[4.5f, 10, 4.5f, 0, 0, 7, 6, 7]],
        ['5'] = [[6, 0, 0.5f, 0, 0, 4.5f, 3, 4, 5, 4.5f, 6, 6.5f, 5.5f, 9, 3.5f, 10, 1, 10, 0, 9]],
        ['6'] = [[5, 0.5f, 3, 0, 1, 1, 0, 4, 0, 7.5f, 1, 9.5f, 3, 10, 5, 9.5f, 6, 7.5f, 5.5f, 5.5f, 3.5f, 4.5f, 1.5f, 5, 0, 6.5f]],
        ['7'] = [[0, 0, 6, 0, 2.5f, 10]],
        ['8'] = [[3, 5, 1, 4, 0.5f, 2, 1.5f, 0.3f, 3, 0, 4.5f, 0.3f, 5.5f, 2, 5, 4, 3, 5, 0.5f, 6.5f, 0, 8.5f, 1.5f, 10, 4.5f, 10, 6, 8.5f, 5.5f, 6.5f, 3, 5]],
        ['9'] = [[6, 3.5f, 4.5f, 5, 2.5f, 5.5f, 0.5f, 4.5f, 0, 2.5f, 1, 0.5f, 3, 0, 5, 0.5f, 6, 2.5f, 6, 6, 5, 9, 3, 10, 1, 9.5f]],
        ['A'] = [[0, 10, 3, 0, 6, 10], [1.2f, 6.5f, 4.8f, 6.5f]],
        ['B'] = [[0, 0, 0, 10, 4, 10, 5.7f, 9, 6, 7.3f, 5, 5.5f, 0, 5], [0, 0, 4, 0, 5.5f, 1, 5.5f, 3.7f, 4, 5]],
        ['C'] = [[6, 1.5f, 4.5f, 0, 2, 0, 0.5f, 1.5f, 0, 5, 0.5f, 8.5f, 2, 10, 4.5f, 10, 6, 8.5f]],
        ['D'] = [[0, 0, 0, 10, 3, 10, 5, 9, 6, 6.5f, 6, 3.5f, 5, 1, 3, 0, 0, 0]],
        ['E'] = [[6, 0, 0, 0, 0, 10, 6, 10], [0, 5, 4.5f, 5]],
        ['F'] = [[6, 0, 0, 0, 0, 10], [0, 5, 4.5f, 5]],
        ['G'] = [[6, 1.5f, 4.5f, 0, 2, 0, 0.5f, 1.5f, 0, 5, 0.5f, 8.5f, 2, 10, 4.5f, 10, 6, 8.5f, 6, 5.5f, 3.5f, 5.5f]],
        ['H'] = [[0, 0, 0, 10], [6, 0, 6, 10], [0, 5, 6, 5]],
        ['J'] = [[6, 0, 6, 7.5f, 5, 9.5f, 3, 10, 1, 9.5f, 0, 7.5f]],
        ['K'] = [[0, 0, 0, 10], [6, 0, 0, 6], [2, 4, 6, 10]],
        ['M'] = [[0, 10, 0, 0, 3, 6, 6, 0, 6, 10]],
        ['N'] = [[0, 10, 0, 0, 6, 10, 6, 0]],
        ['P'] = [[0, 10, 0, 0, 4, 0, 5.5f, 1, 6, 2.75f, 5.5f, 4.5f, 4, 5.5f, 0, 5.5f]],
        ['Q'] = [[3, 0, 1, 0.8f, 0, 3, 0, 7, 1, 9.2f, 3, 10, 5, 9.2f, 6, 7, 6, 3, 5, 0.8f, 3, 0], [3.5f, 7, 6.5f, 10.5f]],
        ['R'] = [[0, 10, 0, 0, 4, 0, 5.5f, 1, 6, 2.75f, 5.5f, 4.5f, 4, 5.5f, 0, 5.5f], [3, 5.5f, 6, 10]],
        ['S'] = [[6, 1.5f, 4.5f, 0, 1.5f, 0, 0, 1.5f, 0.3f, 3.5f, 2, 4.6f, 4, 5.4f, 5.7f, 6.5f, 6, 8.5f, 4.5f, 10, 1.5f, 10, 0, 8.5f]],
        ['T'] = [[0, 0, 6, 0], [3, 0, 3, 10]],
        ['U'] = [[0, 0, 0, 7.5f, 1, 9.5f, 3, 10, 5, 9.5f, 6, 7.5f, 6, 0]],
        ['V'] = [[0, 0, 3, 10, 6, 0]],
        ['W'] = [[0, 0, 1.5f, 10, 3, 4, 4.5f, 10, 6, 0]],
        ['X'] = [[0, 0, 6, 10], [6, 0, 0, 10]],
        ['Y'] = [[0, 0, 3, 5, 6, 0], [3, 5, 3, 10]],
        ['Z'] = [[0, 0, 6, 0, 0, 10, 6, 10]],
    };

    /// <summary>有筆畫定義的字元，測試用來確認涵蓋整個字元集。</summary>
    public static IEnumerable<char> Defined => Strokes.Keys;

    public static SKPath Build(char c)
    {
        using var builder = new SKPathBuilder();
        foreach (var stroke in Strokes[c])
        {
            builder.MoveTo(stroke[0], stroke[1]);
            for (var i = 2; i < stroke.Length; i += 2)
            {
                builder.LineTo(stroke[i], stroke[i + 1]);
            }
        }
        return builder.Detach();
    }
}
