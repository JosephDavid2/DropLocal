using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

if(args.Length==2&&args[0]=="--verify-github-release"){await UpdateChecks.VerifyPublished(Path.GetFullPath(args[1]));return;}
var temp = Path.Combine(Path.GetTempPath(), "drop-local-tests-" + Guid.NewGuid());
Directory.CreateDirectory(temp);
try
{
    var paths = new[] { Path.Combine(temp,"aÃ§Ã£o.txt"), Path.Combine(temp,"vazio.bin"), Path.Combine(temp,"large.bin") };
    var contents = new[] { Encoding.UTF8.GetBytes("OlÃ¡ Android!"), Array.Empty<byte>(), new byte[2*1024*1024+17] };
    Random.Shared.NextBytes(contents[2]);
    for (int i=0;i<paths.Length;i++) await File.WriteAllBytesAsync(paths[i],contents[i]);
    foreach (var scenario in new[] { "accept", "reject", "disconnect", "cancel", "bad-ack" })
    {
        using var listener = new TcpListener(IPAddress.Loopback,0); listener.Start(); int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var receiver = Task.Run(async () => {
            using var peer = await listener.AcceptTcpClientAsync(timeout.Token); using var stream = peer.GetStream();
            using var header = new MemoryStream(); var b = new byte[1];
            while (await stream.ReadAsync(b,timeout.Token) != 0 && b[0] != 10) header.WriteByte(b[0]);
            using var offer = JsonDocument.Parse(header.ToArray());
            if (offer.RootElement.GetProperty("token").GetString() != "12345678") throw new Exception("Wrong token");
            var files = offer.RootElement.GetProperty("files").EnumerateArray().ToArray();
            if (files.Length != 3 || files[0].GetProperty("name").GetString() != "aÃ§Ã£o.txt") throw new Exception("Wrong metadata");
            if (scenario == "cancel") { if (await stream.ReadAsync(b,timeout.Token) != 0) throw new Exception("Bytes sent before acceptance"); return; }
            await stream.WriteAsync(Encoding.UTF8.GetBytes(scenario == "reject" ? "{\"accepted\":false}\n" : "{\"accepted\":true}\n"),timeout.Token);
            if (scenario == "reject" || scenario == "disconnect") return;
            for (int i=0;i<files.Length;i++) {
                var bytes = new byte[files[i].GetProperty("size").GetInt32()]; await stream.ReadExactlyAsync(bytes,timeout.Token);
                if (!bytes.SequenceEqual(contents[i])) throw new Exception("Bytes differ");
                await stream.WriteAsync(Encoding.UTF8.GetBytes(scenario == "bad-ack" ? "{\"ok\":false}\n" : "{\"ok\":true}\n"),timeout.Token);
                if (scenario == "bad-ack") return;
            }
        });
        using var senderCancel = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
        if (scenario == "cancel") senderCancel.CancelAfter(200);
        Exception? failure = null; long progress = 0;
        try { await LocalTransfer.SendAsync(IPAddress.Loopback,"12345678",paths,(done,total,name) => progress = done,_ => {},senderCancel.Token,port); }
        catch (Exception ex) { failure = ex; }
        await receiver;
        if (scenario == "accept" && (failure != null || progress != contents.Sum(c => (long)c.Length))) throw new Exception("Accepted transfer failed",failure);
        if (scenario != "accept" && failure == null) throw new Exception("Expected failure: " + scenario);
        if (scenario == "cancel" && failure is not OperationCanceledException) throw new Exception("Cancellation failed",failure);
        Console.WriteLine("PASS " + scenario);
    }
}
finally { Directory.Delete(temp,true); }


