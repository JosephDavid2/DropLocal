using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

internal sealed record UpdateRelease(Version Version,Uri Download,long Size,string Sha256);

internal static class GitHubUpdate
{
    internal const string Api="https://api.github.com/repos/JosephDavid2/DropLocal/releases/latest";
    internal static Version ParseVersion(string text)
    {
        if(!Regex.IsMatch(text,@"^v?\d{1,5}\.\d{1,5}\.\d{1,5}$"))throw new IOException("A release precisa usar uma versão estável como v0.8.3.");
        return Version.Parse(text.TrimStart('v'));
    }
    internal static UpdateRelease? Parse(string json,Version installed)
    {
        using var doc=JsonDocument.Parse(json);var release=doc.RootElement;
        if(release.GetProperty("draft").GetBoolean()||release.GetProperty("prerelease").GetBoolean())return null;
        var version=ParseVersion(release.GetProperty("tag_name").GetString()!);
        if(version<=new Version(installed.Major,installed.Minor,installed.Build))return null;
        foreach(var asset in release.GetProperty("assets").EnumerateArray())
        {
            if(asset.GetProperty("name").GetString()!="DropLocal-Setup.msi")continue;
            var url=new Uri(asset.GetProperty("browser_download_url").GetString()!);
            if(url.Scheme!="https"||url.Host!="github.com"||!url.IsDefaultPort||!url.AbsolutePath.StartsWith("/JosephDavid2/DropLocal/releases/download/",StringComparison.Ordinal)||url.Query.Length!=0||url.Fragment.Length!=0||url.UserInfo.Length!=0)throw new IOException("Endereço da atualização inválido.");
            long size=asset.GetProperty("size").GetInt64();string digest=asset.GetProperty("digest").GetString()??"";
            if(size<=0||size>500_000_000||!Regex.IsMatch(digest,@"^sha256:[a-fA-F0-9]{64}$"))throw new IOException("A release não oferece tamanho e SHA-256 válidos para o MSI.");
            return new(version,url,size,digest[7..]);
        }
        throw new IOException("A release nova ainda não contém DropLocal-Setup.msi.");
    }
    internal static HttpClient Client()
    {
        var client=new HttpClient{Timeout=TimeSpan.FromMinutes(10)};
        client.DefaultRequestHeaders.UserAgent.ParseAdd("DropLocal-Updater/0.8.3");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");return client;
    }
    internal static async Task<UpdateRelease?> Check(HttpClient client,Version installed,CancellationToken cancel)
    {
        using var deadline=CancellationTokenSource.CreateLinkedTokenSource(cancel);deadline.CancelAfter(TimeSpan.FromSeconds(20));
        return Parse(await client.GetStringAsync(Api,deadline.Token),installed);
    }
    internal static async Task Download(HttpClient client,UpdateRelease release,string destination,CancellationToken cancel)
    {
        using var deadline=CancellationTokenSource.CreateLinkedTokenSource(cancel);deadline.CancelAfter(TimeSpan.FromMinutes(10));cancel=deadline.Token;
        using var reply=await client.GetAsync(release.Download,HttpCompletionOption.ResponseHeadersRead,cancel);reply.EnsureSuccessStatusCode();
        await using var input=await reply.Content.ReadAsStreamAsync(cancel);
        await using var output=new FileStream(destination,FileMode.CreateNew,FileAccess.Write,FileShare.None);
        using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);byte[] buffer=new byte[65536];long total=0;int read;
        while((read=await input.ReadAsync(buffer,cancel))!=0){total+=read;if(total>release.Size)throw new IOException("Download maior que o arquivo publicado.");hash.AppendData(buffer,0,read);await output.WriteAsync(buffer.AsMemory(0,read),cancel);}
        if(total!=release.Size||!Convert.ToHexString(hash.GetHashAndReset()).Equals(release.Sha256,StringComparison.OrdinalIgnoreCase))throw new IOException("O download não corresponde ao SHA-256 da release. Tente novamente.");
    }
}
