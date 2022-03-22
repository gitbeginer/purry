namespace purry;
internal class TagNode : ParseNode
{
    public TagNode(ParseNode parent) : base(parent) { }

    public override NTYPE NType => NTYPE.CLIENT;

    protected override void Load()
    {
        var tagName = Offset(1)?.ToLower();

        Loop();

        if (Offset(-1).Last() == '/') return;

        Invoke(tagName switch
        {
            "script" => typeof(JS_InNode),
            "style" => typeof(CSS_node),
            _ => typeof(InTagNode)
        });

        Loop();
    }

    private void Loop()
    {
        while (Next() != null)
        {
            if (Razor()) continue;

            if (tk is "\"" or "'")
            {
                AddStr(pi, i + 1);
                Invoke(typeof(TagStrNode));
                continue;
            }

            if (tk == ">") break;
        }
        AddStr(pi, i + 1);
    }
}

internal class TagStrNode : ParseNode
{
    public TagStrNode(ParseNode parent) : base(parent) { }

    public override NTYPE NType => NTYPE.CLIENT;

    protected override void Load()
    {
        var q = Offset(0);
        while (Next() != null)
        {
            if (Razor()) continue;
            if (tk == q) break;
        }

        AddStr();
    }
}
