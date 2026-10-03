using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
[assembly: AssemblyTitle("Drop Local — by Joseph David")]
[assembly: AssemblyVersion("0.5.1.0")]
internal static class Launcher {
    [STAThread] static int Main(string[] args) {
        try {
            string root=AppDomain.CurrentDomain.BaseDirectory;
            string quoted="\""+Path.Combine(root,"DropLocal.dll")+"\"";
            foreach(string arg in args)quoted+=" "+Quote(arg);
            var info=new ProcessStartInfo(Path.Combine(root,"runtime","dotnet.exe"),quoted){UseShellExecute=false,WorkingDirectory=root};
            using(var process=Process.Start(info)){process.WaitForExit();return process.ExitCode;}
        }catch(Exception e){if(Array.Exists(args,a=>a.StartsWith("--verify") || a.StartsWith("--render"))){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"launcher-check-error.txt"),e.ToString());return 1;}MessageBox.Show("Não foi possível abrir Drop Local. Reinstale o aplicativo.\n"+e.Message,"Drop Local",MessageBoxButtons.OK,MessageBoxIcon.Error);return 1;}
    }
    // Windows CommandLineToArgvW quoting, including trailing backslashes.
    static string Quote(string value){var output=new System.Text.StringBuilder("\"");int slashes=0;foreach(char c in value){if(c=='\\'){slashes++;continue;}if(c=='\"'){output.Append('\\',slashes*2+1);output.Append(c);}else{output.Append('\\',slashes);output.Append(c);}slashes=0;}output.Append('\\',slashes*2);output.Append('"');return output.ToString();}
}


