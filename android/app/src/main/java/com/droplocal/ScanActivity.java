package com.droplocal;

import android.app.Activity;
import android.os.*;
import android.content.Intent;
import android.hardware.Camera;
import android.graphics.ImageFormat;
import android.view.*;
import android.widget.*;
import android.util.Log;
import java.util.*;
import java.util.concurrent.*;
import java.util.concurrent.atomic.AtomicBoolean;

@SuppressWarnings("deprecation")
public class ScanActivity extends Activity implements SurfaceHolder.Callback {
    Camera camera;
    SurfaceView surface;
    FrameLayout preview;
    TextView message;
    Button focus,light;
    volatile boolean active;
    long last,started,lastFrame,lastFeedback,lastFocus;
    int width,height,frames,generation;
    String focusMode;
    boolean focusing,torch;
    final Handler ui=new Handler(Looper.getMainLooper());
    final AtomicBoolean decoding=new AtomicBoolean();
    final ExecutorService worker=Executors.newSingleThreadExecutor();
    final Runnable health=new Runnable(){public void run(){if(!active)return;if(camera!=null&&SystemClock.elapsedRealtime()-lastFrame>3000)message.setText("A câmera abriu, mas não está entregando imagens. Toque em Reiniciar câmera.");ui.postDelayed(this,1500);}};

