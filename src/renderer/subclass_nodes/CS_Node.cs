namespace purry;
internal class CS_inNode : ParseNode
{
    public CS_inNode(ParseNode parent) : base(parent) { }

    public override NTYPE NType => NTYPE.CSHARP;

    protected override void Load() => BlockSection(true);
}
internal class CS_FunctionNode : ParseNode
{
    public CS_FunctionNode(ParseNode parent) : base(parent) { }

    public override NTYPE NType => NTYPE.FUNCTION;

    protected override void Load() => BlockSection(true);
}
internal class LayoutNode : ParseNode
{
    public LayoutNode(ParseNode parent) : base(parent) { }

    public override NTYPE NType => NTYPE.LAYOUT;

    protected override void Load() => ArgsSection(true);
}
internal class CS_UsingNode : ParseNode
{
    public CS_UsingNode(ParseNode parent) : base(parent) { }

    public override NTYPE NType => NTYPE.USING;

    protected override void Load()
    {
        bool isStatic = false;
        while (Next() != null)
        {

            if (tk == "static")
            {
                if (isStatic) Err("CRAZY SYNTAX!");
                isStatic = true;
                continue;
            }

            if (GetType(tk) != TTYPE.LITERAL) Err();
            if (OffsetS(1) == "=")
            {
                Next();
                continue;
            }
            if (GetType(Offset(+1)) == TTYPE.WHITE_SPACE) break;

            Next();
            if (tk == ";") break;
            if (tk != ".") Err();
        }
        AddStr(pi, i + 1);
    }
}
internal class CS_StrNode : ParseNode
{
    public CS_StrNode(ParseNode parent) : base(parent) { }

    public override NTYPE NType => NTYPE.STRING;

    protected override void Load()
    {
        bool isInterpol = Offset(-1).LastOrDefault() == '$';
        while (Next() != null)
        {
            if (tk == "{")
            {
                if (Offset(1) == "{")
                {
                    Next();
                    continue;
                }
                if (isInterpol)
                {
                    BlockSection(false, false);
                    continue;
                }
            }

            if (tk == "\"" && Offset(-1) != "\\") break;
        }
        AddStr();
    }
}

