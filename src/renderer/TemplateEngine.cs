using System.IO;
using System.Configuration;
namespace purry;
public partial class TemplateEngine
{
    private DirectoryInfo dir;
    public bool MakeSourceAll(string folderPath)
    {

        if (Util.Settings["last_cshtml_edit"] == null){
            Util.Settings.Add("last_cshtml_edit", "1970-01-01 00:00:00");
        } 
        var lastEdit = DateTime.Parse(Util.Settings["last_cshtml_edit"].Value);

        dir = new DirectoryInfo(folderPath);
        bool modified = false;
        foreach (FileInfo info in dir.EnumerateFiles("*.cshtml", SearchOption.AllDirectories))
        {
            if(info.LastWriteTime < lastEdit) continue;
            MakeSource(info);
            modified = true;
        }

        if(modified){
            Util.Settings["last_cshtml_edit"].Value = DateTime.Now.ToString();
            Util.SaveSettings();
        }

        return modified;;
    }

    public string MakeSource(string path) => MakeSource(new FileInfo(path));
    public string MakeSource(FileInfo info)
    {
        dir ??= new DirectoryInfo(info.Directory.FullName);

        Console.WriteLine(info.FullName);

        var txt = File.ReadAllText(info.FullName, Encoding.UTF8);

        List<string> tokens = Tokenize(txt);


        var node = new InTagNode(tokens);

        var classNm = info.FullName[(Util.projPath.Length + 1)..];
        classNm =  View.GetClassName(classNm);

        var rt = Node2ViewClass(classNm, node);

        var fname = Path.GetFileName(info.FullName);

        Directory.CreateDirectory(Util.projPath + "/Views/out/");

        File.WriteAllText(Util.projPath + "/Views/out/" + fname + ".cs", rt, Encoding.UTF8);

        return rt;
    }
}