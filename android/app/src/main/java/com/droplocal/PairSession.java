package com.droplocal;
import java.security.SecureRandom;
import java.io.IOException;

final class PairSession {
    static final class Peer {final String ip,sendToken,receiveToken;final int port;Peer(String ip,int port,String send,String receive){this.ip=ip;this.port=port;sendToken=send;receiveToken=receive;}}
    private Peer peer;private int generation;
    static String newToken(){byte[] bytes=new byte[32];new SecureRandom().nextBytes(bytes);StringBuilder text=new StringBuilder();for(byte b:bytes)text.append(String.format(java.util.Locale.US,"%02x",b&255));return text.toString();}
    static boolean validToken(String token){return token!=null&&token.matches("[a-f0-9]{64}");}
    synchronized Peer get(){return peer;}
    synchronized void clear(){generation++;peer=null;}
    synchronized int epoch(){return generation;}
    synchronized void setIfCurrent(int epoch,Peer value)throws IOException{if(epoch!=generation)throw new IOException("Sessão encerrada durante o pareamento");set(value);}
    synchronized void set(Peer value)throws IOException{if(value.port<1||value.port>65535||!validToken(value.sendToken)||!validToken(value.receiveToken))throw new IOException("Sessão inválida");peer=value;}
    synchronized boolean accepts(String ip,String token){return peer!=null&&peer.ip.equals(ip)&&peer.receiveToken.equals(token);}
}

