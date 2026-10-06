package com.droplocal;

import android.app.*;
import android.content.*;
import android.database.Cursor;
import android.net.Uri;
import android.os.*;
import android.provider.Settings;
import android.widget.Button;
import java.io.*;
import java.security.MessageDigest;
import java.util.concurrent.*;
import java.util.function.*;

final class AndroidUpdate {
    final Activity activity;final Button button;final SharedPreferences prefs;
    final BooleanSupplier busy;final Consumer<String> status;
    final Handler handler=new Handler(Looper.getMainLooper());
    final ExecutorService worker=Executors.newSingleThreadExecutor();
    boolean checking,verifying,resumed,closed,permissionPrompt;
    final Runnable poll=()->poll();
    AndroidUpdate(Activity activity,Button button,SharedPreferences prefs,BooleanSupplier busy,Consumer<String> status){
        this.activity=activity;this.button=button;this.prefs=prefs;this.busy=busy;this.status=status;button.setOnClickListener(v->check());
    }
    void ui(Runnable action){handler.post(()->{if(!closed&&!activity.isDestroyed())action.run();});}
    void resume(){resumed=true;handler.removeCallbacks(poll);poll();}
    void pause(){resumed=false;handler.removeCallbacks(poll);}
    void close(){closed=true;pause();worker.shutdownNow();}
    DownloadManager manager(){return (DownloadManager)activity.getSystemService(Context.DOWNLOAD_SERVICE);}
    long job(){return prefs.getLong("updateDownload",-1);}
    void clear(){prefs.edit().remove("updateDownload").remove("updateSha256").remove("updateSize").apply();}
    void check(){
        if(checking||verifying)return;
        if(busy.getAsBoolean()){status.accept("Aguarde terminar as transferências antes de atualizar.");return;}
        if(job()!=-1){poll();return;}
        checking=true;button.setEnabled(false);status.accept("Consultando a última release no GitHub…");
        worker.execute(()->{try{
            String installed=activity.getPackageManager().getPackageInfo(activity.getPackageName(),0).versionName;
            GitHubUpdate.Release release=GitHubUpdate.check(installed);
            ui(()->{if(release==null){status.accept("Drop Local já está na versão mais recente publicada.");return;}
                new AlertDialog.Builder(activity).setTitle("Atualizar Drop Local").setMessage("Versão "+release.version+" disponível. Baixar e abrir a atualização? Seus arquivos e a pasta de recebimento serão preservados.").setNegativeButton("Agora não",null).setPositiveButton("Atualizar",(d,w)->download(release)).show();});
        }catch(Exception e){ui(()->status.accept("Não foi possível consultar atualizações: "+e.getMessage()));}
        finally{ui(()->{checking=false;button.setEnabled(true);});}});
    }
    void download(GitHubUpdate.Release release){
        if(busy.getAsBoolean()){status.accept("Aguarde terminar a transferência e tente atualizar novamente.");return;}
        try{
            DownloadManager.Request request=new DownloadManager.Request(Uri.parse(release.url));
            request.setTitle("Drop Local "+release.version).setMimeType("application/vnd.android.package-archive").setNotificationVisibility(DownloadManager.Request.VISIBILITY_VISIBLE);
            request.setDestinationInExternalFilesDir(activity,Environment.DIRECTORY_DOWNLOADS,"DropLocal-"+System.currentTimeMillis()+".apk");
            long id=manager().enqueue(request);prefs.edit().putLong("updateDownload",id).putString("updateSha256",release.sha256).putLong("updateSize",release.size).apply();
            status.accept("Baixando atualização. Volte ao Drop Local para confirmar a instalação.");if(resumed)resume();
        }catch(Exception e){status.accept("Não foi possível baixar a atualização: "+e.getMessage());}
    }
    void poll(){
        handler.removeCallbacks(poll);if(closed||!resumed||verifying||permissionPrompt||job()==-1)return;
        long id=job();
        try(Cursor result=manager().query(new DownloadManager.Query().setFilterById(id))){
            if(result==null||!result.moveToFirst()){clear();status.accept("Download da atualização não encontrado. Clique em Atualizar novamente.");return;}
            int state=result.getInt(result.getColumnIndexOrThrow(DownloadManager.COLUMN_STATUS));
            if(state==DownloadManager.STATUS_FAILED){clear();manager().remove(id);status.accept("Download da atualização falhou. Confira a internet e tente novamente.");return;}
            if(state==DownloadManager.STATUS_SUCCESSFUL){if(!busy.getAsBoolean())verify(id);else handler.postDelayed(poll,2000);return;}
            handler.postDelayed(poll,1500);
        }catch(Exception e){clear();status.accept("Não foi possível acompanhar a atualização: "+e.getMessage());}
    }
    void verify(long id){
        verifying=true;button.setEnabled(false);status.accept("Verificando atualização…");
        String expected=prefs.getString("updateSha256","");long size=prefs.getLong("updateSize",-1);
        worker.execute(()->{try{
            MessageDigest hash=MessageDigest.getInstance("SHA-256");long total=0;
            try(InputStream input=new ParcelFileDescriptor.AutoCloseInputStream(manager().openDownloadedFile(id))){byte[] buffer=new byte[65536];int n;while((n=input.read(buffer))!=-1){total+=n;if(total>size)throw new IOException("Tamanho do APK inválido.");hash.update(buffer,0,n);}}
            StringBuilder digest=new StringBuilder();for(byte b:hash.digest())digest.append(String.format(java.util.Locale.ROOT,"%02x",b&255));
            if(total!=size||!digest.toString().equalsIgnoreCase(expected))throw new IOException("O APK não corresponde ao SHA-256 da release.");
            ui(()->{if(resumed&&!busy.getAsBoolean())install(id);});
        }catch(Exception e){ui(()->{clear();manager().remove(id);status.accept("Não foi possível verificar a atualização: "+e.getMessage());});}
        finally{ui(()->{verifying=false;button.setEnabled(true);if(resumed&&job()!=-1)handler.postDelayed(poll,2000);});}});
    }
    void install(long id){
        if(!activity.getPackageManager().canRequestPackageInstalls()){
            permissionPrompt=true;new AlertDialog.Builder(activity).setTitle("Permitir atualização").setMessage("O Android precisa autorizar o Drop Local a instalar a atualização. Ative a permissão na próxima tela e volte ao aplicativo.")
                .setNegativeButton("Agora não",(d,w)->clear()).setOnCancelListener(d->clear())
                .setOnDismissListener(d->{permissionPrompt=false;if(resumed&&job()!=-1)handler.postDelayed(poll,2000);})
                .setPositiveButton("Abrir configuração",(d,w)->{try{activity.startActivity(new Intent(Settings.ACTION_MANAGE_UNKNOWN_APP_SOURCES,Uri.parse("package:"+activity.getPackageName())));}catch(Exception e){clear();status.accept("Abra a permissão de instalar apps nas configurações do Android e tente novamente.");}}).show();return;
        }
        try{Uri uri=manager().getUriForDownloadedFile(id);if(uri==null)throw new IOException("APK não encontrado.");
            Intent intent=new Intent(Intent.ACTION_VIEW).setDataAndType(uri,"application/vnd.android.package-archive").addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
            intent.setClipData(ClipData.newRawUri("Atualização Drop Local",uri));activity.startActivity(intent);clear();status.accept("Confirme a atualização na tela do Android. Não é necessário desinstalar.");
        }catch(Exception e){clear();status.accept("Não foi possível abrir o instalador: "+e.getMessage());}
    }
}
