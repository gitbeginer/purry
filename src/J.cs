using System.Collections;
using System.Web;
namespace purry;

public abstract class J
{
    public static JO O(params ValueTuple<object, object>[] args) => new(args);
    public static JO O<T>(Dictionary<string,T> dic) => new(dic.ToDictionary(x=>x.Key, x=>(object)x.Value));
    public static JL L(params object[] args) => new(args);
    public static JL L<T>(List<T> li) => new(li.Select(x=>(object)x).ToList());

    public dynamic this[object key]
    {
        get => GetValue(key);
        set => SetValue(key, value);
    }
    public abstract void SetValue(object key, object value);
    public abstract object GetValue(object key);
    public abstract void Add(object value);
    public abstract void Add(object key, object value);
    public abstract List<Object> GetList();
    public abstract bool Remove(Object key);

    public static string IfAddQuotes(object o) => o switch
    {
        _ when o is string or char => $"\"{EscapeScript(o.ToString())}\"",
        null => "null",
        _ => o.ToString()
    };



    public abstract string Stringify();

    public static J Parse(string str)
    {
        return FromString(str, 0).rt;
    }

    static readonly Exception syntaxErr = new ArgumentException("Syntax Error.");
    static (J rt, int ix) FromString(string str, int ix)
    {
        void skipEmpty()
        {
            for (; ix < str.Length && Char.IsWhiteSpace(str[ix]); ix++) ;
            if (ix == str.Length) throw syntaxErr;
        }

        skipEmpty();

        J rt = str[ix] switch
        {
            '[' => new JL(),
            '{' => new JO(),
            _ => throw syntaxErr
        };
        ix++;

        bool keyPhase = rt is JO;

        string next()
        {
            skipEmpty();
            if (str[ix] is '[' or '{') return "" + str[ix];
            int st = ix;
            if (str[ix] == '"')
            {
                ix++;
                for (bool esc = false; ix < str.Length; ix++)
                {
                    var c = str[ix];
                    if (c == '"' && !esc) break;
                    esc = !esc && c == '\\';
                }
                if (ix == str.Length) throw syntaxErr;
                ix++;
            }

            string ckstr = "," + (rt is JL ? "]" : keyPhase ? ":}" : "}");
            ix = str.IndexOfAny((" " + ckstr).ToArray(), ix);
            if (ix == -1) throw syntaxErr;

            var rtstr = str[st..ix];
            skipEmpty();
            ix = str.IndexOfAny(ckstr.ToArray(), ix);
            if (ix == -1) throw syntaxErr;

            return rtstr;
        }

        bool endCheck(char c)
        {
            return (rt, c) switch
            {
                (JO, '}') => true,
                (JL, ']') => true,
                (_, ',') => false,
                _ => throw syntaxErr
            };
        }

        for (string key = "?"; ix < str.Length; ix++)
        {
            string tk = next();
            if (tk.Length == 0) break;

            char c = str[ix];
            bool isStr = tk.StartsWith("\"");
            bool isSub = tk is "[" or "{";

            if (isSub)
            {
                if (keyPhase) throw syntaxErr;
                (J child, ix) = FromString(str, ix);
                rt.Add(key, child);
                ix++;
                tk = next();
                c = str[ix];
                if (tk.Length > 0) throw syntaxErr;

                keyPhase = rt is JO;
                if (endCheck(c)) break;

                continue;
            }

            tk = tk.Trim();
            object value;

            if (isStr) value = UnEscapeScript(tk[1..^1]);
            else
            {
                value = tk switch
                {
                    "null" => null,
                    _ when int.TryParse(tk, out int ival) => ival,
                    _ when double.TryParse(tk, out double dval) => dval,
                    _ => tk
                };
            }

            if (rt is JO)
            {
                if (keyPhase && (!isStr || c != ':')) throw syntaxErr;

                if (keyPhase) key = value.ToString();
                else rt.Add(key, value);

                keyPhase = !keyPhase;

                if (keyPhase == false) continue;
            }
            else rt.Add(value);

            if (endCheck(c)) break;
        }

        if (ix == str.Length) throw syntaxErr;

        return (rt, ix);
    }

    public static object EscapeScript(string str)
    {
        return HttpUtility.JavaScriptStringEncode(str);
    }
    
    public static string UnEscapeScript(string str)
    {
        StringBuilder sb = new(str.Length);
        for (int i = 0; i < str.Length; i++)
        {
            if (str[i] == '\\' && i + 1 < str.Length)
            {
                char c = str[++i];
                switch (c)
                {
                    case '\\':
                    case '"':
                        sb.Append(c);
                        break;
                    case 'b':
                        sb.Append('\b');
                        break;
                    case 't':
                        sb.Append('\t');
                        break;
                    case 'n':
                        sb.Append('\t');
                        break;
                    case 'f':
                        sb.Append('\f');
                        break;
                    case 'r':
                        sb.Append('\r');
                        break;
                    default:
                        if (c == 'u' && str.Length > i + 4)
                        {
                            ++i;
                            int hex = Convert.ToInt32(str[i..(i+4)] , 16);
                            sb.Append((char)hex);
                            i+=3;
                            break;
                        }
                        sb.Append(c);
                        break;
                }
            }
            else sb.Append(str[i]);
        }
        return sb.ToString();
    }


    public abstract bool Has(object val);
}

public class JO : J
{
    readonly Dictionary<String, object> dic = new();
    public JO() { }
    public JO(Dictionary<string,object> dic) => this.dic = dic;
    public JO(ValueTuple<object, object>[] args)
    {
        foreach (var tem in args) Add(tem.Item1, tem.Item2);
    }
    public String[] Keys => dic.Keys.ToArray();

    public Dictionary<String, object> Dic => dic;

    public override object GetValue(object key) => dic.GetValueOrDefault(key.ToString());
    public override void SetValue(object key, object value) => dic[key?.ToString() ?? "null"] = value;
    public override void Add(Object value) => dic.Add(value?.ToString() ?? "null", null);

    public override List<Object> GetList() => dic.Keys.Select(x => x as object).ToList();

    public override string Stringify()
    {
        var lq = from x in dic
                 select IfAddQuotes(x.Key) + ":"
                 + ((x.Value as J)?.Stringify() ?? IfAddQuotes(x.Value));

        return $"{{{String.Join(", ", lq)}}}";
    }

    public override void Add(object key, object value) => SetValue(key, value);

    public override bool Has(object key) => dic.Has(key as string);

    public override bool Remove(Object key) => dic.Remove("" + key);
}

public class JL : J, IEnumerable<object>
{
    readonly List<Object> li = new();
    public JL() { }
    public JL(List<object> li)=> this.li = li;
    public JL(object[] args)=> this.li = new List<object>(args);
    public override object GetValue(object key) => li.ElementAtOrDefault((int)key);
    public override void SetValue(object key, object value) => li[(int)key] = value;
    public override void Add(Object value) => li.Add(value);
    public override bool Remove(Object value) => li.Remove(value);
    public override List<Object> GetList() => li;

    public override string Stringify()
    {
        var lq = li.Select(x => (x as J)?.Stringify() ?? IfAddQuotes(x));
        return $"[{String.Join(", ", lq)}]";
    }

    public override void Add(object key, object value) => Add(value ?? key);

    public override bool Has(object val) => li.Contains(val);

    public IEnumerator<object> GetEnumerator() => li.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => li.GetEnumerator();
}
