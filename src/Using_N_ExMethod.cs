global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Text;
global using static System.Console;
global using Env = System.Environment;
using System.Runtime.CompilerServices;

namespace purry;

static class ExMethod
{
    public static T[] Shuffle<T>(this IEnumerable<T> en)
    {
        Random rng = new(Environment.TickCount);
        var array = en.ToArray();
        int n = array.Length;
        int c = new Func<int>(()=>n)();
        while (n > 1)
        {
            int k = rng.Next(n--);
            (array[k], array[n]) = (array[n], array[k]);
        }
        return array;
    }
    public static bool Has<Tk, Tv>(this Dictionary<Tk, Tv> dic, Tk key) => key != null && dic.ContainsKey(key);
    public static Tv GetD<Tk, Tv>(this Dictionary<Tk, Tv> dic, Tk key) => dic.Has(key) ? dic[key] : default;
    public static Tv GetD<Tk, Tv>(this Dictionary<Tk, Tv> dic, Tk key, Tv val) => dic.Has(key) ? dic[key] : val;

    public static StringBuilder AddL(this StringBuilder sb, string str) => sb.AppendLine(str);
    public static StringBuilder AddL(this StringBuilder sb) => sb.AppendLine();


    private static IEnumerable<object> GetObjs(object value_tuple)
    {
        if (value_tuple is not ITuple ituple) yield break;
        for (int i = 0; i < ituple.Length; i++) yield return ituple[i];
    }
    public static IEnumerable<object> Enum<T1, T2>(this ValueTuple<T1, T2> vt) => GetObjs(vt);
    public static IEnumerable<object> Enum<T1, T2, T3>(this ValueTuple<T1, T2, T3> vt) => GetObjs(vt);
    public static IEnumerable<object> Enum<T1, T2, T3, T4>(this ValueTuple<T1, T2, T3, T4> vt) => GetObjs(vt);
    public static IEnumerable<object> Enum<T1, T2, T3, T4, T5>(this ValueTuple<T1, T2, T3, T4, T5> vt) => GetObjs(vt);
    public static IEnumerable<object> Enum<T1, T2, T3, T4, T5, T6>(this ValueTuple<T1, T2, T3, T4, T5, T6> vt) => GetObjs(vt);
    public static IEnumerable<object> Enum<T1, T2, T3, T4, T5, T6, T7>(this ValueTuple<T1, T2, T3, T4, T5, T6, T7> vt) => GetObjs(vt);
}
