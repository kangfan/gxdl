using System;
using System.IO;
namespace UnityEngine {
 public static class Mathf {public static int Clamp(int v,int a,int b)=>Math.Clamp(v,a,b);public static float Clamp(float v,float a,float b)=>Math.Clamp(v,a,b);public static int RoundToInt(float v)=>(int)Math.Round(v,MidpointRounding.ToEven);}
 public class TextAsset {public string text;public static bool operator !(TextAsset asset)=>asset==null;}
 public static class Resources {public static string Root;public static T Load<T>(string path) where T:class{string candidate=Environment.GetEnvironmentVariable("GXDL_CONFIG_ROOT"),file=candidate==null?null:Path.Combine(candidate,path+".json");if(file==null||!File.Exists(file))file=Path.Combine(Root,"Assets/Resources",path+".json");return new TextAsset{text=File.ReadAllText(file)} as T;}}
 public static class Application {public static string persistentDataPath=>Path.Combine(Resources.Root,"Reference/ExperienceAccounts20261112-r01/isolated");}
 public static class Debug {public static void Log(object o){}public static void LogWarning(object o){Console.WriteLine(o);}}
}
namespace Gxdl {
 public static class GuxuPhoneTestLogin {public static bool Enabled=true;}
 public static class GuxuAccountConfig {public static string Hash(string text)=>Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text))).ToLowerInvariant();}
}
