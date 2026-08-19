using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorreWithCare.Utils;

public static class Prt
{
    public static void In(ConsoleColor color = ConsoleColor.White, 
        LogLevel level = LogLevel.Info, 
        Func<object, string> objectParser = null, params object[] objs)
    {
        Console.ForegroundColor = color;
        
        if(objectParser == null)
        {
            objectParser = obj => obj == null ? "null" : obj.ToString();
        }

        foreach (object obj in objs)
        {
            Logger.Log(level, CWCModule.Name, objectParser(obj));
        }

        Console.ResetColor();
    }

    public static void Info(params object[] objs)
    {
        In(ConsoleColor.White, LogLevel.Info, null, objs);
    }

    public static void Warn(params object[] objs)
    {
        In(ConsoleColor.Yellow, LogLevel.Warn, null, objs);
    }

    public static void Error(params object[] objs)
    {
        In(ConsoleColor.Red, LogLevel.Error, null, objs);
    }

    public static void Debug(params object[] objs)
    {
        In(ConsoleColor.Cyan, LogLevel.Debug, null, objs);
    }

    public static void Verbose(params object[] objs)
    {
        In(ConsoleColor.Magenta, LogLevel.Verbose, null, objs);
    }
}
