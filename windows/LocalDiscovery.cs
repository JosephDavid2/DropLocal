using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;

public sealed record NearbyDevice(string Id,string Name,string Kind,IPAddress Address,int Port,string Token,bool Ready,DateTimeOffset Seen)
{
    public override string ToString()=>$"{(Kind=="android"?"Celular":"Computador")} · {Name}{(Ready?"":" · preparando recebimento")}";
}

// The announcement grants permission to ask, never permission to write files.
public sealed class LocalDiscovery : IDisposable
{
    public const int Port=45834;
    public static readonly IPAddress Group=IPAddress.Parse("239.255.45.83");
    readonly string id=Guid.NewGuid().ToString("N"),name,kind;readonly int receiverPort;
    readonly Func<string> token;readonly Func<bool> ready;readonly Dictionary<string,NearbyDevice> devices=new();
    CancellationTokenSource? lifetime;UdpClient? socket;readonly object gate=new();
    public event Action<IReadOnlyList<NearbyDevice>>? Changed;public event Action<string>? Error;
    public LocalDiscovery(string name,string kind,int receiverPort,Func<string> token,Func<bool> ready){this.name=name.Length>64?name[..64]:name;this.kind=kind;this.receiverPort=receiverPort;this.token=token;this.ready=ready;}
    public void Start(){if(lifetime!=null)return;lifetime=new();_ = Run(lifetime.Token);}
    public IReadOnlyList<NearbyDevice> Snapshot(){lock(gate)return devices.Values.OrderBy(d=>d.Name).ToArray();}
    public static NearbyDevice? Parse(ReadOnlySpan<byte> bytes,IPAddress address,string ownId,DateTimeOffset now)
    {
        if(bytes.Length>2048)return null;
        try{using var doc=JsonDocument.Parse(bytes.ToArray());var r=doc.RootElement;
            if(r.GetProperty("service").GetString()!="DropLocal"||r.GetProperty("version").GetInt32()!=3)return null;
            string id=r.GetProperty("id").GetString()??"",name=r.GetProperty("name").GetString()??"",kind=r.GetProperty("kind").GetString()??"",token=r.GetProperty("token").GetString()??"";int port=r.GetProperty("port").GetInt32();
            if(id==ownId||id.Length!=32||!id.All(Uri.IsHexDigit)||name.Length is <1 or >64||name.Any(char.IsControl)||kind is not("windows" or "android")||port is <1 or >65535||!PairSession.ValidToken(token)||address.AddressFamily!=AddressFamily.InterNetwork||address.Equals(IPAddress.Any)||address.GetAddressBytes()[0]>=224)return null;
            return new(id,name,kind,address,port,token,r.GetProperty("ready").GetBoolean(),now);
        }catch(Exception e)when(e is JsonException or InvalidOperationException or KeyNotFoundException or FormatException){return null;}
    }
    async Task Run(CancellationToken ct)
    {
        try{
            using var udp=new UdpClient(AddressFamily.InterNetwork);socket=udp;udp.ExclusiveAddressUse=false;udp.Client.SetSocketOption(SocketOptionLevel.Socket,SocketOptionName.ReuseAddress,true);udp.Client.Bind(new IPEndPoint(IPAddress.Any,Port));udp.Ttl=1;
            var locals=NetworkInterface.GetAllNetworkInterfaces().Where(n=>n.OperationalStatus==OperationalStatus.Up&&n.NetworkInterfaceType!=NetworkInterfaceType.Loopback).SelectMany(n=>n.GetIPProperties().UnicastAddresses).Select(a=>a.Address).Where(a=>a.AddressFamily==AddressFamily.InterNetwork).Distinct().ToArray();
            foreach(var local in locals){try{udp.JoinMulticastGroup(Group,local);}catch(SocketException){}}
            if(locals.Length==0)udp.JoinMulticastGroup(Group);
            var listening=Listen(udp,ct);
            try{while(!ct.IsCancellationRequested){var bytes=JsonSerializer.SerializeToUtf8Bytes(new{service="DropLocal",version=3,id,name,kind,port=receiverPort,token=token(),ready=ready()});
                if(locals.Length==0)await udp.SendAsync(bytes,new IPEndPoint(Group,Port),ct);else foreach(var local in locals){try{udp.Client.SetSocketOption(SocketOptionLevel.IP,SocketOptionName.MulticastInterface,local.GetAddressBytes());await udp.SendAsync(bytes,new IPEndPoint(Group,Port),ct);}catch(SocketException){}}
                lock(gate){foreach(var expired in devices.Where(d=>DateTimeOffset.UtcNow-d.Value.Seen>TimeSpan.FromSeconds(10)).Select(d=>d.Key).ToArray())devices.Remove(expired);}Changed?.Invoke(Snapshot());await Task.Delay(2000,ct);
            }}finally{udp.Close();try{await listening;}catch(OperationCanceledException){}catch(ObjectDisposedException){}catch(SocketException){}}
        }catch(OperationCanceledException){}catch(ObjectDisposedException){}catch(Exception e){if(!ct.IsCancellationRequested)Error?.Invoke("Descoberta indisponível: "+e.Message+". Use a conexão manual.");}finally{socket=null;}
    }
    async Task Listen(UdpClient udp,CancellationToken ct){while(!ct.IsCancellationRequested){var packet=await udp.ReceiveAsync(ct);var device=Parse(packet.Buffer,packet.RemoteEndPoint.Address,id,DateTimeOffset.UtcNow);if(device==null)continue;lock(gate)devices[device.Id]=device;Changed?.Invoke(Snapshot());}}
    public void Dispose(){lifetime?.Cancel();socket?.Close();lifetime?.Dispose();lifetime=null;}
}
