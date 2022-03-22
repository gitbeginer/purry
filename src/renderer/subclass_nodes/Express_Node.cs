namespace purry;
internal class Express_Node : ParseNode //정신적인 상속..
{
    protected Express_Node(ParseNode parent) : base(parent) { }

    public override NTYPE NType => NTYPE.EXPRESS;

    protected override void Load() => throw new NotImplementedException();
}

internal class InlineNode : Express_Node
{
    public InlineNode(ParseNode parent) : base(parent) { }
    protected override void Load() => ArgsSection(true);
}
internal class ImplicitNode : Express_Node
{
    public ImplicitNode(ParseNode parent) : base(parent) { }

    protected override void Load()
    {
        while (NextRaw() != null)
        {
            if (GetType(tk) == TTYPE.WHITE_SPACE) break;
            if ((tk is "?." or "." or "[" or "(") == false) break;
            if (tk == "(")
            {
                ArgsSection(false, false);
                continue;
            }
            if (tk == "[")
            {
                IndexSection(false, false);
                continue;
            }
            if (GetType(NextRaw()) != TTYPE.LITERAL) Err();
        }
        AddStr(pi, i--);
    }
}

