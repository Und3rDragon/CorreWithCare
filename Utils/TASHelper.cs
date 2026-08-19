using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Xna.Framework;
using Monocle;

namespace CorreWithCare.Utils;

/// <summary>
/// TAS 帮助类 - 提供 .tas 文件解析和指令执行功能
/// </summary>
public static class TASHelper
{
    // ==================== 数据结构 ====================

    /// <summary>
    /// TAS 输入帧
    /// </summary>
    public struct TASInputFrame
    {
        public Vector2 Aim;
        public bool Jump;
        public bool Dash;
        public bool Grab;
        public bool CrouchDash;

        public TASInputFrame(Vector2 aim, bool jump, bool dash, bool grab, bool crouchDash)
        {
            Aim = aim;
            Jump = jump;
            Dash = dash;
            Grab = grab;
            CrouchDash = crouchDash;
        }

        public static readonly TASInputFrame Empty = new TASInputFrame(Vector2.Zero, false, false, false, false);
    }

    /// <summary>
    /// TAS 段 - 包含持续帧数和对应的输入
    /// </summary>
    public struct TASSegment
    {
        public int FrameCount;
        public TASInputFrame Input;

        public TASSegment(int frameCount, TASInputFrame input)
        {
            FrameCount = frameCount;
            Input = input;
        }
    }

    /// <summary>
    /// TAS 程序 - 包含所有段和总帧数
    /// </summary>
    public class TASProgram
    {
        public IReadOnlyList<TASSegment> Segments { get; }
        public long TotalFrames { get; }

        public TASProgram(IReadOnlyList<TASSegment> segments, long totalFrames)
        {
            Segments = segments;
            TotalFrames = totalFrames;
        }
    }

    // ==================== TAS 执行器 ====================

    /// <summary>
    /// TAS 执行器 - 按顺序播放 TAS 指令
    /// </summary>
    public class TASExecutor
    {
        private IReadOnlyList<TASSegment> _segments;
        private int _segmentIndex;
        private int _remainingFrames;

        public bool IsPlaying => _segments is not null;

        public void Play(TASProgram program)
        {
            if (program == null)
            {
                Stop();
                return;
            }

            _segments = program.Segments;
            _segmentIndex = 0;
            _remainingFrames = 0;
        }

        public void Stop()
        {
            _segments = null;
            _segmentIndex = 0;
            _remainingFrames = 0;
        }

        public TASInputFrame Advance()
        {
            if (_segments is null || _segmentIndex >= _segments.Count)
            {
                Stop();
                return TASInputFrame.Empty;
            }

            var segment = _segments[_segmentIndex];
            if (_remainingFrames == 0)
                _remainingFrames = segment.FrameCount;

            _remainingFrames--;
            var input = segment.Input;

            if (_remainingFrames == 0)
            {
                _segmentIndex++;
                if (_segmentIndex >= _segments.Count)
                    _segments = null;
            }

            return input;
        }

        /// <summary>
        /// 获取当前进度（0-1）
        /// </summary>
        public float GetProgress()
        {
            if (_segments is null || _segments.Count == 0)
                return 0f;

            long totalFrames = 0;
            foreach (var seg in _segments)
                totalFrames += seg.FrameCount;

            if (totalFrames == 0) return 0f;

            long playedFrames = 0;
            for (int i = 0; i < _segmentIndex && i < _segments.Count; i++)
                playedFrames += _segments[i].FrameCount;

            playedFrames += (_segments[_segmentIndex].FrameCount - _remainingFrames);
            return MathHelper.Clamp((float)playedFrames / totalFrames, 0f, 1f);
        }

        /// <summary>
        /// 重置执行器
        /// </summary>
        public void Reset()
        {
            _segmentIndex = 0;
            _remainingFrames = 0;
        }
    }

    // ==================== TAS 文件解析 ====================

