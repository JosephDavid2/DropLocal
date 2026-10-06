using System.Diagnostics;
using System.Reflection;

internal sealed partial class MainForm
{
    readonly ActionButton update=new(){Text="Atualizar",Size=new Size(150,30),Anchor=AnchorStyles.Top|AnchorStyles.Right};
    readonly CancellationTokenSource updateLifetime=new();bool updating;
    async Task CheckUpdate()
    {
        if(updating)return;
        if(active!=null||receiveView.Busy){MessageBox.Show(this,"Aguarde terminar as transferências antes de atualizar.","Drop Local");return;}
        updating=true;update.Enabled=false;
        try
        {
            status.Text="Consultando a última release no GitHub…";
            using var client=GitHubUpdate.Client();var installed=Assembly.GetExecutingAssembly().GetName().Version!;
            var release=await GitHubUpdate.Check(client,installed,updateLifetime.Token);
            if(closing)return;
            if(release==null){status.Text="Drop Local já está na versão mais recente publicada.";return;}
            if(MessageBox.Show(this,$"Versão {release.Version} disponível. Baixar e abrir a atualização? Seus arquivos e a pasta de recebimento serão preservados. O aplicativo será fechado para instalar.","Atualizar Drop Local",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
            status.Text=$"Baixando Drop Local {release.Version}…";
            string directory=Path.Combine(settingsDirectory,"updates",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            string package=Path.Combine(directory,"DropLocal-Setup.msi");
            await GitHubUpdate.Download(client,release,package,updateLifetime.Token);
            if(closing)return;
            if(active!=null||receiveView.Busy){status.Text="Download concluído. Aguarde a transferência e clique em Atualizar novamente.";return;}
            var start=new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"msiexec.exe")){UseShellExecute=true};
            start.ArgumentList.Add("/i");start.ArgumentList.Add(package);
            if(Process.Start(start)==null)throw new IOException("Não foi possível abrir o Windows Installer.");
            Close();
        }
        catch(OperationCanceledException){if(!closing)status.Text="A consulta ou download demorou demais. Tente atualizar novamente.";}
        catch(Exception e){if(!closing)status.Text="Não foi possível atualizar: "+e.Message;}
        finally{updating=false;if(!closing)update.Enabled=true;}
    }
}
