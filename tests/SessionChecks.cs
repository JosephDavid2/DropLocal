using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

static class SessionChecks
{
    static int Port(){using var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();return ((IPEndPoint)listener.LocalEndpoint).Port;}
    public static async Task Run()
    {
        string root=Path.Combine(Path.GetTempPath(),"drop-local-session-"+Guid.NewGuid());Directory.CreateDirectory(root);
        using var cancel=new CancellationTokenSource(TimeSpan.FromSeconds(30));var a=new DesktopReceiver();var b=new DesktopReceiver();int pa=Port(),pb=Port(),pairs=0,files=0;bool approve=true;
        Task<bool> Approve(string message){if(message.StartsWith("Parear"))pairs++;else files++;return Task.FromResult(approve);}
        var ta=a.RunAsync(Path.Combine(root,"a"),Approve,(_,_,_)=>{},_=>{},(_,_)=>{},cancel.Token,pa);var tb=b.RunAsync(Path.Combine(root,"b"),Approve,(_,_,_)=>{},_=>{},(_,_)=>{},cancel.Token,pb);
        try {
            bool rejected=false;try{await a.Session.ConnectAsync(IPAddress.Loopback,pb,"wrong",pa,cancel.Token);}catch(IOException){rejected=true;}if(!rejected||pairs!=0)throw new Exception("Invalid pairing code was approved");
            approve=false;rejected=false;try{await a.Session.ConnectAsync(IPAddress.Loopback,pb,b.Code,pa,cancel.Token);}catch(IOException){rejected=true;}if(!rejected||a.Session.Peer!=null||b.Session.Peer!=null)throw new Exception("Refused pairing created session");approve=true;pairs=0;
            await a.Session.ConnectAsync(IPAddress.Loopback,pb,b.Code,pa,cancel.Token);if(pairs!=1||a.Session.Peer?.SendToken!=b.Session.Peer?.ReceiveToken||a.Session.Peer?.ReceiveToken!=b.Session.Peer?.SendToken)throw new Exception("One pairing did not configure both directions");
            string binary=Path.Combine(root,"ação.bin"),empty=Path.Combine(root,"empty.bin");byte[] bytes=Enumerable.Range(0,190003).Select(i=>(byte)(i%251)).ToArray();File.WriteAllBytes(binary,bytes);File.WriteAllBytes(empty,[]);
            foreach(var sender in new[]{a,b,a}){var peer=sender.Session.Peer!;await LocalTransfer.SendAsync(peer.Address,peer.SendToken,new[]{binary,empty},(_,_,_)=>{},_=>{},cancel.Token,peer.Port,2);}
            if(pairs!=1||files!=3||Directory.GetFiles(Path.Combine(root,"a")).Length!=2||Directory.GetFiles(Path.Combine(root,"b")).Length!=4)throw new Exception("Bidirectional session or per-lot approval failed");
            foreach(var path in Directory.GetFiles(root,"*",SearchOption.AllDirectories).Where(p=>p!=binary&&p!=empty))if(new FileInfo(path).Length>0&&!File.ReadAllBytes(path).SequenceEqual(bytes))throw new Exception("Session transfer bytes differ");
            var peerBefore=a.Session.Peer!;approve=false;rejected=false;try{await LocalTransfer.SendAsync(peerBefore.Address,peerBefore.SendToken,new[]{binary},(_,_,_)=>{},_=>{},cancel.Token,pb,2);}catch(IOException){rejected=true;}if(!rejected||b.Session.Peer==null||Directory.GetFiles(Path.Combine(root,"b")).Length!=4)throw new Exception("File refusal removed session or wrote data");approve=true;
            foreach(var key in new[]{b.Code,PairSession.NewToken()}){using var client=new TcpClient();await client.ConnectAsync(IPAddress.Loopback,pb,cancel.Token);using var stream=client.GetStream();await stream.WriteAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{version=2,operation="ping",token=key})+"\n"),cancel.Token);using var response=await DesktopReceiver.ReadAsync(stream,cancel.Token);if(response.RootElement.GetProperty("paired").GetBoolean())throw new Exception("Unauthenticated session request accepted");}
            if(b.Session.Accepts(IPAddress.Parse("127.0.0.2"),peerBefore.SendToken))throw new Exception("Key not bound to peer address");b.Reset();if(b.Session.Accepts(IPAddress.Loopback,peerBefore.SendToken)||b.Session.Peer!=null)throw new Exception("Closed session retained credentials");
            Console.WriteLine("PASS Windows↔Windows: one confirmed pair, three alternating lots, per-lot refusal, exact bytes, key/IP validation and revocation");
        } finally {cancel.Cancel();foreach(var task in new[]{ta,tb})try{await task;}catch(OperationCanceledException){}Directory.Delete(root,true);}
    }
}