    /// <summary>
    /// 从 Mod 资源加载 TAS 文件
    /// </summary>
    /// <param name="modContent">Mod 内容实例</param>
    /// <param name="tasPath">TAS 文件路径（相对于 Mod 根目录）</param>
    /// <param name="program">解析出的 TAS 程序</param>
    /// <returns>是否成功</returns>
    public static bool TryLoadTAS(ModContent modContent, string tasPath, out TASProgram program)
    {
        program = null;

        if (modContent == null)
        {
            Logger.Log(LogLevel.Error, "TASHelper", "ModContent is null");
            return false;
        }

        if (string.IsNullOrEmpty(tasPath))
        {
            Logger.Log(LogLevel.Error, "TASHelper", "TAS path is empty");
            return false;
        }

        string modName = modContent.Mod?.Name ?? modContent.Name ?? "<unknown mod>";

        // 规范化路径
        if (!TryNormalizeTasPath(tasPath, out string normalizedPath))
        {
            Logger.Log(LogLevel.Error, "TASHelper", $"Mod '{modName}' supplied unsafe or empty TAS path '{tasPath}'.");
            return false;
        }

        // 查找资源
        if (!TryFindAsset(modContent, normalizedPath, out ModAsset asset) || asset == null)
        {
            Logger.Log(LogLevel.Error, "TASHelper", $"TAS asset '{normalizedPath}' was not found in mod '{modName}'.");
            return false;
        }

        try
        {
            using var stream = asset.Stream;
            using var reader = new StreamReader(stream);
            program = Parse(reader, $"{modName}/{normalizedPath}");
            return true;
        }
        catch (Exception e)
        {
            Logger.Log(LogLevel.Error, "TASHelper", $"Failed to parse TAS asset '{normalizedPath}' from mod '{modName}': {e.Message}");
            Logger.LogDetailed(e);
            return false;
        }
    }

    /// <summary>
    /// 从文件路径加载 TAS 文件
    /// </summary>
    public static bool TryLoadTASFromFile(string filePath, out TASProgram program)
    {
        program = null;

        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            Logger.Log(LogLevel.Error, "TASHelper", $"File not found: {filePath}");
            return false;
        }

