using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CorreWithCare.Core;

public static class LoadingManager
{
    public static HashSet<Type> forceLoadingHooks { get; private set; } = new();
    public static HashSet<Type> selectiveLoadingHooks { get; private set; } = new();

    /// <summary>
    /// Manually load hooks with [SelectiveLoadHook] labelled
    /// </summary>
    /// <param name="t"></param>
    public static void LoadHook(Type t)
    {
        // if registered and loaded, return
        if (selectiveLoadingHooks.Contains(t))
        {
            return;
        }

        // if not, load the hook
        MethodInfo[] methods = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
        foreach (var method in methods)
        {
            if (method.GetCustomAttribute(typeof(ExtendedAttributes.SelectLoad)) != null)
            {
                object instance = method.IsStatic ? null : Activator.CreateInstance(t);
                method.Invoke(instance, null);
            }
        }

        selectiveLoadingHooks.Add(t);
    }

    public static void LoadHook<T>()
    {
        LoadHook(typeof(T));
    }

    public static void LoadHook(object obj)
    {
        LoadHook(obj.GetType());
    }

    /// <summary>
    /// Manually unload hooks with [SelectiveUnloadHook] labelled
    /// </summary>
    /// <param name="t"></param>
    public static void UnloadHook(Type t)
    {
        // if unregistered or unloaded, return
        if (!selectiveLoadingHooks.Contains(t))
        {
            return;
        }

        // if not, unload the hook
        MethodInfo[] methods = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
        foreach (var method in methods)
        {
            if (method.GetCustomAttribute(typeof(ExtendedAttributes.SelectUnload)) != null)
            {
                object instance = method.IsStatic ? null : Activator.CreateInstance(t);
                method.Invoke(instance, null);
            }
        }

        selectiveLoadingHooks.Remove(t);
    }

    public static void UnloadHook<T>()
    {
        UnloadHook(typeof(T));
    }

    public static void UnloadHook(object obj)
    {
        UnloadHook(obj.GetType());
    }

    public static void Load()
    {
        Execute(typeof(ExtendedAttributes.Load), "CorreWithCare");
    }

    public static void Unload()
    {
        Execute(typeof(ExtendedAttributes.Unload), "CorreWithCare");

        // do Selective Unload here
        Type[] types = Assembly.GetExecutingAssembly().GetTypesSafe();

        foreach (var t in types)
        {
            if (!t.FullName.StartsWith("CorreWithCare"))
                continue;

            UnloadHook(t);
        }
    }

    private static void Execute(Type attributeType, string targetNamespace = null)
    {
        Type[] types = Assembly.GetExecutingAssembly().GetTypesSafe();

        foreach (var t in types)
        {
            if (!string.IsNullOrEmpty(targetNamespace) && !t.FullName.StartsWith(targetNamespace))
                continue;

            MethodInfo[] methods = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
            foreach (var method in methods)
            {
                if (method.GetCustomAttribute(attributeType) != null)
                {
                    if (attributeType == typeof(ExtendedAttributes.Load))
                    {
                        forceLoadingHooks.Add(t);
                    }
                    else if (attributeType == typeof(ExtendedAttributes.Unload))
                    {
                        forceLoadingHooks.Remove(t);
                    }

                    object instance = method.IsStatic ? null : Activator.CreateInstance(t);
                    method.Invoke(instance, null);
                }
            }
        }
    }
}
