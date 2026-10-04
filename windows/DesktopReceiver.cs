using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

public sealed class DesktopReceiver
{
    public PairSession Session { get; } = new();
    public string Code { get; private set; } = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000000).ToString("D8");
    public const int Port=45833;
    public void Reset(){Session.Clear();Code=System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000000).ToString("D8");}
    public async Task RunAsync(string directory, Func<string,Task<bool>> approve, Action<long,long,string> progress, Action<string> state, Action<IPAddress,string> paired, CancellationToken ct, int port=Port)
    {
        Directory.CreateDirectory(directory);var listener=new TcpListener(IPAddress.Any,port);listener.Start();
        try {
            state("Receptor disponível para novos envios.");
            while(!ct.IsCancellationRequested){using var client=await listener.AcceptTcpClientAsync(ct);
                try { using var deadline=CancellationTokenSource.CreateLinkedTokenSource(ct);deadline.CancelAfter(TimeSpan.FromMinutes(5));var token=deadline.Token;using var stream=client.GetStream();
                    using var offer=await ReadAsync(stream,token);var r=offer.RootElement;
                    var ip=((IPEndPoint)client.Client.RemoteEndPoint!).Address; int version=r.GetProperty("version").GetInt32(); string? supplied=r.GetProperty("token").GetString(); bool pairing=r.TryGetProperty("operation",out var operation)&&operation.GetString()=="pair";
                    if((version!=1&&version!=2) || (pairing ? supplied!=Code : version==2 ? !Session.Accepts(ip,supplied) : supplied!=Code)){await WriteAsync(stream,new{accepted=false,paired=false},token);continue;}
                    if(r.TryGetProperty("operation",out var op)&&op.GetString()=="pair"){
                        if(version==2&&(!r.TryGetProperty("peerPort",out var advertised)||advertised.GetInt32() is <1 or >65535||!r.TryGetProperty("peerToken",out var remoteKey)||!PairSession.ValidToken(remoteKey.GetString()))){await WriteAsync(stream,new{paired=false},token);continue;}
                        bool accepted=await approve($"Parear com o aparelho {ip}?\nO código confirma que o QR/entrada manual pertence a este receptor.").WaitAsync(TimeSpan.FromMinutes(2),token);
                        if(accepted&&r.TryGetProperty("androidToken",out var phoneCode)&&phoneCode.GetString() is string code&&System.Text.RegularExpressions.Regex.IsMatch(code,"^[0-9]{8}$"))paired(ip,code);
                        if(version==2){string localKey=PairSession.NewToken();if(accepted){Session.Set(new(ip,r.GetProperty("peerPort").GetInt32(),r.GetProperty("peerToken").GetString()!,localKey,DateTimeOffset.MaxValue));paired(ip,"");}await WriteAsync(stream,new{paired=accepted,sessionToken=accepted?localKey:null,expiresIn=0},token);}else await WriteAsync(stream,new{paired=accepted},token);continue;
                    }
                    if(operation.ValueKind==JsonValueKind.String&&operation.GetString()=="ping"){await WriteAsync(stream,new{paired=true},token);continue;}
                    var files=r.GetProperty("files").EnumerateArray().Select(e=>(Name:e.GetProperty("name").GetString()??"",Size:e.GetProperty("size").GetInt64())).ToArray();
                    if(files.Length<1||files.Length>1000)throw new IOException("Quantidade inválida de arquivos.");long total=0;
                    foreach(var file in files){if(file.Name.Length<1||file.Name.Length>255||file.Name.Contains('/')||file.Name.Contains('\\')||file.Name.Contains('\0')||file.Size<0)throw new IOException("Metadados inválidos.");total=checked(total+file.Size);}
                    state("Pedido recebido. Aguardando sua aceitação.");
                    if(!await approve($"Receber de {ip}?\n\n"+string.Join("\n",files.Select(f=>$"{f.Name} — {f.Size:N0} bytes"))+"\n\nPasta: "+directory).WaitAsync(TimeSpan.FromMinutes(2),token)){await WriteAsync(stream,new{accepted=false},token);state("Pedido recusado. Receptor disponível.");continue;}
                    await WriteAsync(stream,new{accepted=true},token);long done=0;var buffer=new byte[65536];
                    foreach(var file in files){string name=string.Concat(file.Name.Select(c=>Path.GetInvalidFileNameChars().Contains(c)?'_':c));if(name.Length>180)name=name[..150]+Path.GetExtension(name)[..Math.Min(16,Path.GetExtension(name).Length)];string destination=Path.Combine(directory,Guid.NewGuid().ToString("N")+"_"+name);bool complete=false;
                        try{await using(var output=new FileStream(destination,FileMode.CreateNew,FileAccess.Write,FileShare.None,65536,true)){long left=file.Size;while(left>0){int n=await stream.ReadAsync(buffer.AsMemory(0,(int)Math.Min(buffer.Length,left)),token);if(n==0)throw new EndOfStreamException("Conexão interrompida.");await output.WriteAsync(buffer.AsMemory(0,n),token);left-=n;done+=n;progress(done,total,file.Name);}await output.FlushAsync(token);}complete=true;}
                        finally{if(!complete&&File.Exists(destination))File.Delete(destination);}
                        await WriteAsync(stream,new{ok=true},token);
                    }
                    state($"Transferência concluída • {files.Length} arquivo(s) salvo(s). Receptor disponível.");
                }catch(OperationCanceledException)when(ct.IsCancellationRequested){break;}catch(Exception e){state("Falha: "+e.Message+" Receptor disponível para tentar novamente.");}
            }
        }finally{listener.Stop();}
    }
    public static async Task<JsonDocument> ReadAsync(Stream stream,CancellationToken ct){using var bytes=new MemoryStream();var b=new byte[1];while(bytes.Length<1048576){if(await stream.ReadAsync(b,ct)==0)throw new EndOfStreamException();if(b[0]==10)return JsonDocument.Parse(bytes.ToArray());bytes.WriteByte(b[0]);}throw new IOException("Pedido grande demais.");}
    static Task WriteAsync(Stream stream,object value,CancellationToken ct)=>stream.WriteAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value)+"\n"),ct).AsTask();
}


