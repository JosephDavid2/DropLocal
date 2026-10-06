package com.droplocal;

import java.util.*;

public final class AndroidFilePickCheck {
    static void check(boolean value,String message){if(!value)throw new AssertionError(message);}
    static LocalDiscovery.Device device(String id,String ip,String token,boolean ready,long seen){
        return new LocalDiscovery.Device(id,"Notebook","windows",ip,45833,token,ready,seen);
    }
    public static void main(String[] args) {
        LocalDiscovery.Device original=device("chosen","192.168.1.20","old",true,0);
        PairSession.Peer manual=new PairSession.Peer("192.168.1.30",45833,"send","receive");
        FilePickDestination pending=new FilePickDestination(original,manual);
        for(int time=12000;time<=120000;time+=2000)pending.refresh(Collections.emptyList());
        check(pending.device()==original,"destination lost after discovery expiry during long selection");
        pending.refresh(Collections.singletonList(device("other","192.168.1.40","other",true,120000)));
        check(pending.device()==original&&pending.peer==null,"nearby send switched to another device or manual peer");
        LocalDiscovery.Device latest=device("chosen","192.168.1.21","new",true,122000);
        pending.refresh(Collections.singletonList(latest));
        check(pending.device()==latest,"same recipient endpoint/token not refreshed");
        LocalDiscovery.Device unavailable=device("chosen",latest.ip,latest.token,false,124000);
        pending.refresh(Collections.singletonList(unavailable));
        check(!pending.device().ready,"explicit receiver unavailability ignored");
        FilePickDestination manualPick=new FilePickDestination(null,manual);
        manualPick.refresh(Collections.singletonList(latest));
        check(manualPick.device()==null&&manualPick.peer==manual,"manual pick switched to discovery");
        FilePickDestination next=new FilePickDestination(latest,null);
        check(next.device()==latest,"new operation reused prior selection");
        System.out.println("PASS file-picker destination: long expiry, unrelated peer, refreshed endpoint/token, unavailability, manual capture, new operation.");
    }
}
