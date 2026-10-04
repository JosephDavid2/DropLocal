using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

using System.Runtime.InteropServices;
using System.Threading;

// Run as WinExe, so no test console can be inherited by the launcher.
internal static class LauncherWindowCheck {
    [DllImport("kernel32.dll",SetLastError=true)]static extern bool AttachConsole(uint pid);
    [DllImport("kernel32.dll")]static extern IntPtr GetConsoleWindow();
    [DllImport("kernel32.dll")]static extern bool FreeConsole();
    [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr hwnd);
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]struct Entry{public uint size,usage,pid;public UIntPtr heap;public uint module,threads,parent;public int priority;public uint flags;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=260)]public string exe;}
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern bool Process32FirstW(IntPtr snapshot,ref Entry entry);
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern bool Process32NextW(IntPtr snapshot,ref Entry entry);
    [DllImport("kernel32.dll")]static extern IntPtr CreateToolhelp32Snapshot(uint flags,uint pid);
    [DllImport("kernel32.dll")]static extern bool CloseHandle(IntPtr handle);
    static uint Child(uint parent){IntPtr snapshot=CreateToolhelp32Snapshot(2,0);try{var entry=new Entry{size=(uint)Marshal.SizeOf(typeof(Entry))};if(Process32FirstW(snapshot,ref entry))do{if(entry.parent==parent&&entry.exe=="dotnet.exe")return entry.pid;}while(Process32NextW(snapshot,ref entry));return 0;}finally{CloseHandle(snapshot);}}
    delegate bool EnumCallback(IntPtr window,IntPtr param);
    [DllImport("user32.dll")]static extern bool EnumWindows(EnumCallback callback,IntPtr param);
    [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr window,out uint pid);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetWindowText(IntPtr window,System.Text.StringBuilder text,int length);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetClassName(IntPtr window,System.Text.StringBuilder text,int length);
    [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr window,uint message,IntPtr wParam,IntPtr lParam);
    static string observed="";
    static IntPtr AppWindow(uint process){IntPtr result=IntPtr.Zero;observed="";EnumWindows((window,param)=>{uint pid;GetWindowThreadProcessId(window,out pid);if(pid==process){var text=new System.Text.StringBuilder(512);var kind=new System.Text.StringBuilder(128);GetWindowText(window,text,512);GetClassName(window,kind,128);observed+=text+" ["+kind+"] visible="+IsWindowVisible(window)+"\n";if(text.ToString()=="Drop Local"&&IsWindowVisible(window))result=window;}return true;},IntPtr.Zero);return result;}
    [STAThread]static int Main(string[] args){try{string target=Path.GetFullPath(args[0]);bool expectedConsole=args[2]=="console";using(var launcher=Process.Start(new ProcessStartInfo(target){UseShellExecute=true,WorkingDirectory=Path.GetDirectoryName(target)})){
        Process app=null;for(int i=0;i<100&&app==null;i++){uint childId=Child((uint)launcher.Id);if(childId!=0)app=Process.GetProcessById((int)childId);Thread.Sleep(100);}if(app==null)throw new Exception("No application child");using(app){IntPtr appWindow=IntPtr.Zero;for(int i=0;i<100;i++){appWindow=AppWindow((uint)app.Id);if(appWindow!=IntPtr.Zero)break;Thread.Sleep(100);}if(appWindow==IntPtr.Zero)throw new Exception("Normal app UI absent: "+observed);bool attached=AttachConsole((uint)app.Id);bool console=attached&&GetConsoleWindow()!=IntPtr.Zero;if(attached)FreeConsole();if(console!=expectedConsole)throw new Exception("Console="+console+" expected="+expectedConsole);Thread.Sleep(500);SendMessage(appWindow,0x10,IntPtr.Zero,IntPtr.Zero);bool close=true;bool appExit=app.WaitForExit(10000);bool launcherExit=launcher.WaitForExit(10000);if(!close||!appExit||!launcherExit)throw new Exception("Closing app failed: close="+close+" appExit="+appExit+" launcherExit="+launcherExit+" title="+(!appExit?app.MainWindowTitle:"closed"));File.WriteAllText(args[1],"PASS normal UI; console="+console+"; close UI ends child and launcher; target="+target);}}
        return 0;
    }catch(Exception e){File.WriteAllText(args[1],e.ToString());return 1;}}
}




