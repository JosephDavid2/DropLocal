using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

public static class LocalTransfer
{
    public static async Task SendAsync(IPAddress ip, string token, IReadOnlyList<string> paths,
        Action<long,long,string> progress, Action<string> state, CancellationToken ct, int port = 45832, int version = 1)
    {
        if (paths.Count == 0 || paths.Count > 1000) throw new IOException("Selecione de 1 a 1000 arquivos por envio.");
        var opened = new List<FileStream>();
        try
        {
            foreach (var path in paths) opened.Add(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, true));
            long total = opened.Sum(f => f.Length), done = 0;
            using var client = new TcpClient();
            state("Conectando…"); await client.ConnectAsync(ip, port, ct);
            using var stream = client.GetStream();
            var metadata = JsonSerializer.SerializeToUtf8Bytes(new { version, token, files = paths.Select((p,i) => new { name = Path.GetFileName(p), size = opened[i].Length }) });
            if (metadata.Length >= 1048576) throw new IOException("Pedido muito grande.");
            await stream.WriteAsync(metadata, ct); await stream.WriteAsync(new byte[] { 10 }, ct);
            state("Aguardando aceitação no aparelho…");
            using var reply = await ReadJson(stream, ct);
            if (!reply.RootElement.GetProperty("accepted").GetBoolean()) throw new IOException("Pedido recusado ou código incorreto.");
            var buffer = new byte[65536];
            for (int i = 0; i < opened.Count; i++)
            {
                long remaining = opened[i].Length;
                while (remaining > 0)
                {
                    int n = await opened[i].ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, remaining)), ct);
                    if (n == 0) throw new IOException("Arquivo mudou durante o envio.");
                    await stream.WriteAsync(buffer.AsMemory(0, n), ct); remaining -= n; done += n;
                    progress(done,total,Path.GetFileName(paths[i]));
                }
                using var ack = await ReadJson(stream, ct);
                if (!ack.RootElement.GetProperty("ok").GetBoolean()) throw new IOException("O aparelho não confirmou a gravação.");
            }
        }
        finally { foreach (var f in opened) f.Dispose(); }
    }
    static async Task<JsonDocument> ReadJson(Stream stream, CancellationToken ct)
    {
        using var data = new MemoryStream(); var one = new byte[1];
        while (data.Length < 65536) { if (await stream.ReadAsync(one, ct) == 0) throw new IOException("O aparelho desconectou."); if (one[0] == 10) return JsonDocument.Parse(data.ToArray()); data.WriteByte(one[0]); }
        throw new IOException("Resposta muito grande.");
    }
}

