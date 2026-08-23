using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLua;
using Monocle;

namespace CorreWithCare.Utils;

/// <summary>
/// Lua 帮助类 - 提供 Lua 文件加载、执行和环境管理功能
/// </summary>
public static class LuaHelper
{
    // ==================== 内置 Lua 环境 ====================

    /// <summary>
    /// 获取 Everest 内置的 Lua 上下文。
    /// 注意：不要自行 new Lua()，因为那会创建独立于 Everest 的 Lua 宿主，
    /// 并可能触使 Everest relinker 处理 NLua/KeraLua 程序集而导致被拉黑。
    /// 使用 Everest.LuaLoader.Context 可复用 Everest 已初始化的 Lua 运行环境。
    /// </summary>
    public static Lua LuaContext => Everest.LuaLoader.Context;

    // ==================== 核心方法 ====================

    /// <summary>
    /// 加载并执行一个 Lua 文件
    /// </summary>
    public static object[] Require(Lua lua, string filePath)
    {
        if (lua == null) throw new ArgumentNullException(nameof(lua));
        if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));

        try
        {
            string content = GetFileContent(filePath);
            if (string.IsNullOrEmpty(content))
            {
                Logger.Log(LogLevel.Error, "CorreWithCare", $"Failed to load Lua file: {filePath}");
                return null;
            }

            return lua.DoString(content, filePath);
        }
        catch (Exception e)
        {
            Logger.Log(LogLevel.Error, "CorreWithCare", $"Failed to execute Lua file {filePath}: {e.Message}");
            Logger.LogDetailed(e);
            return null;
        }
    }

    /// <summary>
    /// 使用 Everest 内置 Lua 上下文加载并执行一个 Lua 文件
    /// </summary>
    public static object[] Require(string filePath)
    {
        return Require(LuaContext, filePath);
    }

    /// <summary>
    /// 从 Mod 资源中获取文件内容
    /// </summary>
    public static string GetFileContent(string path)
    {
        try
        {
            var stream = Everest.Content.Get(path)?.Stream;
            if (stream == null)
            {
                Logger.Log(LogLevel.Warn, "CorreWithCare", $"File not found: {path}");
                return null;
            }

            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
        catch (Exception e)
        {
            Logger.Log(LogLevel.Error, "CorreWithCare", $"Failed to read file {path}: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 获取 Everest 内置的 Lua 环境（含基础库与 import 函数）。
    /// 原 CreateLuaEnvironment()（new Lua()）已移除，改用 Everest.LuaLoader.Context，
    /// 避免触发 Everest relinker 对 NLua/KeraLua 程序集的拉黑。
    /// </summary>
    [Obsolete("请使用 LuaContext / Everest.LuaLoader.Context 替代；new Lua() 会触发 Everest relinker 拉黑问题")]
    public static Lua CreateLuaEnvironment()
    {
        var lua = LuaContext;

        // 加载基础库
        lua.LoadCLRPackage();
        lua.DoString(@"
                import = function(className)
                    if type(className) == 'string' then
                        return clr.Import(className)
                    end
                end
            ");

        return lua;
    }

    // ==================== 协程支持 ====================

    /// <summary>
    /// 将 Lua 协程转换为 C# IEnumerator
    /// </summary>
    public static IEnumerator LuaCoroutineToIEnumerator(LuaFunction coroutineFunc)
    {
        if (coroutineFunc == null) yield break;

        object[] result = null;
        LuaCoroutine coroutine = null;

        try
        {
            result = coroutineFunc.Call();
            if (result != null && result.Length > 0)
            {
                coroutine = result[0] as LuaCoroutine;
            }
        }
        catch (Exception e)
        {
            Logger.Log(LogLevel.Error, "CorreWithCare", $"Failed to create coroutine: {e.Message}");
            Logger.LogDetailed(e);
            yield break;
        }

        if (coroutine == null) yield break;

        while (true)
        {
            bool hasNext = false;
            object current = null;
            Exception error = null;

            try
            {
                hasNext = coroutine.MoveNext();
                if (hasNext)
                {
                    current = coroutine.Current;
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            if (error != null)
            {
                Logger.Log(LogLevel.Error, "CorreWithCare", $"Coroutine error: {error.Message}");
                Logger.LogDetailed(error);
                yield break;
            }

            if (!hasNext)
                break;

            if (current is double || current is float || current is long || current is int)
            {
                yield return Convert.ToSingle(current);
            }
            else if (current is string str)
            {
                yield return str;
            }
            else if (current is IEnumerator enumerator)
            {
                yield return enumerator;
            }
            else
            {
                yield return current;
            }
        }
    }

    /// <summary>
    /// 将 Lua 协程封装为 IEnumerator
    /// </summary>
    public static IEnumerator WrapCoroutine(LuaFunction coroutineFunc, params object[] args)
    {
        if (coroutineFunc == null) yield break;

        object[] result = null;
        LuaCoroutine coroutine = null;

        try
        {
            result = coroutineFunc.Call(args);
            if (result != null && result.Length > 0)
            {
                coroutine = result[0] as LuaCoroutine;
            }
        }
        catch (Exception e)
        {
            Logger.Log(LogLevel.Error, "CorreWithCare", $"Failed to call coroutine function: {e.Message}");
            Logger.LogDetailed(e);
            yield break;
        }

        if (coroutine != null)
        {
            while (true)
            {
                bool hasNext = false;
                object current = null;
                Exception error = null;

                try
                {
                    hasNext = coroutine.MoveNext();
                    if (hasNext)
                    {
                        current = coroutine.Current;
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }

                if (error != null)
                {
                    Logger.Log(LogLevel.Error, "CorreWithCare", $"Coroutine error: {error.Message}");
                    Logger.LogDetailed(error);
                    yield break;
                }

                if (!hasNext)
                    break;

                if (current is double || current is float || current is long || current is int)
                {
                    yield return Convert.ToSingle(current);
                }
                else
                {
                    yield return current;
                }
            }
        }
        else if (result != null && result.Length > 0 && result[0] is IEnumerator enumerator)
        {
            yield return enumerator;
        }
    }

    // ==================== 数据转换 ====================

    /// <summary>
    /// 将 C# Dictionary 转换为 Lua Table
    /// </summary>
    public static LuaTable DictionaryToLuaTable(Lua lua, IDictionary<object, object> dict)
    {
        if (lua == null) throw new ArgumentNullException(nameof(lua));
        if (dict == null) return null;

        // 使用 DoString 创建表并填充数据
        // 因为 NewTable(string) 返回 void，无法直接使用
        lua.DoString("return {}");
        var table = lua["_"] as LuaTable;

        if (table == null)
        {
            // 备用方案：通过 DoString 直接创建
            var result = lua.DoString("local t = {}; return t");
            if (result != null && result.Length > 0)
            {
                table = result[0] as LuaTable;
            }
        }

        if (table == null)
        {
            Logger.Log(LogLevel.Error, "CorreWithCare", "Failed to create Lua table");
            return null;
        }

        foreach (var pair in dict)
        {
            table[pair.Key] = pair.Value;
        }
        return table;
    }

    /// <summary>
    /// 使用 Everest 内置 Lua 上下文将 C# Dictionary 转换为 Lua Table
    /// </summary>
    public static LuaTable DictionaryToLuaTable(IDictionary<object, object> dict)
    {
        return DictionaryToLuaTable(LuaContext, dict);
    }

    /// <summary>
    /// 将 C# List 转换为 Lua Table
    /// </summary>
    public static LuaTable ListToLuaTable(Lua lua, IList list)
    {
        if (lua == null) throw new ArgumentNullException(nameof(lua));
        if (list == null) return null;

        // 通过 DoString 创建表
        var result = lua.DoString("return {}");
        if (result == null || result.Length == 0)
        {
            Logger.Log(LogLevel.Error, "CorreWithCare", "Failed to create Lua table");
            return null;
        }

        var table = result[0] as LuaTable;
        if (table == null)
        {
            Logger.Log(LogLevel.Error, "CorreWithCare", "Failed to create Lua table");
            return null;
        }

        int index = 1;
        foreach (var item in list)
        {
            table[index++] = item;
        }
        return table;
    }

    /// <summary>
    /// 使用 Everest 内置 Lua 上下文将 C# List 转换为 Lua Table
    /// </summary>
    public static LuaTable ListToLuaTable(IList list)
    {
        return ListToLuaTable(LuaContext, list);
    }

    /// <summary>
    /// 将 Lua Table 转换为 C# Dictionary
    /// </summary>
    public static Dictionary<object, object> LuaTableToDictionary(LuaTable table)
    {
        if (table == null) return null;

        var dict = new Dictionary<object, object>();
        foreach (DictionaryEntry entry in table)
        {
            dict[entry.Key] = entry.Value;
        }
        return dict;
    }

    // ==================== 简单类型判断 ====================

    /// <summary>
    /// 判断值是否为数字类型
    /// </summary>
    public static bool IsNumeric(object value)
    {
        return value is double || value is float || value is int || value is long || value is short;
    }

    /// <summary>
    /// 获取数字的 float 值
    /// </summary>
    public static float GetFloatValue(object value)
    {
        return Convert.ToSingle(value);
    }

    // ==================== 工具方法 ====================

    /// <summary>
    /// 安全地调用 Lua 函数
    /// </summary>
    public static object[] SafeCall(LuaFunction function, params object[] args)
    {
        if (function == null) return null;

        try
        {
            return function.Call(args);
        }
        catch (Exception e)
        {
            Logger.Log(LogLevel.Error, "CorreWithCare", $"Failed to call Lua function: {e.Message}");
            Logger.LogDetailed(e);
            return null;
        }
    }

    /// <summary>
    /// 从 Lua 函数获取值
    /// </summary>
    public static T SafeCall<T>(LuaFunction function, params object[] args)
    {
        var result = SafeCall(function, args);
        if (result == null || result.Length == 0)
            return default;

        try
        {
            return (T)result[0];
        }
        catch (InvalidCastException)
        {
            Logger.Log(LogLevel.Error, "CorreWithCare", $"Failed to cast result to {typeof(T).Name}");
            return default;
        }
    }

    /// <summary>
    /// 检查 Lua 函数是否存在
    /// </summary>
    public static bool FunctionExists(LuaTable table, string functionName)
    {
        if (table == null || string.IsNullOrEmpty(functionName))
            return false;

        var func = table[functionName] as LuaFunction;
        return func != null;
    }

    /// <summary>
    /// 从 Lua Table 获取函数
    /// </summary>
    public static LuaFunction GetFunction(LuaTable table, string functionName)
    {
        if (table == null || string.IsNullOrEmpty(functionName))
            return null;

        return table[functionName] as LuaFunction;
    }

    /// <summary>
    /// 创建一个空的 Lua Table（兼容版本）
    /// </summary>
    public static LuaTable CreateLuaTable(Lua lua)
    {
        if (lua == null) throw new ArgumentNullException(nameof(lua));

        var result = lua.DoString("return {}");
        if (result != null && result.Length > 0)
        {
            return result[0] as LuaTable;
        }

        return null;
    }

    /// <summary>
    /// 使用 Everest 内置 Lua 上下文创建一个空的 Lua Table
    /// </summary>
    public static LuaTable CreateLuaTable()
    {
        return CreateLuaTable(LuaContext);
    }
}