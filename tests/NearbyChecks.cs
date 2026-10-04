using System.Net;
using System.Net.Sockets;
using System.Text.Json;

static class NearbyChecks
{
    static int FreePort(){using var tcp=new TcpListener(IPAddress.Loopback,0);tcp.Start();return ((IPEndPoint)tcp.LocalEndpoint).Port;}
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static async Task Run()
    {
        string root=Path.Combine(Path.GetTempPath(),"drop-local-nearby-"+Guid.NewGuid());Directory.CreateDirectory(root);using var cancel=new CancellationTokenSource(TimeSpan.FromSeconds(45));
        var a=new DesktopReceiver();var b=new DesktopReceiver();int pa=FreePort(),pb=FreePort(),decisions=0;bool approved=true;
        Task<bool> Consent(string message){Check(!message.StartsWith("Parear"),"Discovery requested a separate pairing");Interlocked.Increment(ref decisions);return Task.FromResult(approved);}
        var ta=a.RunAsync(Path.Combine(root,"a"),Consent,(_,_,_)=>{},_=>{},(_,_)=>{},cancel.Token,pa);var tb=b.RunAsync(Path.Combine(root,"b"),Consent,(_,_,_)=>{},_=>{},(_,_)=>{},cancel.Token,pb);
        using var da=new LocalDiscovery("A","windows",pa,()=>a.DiscoveryToken,()=>true);using var db=new LocalDiscovery("B","windows",pb,()=>b.DiscoveryToken,()=>true);using var dc=new LocalDiscovery("Phone fixture","android",45832,PairSession.NewToken,()=>false);
        try{
            da.Error+=Console.WriteLine;db.Error+=Console.WriteLine;dc.Error+=Console.WriteLine;da.Start();db.Start();dc.Start();for(int i=0;i<80&&(da.Snapshot().Count<2||!db.Snapshot().Any(d=>d.Name=="A"));i++)await Task.Delay(100,cancel.Token);Check(da.Snapshot().Count==2&&db.Snapshot().Any(d=>d.Name=="A"),"Real LAN multicast discovery failed: A="+string.Join(",",da.Snapshot().Select(d=>d.Name))+"; B="+string.Join(",",db.Snapshot().Select(d=>d.Name)));Check(da.Snapshot().Single(d=>d.Name=="Phone fixture").Ready==false,"Unavailable recipient was presented as ready");
            var target=da.Snapshot().Single(d=>d.Name=="B");Check(target.Token==b.DiscoveryToken&&target.Port==pb,"Discovery did not expose correct endpoint");
            string binary=Path.Combine(root,"ação.bin"),empty=Path.Combine(root,"empty.bin");byte[] data=Enumerable.Range(0,190003).Select(i=>(byte)(i%251)).ToArray();File.WriteAllBytes(binary,data);File.WriteAllBytes(empty,[]);
            // Loopback transfer isolates byte/protocol tests from local firewall rules.
            foreach(var destination in new[]{(a,pa),(b,pb),(a,pa)})await LocalTransfer.SendAsync(IPAddress.Loopback,destination.Item1.DiscoveryToken,[binary,empty],(_,_,_)=>{},_=>{},cancel.Token,destination.Item2,3,"fixture");
            Check(decisions==3&&a.Session.Peer==null&&b.Session.Peer==null,"Each lot must ask once without a pairing session");foreach(var saved in Directory.GetFiles(root,"*",SearchOption.AllDirectories).Where(p=>p!=binary&&p!=empty))Check(new FileInfo(saved).Length==0||File.ReadAllBytes(saved).SequenceEqual(data),"Received bytes differ");
            approved=false;bool refused=false;try{await LocalTransfer.SendAsync(IPAddress.Loopback,b.DiscoveryToken,[binary],(_,_,_)=>{},_=>{},cancel.Token,pb,3);}catch(System.IO.IOException){refused=true;}Check(refused&&decisions==4&&Directory.GetFiles(Path.Combine(root,"b")).Length==2,"Refused offer wrote data or asked more than once");
            approved=true;string previous=b.DiscoveryToken;b.Reset();foreach(var invalid in new[]{previous,b.Code,PairSession.NewToken()}){refused=false;try{await LocalTransfer.SendAsync(IPAddress.Loopback,invalid,[binary],(_,_,_)=>{},_=>{},cancel.Token,pb,3);}catch(System.IO.IOException){refused=true;}Check(refused&&decisions==4,"Old/invalid announcement token reached consent");}
            var payload=JsonSerializer.SerializeToUtf8Bytes(new{service="DropLocal",version=3,id=new string('a',32),name="Name",kind="windows",port=pb,token=b.DiscoveryToken,ready=true});Check(LocalDiscovery.Parse(payload,IPAddress.Loopback,"own",DateTimeOffset.UtcNow)!=null,"Valid announcement rejected");Check(LocalDiscovery.Parse(payload,IPAddress.Loopback,new string('a',32),DateTimeOffset.UtcNow)==null,"Own announcement shown");Check(LocalDiscovery.Parse(new byte[3000],IPAddress.Loopback,"own",DateTimeOffset.UtcNow)==null&&LocalDiscovery.Parse("{}"u8,IPAddress.Loopback,"own",DateTimeOffset.UtcNow)==null,"Malformed announcement accepted");
            dc.Dispose();for(int i=0;i<130&&da.Snapshot().Any(d=>d.Kind=="android");i++)await Task.Delay(100,cancel.Token);Check(!da.Snapshot().Any(d=>d.Kind=="android"),"Closed app never expired from device list");
            Console.WriteLine("PASS nearby: real multicast, three peers, unavailable state, expiry; v3 alternating transfers, one consent per lot, exact binary/Unicode/empty bytes, refusal and rotated-token rejection.");
        }finally{cancel.Cancel();foreach(var task in new[]{ta,tb})try{await task;}catch(OperationCanceledException){}Directory.Delete(root,true);}
    }
}
