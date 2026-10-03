package com.droplocal;
import android.content.Context;
import android.net.Uri;
import android.provider.OpenableColumns;
import android.database.Cursor;
import org.json.*;
import java.io.*;
import java.net.*;
import java.nio.charset.StandardCharsets;
import java.util.*;

final class PhoneSender {
    interface Progress {void update(long done,long total,String name);}
    interface State {void show(String text);}
    volatile boolean cancelled;volatile Socket current;
    void cancel(){cancelled=true;try{if(current!=null)current.close();}catch(IOException ignored){}}
    static String ipv4(String text) throws IOException {if(!text.matches("[0-9]{1,3}(\\.[0-9]{1,3}){3}"))throw new IOException("Informe um IPv4 válido do computador.");for(String p:text.split("\\."))if(Integer.parseInt(p)>255)throw new IOException("IP inválido.");return text;}
    void pair(String ip,String code,String androidToken) throws Exception {ipv4(ip);try(Socket socket=new Socket()){socket.connect(new InetSocketAddress(ip,45833),10000);socket.setSoTimeout(130000);JSONObject offer=new JSONObject().put("version",1).put("operation","pair").put("token",code);if(androidToken!=null)offer.put("androidToken",androidToken);write(socket.getOutputStream(),offer);JSONObject reply=read(socket.getInputStream());if(!reply.optBoolean("paired"))throw new IOException("Pareamento recusado. Confira o código no Windows.");}}
    void send(Context context,List<Uri> uris,String ip,String code,Progress progress,State state) throws Exception {
        ipv4(ip);if(!code.matches("[0-9]{8}"))throw new IOException("Informe o código de oito dígitos do Windows.");if(uris.isEmpty()||uris.size()>1000)throw new IOException("Selecione de 1 a 1000 arquivos.");
        File batch=new File(context.getCacheDir(),"outgoing-"+UUID.randomUUID());if(!batch.mkdir())throw new IOException("Não foi possível preparar arquivos.");ArrayList<File> copies=new ArrayList<>();ArrayList<String> names=new ArrayList<>();long total=0;
        try{byte[] buffer=new byte[65536];state.show("Preparando arquivos para envio…");for(Uri uri:uris){String name="arquivo";try(Cursor c=context.getContentResolver().query(uri,new String[]{OpenableColumns.DISPLAY_NAME},null,null,null)){if(c!=null&&c.moveToFirst())name=c.getString(0);}if(name==null||name.isEmpty())name="arquivo";name=name.replace('/','_').replace('\\','_');if(name.length()>200)name=name.substring(0,180);File temp=new File(batch,"file-"+copies.size());copies.add(temp);names.add(name);try(InputStream input=context.getContentResolver().openInputStream(uri);OutputStream output=new FileOutputStream(temp)){if(input==null)throw new IOException("Não foi possível abrir "+name);int n;while((n=input.read(buffer))!=-1){if(cancelled)throw new IOException("Envio cancelado.");output.write(buffer,0,n);}}total=Math.addExact(total,temp.length());}
            if(cancelled)throw new IOException("Envio cancelado.");try(Socket socket=new Socket()){current=socket;socket.connect(new InetSocketAddress(ip,45833),10000);socket.setSoTimeout(130000);OutputStream out=socket.getOutputStream();InputStream in=new BufferedInputStream(socket.getInputStream());JSONArray files=new JSONArray();for(int i=0;i<copies.size();i++)files.put(new JSONObject().put("name",names.get(i)).put("size",copies.get(i).length()));write(out,new JSONObject().put("version",1).put("token",code).put("files",files));state.show("Aguardando aceitação no Windows…");if(!read(in).optBoolean("accepted"))throw new IOException("Pedido recusado ou código incorreto.");long done=0,last=0;for(int i=0;i<copies.size();i++){try(InputStream input=new FileInputStream(copies.get(i))){int n;while((n=input.read(buffer))!=-1){if(cancelled)throw new IOException("Envio cancelado.");out.write(buffer,0,n);done+=n;long now=System.currentTimeMillis();if(now-last>100||done==total){last=now;progress.update(done,total,names.get(i));}}}out.flush();if(!read(in).optBoolean("ok"))throw new IOException("Windows não confirmou a gravação.");}state.show("Transferência concluída • "+copies.size()+" arquivo(s) salvo(s) no computador.");}
        }finally{current=null;for(File file:copies)file.delete();batch.delete();}
    }
    static void write(OutputStream out,JSONObject value)throws IOException{byte[] bytes=(value.toString()+"\n").getBytes(StandardCharsets.UTF_8);if(bytes.length>1048576)throw new IOException("Pedido grande demais.");out.write(bytes);out.flush();}
    static JSONObject read(InputStream in)throws Exception{ByteArrayOutputStream data=new ByteArrayOutputStream();for(int i=0;i<1048576;i++){int b=in.read();if(b<0)throw new EOFException("Computador desconectou.");if(b==10)return new JSONObject(data.toString("UTF-8"));data.write(b);}throw new IOException("Resposta grande demais.");}
}