var qrPayload="droplocal://pair?ip=192.168.1.20&port=45833&token=12345678";
var (qrPixels,reserved)=PairQr.Layout();
if(37*37-reserved.Cast<bool>().Count(b=>b)!=1079)throw new Exception("Unexpected QR capacity");
var matrix=PairQr.Encode(qrPayload);
Directory.CreateDirectory("dist");
await File.WriteAllTextAsync("dist/qr-test.matrix",qrPayload+"\n"+string.Join("\n",Enumerable.Range(0,37).Select(y=>string.Concat(Enumerable.Range(0,37).Select(x=>matrix[y,x]?'1':'0')))));
Console.WriteLine("PASS QR version 5-L structure/capacity and encoding");
var receiveRoot=Path.Combine(Path.GetTempPath(),"drop-local-receive-tests-"+Guid.NewGuid());
Directory.CreateDirectory(receiveRoot);
try {
    using var probe=new TcpListener(IPAddress.Loopback,0);probe.Start();int port=((IPEndPoint)probe.LocalEndpoint).Port;probe.Stop();
    using var cancelReceiver=new CancellationTokenSource(TimeSpan.FromSeconds(15));
    var receiver=new DesktopReceiver();bool approve=true;string? pairedCode=null;
    var receiving=receiver.RunAsync(receiveRoot,_=>Task.FromResult(approve),(_,_,_)=>{},_=>{},(_,code)=>pairedCode=code,cancelReceiver.Token,port);
    using(var peer=new TcpClient()){await peer.ConnectAsync(IPAddress.Loopback,port);using var stream=peer.GetStream();await stream.WriteAsync(Encoding.UTF8.GetBytes("{\"version\":1,\"token\":\"wrong\",\"files\":[]}\n"));using var denied=await DesktopReceiver.ReadAsync(stream,cancelReceiver.Token);if(denied.RootElement.GetProperty("accepted").GetBoolean())throw new Exception("Invalid code accepted");}
    Console.WriteLine("PASS Windows invalid verification code rejected");
    using(var peer=new TcpClient()){await peer.ConnectAsync(IPAddress.Loopback,port);using var stream=peer.GetStream();await stream.WriteAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new {version=1,operation="pair",token=receiver.Code,androidToken="87654321"})+"\n"));using var pair=await DesktopReceiver.ReadAsync(stream,cancelReceiver.Token);if(!pair.RootElement.GetProperty("paired").GetBoolean()||pairedCode!="87654321")throw new Exception("Pairing failed");}
    Console.WriteLine("PASS bidirectional pairing handshake");
    string first=Path.Combine(receiveRoot,"source-ação.bin"),empty=Path.Combine(receiveRoot,"source-empty.bin");var bytes=new byte[200017];Random.Shared.NextBytes(bytes);await File.WriteAllBytesAsync(first,bytes);await File.WriteAllBytesAsync(empty,[]);
    await LocalTransfer.SendAsync(IPAddress.Loopback,receiver.Code,new[]{first,empty},(_,_,_)=>{},_=>{},cancelReceiver.Token,port);
    var received=Directory.GetFiles(receiveRoot).Except(new[]{first,empty}).ToArray();if(received.Length!=2||!received.Any(p=>File.ReadAllBytes(p).SequenceEqual(bytes))||!received.Any(p=>new FileInfo(p).Length==0))throw new Exception("Windows bytes mismatch");
    Console.WriteLine("PASS Windows receives multiple binary/Unicode/empty files");
    approve=false;bool refused=false;try{await LocalTransfer.SendAsync(IPAddress.Loopback,receiver.Code,new[]{first},(_,_,_)=>{},_=>{},cancelReceiver.Token,port);}catch(IOException){refused=true;}if(!refused||Directory.GetFiles(receiveRoot).Length!=4)throw new Exception("Refusal wrote files");
    Console.WriteLine("PASS Windows explicit refusal and no writes");
    approve=true;
    using(var peer=new TcpClient()){await peer.ConnectAsync(IPAddress.Loopback,port);using var stream=peer.GetStream();await stream.WriteAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{version=1,token=receiver.Code,files=new[]{new{name="partial.bin",size=500L}}})+"\n"));using var reply=await DesktopReceiver.ReadAsync(stream,cancelReceiver.Token);await stream.WriteAsync(new byte[]{1,2,3});}
    for(int attempt=0;attempt<100&&Directory.GetFiles(receiveRoot).Any(p=>p.EndsWith("partial.bin"));attempt++)await Task.Delay(10);
    await LocalTransfer.SendAsync(IPAddress.Loopback,receiver.Code,new[]{empty},(_,_,_)=>{},_=>{},cancelReceiver.Token,port);
    if(Directory.GetFiles(receiveRoot).Any(p=>p.EndsWith("partial.bin")))throw new Exception("Partial file retained");
    Console.WriteLine("PASS interrupted Windows receive cleanup and subsequent transfer");
    cancelReceiver.Cancel();try{await receiving;}catch(OperationCanceledException){}
} finally { if(!receiveRoot.StartsWith(Path.GetTempPath(),StringComparison.OrdinalIgnoreCase))throw new Exception("Unexpected cleanup path");Directory.Delete(receiveRoot,true); }
await UpdateChecks.Run();
await SessionChecks.Run();
await NearbyChecks.Run();
if(args.Length==2)await JavaInteropChecks.Run(args[0],Path.GetFullPath(args[1]));
