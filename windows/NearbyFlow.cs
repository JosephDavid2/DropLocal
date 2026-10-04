internal sealed partial class MainForm
{
    readonly ListBox nearby=new(){Dock=DockStyle.Fill,BackColor=Palette.Field,ForeColor=Palette.Text,BorderStyle=BorderStyle.None,ItemHeight=48,DrawMode=DrawMode.OwnerDrawFixed,AccessibleName="Aparelhos disponíveis",IntegralHeight=false};
    readonly Label nearbyHint=new(){Text="Procurando aparelhos com Drop Local aberto na mesma rede…",Dock=DockStyle.Fill,ForeColor=Palette.Muted};
    NearbyDevice? selectedDevice;LocalDiscovery? discovery;bool nearbyInitialized;
    Control CreateNearbyPanel()
    {
        var card=new Card{Dock=DockStyle.Fill,Margin=new Padding(0,0,0,12)};
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,BackColor=Palette.Panel};layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,28));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,40));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        layout.Controls.Add(new Label{Text="Aparelhos próximos",Dock=DockStyle.Fill,ForeColor=Palette.Text,Font=new Font("Segoe UI",13,FontStyle.Bold)},0,0);layout.Controls.Add(nearbyHint,0,1);layout.Controls.Add(nearby,0,2);card.Controls.Add(layout);
        nearby.DrawItem+=(_,e)=>{if(e.Index<0)return;var device=(NearbyDevice)nearby.Items[e.Index];using var fill=new SolidBrush((e.State&DrawItemState.Selected)!=0?Palette.Accent:Palette.Field);e.Graphics.FillRectangle(fill,e.Bounds);var ink=(e.State&DrawItemState.Selected)!=0?Palette.Background:Palette.Accent;using var pen=new Pen(ink,2);int y=e.Bounds.Y+10;if(device.Kind=="android"){e.Graphics.DrawRectangle(pen,e.Bounds.X+13,y,16,28);e.Graphics.DrawLine(pen,e.Bounds.X+18,y+23,e.Bounds.X+24,y+23);}else{e.Graphics.DrawRectangle(pen,e.Bounds.X+8,y+3,28,20);e.Graphics.DrawLine(pen,e.Bounds.X+22,y+23,e.Bounds.X+22,y+28);e.Graphics.DrawLine(pen,e.Bounds.X+14,y+28,e.Bounds.X+30,y+28);}TextRenderer.DrawText(e.Graphics,device.ToString(),Font,new Rectangle(e.Bounds.X+46,e.Bounds.Y,e.Bounds.Width-46,e.Bounds.Height),(e.State&DrawItemState.Selected)!=0?Palette.Background:Palette.Text,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);};
        nearby.MouseClick+=async(_,_)=>{if(selectedDevice?.Ready!=true||active!=null)return;if(SelectFiles())await Send();};
        nearby.AllowDrop=true;nearby.DragEnter+=(_,e)=>e.Effect=active==null&&e.Data?.GetDataPresent(DataFormats.FileDrop)==true?DragDropEffects.Copy:DragDropEffects.None;
        nearby.DragDrop+=async(_,e)=>{if(active!=null)return;var p=nearby.PointToClient(new Point(e.X,e.Y));int index=nearby.IndexFromPoint(p);if(index<0)return;nearby.SelectedIndex=index;if(selectedDevice?.Ready!=true){status.Text="O destinatário ainda está preparando o recebimento.";return;}if(e.Data?.GetData(DataFormats.FileDrop) is string[] paths){list.Items.Clear();AddFiles(paths);await Send();}};
        return card;
    }
    void StartDiscovery()
    {
        discovery=new LocalDiscovery(Environment.MachineName,"windows",DesktopReceiver.Port,()=>receiveView.DiscoveryToken,()=>receiveView.IsListening);
        discovery.Changed+=devices=>{if(IsDisposed||!IsHandleCreated)return;Ui(()=>UpdateNearby(devices));};discovery.Error+=message=>{if(!IsDisposed&&IsHandleCreated)Ui(()=>nearbyHint.Text=message);};discovery.Start();
    }
    void UpdateNearby(IReadOnlyList<NearbyDevice> devices)
    {
        if(IsDisposed)return;if(nearbyInitialized&&nearby.Items.Cast<NearbyDevice>().Select(d=>d with{Seen=default}).SequenceEqual(devices.Select(d=>d with{Seen=default})))return;nearbyInitialized=true;string? chosen=selectedDevice?.Id;nearby.BeginUpdate();nearby.Items.Clear();foreach(var device in devices)nearby.Items.Add(device);selectedDevice=devices.FirstOrDefault(d=>d.Id==chosen);if(selectedDevice!=null)nearby.SelectedItem=selectedDevice;nearby.EndUpdate();
        nearbyHint.Text=devices.Count==0?"Abra Drop Local no outro aparelho. Se não aparecer, use IP / QR.":"Arraste arquivos para um aparelho ou clique para escolher arquivos.";RefreshConnection();
        // Discovery refresh must respect the current tutorial gate.
        
    }
    public void PreviewNearby(){receiveView.Preview();UpdateNearby(new[]{new NearbyDevice("demo","Celular de Joseph","android",System.Net.IPAddress.Parse("192.168.1.20"),45832,new string('a',64),true,DateTimeOffset.UtcNow),new NearbyDevice("demo2","Notebook","windows",System.Net.IPAddress.Parse("192.168.1.21"),45833,new string('b',64),true,DateTimeOffset.UtcNow)});}
    public async Task VerifyNearbyUi(string root)
    {
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(25));var b=new DesktopReceiver();var c=new DesktopReceiver();
        int FreePort(){using var probe=new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback,0);probe.Start();return ((System.Net.IPEndPoint)probe.LocalEndpoint).Port;}
        int pb=FreePort(),pc=FreePort(),consents=0;Task<bool> Approve(string message){if(message.StartsWith("Parear"))throw new Exception("Extra pairing dialog");consents++;return Task.FromResult(true);}
        var tb=b.RunAsync(Path.Combine(root,"b"),Approve,(_,_,_)=>{},_=>{},(_,_)=>{},timeout.Token,pb);var tc=c.RunAsync(Path.Combine(root,"c"),Approve,(_,_,_)=>{},_=>{},(_,_)=>{},timeout.Token,pc);
        try{
            string binary=Path.Combine(root,"ui.bin"),empty=Path.Combine(root,"empty.bin");byte[] bytes=Enumerable.Range(0,131079).Select(i=>(byte)(i%251)).ToArray();File.WriteAllBytes(binary,bytes);File.WriteAllBytes(empty,[]);
            UpdateNearby(new[]{new NearbyDevice("b","B","windows",System.Net.IPAddress.Loopback,pb,b.DiscoveryToken,true,DateTimeOffset.UtcNow),new NearbyDevice("c","C","android",System.Net.IPAddress.Loopback,pc,c.DiscoveryToken,true,DateTimeOffset.UtcNow)});
            for(int index=0;index<2;index++){
                nearby.SelectedIndex=index;Application.DoEvents();var screen=nearby.PointToScreen(new Point(15,nearby.ItemHeight*index+12));var data=new DataObject();data.SetData(DataFormats.FileDrop,new[]{binary,empty});
                // Exercise the production drop handler with screen coordinates and the real v3 transport.
                typeof(Control).GetMethod("OnDragDrop",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.Invoke(nearby,new object[]{new DragEventArgs(data,0,screen.X,screen.Y,DragDropEffects.Copy,DragDropEffects.Copy)});
                for(int i=0;i<200&&active!=null;i++)await Task.Delay(20,timeout.Token);
                if(active!=null||!status.Text.StartsWith("Concluído"))throw new Exception("Drop did not complete: "+status.Text);
            }
            if(consents!=2||Directory.GetFiles(Path.Combine(root,"b")).Length!=2||Directory.GetFiles(Path.Combine(root,"c")).Length!=2)throw new Exception("Drop recipient or consent count differs");
            foreach(var file in Directory.GetFiles(root,"*",SearchOption.AllDirectories).Where(p=>p!=binary&&p!=empty))if(new FileInfo(file).Length!=0&&!File.ReadAllBytes(file).SequenceEqual(bytes))throw new Exception("UI transfer bytes differ");
            if(receiveView.Pairing.Peer!=null)throw new Exception("Drop created manual pairing");
        }finally{timeout.Cancel();foreach(var task in new[]{tb,tc})try{await task;}catch(OperationCanceledException){}}
    }
}
