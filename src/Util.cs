using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Configuration;
namespace purry;
public class Ftag
{
    public FileInfo info = null;
    public string etag = null;
    public string modiTime = null;
    public string minetype = null;
}
public static class Util
{
    public static readonly string projPath;
    public static readonly Dictionary<string, string> mineTypeDic;
    public static readonly string[] animals;

    static Util(){
        projPath = Environment.CurrentDirectory;
        var m = Regex.Match(projPath, "[\\\\/]{1}bin[\\\\/]{1}(Debug|Release)");
        if (m.Success) projPath = projPath[0..m.Index];

        config =  ConfigurationManager.OpenMappedExeConfiguration(
            new ExeConfigurationFileMap(){ExeConfigFilename = projPath + "/App.config"}, 
            ConfigurationUserLevel.None);
 
        animals = File.ReadAllLines(projPath + "/Z_animals").Shuffle();

        mineTypeDic = File.ReadAllLines(projPath + "/minetype.csv").Select(x => x.Split(','))
        .ToDictionary(x => x[0].Trim(), x => x.LastOrDefault().Trim());
    }

    static readonly Configuration config ;
    public static KeyValueConfigurationCollection Settings => config.AppSettings.Settings;
    public static void SaveSettings() => config.Save(ConfigurationSaveMode.Modified);

    const string dateFormat = "ddd, dd MMM yyy HH':'mm':'ss 'GMT'";

    static readonly CultureInfo culInfo = new("en-US");

    public static string UTime => UtcEng(DateTime.Now);

    public static string UtcEng(DateTime dt) => dt.ToUniversalTime().ToString(dateFormat, Util.culInfo);

    public static string GetNickName(int nth) => animals[nth % animals.Length];

    public static Dictionary<string, string> GetCookie(string s)
    {
        var dic = s.Split(';').Select(x => x.Split('='))
        .ToDictionary(x => x[0].Trim(), x => x.LastOrDefault()?.Trim());
        return dic;
    }

    static readonly Dictionary<string, Ftag> fileDic = new();

    public static Ftag GetFInfo(string fulPath)
    {
        var info = new FileInfo(fulPath);
        if (info.Exists == false)
        {
            return new Ftag() { info = info };
        }

        var modiTime = Util.UtcEng(info.LastWriteTime);

        if (fileDic.GetD(fulPath)?.modiTime == modiTime)
        {
            return fileDic[fulPath];
        }


        var ext = Path.GetExtension(info.Name);
        var etag = DateTime.UtcNow.Ticks.ToString("x2");
        if (mineTypeDic.Has(ext) == false) ext = "?";

        var newItem = new Ftag()
        {
            info = info,
            etag = etag,
            modiTime = modiTime,
            minetype = mineTypeDic[ext]
        };


        fileDic[fulPath] = newItem;
        return newItem;
    }

    public static string NewSessionID(Socket client)
    {
        int seed = (int)(DateTime.UtcNow.Ticks % int.MaxValue);
        var intNx = new Random(seed).Next();
        var id = CreateMD5(client.GetHashCode() + "^_^" + intNx);
        return id;
    }

    static readonly System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
    static string CreateMD5(string input)
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = md5.ComputeHash(inputBytes);
        return String.Join(null, hashBytes.Select(x => x.ToString("x2")));
    }

    public static void SaveFile(string path, byte[] bytes)
    {
        path = Path.Combine(projPath, path);
        var dir = Path.GetDirectoryName(path);
        Directory.CreateDirectory(dir);

        File.WriteAllBytes(path, bytes);
    }

    public static byte[] GetBytes(string str)
    {
        byte[] bytes = new byte[str.Length];
        var arr = str.ToCharArray();

        for (int i = 0; i < arr.Length; i++)
        {
            bytes[i] = (byte)(arr[i]);
        }
        return bytes;
    }

    public static string GetString(byte[] bytes, int strOff = 0, int cnt = 0)
    {
        if (cnt == 0) cnt = bytes.Length;
        char[] chars = new char[cnt];

        for (int i = 0; i < chars.Length; i++)
        {
            chars[i] = (char)(bytes[i + strOff]);
        }
        return new string(chars);
    }
}