    @Override public void onCreate(Bundle state){
        super.onCreate(state);getWindow().addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
        LinearLayout layout=DarkUi.column(this);int pad=DarkUi.dp(this,16);layout.setPadding(pad,pad,pad,pad);
        layout.setOnApplyWindowInsetsListener((view,insets)->{view.setPadding(pad+insets.getSystemWindowInsetLeft(),pad+insets.getSystemWindowInsetTop(),pad+insets.getSystemWindowInsetRight(),pad+insets.getSystemWindowInsetBottom());return insets;});layout.setBackgroundColor(DarkUi.BG);
        layout.addView(DarkUi.text(this,"Drop Local · Escanear QR",18,DarkUi.TEXT,true));
        message=DarkUi.text(this,"Iniciando câmera…",14,DarkUi.MUTED,false);message.setLines(4);layout.addView(message);
        preview=new FrameLayout(this);surface=new SurfaceView(this);preview.addView(surface,new FrameLayout.LayoutParams(-1,-1,Gravity.CENTER));layout.addView(preview,new LinearLayout.LayoutParams(-1,0,1));
        preview.addOnLayoutChangeListener((v,l,t,r,b,ol,ot,or,ob)->fitPreview());surface.setOnClickListener(v->refocus());
        LinearLayout actions=new LinearLayout(this);focus=DarkUi.button(this,"Focar",false,false);light=DarkUi.button(this,"Luz",false,false);
        actions.addView(focus,new LinearLayout.LayoutParams(0,DarkUi.dp(this,44),1));actions.addView(light,new LinearLayout.LayoutParams(0,DarkUi.dp(this,44),1));layout.addView(actions);
        focus.setOnClickListener(v->refocus());light.setOnClickListener(v->toggleLight());
        Button retry=DarkUi.button(this,"Reiniciar câmera",false,false);retry.setOnClickListener(v->{release();open();});layout.addView(retry);
        Button cancel=DarkUi.button(this,"Voltar para IP e código",false,false);cancel.setOnClickListener(v->finish());layout.addView(cancel);
        setContentView(layout);surface.getHolder().addCallback(this);
    }
    @Override protected void onResume(){super.onResume();active=true;if(surface.getHolder().getSurface().isValid())open();ui.postDelayed(health,1500);}
    void open(){
        if(camera!=null||!active||!surface.getHolder().getSurface().isValid())return;
        try{
            int cameraId=0;for(int id=0;id<Camera.getNumberOfCameras();id++){Camera.CameraInfo item=new Camera.CameraInfo();Camera.getCameraInfo(id,item);if(item.facing==Camera.CameraInfo.CAMERA_FACING_BACK){cameraId=id;break;}}
            camera=Camera.open(cameraId);Camera.Parameters p=camera.getParameters();Camera.Size selected=null;
            // Enough pixels for small modules; prefer 1280-wide over the old 640-wide feed.
            for(Camera.Size size:p.getSupportedPreviewSizes()){if(size.width*size.height>1920*1080)continue;if(selected==null||Math.abs(size.width-1280)<Math.abs(selected.width-1280)||size.width==selected.width&&size.height>selected.height)selected=size;}
            if(selected!=null)p.setPreviewSize(selected.width,selected.height);
            if(!p.getSupportedPreviewFormats().contains(ImageFormat.NV21))throw new IllegalStateException("Câmera sem formato NV21 compatível");p.setPreviewFormat(ImageFormat.NV21);
            List<String> modes=p.getSupportedFocusModes();focusMode=null;
            if(modes!=null)for(String mode:new String[]{Camera.Parameters.FOCUS_MODE_CONTINUOUS_PICTURE,Camera.Parameters.FOCUS_MODE_CONTINUOUS_VIDEO,Camera.Parameters.FOCUS_MODE_AUTO})if(modes.contains(mode)){focusMode=mode;p.setFocusMode(mode);break;}
            camera.setParameters(p);Camera.Size actual=camera.getParameters().getPreviewSize();width=actual.width;height=actual.height;
            Camera.CameraInfo info=new Camera.CameraInfo();Camera.getCameraInfo(cameraId,info);int rotation=getWindowManager().getDefaultDisplay().getRotation()*90;
            int orientation=info.facing==Camera.CameraInfo.CAMERA_FACING_FRONT?(360-(info.orientation+rotation)%360)%360:(info.orientation-rotation+360)%360;
            camera.setDisplayOrientation(orientation);preview.setTag(orientation);fitPreview();camera.setPreviewDisplay(surface.getHolder());
            started=lastFrame=SystemClock.elapsedRealtime();last=lastFeedback=lastFocus=0;frames=0;torch=false;light.setText("Luz");
            List<String> flashes=p.getSupportedFlashModes();light.setEnabled(flashes!=null&&flashes.contains(Camera.Parameters.FLASH_MODE_TORCH));focus.setEnabled(modes!=null&&modes.contains(Camera.Parameters.FOCUS_MODE_AUTO));
            camera.setErrorCallback((error,c)->{if(camera!=c)return;Log.e("DropLocalQR","Camera error "+error);release();message.setText("A câmera interrompeu a captura (erro "+error+").\nToque em Reiniciar câmera ou use IP e código.");});
            camera.setPreviewCallback((data,c)->onFrame(data));camera.startPreview();message.setText("Câmera pronta. Mostre o QR completo do Windows.\nToque na imagem ou em Focar para ajustar a nitidez.");
            if(Camera.Parameters.FOCUS_MODE_AUTO.equals(focusMode))refocus();
        }catch(Exception e){Log.e("DropLocalQR","Camera startup",e);release();message.setText("Não foi possível iniciar a câmera: "+e.getClass().getSimpleName()+".\nReinicie a câmera ou use IP e código.");}
    }
    void fitPreview(){if(width==0||height==0||preview.getWidth()==0||preview.getHeight()==0)return;int orientation=preview.getTag() instanceof Integer?(Integer)preview.getTag():0;double ratio=(orientation%180==0?(double)width/height:(double)height/width);int pw=preview.getWidth(),ph=(int)(pw/ratio);if(ph>preview.getHeight()){ph=preview.getHeight();pw=(int)(ph*ratio);}FrameLayout.LayoutParams p=(FrameLayout.LayoutParams)surface.getLayoutParams();if(p.width!=pw||p.height!=ph){p.width=pw;p.height=ph;p.gravity=Gravity.CENTER;surface.setLayoutParams(p);}}
    void onFrame(byte[] data){
        long now=SystemClock.elapsedRealtime();lastFrame=now;if(!active||data==null||now-last<300||!decoding.compareAndSet(false,true))return;last=now;
        final int w=width,h=height,session=generation;final byte[] luminance=Arrays.copyOf(data,Math.min(data.length,w*h));
        try{worker.execute(()->{try{PairQrDecoder.ScanResult result=PairQrDecoder.scanDetailed(luminance,w,h);ui.post(()->{if(!active||session!=generation)return;frames++;if(result.value!=null){message.setText("QR reconhecido. Abrindo pareamento…");active=false;setResult(RESULT_OK,new Intent().putExtra("pair",result.value));finish();return;}
                    long time=SystemClock.elapsedRealtime();if(time-lastFeedback<1000)return;lastFeedback=time;
                    String status=result.sampled?"Padrões compatíveis com QR; tentando ler…":result.finders>=1?"Padrões detectados; buscando o QR completo…":"Buscando QR do Drop Local…";
                    String tip=time-started>6000?"Ainda sem leitura. Toque em Focar; evite reflexos e deixe o QR maior na imagem.":"Mantenha o QR inteiro visível. O reconhecimento é automático.";
                    message.setText(status+"\n"+tip+"\nImagens analisadas: "+frames+" · "+w+" × "+h);
                    if(Camera.Parameters.FOCUS_MODE_AUTO.equals(focusMode)&&time-lastFocus>2500&&!focusing)refocus();
                });}catch(Exception e){Log.e("DropLocalQR","Frame decoding",e);ui.post(()->{if(active&&session==generation)message.setText("Falha ao analisar imagem: "+e.getClass().getSimpleName()+".\nReinicie a câmera ou use IP e código.");});}finally{decoding.set(false);}});}catch(RejectedExecutionException e){decoding.set(false);}
    }
    void refocus(){Camera current=camera;if(current==null||focusing)return;try{List<String> modes=current.getParameters().getSupportedFocusModes();if(modes==null||!modes.contains(Camera.Parameters.FOCUS_MODE_AUTO))return;focusing=true;lastFocus=SystemClock.elapsedRealtime();current.cancelAutoFocus();Camera.Parameters p=current.getParameters();p.setFocusMode(Camera.Parameters.FOCUS_MODE_AUTO);current.setParameters(p);message.setText("Ajustando foco…");current.autoFocus((success,c)->{if(camera!=current)return;focusing=false;try{if(focusMode!=null){Camera.Parameters restored=c.getParameters();restored.setFocusMode(focusMode);c.setParameters(restored);}}catch(RuntimeException e){Log.w("DropLocalQR","Restore focus",e);}message.setText(success?"Foco ajustado. Buscando QR…":"A câmera ainda não confirmou foco. A leitura continua.");});ui.postDelayed(()->{if(camera==current)focusing=false;},2500);}catch(RuntimeException e){focusing=false;Log.w("DropLocalQR","Autofocus",e);message.setText("Não foi possível ajustar o foco; a leitura continua.");}}
    void toggleLight(){if(camera==null)return;try{Camera.Parameters p=camera.getParameters();p.setFlashMode(torch?Camera.Parameters.FLASH_MODE_OFF:Camera.Parameters.FLASH_MODE_TORCH);camera.setParameters(p);torch=!torch;light.setText(torch?"Apagar luz":"Luz");}catch(RuntimeException e){message.setText("Luz indisponível nesta câmera. A leitura continua.");}}
    void release(){generation++;focusing=false;if(camera!=null){Camera old=camera;camera=null;try{old.setPreviewCallback(null);old.stopPreview();}catch(RuntimeException e){Log.w("DropLocalQR","Stop preview",e);}finally{old.release();}}}
    public void surfaceCreated(SurfaceHolder holder){open();}
    public void surfaceChanged(SurfaceHolder holder,int format,int width,int height){fitPreview();}
    public void surfaceDestroyed(SurfaceHolder holder){release();}
    @Override protected void onPause(){active=false;ui.removeCallbacksAndMessages(null);release();super.onPause();}
    @Override protected void onDestroy(){worker.shutdownNow();super.onDestroy();}
}
