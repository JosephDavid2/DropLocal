package com.droplocal;
import java.io.*;
import java.net.*;
import java.nio.charset.StandardCharsets;
import java.util.regex.*;

// JVM interoperability fixture. It uses the Android production session store;
// the fixture's minimal JSON reader only handles the known test messages.
public final class JavaSessionPeer {
    static String line(InputStream in)throws Exception{ByteArrayOutputStream bytes=new ByteArrayOutputStream();for(int i=0;i<1048576;i++){int b=in.read();if(b<0)throw new EOFException();if(b==10)return bytes.toString("UTF-8");bytes.write(b);}throw new IOException("header limit");}
    static String field(String text,String name)throws Exception{Matcher m=Pattern.compile("\\\""+name+"\\\"\\s*:\\s*\\\"([^\\\"]*)\\\"").matcher(text);if(!m.find())throw new IOException("field "+name);return m.group(1);}
    static int number(String text,String name)throws Exception{Matcher m=Pattern.compile("\\\""+name+"\\\"\\s*:\\s*(\\d+)").matcher(text);if(!m.find())throw new IOException("number "+name);return Integer.parseInt(m.group(1));}
    static void write(OutputStream out,String json)throws Exception{out.write((json+"\n").getBytes(StandardCharsets.UTF_8));out.flush();}
    static void check(boolean condition,String message)throws Exception{if(!condition)throw new IOException(message);}
    public static void main(String[] args)throws Exception{
        PairSession store=new PairSession();try(ServerSocket listener=new ServerSocket(0,2,InetAddress.getLoopbackAddress())){listener.setSoTimeout(20000);System.out.println("PORT:"+listener.getLocalPort());System.out.flush();
            try(Socket socket=listener.accept()){socket.setSoTimeout(15000);String offer=line(socket.getInputStream());check(number(offer,"version")==2&&field(offer,"operation").equals("pair")&&field(offer,"token").equals("12345678"),"pair request");String local=PairSession.newToken();store.set(new PairSession.Peer(socket.getInetAddress().getHostAddress(),number(offer,"peerPort"),field(offer,"peerToken"),local));write(socket.getOutputStream(),"{\"paired\":true,\"sessionToken\":\""+local+"\",\"expiresIn\":0}");}
            PairSession.Peer peer=store.get();check(!store.accepts("127.0.0.2",peer.receiveToken),"peer binding");
            byte[] expected=new byte[190003];for(int i=0;i<expected.length;i++)expected[i]=(byte)(i%251);
            try(Socket socket=listener.accept()){socket.setSoTimeout(15000);InputStream in=socket.getInputStream();String offer=line(in);check(number(offer,"version")==2&&store.accepts(socket.getInetAddress().getHostAddress(),field(offer,"token")),"transfer authentication");check(offer.contains("interop.bin")&&offer.contains("empty.bin"),"metadata");write(socket.getOutputStream(),"{\"accepted\":true}");byte[] received=in.readNBytes(expected.length);check(java.util.Arrays.equals(received,expected),"Windows to Java bytes");write(socket.getOutputStream(),"{\"ok\":true}");write(socket.getOutputStream(),"{\"ok\":true}");}
            try(Socket socket=new Socket(peer.ip,peer.port)){socket.setSoTimeout(15000);OutputStream out=socket.getOutputStream();InputStream in=socket.getInputStream();write(out,"{\"version\":2,\"token\":\""+peer.sendToken+"\",\"files\":[{\"name\":\"java-ação.bin\",\"size\":"+expected.length+"},{\"name\":\"java-empty.bin\",\"size\":0}]}");check(line(in).contains("true"),"Windows consent");out.write(expected);out.flush();check(line(in).contains("true"),"binary ack");check(line(in).contains("true"),"empty ack");}
            store.clear();check(!store.accepts(peer.ip,peer.receiveToken)&&store.get()==null,"revocation");check(PairSession.newToken().length()==64,"key size");System.out.println("PASS Java↔Windows paired authentication, binary/Unicode/empty bytes and revocation");
        }
    }
}
