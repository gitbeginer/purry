namespace purry;

internal class InTagNode : ParseNode
{
    public InTagNode(List<String> tokens) : base(tokens) { }
    public InTagNode(ParseNode parent) : base(parent) { }

    public override NTYPE NType => NTYPE.CLIENT;

    protected override void Load()
    {
        bool isRoot = (i == -1);
        bool comment = false;

        while (Next() != null)
        {
            if (Razor()) continue;

            if (comment == false)
            {
                if (tk == "-" && Offset(-3, 0) == "<!--")
                {
                    comment = true;
                    continue;
                }

                string ns = Offset(1);
                if (tk == "<" && (GetType(ns) == TTYPE.LITERAL || ns == "@"))
                {
                    AddStr();
                    Invoke(typeof(TagNode));
                    continue;
                }

                if (isRoot == false)
                {
                    if (parent is SectionNode)
                    {
                        if (tk == "}") break;
                    }
                    else
                    {
                        if (tk == "<" && ns == "/") break;
                    }
                }
            }
            else
            {
                if (tk == ">" && Offset(-2, 0) == "-->")
                {
                    comment = false;
                    continue;
                }
            }
        }
        AddStr();
    }
}
internal class SectionNode : ParseNode
{
    public string Name { get; private set; }

    public override NTYPE NType => NTYPE.SECTION;

    public SectionNode(ParseNode parent) : base(parent) { }

    protected override void Load()
    {
        this.Name = tk;
        Next();  // {
        pi = i + 1;
        Invoke(typeof(InTagNode));
        pi++; // }
    }
}
