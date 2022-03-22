namespace purry;
public abstract class View
{
    protected View sub = null;
    protected Request req;
    protected Response res;
    protected TempJO TempData;
    protected JO ViewData;
    protected dynamic ViewBag;


    protected String Layout = null;
    private StringBuilder html = new();
    private readonly Stack<string> secStack = new(new[] { "@Body" });
    private readonly Dictionary<string, StringBuilder> section;

    public View(View sub) : this(sub.res, sub.ViewData, (object)sub.ViewBag, sub.TempData)
    {
        this.sub = sub;
    }

    public View(Response res, JO ViewData, dynamic ViewBag, TempJO TempData)
    {
        this.req = res?.req;
        this.res = res;
        this.ViewData = ViewData ?? new JO();
        this.ViewBag = ViewBag ?? new System.Dynamic.ExpandoObject();
        this.TempData = TempData ?? new TempJO();
        this.section = new() { [secStack.Peek()] = html };
    }

    public abstract string GetHTML();
    protected void W(object str) => html.AddL("" + str);
    protected string GetW() => html.ToString().Replace('＂', '"');

    protected string RenderBody() => RenderSection(secStack.First());
    protected string RenderSection(string name, bool required = false)
    {
        if (sub.section.ContainsKey(name) == false)
        {
            if (required)
            {
                ArgumentException ex = new("there is no Section", nameof(name));
                throw ex;
            }
            return "";
        }
        return sub.section[name].ToString();

    }
    protected void SetSection(string name)
    {
        secStack.Push(name);
        if (section.ContainsKey(name)) html = section[name];
        else html = section[name] = new StringBuilder();

    }
    protected void OffSection(string name)
    {
        if (secStack.Pop() != name)
        {
            ArgumentException ex = new("Not match", nameof(name));
            throw ex;
        }

        html = section[secStack.Peek()];
    }

    protected string GetClassName() => GetClassName(Layout);
    public static string GetClassName(string Layout)
    {
        var name = Layout.Trim('"').Trim('~');
        name = name.Replace('/', '_').Replace('\\', '_');
        name = name[0..name.LastIndexOf('.')];
        return name;
    }
}
