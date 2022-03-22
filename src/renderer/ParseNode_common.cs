namespace purry;
public abstract partial class ParseNode
{
    protected bool RazorComment()
    {//  @*로 시작하는 전역 코맨트 
        if (tk == "@" && Offset(1)?.First() == '*')
        {
            AddStr();
            while (Next(true) != null) if (Offset(-1)?.Last() == '*' && tk == "@") break;
            pi = i + 1; // 코맨트는 버린다.

            Set_tk_toBack();
            return true;
        }
        return false;
    }
    protected bool Razor()
    { // Razor 구문 전반.
        if (tk != "@") return false;

        int rcnt = 0;
        for (int x = i; --x >= pi && tokens[x] == "@"; rcnt++) ;
        if (rcnt % 2 == 1) return false;

        if (RazorComment()) return true;

        if (Offset(1) is "(" or "{")
        {
            AddStr();
            Invoke(Offset(1) == "{" ? typeof(CS_inNode) : typeof(InlineNode));
            return true;
        }

        if (GetType(Offset(-1)) == TTYPE.LITERAL) return false;
        if (GetType(Offset(+1)) != TTYPE.LITERAL) return false;

        AddStr();
        Next();
        pi = i; //@ 제거.

        if (tk == "using")
        {
            Invoke(typeof(CS_UsingNode));
            return true;
        }

        if (tk == "layout")
        {
            Next();
            pi = i + 1;
            Invoke(typeof(LayoutNode));
            return true;
        }

        if (tk == "functions")
        {
            Next();
            pi = i + 1;
            Invoke(typeof(CS_FunctionNode));
            return true;
        }

        if (tk == "section")
        {
            Next();
            pi = i;
            Invoke(typeof(SectionNode));
            return true; ;
        }

        if ("if,do,for,foreach,while,look,switch,try".Split(",").Contains(tk))
        {
            BlockStatement();
            return true;
        }

        Invoke(typeof(ImplicitNode));
        return true;
    }

    
    private void BlockStatement(bool inSide = false)
    {
        var newChild = Invoke(typeof(CS_BlockNode));
        if (inSide == true) //입양 절차
        {
            childs.Remove(newChild);
            var adoptive_parent = childs.Last() as CS_BlockNode;
            newChild.parent = adoptive_parent;   //부모가 되고
            adoptive_parent.childs.Add(newChild);//이윽고 자식을 갖는다.
        }

        if (tk == "if")
        {  // if.. else.. if.. else.. if.. else.. if..
            var back = i;
            NextCommentSkip();
            if (tk == "else")
            {   // I lost something else.
                back = i;
                NextCommentSkip();
                if (tk != "if")
                {
                    tk = "else";
                    i = back;
                }
                BlockStatement(true);
            }
            else i = back;
            return;
        }

        if (tk is "try" or "catch")
        {  // try + [catch] * N + finally.
            var back = i;
            NextCommentSkip();
            if (tk is "catch" or "finally")
            {
                BlockStatement(true);
            }
            else i = back;
        }
    }

    protected string NextCommentSkip()
    {
        Next(true);
        while (CsComment() || RazorComment()) Next(true);
        return tk;
    }


    protected bool CsComment()
    { // csharp 코맨트
        if (tk != "/") return false;

        if (Offset(1)?.First() == '*')
        {
            AddStr();
            Next(true);
            while (Next(true) != null)
            {
                if (tk == "/" && Offset(-1)?.Last() == '*') break;
            }
            pi = i + 1; //코맨트는 버린다.

            Set_tk_toBack();

            return true;
        }

        if (Offset(1)?.First() == '/')
        {
            AddStr();
            NextRaw(true);
            while (NextRaw(true) != null)
            {
                if (tk.Contains('\n')) break;
            }

            pi = i; //코맨트는 버린다.

            Set_tk_toBack();

            return true;
        }

        return false;
    }

    protected bool CsString()
    {
        if (tk == "\"" && Offset(-1) != "'")
        {
            AddStr(pi, i + 1);
            Invoke(typeof(CS_StrNode));
            return true;
        }
        return false;
    }

    private void CsSection(string st, string ed, bool cutOut, bool add)
    {
        do
        {
            if (tk == st) break;
            CsComment();
            RazorComment(); //VS2022에서는 구문오류가 된다.(???)
        } while (Next() != null);

        if (cutOut) pi = i + 1;

        int closeCnt = 1;
        while (Next() != null)
        {
            if (CsComment() || CsString() || Razor()) continue;

            //태그 구간 찾기
            if (st == "{" && tk == "<" && (GetType(Offset(1)) == TTYPE.LITERAL || Offset(1) == "@"))
            {
                string p = null;
                for (int x = i; --x >= pi;)
                {
                    if (!String.IsNullOrWhiteSpace(tokens[x]))
                    {
                        p = tokens[x];
                        break;
                    }
                }

                if (p == null)
                {
                    for (int x = childs.Count; --x >= 0;)
                    {
                        if (childs[x] is string)
                        {
                            p = childs[x].ToString().Trim();
                            if (!String.IsNullOrWhiteSpace(p)) break;
                        }
                        else break;
                    }
                    if (p == null) p = ";";
                }


                if ("{};".Contains(p?.Last() ?? '?'))
                {
                    AddStr();
                    Invoke(typeof(TagNode));
                    continue;
                }
            }

            if (tk == st) closeCnt++;
            else if (tk == ed && --closeCnt == 0) break;
        }

        if (add) AddStr();
        if (cutOut) pi++;
    }

    protected void ArgsSection(bool cutOuter = false, bool add = true)
    {
        CsSection("(", ")", cutOuter, add);
    }
    protected void BlockSection(bool cutOuter = false, bool add = true)
    {
        CsSection("{", "}", cutOuter, add);
    }
    protected void IndexSection(bool cutOuter = false, bool add = true)
    {
        CsSection("[", "]", cutOuter, add);
    }
}