        try
        {
            using var reader = new StreamReader(filePath);
            program = Parse(reader, filePath);
            return true;
        }
        catch (Exception e)
        {
            Logger.Log(LogLevel.Error, "TASHelper", $"Failed to parse TAS file '{filePath}': {e.Message}");
            Logger.LogDetailed(e);
            return false;
        }
    }

    /// <summary>
    /// 从字符串内容加载 TAS
    /// </summary>
    public static bool TryLoadTASFromString(string content, out TASProgram program)
    {
        program = null;

        if (string.IsNullOrEmpty(content))
        {
            Logger.Log(LogLevel.Error, "TASHelper", "TAS content is empty");
            return false;
        }

        try
        {
            using var reader = new StringReader(content);
            program = Parse(reader, "<string>");
            return true;
        }
        catch (Exception e)
        {
            Logger.Log(LogLevel.Error, "TASHelper", $"Failed to parse TAS content: {e.Message}");
            Logger.LogDetailed(e);
            return false;
        }
    }

    // ==================== 私有解析方法 ====================

    private const int MaxSegments = 100_000;
    private const long MaxTotalFrames = 10_000_000;

    private static TASProgram Parse(TextReader reader, string source)
    {
        var segments = new List<TASSegment>();
        long totalFrames = 0;
        string line;
        int lineNumber = 0;

        while ((line = reader.ReadLine()) != null)
        {
            lineNumber++;
            string trimmed = line.Trim();

            // 跳过空行和注释
            if (trimmed.Length == 0 || trimmed.StartsWith("#") || trimmed.StartsWith("//"))
                continue;

            // 解析行：帧数, 按键1, 按键2, ...
            string[] parts = trimmed.Split(',');

            // 第一个参数必须是正整数的帧数
            if (!int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int count) || count <= 0)
            {
                throw new FormatException($"{source}:{lineNumber}: invalid frame count '{parts[0].Trim()}', expected positive integer.");
            }

            // 解析输入
            var frame = ParseFrame(parts, source, lineNumber);

            // 限制检查
            if (segments.Count >= MaxSegments)
                throw new FormatException($"{source}:{lineNumber}: TAS exceeds the {MaxSegments:N0} segment limit.");
            if (totalFrames > MaxTotalFrames - count)
                throw new FormatException($"{source}:{lineNumber}: TAS exceeds the {MaxTotalFrames:N0} total-frame limit.");

            segments.Add(new TASSegment(count, frame));
            totalFrames += count;
        }

        return new TASProgram(segments, totalFrames);
    }

    private static TASInputFrame ParseFrame(string[] parts, string source, int lineNumber)
    {
        bool left = false;
        bool right = false;
        bool up = false;
        bool down = false;
        bool jump = false;
        bool dash = false;
        bool grab = false;
        bool crouchDash = false;

        for (int i = 1; i < parts.Length; i++)
        {
            string action = parts[i].Trim().ToUpperInvariant();

            switch (action)
            {
                case "": break;
                case "L": left = true; break;
                case "R": right = true; break;
                case "U": up = true; break;
                case "D": down = true; break;
                case "J":
                case "K": jump = true; break;
                case "X":
                case "C": dash = true; break;
                case "G":
                case "H": grab = true; break;
                case "Z":
                case "V": crouchDash = true; break;
                default:
                    throw new FormatException($"{source}:{lineNumber}: unsupported TAS action '{action}'.");
            }
        }

        Vector2 aim = new(
            (right ? 1f : 0f) - (left ? 1f : 0f),
            (down ? 1f : 0f) - (up ? 1f : 0f)
        );

        if (aim != Vector2.Zero)
            aim.Normalize();

        return new TASInputFrame(aim, jump, dash, grab, crouchDash);
    }

    // ==================== 路径工具方法 ====================

    private static bool TryNormalizeTasPath(string path, out string normalized)
    {
        string raw = path.Replace('\\', '/').Trim();
        normalized = raw;

        if (raw.Length == 0 || raw.StartsWith("/", StringComparison.Ordinal))
            return false;

        string[] parts = normalized.Split('/');
        foreach (string part in parts)
        {
            if (part.Length == 0 || part == "." || part == "..")
                return false;
        }

        if (!normalized.EndsWith(".tas", StringComparison.OrdinalIgnoreCase))
            normalized += ".tas";

        return true;
    }

    private static bool TryFindAsset(ModContent content, string relativePath, out ModAsset asset)
    {
        string normalized = relativePath.Replace('\\', '/').TrimStart('/');

        // 直接查找
        if (content.Map.TryGetValue(normalized, out asset) && asset != null)
            return true;

        // 尝试不带扩展名
        string withoutExtension = normalized.EndsWith(".tas", StringComparison.OrdinalIgnoreCase)
            ? normalized.Substring(0, normalized.Length - 4)
            : normalized;

        if (content.Map.TryGetValue(withoutExtension, out asset) && asset != null)
            return true;

        // 遍历查找（忽略大小写）
        foreach (var pair in content.Map)
        {
            string key = pair.Key.Replace('\\', '/').TrimStart('/');
            if (string.Equals(key, normalized, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, withoutExtension, StringComparison.OrdinalIgnoreCase))
            {
                asset = pair.Value;
                return asset != null;
            }
        }

        asset = null;
        return false;
    }

    // ==================== 工具方法 ====================

    /// <summary>
    /// 将 TAS 输入帧转换为人类可读的字符串
    /// </summary>
    public static string FrameToString(TASInputFrame frame)
    {
        var parts = new List<string>();

        if (frame.Aim.X < -0.1f) parts.Add("L");
        if (frame.Aim.X > 0.1f) parts.Add("R");
        if (frame.Aim.Y < -0.1f) parts.Add("U");
        if (frame.Aim.Y > 0.1f) parts.Add("D");
        if (frame.Jump) parts.Add("J");
        if (frame.Dash) parts.Add("X");
        if (frame.Grab) parts.Add("G");
        if (frame.CrouchDash) parts.Add("Z");

        return parts.Count > 0 ? string.Join(",", parts) : "---";
    }

    /// <summary>
    /// 获取 TAS 程序的摘要信息
    /// </summary>
    public static string GetProgramSummary(TASProgram program)
    {
        if (program == null)
            return "null";

        return $"Segments: {program.Segments.Count}, TotalFrames: {program.TotalFrames}";
    }
}