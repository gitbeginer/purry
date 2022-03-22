using System.IO;
using System.Net.Sockets;
using System.Reflection;

namespace purry;

public class Response
{
    public readonly Request req;
    public readonly dynamic ViewBag = new System.Dynamic.ExpandoObject();
    internal Socket client;
    private readonly Assembly viewAsm;
    #region stateCodeString
    const string stateCodes = @"100 Continue
101 Switching Protocol
102 Processing (WebDAV)
200 OK
201 Created
202 Accepted
203 Non-Authoritative Information
204 No Content
205 Reset Content
206 Partial Content
207 Multi-Status
208 ALREADY REPORTED
226 IM Used (HTTP Delta encoding)
300 Multiple Choice
301 Moved Permanently
302 Found
303 See Other
304 Not Modified
305 Use Proxy 
306 unused
307 Temporary Redirect
308 Permanent Redirect
400 Bad Request
401 Unauthorized
402 Payment Required
403 Forbidden
404 Not Found
405 Method Not Allowed
406 Not Acceptable
407 Proxy Authentication Required
408 Request Timeout
409 Conflict
410 Gone
411 Length Required
412 Precondition Failed
413 Payload Too Large
414 URI Too Long
415 Unsupported Media Type
416 Requested Range Not Satisfiable
417 Expectation Failed
418 I'm a teapot
421 Misdirected Request
422 Unprocessable Entity (WebDAV)
423 Locked (WebDAV)
424 Failed Dependency (WebDAV)
426 Upgrade Required
428 Precondition Required
429 Too Many Requests
431 Request Header Fields Too Large
451 Unavailable For Legal Reasons
500 Internal Server Error
501 Not Implemented
502 Bad Gateway
503 Service Unavailable
504 Gateway Timeout
505 HTTP Version Not Supported
506 Variant Also Negotiates
507 Insufficient Storage
508 Loop Detected (WebDAV)
510 Not Extended
511 Network Authentication Required";
    #endregion

    readonly Dictionary<int, string> coDic = (from s in stateCodes.Split('\n')
                                              select (num: int.Parse(s[0..3]), str: s)
                                               ).ToDictionary(s => s.num, s => s.str);
    int sCode = 500; //Status code
    private bool isFileReq;

    string SCodeStr => coDic[sCode];

    public bool Sended { get; private set; }

    public Response(Request req, Assembly asm = null)
    {
        this.req = req;
        this.client = req.client;
        this.viewAsm = asm;
    }
    public Response Status(int? code)
    {
        sCode = code ?? 200;
        if (coDic.ContainsKey(sCode)) return this;
        throw new ArgumentException("UNKNOW STATUS CODE");
    }

    private void ToClient(StringBuilder sb, byte[] body = null)
    {
        if (sCode < 500 && req.sessionID == null && !isFileReq)
        {
            sb.AddL("set-cookie: sessionID=" + req.NewSessionID() + "; path=/; httponly");
        }
        sb.AddL();
        client.Send(Encoding.UTF8.GetBytes(sb.ToString()));
        if (body != null) client.Send(body);
        this.Sended = true; 
    }

    public void Send(string msg, int? statusCode = null)
    {
        Status(statusCode);
        var header = new StringBuilder(100);
        var bodyData = Encoding.UTF8.GetBytes(msg);

        header.AddL("HTTP/1.1 " + SCodeStr);
        header.AddL("date: " + Util.UTime);
        header.AddL("Server: test server");
        header.AddL("Content-type:text/plain; charset=UTF-8");
        header.AddL("Content-Length: " + bodyData.Length);
        header.AddL("Connection: close");

        ToClient(header, bodyData);
    }

    public void Send(J json, int? statusCode = null)
    {
        Status(statusCode);
        var header = new StringBuilder(100);
        var bodyData = Encoding.UTF8.GetBytes(json.Stringify());

        header.AddL("HTTP/1.1 " + SCodeStr);
        header.AddL("date: " + Util.UTime);
        header.AddL("Server: test server");
        header.AddL("Content-type: application/json; charset=UTF-8");
        header.AddL("Content-Length: " + bodyData.Length);
        header.AddL("Connection: close");

        ToClient(header, bodyData);
    }

    public void Render(string name, JO ViewData = null, int? statusCode = null)
    {
        Status(statusCode);
        var className = View.GetClassName("Views/cshtml/" + name + ".cshtml");
        var type = viewAsm.GetType("Views." + className);
        var view = Activator.CreateInstance(type, this, ViewData, ViewBag, req.TempData) as View;
        var html = view.GetHTML();

        var header = new StringBuilder(100);
        var bodyData = Encoding.UTF8.GetBytes(html);

        header.AddL("HTTP/1.1 " + SCodeStr);
        header.AddL("date: " + Util.UTime);
        header.AddL("Server: test server");
        header.AddL("Content-type:text/html; charset=UTF-8");
        header.AddL("Content-Length: " + bodyData.Length);
        header.AddL("Connection: close");

        ToClient(header, bodyData);
    }


    public void SendReqFIle()
    {
        string publicPath = Path.Combine(Util.projPath, "public");

        #if !DEBUG
        if(new[] { ".js", ".css", ".html" }.Contains(Path.GetExtension(req.url))){
            publicPath = Path.Combine(Util.projPath, "build");
        }
        #endif
        
        Ftag ftag = Util.GetFInfo(publicPath + req.url);
        FileInfo info = ftag.info;
        if (!info.Exists || !info.FullName.StartsWith(publicPath))
        {
            Send("페이지를 찾을 수 없습니다.", 404);
            return;
        }

        Status(ftag.etag == req.GetD("If-None-Match") ? 304 : 200);

        var header = new StringBuilder(100);

        header.AddL($"HTTP/1.1 " + SCodeStr);
        header.AddL("Accept-Ranges: none");
        header.AddL("Cache-Control: public, max-age=0");
        header.AddL("Last-Modified: " + ftag.modiTime);
        header.AddL("Etag: " + ftag.etag);
        header.AddL("Date: " + Util.UTime);

        if (sCode != 304)
        {
            if(info.Extension == ".gz") {
                header.AddL("Content-Encoding: gzip");
            }
            
            header.AddL("Content-type: " + ftag.minetype);
            header.AddL("Content-Length: " + info.Length);
        }

        header.AddL("Server: test server");
        header.AddL("Connection: close");
        this.isFileReq = true;
        ToClient(header, sCode != 304 ? File.ReadAllBytes(info.FullName) : null);
    }

    public void ReDirect(string url)
    {
        var header = new StringBuilder(100);
        Status(302);
        header.AddL("HTTP/1.1 " + SCodeStr);
        header.AddL("date: " + Util.UTime);
        header.AddL("Server: test server");
        header.AddL("location: " + url);
        header.AddL("Content-Length: 0");
        header.AddL("Connection: close");

        ToClient(header);
    }


}