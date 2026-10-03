using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
[assembly: AssemblyTitle("Instalar Drop Local — by Joseph David")]
[assembly: AssemblyCompany("Joseph David")]
[assembly: AssemblyVersion("0.5.1.0")]
internal static class Setup {
    internal const string Version="0.5.1";
    [STAThread] static int Main(string[] args){
        try{
            if(args.Length==1&&args[0]=="--verify"){Verify();return 0;}
            if(args.Length==2&&args[0]=="--extract-to"){Extract(args[1],null);return 0;}
            Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
            using(var window=new SetupWindow()){
                if(args.Length==2&&args[0]=="--preview"){window.StartPosition=FormStartPosition.Manual;window.Location=new Point(-30000,-30000);window.Show();Application.DoEvents();using(var image=new Bitmap(window.Width,window.Height)){window.DrawToBitmap(image,new Rectangle(Point.Empty,window.Size));image.Save(Path.GetFullPath(args[1]));}return 0;}
                Application.Run(window);
            }return 0;
        }catch(Exception e){if(args.Length>0){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"setup-check-error.txt"),e.ToString());}else MessageBox.Show(e.Message,"Drop Local",MessageBoxButtons.OK,MessageBoxIcon.Error);return 1;}
    }
    static ZipArchive Payload(){var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("DropLocal.Payload");if(stream==null)throw new InvalidOperationException("Pacote ausente");return new ZipArchive(stream,ZipArchiveMode.Read);}
    static string Target(string root,string relative){if(Path.IsPathRooted(relative)||relative.Contains(':'))throw new IOException("Caminho inválido no pacote");string path=Path.GetFullPath(Path.Combine(root,relative.Replace('/',Path.DirectorySeparatorChar)));if(!path.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new IOException("Caminho fora da instalação");return path;}
    internal static void Verify(){using(var zip=Payload()){foreach(var entry in zip.Entries){Target(Path.GetFullPath(Path.Combine(Path.GetTempPath(),"DropLocalVerify")),entry.FullName);using(var stream=entry.Open()){byte[] b=new byte[65536];while(stream.Read(b,0,b.Length)>0){}}}foreach(string name in new[]{"DropLocal.exe","DropLocal.dll","Uninstall.ps1","runtime/dotnet.exe","runtime/host/fxr/10.0.12/hostfxr.dll","runtime/shared/Microsoft.NETCore.App/10.0.12/coreclr.dll","runtime/shared/Microsoft.WindowsDesktop.App/10.0.12/System.Windows.Forms.dll"})if(zip.GetEntry(name)==null)throw new IOException("Arquivo essencial ausente: "+name);}}
    internal static void Extract(string destination,Action<int> progress){string root=Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar);if(Directory.Exists(root)&&Directory.EnumerateFileSystemEntries(root).Any())throw new IOException("Pasta de extração precisa estar vazia");Directory.CreateDirectory(root);
        using(var zip=Payload()){int n=0;foreach(var entry in zip.Entries){string path=Target(root,entry.FullName);Directory.CreateDirectory(Path.GetDirectoryName(path));using(var source=entry.Open())using(var file=new FileStream(path,FileMode.CreateNew)){source.CopyTo(file);}if(progress!=null)progress(++n*100/zip.Entries.Count);}
            File.WriteAllLines(Path.Combine(root,"installed-files.txt"),new[]{"DropLocal.Install/1",Version}.Concat(zip.Entries.Select(e=>e.FullName)).ToArray());}}
    internal static void Install(Action<int> progress,bool desktop){
        string root=Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","DropLocal"));string staging=root+".install-"+Guid.NewGuid().ToString("N"),backup=root+".backup-"+DateTime.Now.ToString("yyyyMMddHHmmss");bool old=false,promoted=false;
        try{Extract(staging,progress);if(Directory.Exists(root)){Directory.Move(root,backup);old=true;}Directory.Move(staging,root);promoted=true;
            using(var key=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\DropLocal.JosephDavid")){
                key.SetValue("DisplayName","Drop Local");key.SetValue("DisplayVersion",Version);key.SetValue("Publisher","Joseph David");key.SetValue("InstallLocation",root);key.SetValue("DisplayIcon",Path.Combine(root,"DropLocal.exe"));
                key.SetValue("UninstallString","\""+Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),@"WindowsPowerShell\v1.0\powershell.exe")+"\" -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File \""+Path.Combine(root,"Uninstall.ps1")+"\"");key.SetValue("NoModify",1);key.SetValue("NoRepair",1);key.SetValue("EstimatedSize",(int)(Directory.GetFiles(root,"*",SearchOption.AllDirectories).Sum(p=>new FileInfo(p).Length)/1024));}
            Shortcut(Environment.GetFolderPath(Environment.SpecialFolder.Programs),root);if(desktop)Shortcut(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),root);
            if(old){File.WriteAllText(Path.Combine(root,"upgrade-backup.txt"),"Versão anterior preservada em: "+backup);File.AppendAllText(Path.Combine(root,"installed-files.txt"),"upgrade-backup.txt"+Environment.NewLine);}
        }catch{if(old&&promoted){string failed=root+".failed-"+Guid.NewGuid().ToString("N");Directory.Move(root,failed);Directory.Move(backup,root);}else if(old&&!Directory.Exists(root))Directory.Move(backup,root);throw;}
    }
    static void Shortcut(string folder,string root){Directory.CreateDirectory(folder);dynamic shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));dynamic shortcut=shell.CreateShortcut(Path.Combine(folder,"Drop Local.lnk"));shortcut.TargetPath=Path.Combine(root,"DropLocal.exe");shortcut.WorkingDirectory=root;shortcut.IconLocation=Path.Combine(root,"DropLocal.exe")+",0";shortcut.Description="Drop Local — by Joseph David";shortcut.Save();}
}
internal sealed class SetupWindow:Form{
    Button install;Label state;ProgressBar progress;CheckBox desktop;
    internal SetupWindow(){Text="Instalar Drop Local";Icon=new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream("DropLocal.Icon"));ClientSize=new Size(580,390);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;StartPosition=FormStartPosition.CenterScreen;BackColor=Color.FromArgb(12,24,37);ForeColor=Color.FromArgb(239,245,251);Font=new Font("Segoe UI",10);
        var title=new Label{Text="Drop Local",Font=new Font("Segoe UI",24,FontStyle.Bold),Location=new Point(28,24),Size=new Size(500,42)};Controls.Add(title);
        Controls.Add(new Label{Text="by Joseph David · versão "+Setup.Version,Location=new Point(30,70),Size=new Size(500,24),ForeColor=Color.FromArgb(163,183,203)});
        Controls.Add(new Label{Text="Envie e receba arquivos entre Windows e Android.\nInstalação para seu usuário, com .NET 10 incluído.\nFeche Drop Local antes de instalar ou atualizar.",Location=new Point(30,110),Size=new Size(510,74)});
        Controls.Add(new Label{Text="Destino: "+Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","DropLocal"),Location=new Point(30,190),Size=new Size(510,42),ForeColor=Color.FromArgb(163,183,203)});
        desktop=new CheckBox{Text="Criar atalho na área de trabalho",Checked=true,Location=new Point(30,238),Size=new Size(500,28)};Controls.Add(desktop);
        state=new Label{Text="Atalho no menu Iniciar e desinstalação incluídos.",Location=new Point(30,274),Size=new Size(510,32)};Controls.Add(state);progress=new ProgressBar{Location=new Point(30,310),Size=new Size(330,12)};Controls.Add(progress);
        install=new Button{Text="Instalar",Location=new Point(382,310),Size=new Size(166,48),BackColor=Color.FromArgb(18,224,222),ForeColor=BackColor,FlatStyle=FlatStyle.Flat};Controls.Add(install);install.Click+=InstallClick;
        FormClosing+=(s,e)=>{if(!install.Enabled)e.Cancel=true;};
    }
    async void InstallClick(object sender,EventArgs args){if(install.Text=="Concluir"){Close();return;}install.Enabled=desktop.Enabled=false;state.Text="Instalando…";try{bool shortcut=desktop.Checked;await Task.Run(()=>Setup.Install(p=>BeginInvoke((Action)(()=>progress.Value=p)),shortcut));state.Text="Instalação concluída. Abra pelo menu Iniciar.";install.Text="Concluir";}catch(Exception e){state.Text="Falha na instalação.";MessageBox.Show(this,"Feche Drop Local e tente novamente.\n"+e.Message,"Drop Local",MessageBoxButtons.OK,MessageBoxIcon.Error);}finally{install.Enabled=true;}}
}
