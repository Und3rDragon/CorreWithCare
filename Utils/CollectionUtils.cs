using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorreWithCare.Utils;

public static class CollectionUtils
{
    public static void Create<A,B>(this Dictionary<A,B> dic, A at, B fill)
    {
        if (!dic.ContainsKey(at))
        {
            dic[at] = fill;
        }
    }

    public static void Create<T>(this List<T> list, int at, T fill)
    {
        if(list == null)
        {
            list = new();
            for(int i = 0; i <= at; i++)
            {
                list[i] = fill;
            }
            return;
        }

        if(list.Count <= at)
        {
            for(int i = list.Count; i <= at; i++)
            {
                list[at] = fill;
            }
            return;
        }
    }

    public static void Create<T>(this List<T> list, T search)
    {
        if (!list.Contains(search))
        {
            list.Add(search);
        }
    }
}
