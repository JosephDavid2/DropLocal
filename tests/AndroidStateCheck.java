package com.droplocal;

public final class AndroidStateCheck {
    static void check(boolean value,String message){if(!value)throw new AssertionError(message);}
    public static void main(String[] args)throws Exception{
        PairSession session=new PairSession();String send=PairSession.newToken(),receive=PairSession.newToken();session.set(new PairSession.Peer("192.168.1.20",45833,send,receive));PairSession.Peer retained=session.get();check(session.get()==retained&&session.accepts("192.168.1.20",receive),"in-memory session retention");check(!session.accepts("192.168.1.21",receive)&&!session.accepts("192.168.1.20",send),"IP/direction binding");int epoch=session.epoch();session.clear();boolean rejected=false;try{session.setIfCurrent(epoch,retained);}catch(java.io.IOException expected){rejected=true;}check(rejected&&session.get()==null,"closed session revived by delayed pair");
        rejected=false;try{session.set(new PairSession.Peer("192.168.1.20",0,send,receive));}catch(java.io.IOException expected){rejected=true;}check(rejected,"invalid port");System.out.println("PASS Android production session retention, IP/direction binding and delayed-pair revocation");
    }
}
