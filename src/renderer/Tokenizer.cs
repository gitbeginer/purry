namespace purry;

public enum TTYPE { WHITE_SPACE, FUNCTIONAL, LITERAL, NULL }
public partial class TemplateEngine
{
    public static readonly HashSet<string> tokStr = new(("@,\",',#,`,&,~,^,<<,>>,!,&&" +
            ",||,==,!=,!==,>,<,>=,<=,?,?.,??,??=,=,+=,-=,%=,*=,+" +
            ",-,*,/,++,--,=,(,),[,],{,},:,>,<,;,\\,\\\\,.").Split(","))
    { "," };
    public static TTYPE GetTokenType(string str)
    {
        if (str == null) return TTYPE.NULL;
        if (String.IsNullOrWhiteSpace(str)) return TTYPE.WHITE_SPACE;
        if (tokStr.Contains(str)) return TTYPE.FUNCTIONAL;
        return TTYPE.LITERAL;
    }

    public static List<string> Tokenize(string txt)
    {
        var buff = ")";
        var rs = new List<String>();
        var preType = GetTokenType(buff.ToString());
        foreach (Char ch in txt)
        {
            var type = GetTokenType(ch.ToString());
            if (type != preType || (type is TTYPE.FUNCTIONAL && type != GetTokenType(buff + ch)))
            {
                preType = type;
                rs.Add(buff);
                buff = "";
            }
            buff += ch;
        }
        rs.Add(buff);
        rs.RemoveAt(0);

        return rs;
    }
}

