using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;

public sealed record PairedPeer(IPAddress Address,int Port,string SendToken,string ReceiveToken,DateTimeOffset Expires);
public sealed class PairSession
{
    readonly object gate=new(); PairedPeer? peer;int generation;
    public static string NewToken()=>Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
    public static bool ValidToken(string? value)=>value is not null&&System.Text.RegularExpressions.Regex.IsMatch(value,"^[a-f0-9]{64}$");
    public PairedPeer? Peer { get { lock(gate) { if(peer?.Expires<=DateTimeOffset.UtcNow)peer=null;return peer; } } }
    public void Clear(){lock(gate){generation++;peer=null;}}
    public void Set(PairedPeer value){if(!ValidToken(value.SendToken)||!ValidToken(value.ReceiveToken)||value.Port<1||value.Port>65535)throw new IOException("Sessão inválida.");lock(gate)peer=value;}
    public bool Accepts(IPAddress ip,string? token)=>Peer is {} p&&p.Address.Equals(ip)&&p.ReceiveToken==token;
    public async Task ConnectAsync(IPAddress ip,int port,string code,int localPort,CancellationToken ct)
    {
        int epoch;lock(gate)epoch=generation;string localToken=NewToken();using var client=new TcpClient();await client.ConnectAsync(ip,port,ct);using var stream=client.GetStream();
        await stream.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{version=2,operation="pair",token=code,peerPort=localPort,peerToken=localToken}).Concat(new byte[]{10}).ToArray(),ct);
        using var reply=await DesktopReceiver.ReadAsync(stream,ct);var r=reply.RootElement;
        if(!r.TryGetProperty("paired",out var paired)||!paired.GetBoolean()||!r.TryGetProperty("sessionToken",out var key)||!ValidToken(key.GetString()))throw new IOException("Pareamento recusado ou versão antiga. Atualize os dois aparelhos.");
        lock(gate){if(epoch!=generation)throw new OperationCanceledException("Sessão encerrada durante o pareamento.");Set(new(ip,port,key.GetString()!,localToken,DateTimeOffset.MaxValue));}
    }
}


