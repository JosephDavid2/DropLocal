using System.Net;
using System.Net.NetworkInformation;

internal sealed class ReceiveView : UserControl
{
    readonly DesktopReceiver receiver=new();
    readonly Action<IPAddress,string> paired;
    readonly Label state=new(){Text="Escolha a pasta e inicie o recebimento.",Dock=DockStyle.Fill,ForeColor=Palette.Muted,AutoSize=false};
    readonly TextBox folder=new(){ReadOnly=true,Multiline=true,BorderStyle=BorderStyle.None,Dock=DockStyle.Fill,BackColor=Palette.Field,ForeColor=Palette.Text};
    readonly ComboBox addresses=new(){Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList,BackColor=Palette.Field,ForeColor=Palette.Text,FlatStyle=FlatStyle.Flat};
    readonly Label endpoint=new(){Dock=DockStyle.Fill,ForeColor=Palette.Muted};
    readonly Label verificationLabel=new(){Dock=DockStyle.Fill,ForeColor=Palette.Accent,Font=new Font("Consolas",18,FontStyle.Bold)};
    readonly PictureBox qr=new(){Width=270,Height=270,BackColor=Color.White,SizeMode=PictureBoxSizeMode.Zoom,Anchor=AnchorStyles.None,Margin=Padding.Empty};
    readonly ActionButton start=new(){Text="Ativar sessão",Primary=true,Dock=DockStyle.Fill};
    readonly ActionButton stop=new(){Text="Encerrar sessão / cancelar",Danger=true,Dock=DockStyle.Fill,Enabled=false};
    readonly TransferProgress progress=new(){Dock=DockStyle.Fill,Visible=false};
    CancellationTokenSource? session;
    bool demoPreview;
    public event Action<bool>? ListeningChanged;
    public ReceiveView(Action<IPAddress,string> paired,Control connectControls)
    {
        this.paired=paired;BackColor=Palette.Background;ForeColor=Palette.Text;Font=new Font("Segoe UI",10);AutoScroll=true;
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,MinimumSize=new Size(692,640),Margin=Padding.Empty,ColumnCount=2,RowCount=2,BackColor=Palette.Background};
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,320));root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,128));Controls.Add(root);
        var pairing=new Card{Dock=DockStyle.Fill,Margin=new Padding(0,0,12,12)};
        var left=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=7,BackColor=Palette.Panel,Margin=Padding.Empty};
        left.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));foreach(var h in new float[]{26,24,34,24,40,270,48})left.RowStyles.Add(new RowStyle(SizeType.Absolute,h));
        left.Controls.Add(TextLabel("Meu IP e código / QR",12,true),0,0);left.Controls.Add(TextLabel("IP do computador",9),0,1);left.Controls.Add(addresses,0,2);left.Controls.Add(endpoint,0,3);
        verificationLabel.Text=receiver.Code;left.Controls.Add(verificationLabel,0,4);left.Controls.Add(qr,0,5);
        left.Controls.Add(TextLabel("Ative a sessão. No outro aparelho, leia este QR ou informe IP e código.",9),0,6);pairing.Controls.Add(left);root.Controls.Add(pairing,0,0);
        addresses.DrawMode=DrawMode.OwnerDrawFixed;addresses.ItemHeight=24;addresses.DrawItem+=(_,e)=>{using var bg=new SolidBrush(Palette.Field);e.Graphics.FillRectangle(bg,e.Bounds);string value=e.Index>=0?addresses.Items[e.Index]?.ToString()??"":"";
            TextRenderer.DrawText(e.Graphics,value,Font,e.Bounds,Palette.Text,TextFormatFlags.VerticalCenter);};
        var ips=NetworkInterface.GetAllNetworkInterfaces().Where(n=>n.OperationalStatus==OperationalStatus.Up).OrderBy(n=>n.NetworkInterfaceType==NetworkInterfaceType.Wireless80211?0:1).SelectMany(n=>n.GetIPProperties().UnicastAddresses).Select(a=>a.Address).Where(a=>a.AddressFamily==System.Net.Sockets.AddressFamily.InterNetwork&&!IPAddress.IsLoopback(a)).Distinct().ToArray();
        addresses.Items.AddRange(ips.Select(ip=>(object)ip.ToString()).ToArray());if(ips.Length>0)addresses.SelectedIndex=0;addresses.SelectedIndexChanged+=(_,_)=>DrawQr();DrawQr();
        var destination=new Card{Dock=DockStyle.Fill,Margin=new Padding(0,0,0,12)};
        var right=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=10,BackColor=Palette.Panel,Margin=Padding.Empty};right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        foreach(var h in new float[]{26,0,24,54,40,0,40,40,0})right.RowStyles.Add(new RowStyle(SizeType.Absolute,h));right.RowStyles.Add(new RowStyle(SizeType.Absolute,190));
        right.Controls.Add(TextLabel("Receber arquivos",12,true),0,0);right.Controls.Add(TextLabel("Defina o destino e deixe o computador pronto para receber.",9),0,1);right.Controls.Add(TextLabel("Pasta de destino",9),0,2);
        folder.Text=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads","DropLocal");
        var field=new InputFrame{Dock=DockStyle.Fill,Padding=new Padding(10),Margin=new Padding(0,0,0,8)};field.Controls.Add(folder);right.Controls.Add(field,0,3);
        chooseFolder=new ActionButton{Text="Escolher pasta",Dock=DockStyle.Fill};chooseFolder.Click+=(_,_)=>{using var d=new FolderBrowserDialog();if(d.ShowDialog(FindForm())==DialogResult.OK){folder.Text=d.SelectedPath;GuideAction?.Invoke("folder");}};right.Controls.Add(chooseFolder,0,4);
        right.Controls.Add(connectControls,0,9);right.Controls.Add(start,0,6);right.Controls.Add(stop,0,7);right.Controls.Add(TextLabel("O recebimento continua ativo ao mudar de aba. Você confirma cada pedido.",9),0,8);destination.Controls.Add(right);root.Controls.Add(destination,1,0);
        var activity=new Card{Dock=DockStyle.Fill,Margin=Padding.Empty};
        var footer=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=3,BackColor=Palette.Panel};footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,190));
        footer.RowStyles.Add(new RowStyle(SizeType.Absolute,24));footer.RowStyles.Add(new RowStyle(SizeType.Percent,100));footer.RowStyles.Add(new RowStyle(SizeType.Absolute,12));
        footer.Controls.Add(TextLabel("Atividade • recebimento",10,true),0,0);footer.Controls.Add(state,0,1);
        var open=new ActionButton{Text="Abrir pasta recebida",Dock=DockStyle.Fill};open.Click+=(_,_)=>{if(Directory.Exists(folder.Text))System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(folder.Text){UseShellExecute=true});};footer.Controls.Add(open,1,1);
        footer.Controls.Add(progress,0,2);footer.SetColumnSpan(progress,2);activity.Controls.Add(footer);root.Controls.Add(activity,0,1);root.SetColumnSpan(activity,2);
        start.Click+=async(_,_)=>{
            session=new();start.Enabled=chooseFolder.Enabled=folder.Enabled=addresses.Enabled=false;stop.Enabled=true;ListeningChanged?.Invoke(true);progress.Value=0;progress.Visible=false;
            try{await receiver.RunAsync(folder.Text,Approve,(done,total,name)=>Ui(()=>{progress.Visible=true;progress.Value=total==0?100:(int)(100.0*done/total);state.Text=$"Recebendo • {progress.Value}%\n{name}";}),
                s=>Ui(()=>{state.Text=s;if(s.StartsWith("Receptor disponível"))GuideAction?.Invoke("start");if(s.StartsWith("Pedido recebido")||s.StartsWith("Receptor disponível")){progress.Value=0;progress.Visible=false;}if(s.StartsWith("Transferência concluída")){progress.Value=100;progress.Visible=true;}}),
                (ip,c)=>Ui(()=>paired(ip,c)),session.Token);}
            catch(OperationCanceledException){if(!IsDisposed)state.Text="Recebimento parado.";}catch(Exception e){if(!IsDisposed)state.Text="Falha: "+e.Message;}
            finally{session.Dispose();session=null;receiver.Reset();DrawQr();if(!IsDisposed){start.Enabled=addresses.Items.Count>0;chooseFolder.Enabled=folder.Enabled=addresses.Enabled=true;stop.Enabled=false;ListeningChanged?.Invoke(false);}}
        };
        stop.Click+=(_,_)=>Stop();
    }
    static Label TextLabel(string text,float size,bool bold=false)=>new(){Text=text,Dock=DockStyle.Fill,AutoSize=false,ForeColor=bold?Palette.Text:Palette.Muted,BackColor=Color.Transparent,Font=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular),Margin=Padding.Empty};
    public void Stop()=>session?.Cancel();
    internal void ApplyAvailability(){chooseFolder.Enabled=!IsListening;start.Enabled=!IsListening;stop.Enabled=IsListening;}
    internal PairSession Pairing=>receiver.Session;
    internal ActionButton chooseFolder=null!;
    internal Control StartControl=>start;
    internal Action<string>? GuideAction;
    internal string VerificationCode=>receiver.Code;
    internal void DemoPreview(){demoPreview=true;folder.Text="Downloads\\DropLocal";addresses.Items.Clear();addresses.Items.Add("192.168.1.20");addresses.SelectedIndex=0;DrawQr();}
    internal bool IsListening=>session!=null;
    internal void StartForCheck(string destination){folder.Text=destination;start.PerformClick();}
    protected override void Dispose(bool disposing){if(disposing){Stop();qr.Image?.Dispose();}base.Dispose(disposing);}
    void Ui(Action action){if(IsDisposed)return;if(InvokeRequired)BeginInvoke(()=>{if(!IsDisposed)action();});else action();}
    Task<bool> Approve(string message)
    {
        var source=new TaskCompletionSource<bool>();
        Ui(() => {
            using var dialog=new Form { Text="Drop Local — autorização", Icon=AppIdentity.Icon, BackColor=Palette.Background, ForeColor=Palette.Text, ClientSize=new Size(540,420), StartPosition=FormStartPosition.CenterParent, MinimizeBox=false, MaximizeBox=false };
            var layout=new TableLayoutPanel { Dock=DockStyle.Fill, Padding=new Padding(18), ColumnCount=2, RowCount=2 };layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,52));dialog.Controls.Add(layout);
            var details=new TextBox { Text=message, Multiline=true, ReadOnly=true, ScrollBars=ScrollBars.Vertical, Dock=DockStyle.Fill, BackColor=Palette.Field, ForeColor=Palette.Text, Font=new Font("Segoe UI",10), BorderStyle=BorderStyle.None };
            layout.Controls.Add(details,0,0);layout.SetColumnSpan(details,2);
            var no=new ActionButton { Text="Recusar", Danger=true, Dock=DockStyle.Fill, DialogResult=DialogResult.No };var yes=new ActionButton { Text="Aceitar", Primary=true, Dock=DockStyle.Fill, DialogResult=DialogResult.Yes };layout.Controls.Add(no,0,1);layout.Controls.Add(yes,1,1);dialog.AcceptButton=yes;dialog.CancelButton=no;
            using var timer=new System.Windows.Forms.Timer { Interval=120000 };timer.Tick+=(_,_)=>{dialog.DialogResult=DialogResult.No;dialog.Close();};timer.Start();source.TrySetResult(dialog.ShowDialog(FindForm())==DialogResult.Yes);
        });return source.Task.WaitAsync(session!.Token);
    }
    void DrawQr(){if(addresses.SelectedItem is not string ip){endpoint.Text="Conecte-se ao Wi-Fi para mostrar o IP.";start.Enabled=false;return;}endpoint.Text="IP: "+ip;string displayedCode=demoPreview?"31415926":receiver.Code;verificationLabel.Text=displayedCode;var cells=PairQr.Encode($"droplocal://pair?ip={ip}&port=45833&token={displayedCode}");var image=new Bitmap(270,270);using(var g=Graphics.FromImage(image)){g.Clear(Color.White);for(int y=0;y<37;y++)for(int x=0;x<37;x++)if(cells[y,x])g.FillRectangle(Brushes.Black,(x+4)*6,(y+4)*6,6,6);}qr.Image?.Dispose();qr.Image=image;}
}


