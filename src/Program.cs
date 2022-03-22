using purry;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Web;
using static System.Text.RegularExpressions.Regex;

var dbClient = new MongoClient(Util.Settings["dbcon"].Value);

var db = dbClient.GetDatabase("inde_game_dev");
var vv_list = db.GetCollection<BsonDocument>("vv_list");
var filter = Builders<BsonDocument>.Filter;
var nickList = vv_list.Distinct<String>("nick", filter.Empty).ToList();
var titleList = vv_list.Distinct<String>("title", filter.Empty).ToList();
var ser_json = J.O(
    ("nicks", J.L(nickList.Select(x => HttpUtility.HtmlDecode(x)).ToList())),
    ("titles", J.L(titleList.Select(x => HttpUtility.HtmlDecode(x)).ToList()))
);

var app = new Express();
app.ErrLog += (req, msg) =>
{
    var decodeUrl = System.Web.HttpUtility.UrlDecode(req.GetD("url") ?? "");
    var err_col = db.GetCollection<BsonDocument>("err_log");
    var doc = new BsonDocument
                {
                    {"timeStemp",  DateTime.Now },
                    {"msg", msg ?? ""},
                    {"sessID",  req.sessionID ?? "" },
                    {"sessCnt", Request.SessionCnt},
                    {"method", req["method"] ?? ""},
                    {"nick", req.Nickname ?? ""},
                    {"url", decodeUrl ?? ""},
                    {"ref", req.GetD("Referer") ??""},
                    {"ori", req.GetD("Origin") ??""},
                };
    err_col.InsertOne(doc);

};
app.ReqLog += req =>
{
    var decodeUrl = System.Web.HttpUtility.UrlDecode(req.GetD("url") ?? "");
    var RefererUrl = System.Web.HttpUtility.UrlDecode(req.GetD("Referer") ?? "");

    if (Env.OSVersion.Platform == PlatformID.Win32NT)
    {
        var log = $"[{req["method"]}]----[{DateTime.Now}]----[{(req.Nickname ?? "newbie")}]----[{Request.SessionCnt}]";
        log += Env.NewLine + decodeUrl;
        log += Env.NewLine + RefererUrl;
        log += Env.NewLine + req.GetD("Origin");

        WriteLine(log);
    }
    else
    {
        var req_col = db.GetCollection<BsonDocument>("req_log");
        var doc = new BsonDocument
                {
                    {"timeStemp",  DateTime.Now },
                    {"sessID",  req.sessionID ?? ""},
                    {"sessCnt", Request.SessionCnt},
                    {"method", req["method"] ?? ""},
                    {"nick", req.Nickname ?? ""},
                    {"url", decodeUrl ?? ""},
                    {"ref", RefererUrl ??""},
                    {"ori", req.GetD("Origin") ??""},
                };
        req_col.InsertOne(doc);
    }
};


app.Get("/main", (req, res) =>
{
    var viewData = J.O(
        ("title", "Hello 수붕"),
        ("jdata", "null"));
    res.Render("list_view", viewData);
});

app.Get("/user", (req, res) =>
{
    int pnum = int.Parse("0" + req.param.Get("p"));
    var docs = vv_list.Find(filter.Eq("page_num", pnum));
    var doc = docs.FirstOrDefault();
    if (doc == null)
    {
        res.Send("404 not found", 404);
        return;
    }
    var nick = doc["nick"].AsString;
    var ip = doc["ip"].AsString;
    var uid = doc["uid"].AsString;
    var title = (nick + (ip == "" ? "" : "(" + ip + ")")) + " 개발일지";
    var jsonStr = J.O(("nick", nick), ("uid", uid), ("ip", ip)).Stringify();
    res.Render("list_view", J.O(("title", title), ("jdata", jsonStr)));
});

app.Post("/items", (req, res) =>
{
    int pnum = int.Parse("0" + req.param.Get("p"));
    var fi = filter.Lt("page_num", pnum);

    var jsonStr = req.param.Get("tar");
    if (!String.IsNullOrWhiteSpace(jsonStr))
    {
        var jUser = J.Parse(jsonStr);
        if (jUser["uid"] != "")
        {
            fi &= filter.Eq("uid", jUser["uid"]);
        }
        else
        {
            fi &= filter.Eq("nick", jUser["nick"]);
            fi &= filter.Eq("ip", jUser["ip"]);
        }
    }

    var seStr = req.param.Get("se");
    if (!String.IsNullOrWhiteSpace(seStr))
    {
        seStr = HttpUtility.HtmlEncode(seStr);
        seStr = Escape(seStr);
        fi &= new BsonDocument { { "nick", new BsonDocument { { "$regex", seStr }, { "$options", "i" } } } };
    }

    var tiStr = req.param.Get("ti");
    if (!String.IsNullOrWhiteSpace(tiStr))
    {
        tiStr = HttpUtility.HtmlEncode(tiStr);
        tiStr = String.Join(".*?", tiStr.ToCharArray().Select(x => Escape(x.ToString())));
        fi &= new BsonDocument { { "title", new BsonDocument { { "$regex", tiStr }, { "$options", "i" } } } };
    }


    var docs = vv_list.Find(fi).Sort("{page_num:-1}").Limit(40);
    var vdata = J.O(("vlist", new JL()));
    foreach (var item in docs.ToList())
    {
        var li = vdata["vlist"] as JL;
        li.Add(new JO()
        {
            ["page_num"] = item["page_num"],
            ["thumbnail"] = item["thumbnail"],
            ["nick"] = item["nick"],
            ["title"] = item["title"],
            ["wdate"] = DateTime.Parse(item["wdate"] + "").ToString("yy.MM.dd"),
            ["ip"] = item["ip"] != "" ? ("(" + item["ip"] + ")") : "",
            ["pri_num"] = "" + item["writer_num"] + "/" + item["writer_tot"],
            ["server"] = Util.Settings["server"].Value
        });
    }

    res.Render("list_item", vdata);
});


app.All("/test/send", (req, res) =>
{
    req.Sess["name"] = req.param.Get("name");
    res.ReDirect("/test/reply");
});

app.Get("/test/reply", (req, res) =>
{
    res.Send(req.Sess.GetD("name", "Null") + "님 반갑습니다.");
});

app.Post("/test/multipart", (req, res) =>
{
    foreach (J tem in (req.mPart["myfile"] as JL).GetList())
    {
        byte[] bytes = tem["data"];
        if (bytes?.Length is null or 0) continue;
        string fanme = tem["filename"].Trim('"');
        Util.SaveFile("public/upload/" + fanme, bytes);
    }
    string fname = req.mPart["fname"][0]["data"];
    string lname = req.mPart["lname"][0]["data"];
    res.Send(fname + " " + lname);
});

app.Forward("/", "/main");

Mutex m_hMutex = new(true, "Global\\indie_web_desu", out bool flagMutex);
if (!flagMutex)
{
    System.Console.WriteLine("실행중입니다. ");
    int pid = int.Parse(Util.Settings["pid"].Value);
    Process.GetProcessById(pid).Kill(true);
    System.Console.WriteLine(pid + " 종료됨.");
}
Util.Settings["pid"].Value = Environment.ProcessId.ToString();
Util.SaveSettings();
File.WriteAllText(Util.projPath + "/public/sedata.json", ser_json.Stringify());


app.Listen(80, port =>
{
    System.Console.WriteLine($"Listening http://localhost:{port}");
    if (Env.OSVersion.Platform == PlatformID.Win32NT)
    {
        Process.Start("explorer", "http://localhost/");
    }
});
Console.WriteLine("종료됨");
m_hMutex.ReleaseMutex();