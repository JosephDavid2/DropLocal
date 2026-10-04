package com.droplocal;
import java.util.*;

// Runs the production discovery snapshot gate on the JVM, not the Android UI.
public final class AndroidDiscoveryStateCheck {
    static void check(boolean value,String message){if(!value)throw new AssertionError(message);}
    public static void main(String[] args){
        LocalDiscovery.Device first=new LocalDiscovery.Device("id","Notebook","windows","192.168.1.20",45833,new String(new char[64]).replace('\0','a'),true,10);
        List<LocalDiscovery.Device> shown=Collections.singletonList(first);
        for(int i=0;i<30;i++)check(LocalDiscovery.sameSnapshot(shown,Collections.singletonList(new LocalDiscovery.Device(first.id,first.name,first.kind,first.ip,first.port,first.token,true,100+i))),"heartbeat triggers unnecessary UI refresh");
        check(!LocalDiscovery.sameSnapshot(shown,Collections.emptyList()),"expiry ignored");check(!LocalDiscovery.sameSnapshot(shown,Collections.singletonList(new LocalDiscovery.Device(first.id,first.name,first.kind,first.ip,first.port,first.token,false,100))),"availability change ignored");
        check(!LocalDiscovery.sameSnapshot(shown,Collections.singletonList(new LocalDiscovery.Device(first.id,first.name,first.kind,"192.168.1.21",first.port,"new key",true,100))),"changed endpoint ignored");
        System.out.println("PASS Android production snapshot gate: unchanged heartbeat suppressed; expiry, availability and endpoint updates retained.");
    }
}
