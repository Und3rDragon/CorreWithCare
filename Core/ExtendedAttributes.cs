using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorreWithCare.Core;

public static class ExtendedAttributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class Load : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class Unload : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class SelectLoad : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class SelectUnload : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.All)]
    public class Credits : Attribute
    {
        public Credits(params object[] info) { }
    }

    [AttributeUsage(AttributeTargets.All)]
    public class Note : Attribute
    {
        public Note(params object[] info) { }
    }
}
