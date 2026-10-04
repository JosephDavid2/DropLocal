package com.droplocal;

import android.content.Context;
import android.net.wifi.WifiManager;
import org.json.JSONObject;
import java.net.*;
import java.nio.charset.StandardCharsets;
import java.util.*;

final class LocalDiscovery implements AutoCloseable {
    static final int PORT=45834;
    static final String GROUP="239.255.45.83";
    static final class Device {
        final String id,name,kind,ip,token;final int port;final boolean ready;final long seen;
        Device(String id,String name,String kind,String ip,int port,String token,boolean ready,long seen){this.id=id;this.name=name;this.kind=kind;this.ip=ip;this.port=port;this.token=token;this.ready=ready;this.seen=seen;}
        boolean sameDisplay(Device other){return id.equals(other.id)&&name.equals(other.name)&&kind.equals(other.kind)&&ip.equals(other.ip)&&port==other.port&&token.equals(other.token)&&ready==other.ready;}
    }
    interface Listener {void changed(List<Device> devices);void error(String message);}
    static boolean sameSnapshot(List<Device> first,List<Device> second){if(first.size()!=second.size())return false;for(int i=0;i<first.size();i++)if(!first.get(i).sameDisplay(second.get(i)))return false;return true;}
    interface Availability {boolean ready();String token();}
    final String id,name;final Listener listener;final Availability availability;final Context context;
    final Map<String,Device> devices=new HashMap<>();volatile boolean running;volatile MulticastSocket socket;WifiManager.MulticastLock lock;Thread thread;
    LocalDiscovery(Context context,String deviceId,String name,Availability availability,Listener listener){this.id=deviceId;this.context=context.getApplicationContext();this.name=name.length()>64?name.substring(0,64):name;this.availability=availability;this.listener=listener;}
    static Device parse(byte[] bytes,int length,InetAddress source,String ownId,long now){
        if(length>2048||!(source instanceof Inet4Address)||source.isAnyLocalAddress()||source.isMulticastAddress())return null;
        try{JSONObject r=new JSONObject(new String(bytes,0,length,StandardCharsets.UTF_8));if(!r.optString("service").equals("DropLocal")||r.optInt("version")!=3)return null;
            String id=r.getString("id"),name=r.getString("name"),kind=r.getString("kind"),token=r.getString("token");int port=r.getInt("port");
            if(id.equals(ownId)||!id.matches("[a-fA-F0-9]{32}")||name.isEmpty()||name.length()>64||name.chars().anyMatch(Character::isISOControl)||(!kind.equals("windows")&&!kind.equals("android"))||!PairSession.validToken(token)||port<1||port>65535)return null;
            return new Device(id,name,kind,source.getHostAddress(),port,token,r.getBoolean("ready"),now);
        }catch(Exception ignored){return null;}
    }
    synchronized void start(){if(running)return;running=true;thread=new Thread(this::run,"DropLocal discovery");thread.start();}
    void run(){
        try{
            WifiManager wifi=(WifiManager)context.getSystemService(Context.WIFI_SERVICE);if(wifi!=null){lock=wifi.createMulticastLock("DropLocal discovery");lock.setReferenceCounted(false);lock.acquire();}
            try(MulticastSocket udp=new MulticastSocket(null)){socket=udp;udp.setReuseAddress(true);udp.bind(new InetSocketAddress(PORT));udp.setTimeToLive(1);udp.setSoTimeout(500);
                InetAddress group=InetAddress.getByName(GROUP);ArrayList<NetworkInterface> locals=new ArrayList<>();Enumeration<NetworkInterface> interfaces=NetworkInterface.getNetworkInterfaces();
                while(interfaces.hasMoreElements()){NetworkInterface ni=interfaces.nextElement();if(!ni.isUp()||ni.isLoopback())continue;boolean ipv4=false;Enumeration<InetAddress> addresses=ni.getInetAddresses();while(addresses.hasMoreElements())if(addresses.nextElement() instanceof Inet4Address)ipv4=true;if(ipv4)try{udp.joinGroup(new InetSocketAddress(group,PORT),ni);locals.add(ni);}catch(Exception ignored){}}
                long next=0;byte[] buffer=new byte[2049];while(running){long now=android.os.SystemClock.elapsedRealtime();if(now>=next){JSONObject message=new JSONObject().put("service","DropLocal").put("version",3).put("id",id).put("name",name).put("kind","android").put("port",45832).put("token",availability.token()).put("ready",availability.ready());byte[] bytes=message.toString().getBytes(StandardCharsets.UTF_8);for(NetworkInterface ni:locals)try{udp.setNetworkInterface(ni);udp.send(new DatagramPacket(bytes,bytes.length,group,PORT));}catch(Exception ignored){}next=now+2000;devices.values().removeIf(d->now-d.seen>10000);publish();}
                    try{DatagramPacket packet=new DatagramPacket(buffer,buffer.length);udp.receive(packet);Device device=parse(buffer,packet.getLength(),packet.getAddress(),id,android.os.SystemClock.elapsedRealtime());if(device!=null){devices.put(device.id,device);publish();}}catch(SocketTimeoutException ignored){}
                }
            }
        }catch(Exception e){if(running)listener.error("Descoberta indisponível. Use IP / QR: "+e.getMessage());}finally{socket=null;if(lock!=null&&lock.isHeld())lock.release();running=false;}
    }
    void publish(){ArrayList<Device> snapshot=new ArrayList<>(devices.values());snapshot.sort(Comparator.comparing(d->d.name));listener.changed(snapshot);}
    @Override public synchronized void close(){running=false;if(socket!=null)socket.close();}
}
