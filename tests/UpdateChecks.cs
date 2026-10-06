using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;

internal static class UpdateChecks
{
    sealed class Reply(byte[] bytes,HttpStatusCode code=HttpStatusCode.OK):HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancel){cancel.ThrowIfCancellationRequested();return Task.FromResult(new HttpResponseMessage(code){Content=new ByteArrayContent(bytes)});}
    }
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    internal static async Task Run()
    {
        var payload=new byte[]{0,1,2,255};var digest=Convert.ToHexString(SHA256.HashData(payload));
        string Json(string tag="v0.8.3",bool draft=false,bool prerelease=false,string name="DropLocal-Setup.msi",string? hash=null,string? url=null,long size=4)=>JsonSerializer.Serialize(new{tag_name=tag,draft,prerelease,assets=new[]{new{name,size,digest="sha256:"+(hash??digest),browser_download_url=url??"https://github.com/JosephDavid2/DropLocal/releases/download/v0.8.3/DropLocal-Setup.msi"}}});
        var installed=new Version(0,8,2,0);var release=GitHubUpdate.Parse(Json(),installed)!;
        Check(release.Version==new Version(0,8,3),"new version ignored");
        Check(GitHubUpdate.Parse(Json("v0.8.2"),installed)==null,"same version offered");
        Check(GitHubUpdate.Parse(Json("v0.8.1"),installed)==null,"downgrade offered");
        Check(GitHubUpdate.Parse(Json(draft:true),installed)==null&&GitHubUpdate.Parse(Json(prerelease:true),installed)==null,"unstable release offered");
        Check(GitHubUpdate.Parse(Json("v0.10.0"),installed)!=null,"version compared lexically");
        foreach(var invalid in new[]{Json("v0.8.3-beta"),Json(name:"source.zip"),Json(hash:"bad"),Json(size:0),Json(url:"https://github.com:8443/JosephDavid2/DropLocal/releases/download/a/a.msi"),Json(size:500_000_001),Json(url:"http://github.com/JosephDavid2/DropLocal/releases/download/a/a.msi"),Json(url:"https://github.com/other/repo/releases/download/a/a.msi"),Json(url:"https://github.com.evil.test/JosephDavid2/DropLocal/releases/download/a/a.msi")}){
            bool rejected=false;try{GitHubUpdate.Parse(invalid,installed);}catch(Exception){rejected=true;}Check(rejected,"invalid release accepted: "+invalid);
        }
        string root=Path.Combine(Path.GetTempPath(),"DropLocal-update-check-"+Guid.NewGuid());Directory.CreateDirectory(root);
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using(var client=new HttpClient(new Reply(payload))){string file=Path.Combine(root,"valid.msi");await GitHubUpdate.Download(client,release,file,deadline.Token);Check(File.ReadAllBytes(file).SequenceEqual(payload),"download bytes changed");}
        int count=0;
        foreach(var reply in new[]{new Reply(new byte[]{9,9,9,9}),new Reply(new byte[]{1}),new Reply(new byte[]{1,2,3,4,5}),new Reply(payload,HttpStatusCode.NotFound)}){
            using var client=new HttpClient(reply);bool rejected=false;try{await GitHubUpdate.Download(client,release,Path.Combine(root,"bad"+(count++)+".msi"),deadline.Token);}catch(Exception){rejected=true;}Check(rejected,"corrupt/truncated/oversized/HTTP failure accepted");
        }
        using(var cancelled=new CancellationTokenSource()){cancelled.Cancel();using var client=new HttpClient(new Reply(payload));bool rejected=false;try{await GitHubUpdate.Download(client,release,Path.Combine(root,"cancel.msi"),cancelled.Token);}catch(OperationCanceledException){rejected=true;}Check(rejected,"cancellation ignored");}
        Console.WriteLine("PASS updates: stable numeric versions; asset/URL/hash/size validation; exact bytes; corruption, truncation, excess, HTTP failure and cancellation rejected. Fixtures: "+root);
    }
    internal static async Task VerifyPublished(string directory)
    {
        Directory.CreateDirectory(directory);using var client=GitHubUpdate.Client();using var deadline=new CancellationTokenSource(TimeSpan.FromMinutes(3));
        var release=await GitHubUpdate.Check(client,new Version(0,0,0),deadline.Token)??throw new Exception("No stable release");
        await GitHubUpdate.Download(client,release,Path.Combine(directory,"published-verified.msi"),deadline.Token);
        var current=await GitHubUpdate.Check(client,new Version(0,8,3),deadline.Token);
        Check(current==null,"published version unexpectedly newer than local 0.8.3");
        Console.WriteLine("PASS live GitHub: latest stable "+release.Version+", exact MSI size/SHA-256 through download redirects; local 0.8.3 needs no downgrade. No installer launched.");
    }
}
