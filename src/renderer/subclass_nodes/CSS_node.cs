namespace purry;
internal class CSS_node : ParseNode
{
    public CSS_node(ParseNode parent) : base(parent) { }

    public override NTYPE NType => NTYPE.CLIENT;

    protected override void Load()
    {
        bool comment = false;
        while (Next != null)
        {
            if (Razor()) continue;
            if (comment == false)
            {
                if (tk == "/" && Offset(1).First() is '*')
                {
                    comment = true;
                    continue;
                }
                if (tk is "\"" or "'")
                {
                    AddStr(pi, i + 1);
                    Invoke(typeof(TagStrNode));
                    continue;
                }
                if (tk == "<" && Offset(1) == "/") break;
            }
            else if (Offset(-1, 0) == "*/") comment = false;
        }
        AddStr();
    }
}
