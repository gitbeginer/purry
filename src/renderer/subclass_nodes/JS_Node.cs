namespace purry;
internal abstract class JS_Node : ParseNode
{
    protected string comment = null; //  // or /*
    protected JS_Node(ParseNode parent) : base(parent) { }
    protected bool JsString()
    {
        if ("\"'`".Contains(tk) && comment == null)
        {
            AddStr(pi, i + 1);
            Invoke(typeof(JS_StrNode));
            return true; ;
        }
        return false;
    }
    protected bool JsComment()
    {
        if (comment == null)
        {
            if (tk == "/" && Offset(1).First() is '*' or '/')
            {
                comment = tk + Offset(1).First();
                return true;
            }
        }
        else
        {
            if (comment == "//" && Offset(1).Contains('\n'))
            {
                comment = null;
                return true;
            }
            if (comment == "/*" && Offset(-1, 0) == "*/")
            {
                comment = null;
                return true;
            }
        }
        return false;
    }

    protected bool JsCommon() => (Razor() || JsComment() || JsString());
}
internal class JS_InNode : JS_Node
{
    public JS_InNode(ParseNode parent) : base(parent) { }

    public override NTYPE NType => NTYPE.CLIENT;

    protected override void Load()
    {

        while (Next() != null)
        {
            if (JsCommon()) continue;
            if (comment == null)
            {
                if (parent is JS_StrNode)
                {
                    if (tk == "}") break;
                }
                else
                {
                    if (tk == "<" && Offset(1) == "/") break;
                }
            }
        }
        AddStr();
    }
}

internal class JS_StrNode : JS_Node
{
    public JS_StrNode(ParseNode parent) : base(parent) { }

    public override NTYPE NType => NTYPE.CLIENT;

    protected override void Load()
    {
        var q = Offset(0);
        while (Next() != null)
        {
            if (Razor()) continue;
            if (q == "`" && tk == "{" && Offset(-1).Last() == '$')
            {
                AddStr(pi, i + 1);
                Invoke(typeof(JS_InNode));
                continue;
            }

            if (tk == q && Offset(-1) != "\\") break;
        }
        AddStr();
    }
}