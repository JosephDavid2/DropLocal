using System.Net;
using System.Runtime.InteropServices;

internal sealed partial class MainForm : Form
{
    readonly ListBox list=new();
    readonly ActionButton cancel=new(){Text="Cancelar envio",Danger=true,Dock=DockStyle.Fill,Enabled=false};
    readonly TransferProgress progress=new(){Dock=DockStyle.Fill,Visible=false};
    readonly Label status=new(){Text="Escolha um aparelho para enviar. Quem recebe só precisa aceitar.",Dock=DockStyle.Fill,ForeColor=Palette.Muted};
    readonly Label summary=new(){Text="Envio",Dock=DockStyle.Fill,ForeColor=Palette.Muted};
    readonly Label pairingStatus=new(){Text="Recebimento automático · cada envio pede aceitação",AutoSize=true,ForeColor=Palette.Muted};
    readonly ReceiveView receiveView;readonly string settingsDirectory;CancellationTokenSource? active;bool closing;
    [DllImport("dwmapi.dll")]static extern int DwmSetWindowAttribute(IntPtr hwnd,int attribute,ref int value,int size);
    public MainForm(bool autoStart=true,string? preferences=null)
    {
        settingsDirectory=preferences??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"DropLocal");
        Text="Drop Local";Icon=AppIdentity.Icon;BackColor=Palette.Background;ForeColor=Palette.Text;Font=new Font("Segoe UI",10);AutoScaleMode=AutoScaleMode.Dpi;StartPosition=FormStartPosition.CenterScreen;ClientSize=new Size(740,Math.Min(730,(Screen.PrimaryScreen?.WorkingArea.Height??1040)-80));MinimumSize=new Size(640,560);
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(24),ColumnCount=1,RowCount=4,BackColor=Palette.Background};root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,118));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,116));root.RowStyles.Add(new RowStyle(SizeType.Absolute,100));Controls.Add(root);
        var header=new Panel{Dock=DockStyle.Fill,Margin=Padding.Empty};header.Controls.Add(new WifiBadge{Location=Point.Empty});header.Controls.Add(Label("Drop Local",22,Palette.Text,true,new Point(70,-2)));header.Controls.Add(Label("Seus arquivos, pela sua rede · 0.8.3",10,Palette.Muted,false,new Point(72,38)));pairingStatus.Location=new Point(72,59);header.Controls.Add(pairingStatus);
        var settings=new ActionButton{Text="Configurações",Size=new Size(150,34),Anchor=AnchorStyles.Top|AnchorStyles.Right};header.Controls.Add(settings);header.Controls.Add(update);update.Click+=async(_,_)=>await CheckUpdate();var credit=Label("by Joseph David",9,Palette.Muted);header.Controls.Add(credit);header.Resize+=(_,_)=>{settings.Location=new Point(header.Width-settings.Width,35);credit.Location=new Point(header.Width-credit.Width,12);update.Location=new Point(header.Width-update.Width,84);};root.Controls.Add(header,0,0);
        root.Controls.Add(CreateNearbyPanel(),0,1);receiveView=new ReceiveView(settingsDirectory){Dock=DockStyle.Fill};root.Controls.Add(receiveView,0,2);receiveView.ListeningChanged+=_=>RefreshConnection();settings.Click+=async(_,_)=>{if(!updating)await receiveView.ShowSettings(this);};
        var outgoing=new Card{Dock=DockStyle.Fill,Margin=Padding.Empty};var activity=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=3,BackColor=Palette.Panel};activity.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));activity.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,140));activity.RowStyles.Add(new RowStyle(SizeType.Absolute,24));activity.RowStyles.Add(new RowStyle(SizeType.Percent,100));activity.RowStyles.Add(new RowStyle(SizeType.Absolute,10));activity.Controls.Add(summary,0,0);activity.Controls.Add(status,0,1);activity.Controls.Add(cancel,1,1);activity.Controls.Add(progress,0,2);activity.SetColumnSpan(progress,2);outgoing.Controls.Add(activity);root.Controls.Add(outgoing,0,3);
        nearby.SelectedIndexChanged+=(_,_)=>{selectedDevice=nearby.SelectedItem as NearbyDevice;RefreshConnection();};cancel.Click+=(_,_)=>active?.Cancel();
        Shown+=(_,_)=>{if(autoStart){receiveView.StartDefault();StartDiscovery();}int dark=1;DwmSetWindowAttribute(Handle,20,ref dark,sizeof(int));};
        FormClosing+=(_,_)=>{closing=true;updateLifetime.Cancel();active?.Cancel();discovery?.Dispose();receiveView.Stop();};
    }
    static Label Label(string text,float size,Color color,bool bold=false,Point? location=null)=>new(){Text=text,AutoSize=true,ForeColor=color,BackColor=Color.Transparent,Font=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular),Location=location??Point.Empty};
    void RefreshConnection(){pairingStatus.Text=selectedDevice==null?"Recebimento automático · cada envio pede aceitação":$"Destino: {selectedDevice.Name} · cada envio pede aceitação";}
    bool SelectFiles(){using var dialog=new OpenFileDialog{Multiselect=true,Title="Escolher arquivos para "+(selectedDevice?.Name??"o aparelho")};if(dialog.ShowDialog(this)!=DialogResult.OK)return false;list.Items.Clear();AddFiles(dialog.FileNames);return list.Items.Count>0;}
    public void AddFiles(string[] paths){if(active!=null)return;foreach(var path in paths)if(File.Exists(path)&&!list.Items.Contains(path))list.Items.Add(path);UpdateSummary();}
    void UpdateSummary(){long total=list.Items.Cast<string>().Sum(p=>{try{return new FileInfo(p).Length;}catch{return 0;}});summary.Text=list.Items.Count==0?"Envio":$"{list.Items.Count} arquivo(s) · {Palette.Size(total)}";}
    async Task Send()
    {
        var target=selectedDevice;if(active!=null||updating)return;if(target?.Ready!=true||list.Items.Count==0){status.Text="Escolha um aparelho pronto para receber.";return;}
        active=new();cancel.Enabled=true;progress.Visible=false;var lifetime=active;
        try{using var deadline=CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);deadline.CancelAfter(TimeSpan.FromMinutes(5));await LocalTransfer.SendAsync(target.Address,target.Token,list.Items.Cast<string>().ToArray(),(done,total,name)=>{if(closing)return;progress.Visible=true;progress.Value=total==0?100:(int)(done*100.0/total);status.Text=$"Enviando para {target.Name} · {progress.Value}%\n{name}";},message=>{if(!closing)status.Text=message;},deadline.Token,target.Port,3,Environment.MachineName);if(!closing){progress.Visible=true;progress.Value=100;status.Text=$"Concluído · arquivos enviados para {target.Name}.";list.Items.Clear();}}
        catch(OperationCanceledException){if(!closing)status.Text=lifetime.IsCancellationRequested?"Envio cancelado.":"O envio demorou demais. Tente novamente.";}
        catch(Exception e){if(!closing)status.Text="Não foi possível enviar: "+e.Message;}
        finally{lifetime.Dispose();active=null;if(!closing){cancel.Enabled=false;UpdateSummary();}}
    }
    void Ui(Action action){if(closing||IsDisposed||!IsHandleCreated)return;try{BeginInvoke(()=>{if(!closing&&!IsDisposed)action();});}catch(InvalidOperationException)when(closing||IsDisposed){}}
}
