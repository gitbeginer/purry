namespace purry;

public enum NTYPE { CLIENT, CSHARP, EXPRESS, STRING, LAYOUT, FUNCTION, SECTION, USING }
public abstract partial class ParseNode
{
    public ParseNode parent; //부모 노드
    public List<object> childs; //문자 또는 자식노드가 올수 있음.
    private List<string> tokens; //토큰나이징된 문자열 
    private Stack<string> pre; //이전 토근 값
    protected int i;  //현재 인덱스
    protected int pi; //과거 인덱스
    protected string tk; //현재 토큰 값
    protected static TTYPE GetType(string str) => TemplateEngine.GetTokenType(str);
    public abstract NTYPE NType { get; }

    public ParseNode(ParseNode parent) : this(parent.tokens, parent)
    { //자식노드를 위한 생성자
        this.parent = parent ?? throw new ArgumentNullException(nameof(parent));

    }
    public ParseNode(List<string> tokens, ParseNode parent = null)
    {  //루트노드를 위한 생성자
        this.parent = parent;
        Init(tokens);
        Load();
    }
    protected abstract void Load(); //구현해야할 메인 로직

    protected void Init(List<string> tokens)
    { //초기화
        this.tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        if (GetType(tokens.LastOrDefault()) != TTYPE.WHITE_SPACE) tokens.Add("\r\n");
        this.childs = new List<object>();
        this.pre = new Stack<string>();
        this.i = parent?.i ?? -1;
        this.pi = parent?.pi ?? 0;
        this.tk = parent?.tk;

    }

    protected string Next(bool noPre = false)
    {  //공백을 스킵하고 다음 토큰 반환
        this.i++;
        string token = SkipSpace();
        if (noPre == false) this.pre.Push(tk);
        this.tk = token;
        return token;
    }

    protected string NextRaw(bool noPre = true)
    {   //다음 토큰 반환
        this.i++;
        var token = (tokens.Count <= i) ? null : tokens[i];
        if (noPre == false) this.pre.Push(tk);
        this.tk = token;
        return token;
    }

    //주석을 만났을 떄 되돌리기 
    protected void Set_tk_toBack() => tk = pre.Count > 0 ? pre.Pop() : null;

    protected string SkipSpace()
    { //공백 문자 스킵
        for (; i < tokens.Count; i++)
        {
            if (GetType(tokens[i]) != TTYPE.WHITE_SPACE) return tokens[i];
        }
        return null;
    }

    protected string Offset(int x)
    {  //상대적 위치 단일 문자열
        if (x + i < 0 || x + i >= tokens.Count) return null;
        return tokens[x + i];
    }

    protected string Offset(int x, int y)
    { // 상대적 위치 범위 문자열 
        if (x > y) return null;
        if (x + i < 0 || x + i >= tokens.Count) return null;
        if (y + i < 0 || y + i >= tokens.Count) return null;
        return String.Join(null, tokens.GetRange(x + i, y - x + 1));
    }

    protected string OffsetS(int x)
    { //공백을 제외한 상대적 위치
        if (x == 0) return Offset(0);
        int pos = i;
        int step = x > 0 ? 1 : -1;
        while (x != 0)
        {
            pos += step;
            if (pos < 0 || tokens.Count <= pos) return null;
            if (!String.IsNullOrWhiteSpace(tokens[pos])) x -= step;
        }
        return tokens[pos];
    }

    protected string AddStr() => AddStr(pi, i);

    protected string AddStr(int s_idx, int e_idx)
    {  //문자열을 자식 노드로 저장
        pi = e_idx;
        if (e_idx > tokens.Count) e_idx = tokens.Count;
        if (e_idx <= s_idx) return null;
        var str = String.Join(null, tokens.GetRange(s_idx, e_idx - s_idx));
        if (str.Length > 0) this.childs.Add(str);

        return str;
    }

    protected ParseNode Invoke(Type type)
    {  //자식 노드로 분기
        var childNode = Activator.CreateInstance(type, this) as ParseNode;
        this.childs.Add(childNode);
        (i, pi) = (childNode.i, childNode.pi);
        if (Offset(0) == null) Err();
        this.pre.Clear();
        return childNode;
    }

    protected void Err(String msg = null)
    {  //에러메세지.. 대충.. 뜨로우..
        int stE = this.i - 10;
        if (stE < 0) stE = 0;
        int edE = this.i + 10;
        if (edE >= tokens.Count) edE = tokens.Count - 1;
        throw new ArgumentException("Invaild syntax: " + msg + "\n"
            + String.Join("", tokens.GetRange(stE, edE - stE)));
    }
}