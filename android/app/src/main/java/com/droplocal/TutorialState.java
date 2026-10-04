package com.droplocal;

// No device/network side effects: demonstrations only advance this state.
final class TutorialState {
    static final String[] EVENTS={"connection","folder","start","pair","files-tab","select","send"};
    int step=-1;
    void begin(){step=0;}
    void finish(){step=-1;}
    boolean action(String event){if(step<0||!EVENTS[step].equals(event))return false;step++;return true;}
    boolean done(){return step==EVENTS.length;}
}
