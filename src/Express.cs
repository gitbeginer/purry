using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NUglify;

namespace purry;
public class Express
{
    readonly Socket server = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    readonly Dictionary<string, Action<Request, Response>> getDic = new();
    readonly Dictionary<string, Action<Request, Response>> postDic = new();
    readonly Dictionary<string, string> forwardDic = new();
    private Assembly viewAsm;
    public int? Port { get; private set; } = null;

    public event Action<Request> ReqLog;
    public event Action<Request, String> ErrLog;

    public Express()
    {
        InitEtag();
        InitCSHTML();
        #if !DEBUG
        BuildClientFile();
        #endif
    }

    public static void BuildClientFile()
    {
        var types = new[] { ".js", ".css", ".html" };
        var dir = new DirectoryInfo(Util.projPath + "/public");
        foreach (FileInfo info in dir.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            string ext = info.Extension.ToLower();
            if (!types.Contains(ext)) continue;

            
            var sub = info.FullName[dir.ToString().Length..^0];
            var newPath = Util.projPath + "/build" + sub;
            var newFileInfo = new FileInfo(newPath);
            Util.GetFInfo(newFileInfo.FullName);

            if(newFileInfo.Exists){
                if(newFileInfo.LastWriteTime >= info.LastWriteTime) continue;
            }else{
                var newdir = Path.GetDirectoryName(newPath);
                Directory.CreateDirectory(newdir);
            }
            
            var str = File.ReadAllText(info.FullName);
            string newStr = ext switch
            {
                ".js" => Uglify.Js(str).Code,
                ".css" => Uglify.Css(str).Code,
                ".html" => Uglify.Html(str).Code,
                _ => throw new Exception("never")
            };
            File.WriteAllText(newPath, newStr);
        }
    }

    private static void InitEtag()
    {
        var dir = new DirectoryInfo(Util.projPath + "/public");
        foreach (FileInfo info in dir.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            Util.GetFInfo(info.FullName);
        }
    }

    public void Listen(int port, Action<int> callback = null)
    {
        this.Port = port;

        var ipep = new IPEndPoint(IPAddress.Any, port);
        server.Bind(ipep);
        server.Listen(100);

        callback?.Invoke(port);

        while (true)
        {
            Socket client = server.Accept();
            Task.Run(() => NewWork(client)); //자식 스레드에 인계
        }
    }
    public void Get(string url, Action<Request, Response> callback) => getDic.Add(url, callback);
    public void Post(string url, Action<Request, Response> callback) => postDic.Add(url, callback);
    public void All(string url, Action<Request, Response> callback)
    {
        getDic.Add(url, callback);
        postDic.Add(url, callback);
    }

    void NewWork(Socket client)
    {
        client.ReceiveTimeout = 5000; //최대 5초 기다려줌을 선언
        Request req = null;
        Response res = null;
        try
        {
            req = new Request(client);
            res = new Response(req, viewAsm);

            Func<bool> troubleCheck = (!client.Connected || req.Count is 0, req.err) switch
            {
                (true, _) => () => { WriteLine("Disconnected: by client"); return true; }
                ,
                (_, null) => () => { /*WriteLine(req.First().Value);*/ return false; }
                ,
                (_, "413") => () => { res.Send("Size is too large.", 413); return true; }
                ,
                (_, var _) => () => { res.Send(req.err, 400); ErrLog?.Invoke(req, req.err); return true; }
            };

            if (troubleCheck()) return;

            req.url = forwardDic.GetD(req.url) ?? req.url;

            if (req.method == Request.Method.GET) getDic.GetD(req.url)?.Invoke(req, res);

            if (res.Sended)
            {
                req.TempData.Commit();
                ReqLog?.Invoke(req);
                return;
            }

            if (req.method == Request.Method.POST) postDic.GetD(req.url)?.Invoke(req, res);

            if (res.Sended)
            {
                req.TempData.Commit();
                ReqLog?.Invoke(req);
                return;
            }

            res.SendReqFIle();
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
        {
            WriteLine("Disconnected: timeout");
        }
        catch (Exception ex)
        {
            WriteLine(ex);
            try
            {
                res.Send("서버 에러!", 500);
                ErrLog.Invoke(req, ex + "");
            }
            catch (Exception ex2) { WriteLine(ex2); }
        }
        finally
        {
            //WriteLine("bye~   " + (req?.nickname ?? "anonymous"));
            client.Close();
        }
    }
    public void InitCSHTML()
    {
        var modi = new TemplateEngine().MakeSourceAll(Util.projPath + "/Views");
        Boolean needNewDll = false;
        #if DEBUG
        FileInfo info = new(Util.projPath + "/Views/bin/Debug/net6.0/Views.dll");
        #else
        FileInfo info = new(Util.projPath + "/Views/bin/Release/net6.0/Views.dll");
        #endif

        if(info.Exists){
            var last =  DateTime.Parse(Util.Settings["last_cshtml_edit"].Value);
            if(last > info.LastWriteTime) needNewDll = true;
        }
        else needNewDll = true;

        
        if (modi || needNewDll)
        {
            var ps = Process.Start(new ProcessStartInfo()
            {
                FileName = "dotnet",
                #if DEBUG
                Arguments = "build",
                #else
                Arguments = "build --configuration Release",
                #endif
                WorkingDirectory = Util.projPath + "/Views"
            }); 
            ps.WaitForExit();
            if (ps.ExitCode != 0) throw new Exception("Compile Error.");
        }
        var asm = Assembly.Load(File.ReadAllBytes(info.FullName));
        viewAsm = asm ?? throw new ArgumentException("Can't find Views.dll");
    }
    
    public void Forward(string url1, string url2) => forwardDic[url1] = url2;
}