using System.Net;

internal sealed partial class MainForm
{
    IEnumerable<Control> Descendants(Control parent){foreach(Control child in parent.Controls){yield return child;foreach(var nested in Descendants(child))yield return nested;}}
    public async Task VerifyStability(string root)
    {
        if(Descendants(this).Any(c=>c.Text.Contains("tutorial",StringComparison.OrdinalIgnoreCase)||c.Text.Contains("Pular")))throw new Exception("Tutorial remains in native UI");
        var first=new NearbyDevice(new string('a',32),"Notebook","windows",IPAddress.Loopback,45833,new string('a',64),true,DateTimeOffset.UtcNow);UpdateNearby(new[]{first});nearby.SelectedIndex=0;nearby.Focus();var item=nearby.Items[0];Control? focus=ActiveControl;
        for(int i=0;i<30;i++){UpdateNearby(new[]{first with{Seen=DateTimeOffset.UtcNow.AddSeconds(i)}});await Task.Delay(10);if(!ReferenceEquals(item,nearby.Items[0])||ActiveControl!=focus||selectedDevice?.Id!=first.Id)throw new Exception("Heartbeat rebuilt unchanged device UI or changed selection/focus");}
        UpdateNearby(new[]{first with{Token=new string('b',64)}});if(selectedDevice?.Token!=new string('b',64)||ActiveControl!=focus)throw new Exception("Changed credential was lost or changed focus");UpdateNearby(Array.Empty<NearbyDevice>());if(selectedDevice!=null||nearby.Items.Count!=0)throw new Exception("Expired recipient retained");
    }
    public async Task VerifySimpleReceive(string root)
    {
        using var probe=new System.Net.Sockets.TcpListener(IPAddress.Loopback,0);probe.Start();int receiverPort=((IPEndPoint)probe.LocalEndpoint).Port;probe.Stop();receiveView.StartForCheck(Path.Combine(root,"received"),receiverPort);for(int i=0;i<100&&!receiveView.IsListening;i++)await Task.Delay(20);if(!receiveView.IsListening)throw new Exception("Automatic receiver did not start");
        string binary=Path.Combine(root,"ação.bin"),empty=Path.Combine(root,"empty.bin");byte[] bytes=Enumerable.Range(0,131079).Select(i=>(byte)(i%251)).ToArray();File.WriteAllBytes(binary,bytes);File.WriteAllBytes(empty,[]);int prompts=0;bool acceptOffer=true;string expectedSender="";
        using var answer=new System.Windows.Forms.Timer{Interval=30};answer.Tick+=(_,_)=>{var dialog=Application.OpenForms.Cast<Form>().FirstOrDefault(f=>f.Text=="Drop Local — autorização");if(dialog==null)return;string text=Descendants(dialog).OfType<TextBox>().Single().Text;if(!text.Contains(expectedSender)||dialog.Owner!=this)throw new Exception("Incoming dialog did not identify sender or belong to main window");prompts++;Descendants(dialog).OfType<ActionButton>().Single(b=>b.Text==(acceptOffer?"Aceitar":"Recusar")).PerformClick();};answer.Start();using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(20));
        try{for(int i=0;i<3;i++){if(i==1){UpdateNearby(Enumerable.Range(0,4).Select(n=>new NearbyDevice(n.ToString().PadLeft(32,'a'),"Other "+n,"android",IPAddress.Loopback,46000+n,new string('c',64),true,DateTimeOffset.UtcNow)).ToArray());nearby.SelectedIndex=3;}if(i==2)nearby.SelectedIndex=1;expectedSender="Incoming "+i;await LocalTransfer.SendAsync(IPAddress.Loopback,receiveView.DiscoveryToken,new[]{binary,empty},(_,_,_)=>{},_=>{},timeout.Token,receiverPort,3,expectedSender);}
            acceptOffer=false;expectedSender="Refused sender";bool refused=false;try{await LocalTransfer.SendAsync(IPAddress.Loopback,receiveView.DiscoveryToken,new[]{binary},(_,_,_)=>{},_=>{},timeout.Token,receiverPort,3,expectedSender);}catch(System.IO.IOException){refused=true;}var saved=Directory.GetFiles(Path.Combine(root,"received"));if(!refused||prompts!=4||saved.Length!=6)throw new Exception("Automatic incoming consent/refusal count or writes differs");foreach(var path in saved)if(new FileInfo(path).Length>0&&!File.ReadAllBytes(path).SequenceEqual(bytes))throw new Exception("Received bytes differ");
        }finally{answer.Stop();receiveView.Stop();for(int i=0;i<100&&receiveView.IsRunning;i++)await Task.Delay(20);}
    }
}
