package com.droplocal;

public final class AndroidUpdateCheck {
    static void check(boolean value,String message){if(!value)throw new AssertionError(message);}
    public static void main(String[] args)throws Exception {
        check(GitHubUpdate.newer("v0.8.3","0.8.2"),"upgrade ignored");
        check(!GitHubUpdate.newer("v0.8.2","0.8.2"),"same version offered");
        check(!GitHubUpdate.newer("v0.8.1","0.8.2"),"downgrade offered");
        check(GitHubUpdate.newer("v0.10.0","0.9.0"),"lexical comparison");
        for(String invalid:new String[]{"v0.8.3-beta","latest","0.8","1.2.3.4","999999.1.1"}){boolean rejected=false;try{GitHubUpdate.version(invalid);}catch(java.io.IOException e){rejected=true;}check(rejected,"invalid version accepted");}
        check(GitHubUpdate.validUrl("https://github.com/JosephDavid2/DropLocal/releases/download/v0.8.3/DropLocal-Android.apk"),"valid URL rejected");
        for(String invalid:new String[]{"http://github.com/JosephDavid2/DropLocal/releases/download/a/a.apk","https://github.com.evil.test/JosephDavid2/DropLocal/releases/download/a/a.apk","https://github.com/other/repo/releases/download/a/a.apk","https://github.com:8443/JosephDavid2/DropLocal/releases/download/a/a.apk"})check(!GitHubUpdate.validUrl(invalid),"invalid URL accepted");
        System.out.println("PASS Android production update versions and repository URL validation.");
    }
}
