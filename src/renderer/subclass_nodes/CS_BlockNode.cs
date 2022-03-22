namespace purry;
internal class CS_BlockNode : ParseNode
{
    private string name;

    public override NTYPE NType => NTYPE.CSHARP;

    public CS_BlockNode(ParseNode parent) : base(parent) { }

    protected override void Load()
    {
        name = tk;
        switch (name)
        {
            case "do":
                DoBlock(); break;
            case "else":
                NextBolockOrColon(); break;
            case "try":
            case "finally":
                BlockSection(false, false); break;
            default:
                NormalBlock(); break;
        }
        AddStr(pi, i + 1);
    }

    private void DoBlock()
    {
        BlockSection(false, false);
        ArgsSection(false, false);
        NextCommentSkip();
        if (tk != ";") Err();
    }

    void NormalBlock()
    {
        ArgsSection(false, false);
        NextBolockOrColon();
    }

    void NextBolockOrColon()
    {
        NextCommentSkip();

        if (tk == "{")
        {
            BlockSection(false, false);
            AddStr(pi, i + 1);
            return;
        }

        if (name is "switch" or "look" or "catch") Err(name + " needs block statement.");

        if (tk == ";" || Razor())
        {
            AddStr(pi, i + 1);
            return;
        }

        if (GetType(tk) != TTYPE.LITERAL) Err();

        while (Next() != null)
        {
            if (tk == ";") break;
            if (CsComment() || CsString() || Razor()) continue;
            if (tk == "{") BlockSection();
        }
    }
}
