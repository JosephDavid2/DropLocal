using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

static class JavaInteropChecks
{
    public static async Task Run(string java,string classes)
    {
        string root=Path.Combine(Path.GetTempPath(),"drop-local-java-"+Guid.NewGuid());Directory.CreateDirectory(root);using var cancel=new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var probe=new TcpListener(IPAddress.Loopback,0);probe.Start();int port=((IPEndPoint)probe.LocalEndpoint).Port;probe.Stop();var receiver=new DesktopReceiver();int approved=0;
        var receiving=receiver.RunAsync(Path.Combine(root,"received"),_=>{approved++;return Task.FromResult(true);},(_,_,_)=>{},_=>{},(_,_)=>{},cancel.Token,port);
        var start=new ProcessStartInfo(java){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};start.ArgumentList.Add("-cp");start.ArgumentList.Add(classes);start.ArgumentList.Add("com.droplocal.JavaSessionPeer");using var peer=Process.Start(start)!;
        try{string endpoint=(await peer.StandardOutput.ReadLineAsync(cancel.Token))!;if(!endpoint.StartsWith("PORT:"))throw new Exception("Java peer did not start: "+endpoint);int javaPort=int.Parse(endpoint[5..]);await receiver.Session.ConnectAsync(IPAddress.Loopback,javaPort,"12345678",port,cancel.Token);
            string binary=Path.Combine(root,"interop.bin"),empty=Path.Combine(root,"empty.bin");var bytes=Enumerable.Range(0,190003).Select(i=>(byte)(i%251)).ToArray();File.WriteAllBytes(binary,bytes);File.WriteAllBytes(empty,[]);var paired=receiver.Session.Peer!;await LocalTransfer.SendAsync(paired.Address,paired.SendToken,new[]{binary,empty},(_,_,_)=>{},_=>{},cancel.Token,paired.Port,2);await peer.WaitForExitAsync(cancel.Token);string output=await peer.StandardOutput.ReadToEndAsync();if(peer.ExitCode!=0)throw new Exception(await peer.StandardError.ReadToEndAsync());
            var saved=Directory.GetFiles(Path.Combine(root,"received"));if(approved!=1||saved.Length!=2||!saved.Any(p=>File.ReadAllBytes(p).SequenceEqual(bytes))||!saved.Any(p=>new FileInfo(p).Length==0))throw new Exception("Java to Windows bytes or consent failed");Console.Write(output);
        }finally{cancel.Cancel();try{await receiving;}catch(OperationCanceledException){}if(!peer.HasExited)peer.Kill();Directory.Delete(root,true);}
    }
}
