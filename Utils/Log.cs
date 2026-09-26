using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorreWithCare.Utils;

public static class Log
{
    public static void Output(ConsoleColor color = ConsoleColor.White, 
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
        Output(ConsoleColor.White, LogLevel.Info, null, objs);
    }

    public static void Warn(params object[] objs)
    {
        Output(ConsoleColor.Yellow, LogLevel.Warn, null, objs);
    }

    public static void Error(params object[] objs)
    {
        Output(ConsoleColor.Red, LogLevel.Error, null, objs);
    }

    public static void Debug(params object[] objs)
    {
        Output(ConsoleColor.Cyan, LogLevel.Debug, null, objs);
    }

    public static void Verbose(params object[] objs)
    {
        Output(ConsoleColor.Magenta, LogLevel.Verbose, null, objs);
    }
}
