using System.Collections.Specialized;
using System.Net.Sockets;
using System.Web;
namespace purry;

public class Request : Dictionary<string, string>
{
    public enum Method { GET, POST, UNKNOW }
    public const int MAXSIZE = 1024 * 1024 * 50;
    public readonly Method method = Method.UNKNOW;
    public NameValueCollection param = new();
    internal readonly Socket client;
    public string err = null, url, sessionID;
    public Dictionary<string, string> cookie = new();
    public readonly JO mPart;
    public readonly string body;
    private readonly static Dictionary<string, Dictionary<string, string>> sessionDic = new();
    private readonly static Dictionary<string, TempJO> tempJoDic = new();
    public Dictionary<string, string> Sess => sessionDic.GetD(sessionID) ?? new();
    public TempJO TempData => tempJoDic.GetD(sessionID) ?? new();
    public string Nickname => Sess.FirstOrDefault().Value;
    public static int SessionCnt => sessionDic.Count;
    public Request(Socket client)
    {
        this.client = client;
        byte[] bytebuffer = new byte[8192];
        int cnt = client.Receive(bytebuffer);
        if (cnt == 0) return;

        var strs = Util.GetString(bytebuffer, 0, cnt);
        int headLast = strs.IndexOf("\r\n\r\n");
        if (headLast == -1)
        {
            this.err = "invaild syntex";
            return;
        }
        strs = strs[0..headLast];

        String[] split = strs.Split("\r\n");

        #region top-line parse        
        var topLine = split.FirstOrDefault();
        this[nameof(topLine)] = topLine;
        var dic = new[] { "method", "url", "http" }
        .Zip(topLine.Split(' '), (a, b) => new { a, b })
        .ToDictionary(x => x.a.ToString(), x => x.b.Trim());

        foreach (Method m in Enum.GetValues(typeof(Method)))
        {
            if (dic["method"] != m.ToString()) continue;
            this.method = m;
            break;
        }

        foreach (var item in dic) this.Add(item.Key, item.Value);

        this.err = (this.method, this.Has("url")) switch
        {
            (Method.UNKNOW, _) => "Invaild method",
            (_, false) => "Can't find url",
            _ => null
        };
        if (this.err != null) return;


        url = this["url"];
        //System.Console.WriteLine(url);
        var startIdx = url.IndexOf('?');
        if (startIdx > -1)  //GET 파라미터
        {
            this.param = HttpUtility.ParseQueryString(url[startIdx..]);
            url = url[..startIdx];
        }
        #endregion

        #region rest-header parse
        var infos = split.Skip(1).GetEnumerator();
        while (infos.MoveNext())
        {
            var val = infos.Current.Trim();
            var sp = val.Split(':', 2);
            this.Add(sp[0].Trim(), sp[^1].Trim());
        }
        #endregion

        this.cookie = ParseColon(this.GetD("Cookie"));
        var reqID = cookie.GetD(nameof(sessionID));
        this.sessionID = sessionDic.GetD(reqID) != null ? reqID : null;

        #region Getting body data
        var bodyLen = int.Parse("0" + this.GetD("Content-Length"));


        var sb = new StringBuilder(Math.Min(MAXSIZE, bodyLen));

        int stbody = strs.Length + 4;
        int curLen = cnt - stbody;
        if (curLen > 0) sb.Append(Util.GetString(bytebuffer, stbody, curLen));

        while (bodyLen > curLen)
        {
            int gap = Math.Min(bodyLen - curLen, bytebuffer.Length);
            cnt = client.Receive(bytebuffer, gap, SocketFlags.None);
            if (bodyLen <= MAXSIZE) sb.Append(Util.GetString(bytebuffer, 0, cnt));
            curLen += cnt;
        }

        if (bodyLen > MAXSIZE)
        {
            this.err = "413"; //Payload large
            WriteLine("" + bodyLen / 1024 + " kbytes! Maximum is " + MAXSIZE / 1024);
            return;
        }

        this.body = sb.ToString();
        this["body"] = this.body;
        //WriteLine("body: "+ body);
        #endregion


        if (this.GetD("Content-Type") == "application/x-www-form-urlencoded")
        {
            this.param = HttpUtility.ParseQueryString(body); //POST 파라미터
        }

        if ((this.GetD("Content-Type") + "").StartsWith("multipart/form-data"))
        {
            this.mPart = MakeMultipart();
        }

        #region console.writeLine for checking 
  
        //WriteLine($"[req]-----------[{DateTime.Now}]-----------["+ (Nickname ?? "newbie")+"]");
        //foreach (var t in this) WriteLine($"{t.Key}: {t.Value}");
        //foreach (var t in param.Keys) WriteLine($"{t}: {param.Get(t.ToString())}");
        #endregion
    }

    public string NewSessionID()
    {
        sessionID = Util.NewSessionID(client);
        sessionDic[sessionID] = new() { [nameof(this.Nickname)] = Util.GetNickName(sessionDic.Count) };
        tempJoDic[sessionID] = new();
        return sessionID;
    }

    public static Dictionary<string, string> ParseColon(string value)
    {
        var dic = (value ?? "").Split(';').Select(x => x.Split('='))
        .ToDictionary(x => x[0].Trim(), x => x[^1].Trim());
        return dic;
    }

    JO MakeMultipart()
    {
        string boundary = "\r\n--" + ParseColon(this["Content-Type"])["boundary"].Trim('"');
        string data = "\r\n" + body;
        var jo = new JO();
        int idx = 0;

        while ((idx = data.IndexOf(boundary, idx)) != -1)
        {
            idx += boundary.Length + 2;
            if (data.Length <= idx + 2) break;

            if (data.Substring(idx, 20) != "Content-Disposition:")
            {
                this.err = "bad format";
                return null;
            }
            idx += 20;

            int idx2 = data.IndexOf(";", idx);
            if (idx2 == -1)
            {
                this.err = "bad format";
                return null;
            }
            JO newObj = J.O(("Content-Disposition", data[idx..idx2].Trim()));

            idx = idx2;
            idx2 = data.IndexOf("\r\n", idx);
            var str = data[idx..idx2];
            var tdic = ParseColon(str);
            string name = tdic.GetD("name").Replace("\"", "");
            if (name == null)
            {
                this.err = "bad format";
                return null;
            }

            jo[name] ??= new JL();
            (jo[name] as JL).Add(newObj);
            foreach (var tem in tdic) newObj[tem.Key] = tem.Value;

            do
            {
                idx = idx2 + 2;
                idx2 = data.IndexOf("\r\n", idx);

                if (idx2 == -1)
                {
                    this.err = "bad format";
                    return null;
                }

                str = data[idx..idx2];

                if (str.Trim().Length > 0)
                {
                    var sp = str.Split(':');
                    newObj.Add(sp.First(), sp.LastOrDefault());
                }
                else break;

            } while (true);

            idx = idx2 + 2;
            idx2 = data.IndexOf(boundary, idx);

            if (idx2 - idx < 0)
            {
                this.err = "bad format";
                return null;
            }
            var substr = data[idx..idx2];
            var bytes = Util.GetBytes(substr);

            newObj["data"] = newObj.Has("filename") ? bytes : Encoding.UTF8.GetString(bytes);
        }

        return jo;
    }
}
