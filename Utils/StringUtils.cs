﻿using System;

namespace CorreWithCare.Utils;

/// <summary>
/// 字符串扩展工具。
/// </summary>
public static class StringUtils
{
    /// <summary>
    /// 判断字符串是否包含有效内容（非空且非空白）。
    /// </summary>
    public static bool HasValidContent(this string str)
    {
        return !string.IsNullOrEmpty(str) && !string.IsNullOrWhiteSpace(str);
    }
}
