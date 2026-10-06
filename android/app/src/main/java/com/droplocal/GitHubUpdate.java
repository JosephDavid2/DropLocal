package com.droplocal;

import org.json.*;
import java.io.*;
import java.net.*;
import java.nio.charset.StandardCharsets;

final class GitHubUpdate {
    static final String API="https://api.github.com/repos/JosephDavid2/DropLocal/releases/latest";
    static final class Release {
        final String version,url,sha256;final long size;
        Release(String version,String url,String sha256,long size){this.version=version;this.url=url;this.sha256=sha256;this.size=size;}
    }
    static int[] version(String text)throws IOException {
        if(text==null||!text.matches("v?[0-9]{1,5}\\.[0-9]{1,5}\\.[0-9]{1,5}"))throw new IOException("A release precisa usar uma versão estável como v0.8.3.");
        String[] parts=text.replaceFirst("^v","").split("\\.");return new int[]{Integer.parseInt(parts[0]),Integer.parseInt(parts[1]),Integer.parseInt(parts[2])};
    }
    static boolean newer(String published,String installed)throws IOException {
        int[] a=version(published),b=version(installed);for(int i=0;i<3;i++){if(a[i]!=b[i])return a[i]>b[i];}return false;
    }
    static boolean validUrl(String text)throws MalformedURLException {
        URL url=new URL(text);return url.getProtocol().equals("https")&&url.getHost().equals("github.com")&&url.getPort()==-1&&url.getUserInfo()==null&&url.getQuery()==null&&url.getRef()==null&&url.getPath().startsWith("/JosephDavid2/DropLocal/releases/download/");
    }
    static Release parse(String json,String installed)throws Exception {
        JSONObject root=new JSONObject(json);if(root.getBoolean("draft")||root.getBoolean("prerelease"))return null;
        String tag=root.getString("tag_name");if(!newer(tag,installed))return null;
        JSONArray assets=root.getJSONArray("assets");for(int i=0;i<assets.length();i++){
            JSONObject asset=assets.getJSONObject(i);if(!asset.getString("name").equals("DropLocal-Android.apk"))continue;
            String url=asset.getString("browser_download_url"),digest=asset.optString("digest");long size=asset.getLong("size");
            if(!validUrl(url)||size<=0||size>200_000_000||!digest.matches("sha256:[a-fA-F0-9]{64}"))throw new IOException("A release não oferece endereço, tamanho e SHA-256 válidos para o APK.");
            return new Release(tag.replaceFirst("^v",""),url,digest.substring(7),size);
        }
        throw new IOException("A release nova ainda não contém DropLocal-Android.apk.");
    }
    static Release check(String installed)throws Exception {
        HttpURLConnection connection=(HttpURLConnection)new URL(API).openConnection();connection.setConnectTimeout(15000);connection.setReadTimeout(20000);connection.setRequestProperty("User-Agent","DropLocal-Updater/0.8.3");connection.setRequestProperty("Accept","application/vnd.github+json");
        try{if(connection.getResponseCode()!=200)throw new IOException("GitHub respondeu "+connection.getResponseCode()+". Tente novamente mais tarde.");
            try(InputStream input=connection.getInputStream();ByteArrayOutputStream bytes=new ByteArrayOutputStream()){
                byte[] buffer=new byte[8192];int count;while((count=input.read(buffer))!=-1){if(bytes.size()+count>1_000_000)throw new IOException("Resposta do GitHub muito grande.");bytes.write(buffer,0,count);}return parse(bytes.toString(StandardCharsets.UTF_8.name()),installed);
            }
        }finally{connection.disconnect();}
    }
}
