package com.droplocal;

import android.app.*;
import android.os.*;
import android.content.*;
import android.net.Uri;
import android.provider.DocumentsContract;
import android.view.WindowManager;
import android.view.View;
import android.view.Gravity;
import android.graphics.Typeface;
import android.content.res.ColorStateList;
import android.graphics.drawable.ColorDrawable;
import android.database.Cursor;
import android.widget.*;
import org.json.*;
import java.io.*;
import java.net.*;
import java.nio.charset.StandardCharsets;
import java.security.SecureRandom;
import java.util.*;
import java.util.concurrent.*;

public class MainActivity extends Activity {
    private TextView status, receiverState, folderView;
    private ProgressBar progress;
    private Button choose;
    private Uri folder;
    private EditText pcIp, pcCode;
    private Button cancelSend, openFiles, openFolder;
    private LinearLayout completionBox;
    
    
    
    private final ArrayList<Uri> receivedUris = new ArrayList<>();
    private final ArrayList<String> receivedNames = new ArrayList<>();
    private final ArrayList<Long> receivedSizes = new ArrayList<>();
    private Uri receivedFolder;
    private boolean openingExternal;
    private volatile PhoneSender sender;
    private final ExecutorService outbound = Executors.newSingleThreadExecutor();
    private volatile boolean running;
    private volatile ServerSocket server;
    private volatile Socket current;
    private String code;
    private final PairSession pairing=new PairSession(); private TextView sessionView; private Button pair,scan; private volatile int serverGeneration; private boolean resumeReception;
    private AlertDialog requestDialog;
    private final String discoveryId=UUID.randomUUID().toString().replace("-","");private LocalDiscovery discovery;private LocalDiscovery.Device selectedDevice;private LinearLayout nearbyList;private TextView nearbyHint;private volatile String discoveryToken=PairSession.newToken();private final ArrayList<LocalDiscovery.Device> nearbyDevices=new ArrayList<>();
    private final ExecutorService worker = Executors.newSingleThreadExecutor();

    @Override public void onCreate(Bundle saved) {
        super.onCreate(saved);getWindow().addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);getWindow().setStatusBarColor(DarkUi.BG);getWindow().setNavigationBarColor(DarkUi.BG);
        LinearLayout shell=DarkUi.column(this);shell.setBackgroundColor(DarkUi.BG);shell.setPadding(DarkUi.dp(this,18),DarkUi.dp(this,12),DarkUi.dp(this,18),DarkUi.dp(this,12));
        LinearLayout heading=DarkUi.column(this);heading.addView(DarkUi.text(this,"Drop Local",24,DarkUi.TEXT,true));heading.addView(DarkUi.text(this,"Seus arquivos, pela sua rede · 0.8.1",12,DarkUi.MUTED,false));heading.addView(DarkUi.text(this,"by Joseph David",11,DarkUi.MUTED,false));shell.addView(heading);sessionView=DarkUi.text(this,"Toque em um aparelho para enviar arquivos.",12,DarkUi.ACCENT,true);shell.addView(sessionView);DarkUi.gap(this,shell,8);
        ScrollView scroll=new ScrollView(this);LinearLayout body=DarkUi.column(this);LinearLayout devices=DarkUi.card(this);devices.addView(DarkUi.text(this,"Aparelhos próximos",18,DarkUi.TEXT,true));nearbyHint=DarkUi.text(this,"Abra Drop Local no outro aparelho, na mesma rede.",12,DarkUi.MUTED,false);devices.addView(nearbyHint);nearbyList=DarkUi.column(this);devices.addView(nearbyList);body.addView(devices);scroll.addView(body);shell.addView(scroll,new LinearLayout.LayoutParams(-1,0,1));
        LinearLayout receiving=DarkUi.card(this);receiverState=DarkUi.text(this,"Escolha uma pasta para receber arquivos.",13,DarkUi.MUTED,false);receiving.addView(receiverState);folderView=DarkUi.text(this,"Uma vez, para escolher onde seus arquivos serão salvos.",12,DarkUi.MUTED,false);receiving.addView(folderView);choose=DarkUi.button(this,"Escolher pasta para receber",false,false);receiving.addView(choose);body.addView(receiving);
        LinearLayout activity=DarkUi.card(this);status=DarkUi.text(this,"Escolha um aparelho. Quem recebe só precisa aceitar.",13,DarkUi.MUTED,false);activity.addView(status);progress=new ProgressBar(this,null,android.R.attr.progressBarStyleHorizontal);progress.setMax(100);progress.setProgressTintList(ColorStateList.valueOf(DarkUi.ACCENT));progress.setProgressBackgroundTintList(ColorStateList.valueOf(DarkUi.FIELD));progress.setVisibility(View.GONE);activity.addView(progress,new LinearLayout.LayoutParams(-1,DarkUi.dp(this,10)));cancelSend=DarkUi.button(this,"Cancelar envio",false,true);cancelSend.setEnabled(false);activity.addView(cancelSend);cancelSend.setOnClickListener(v->{if(sender!=null)sender.cancel();});
        completionBox=DarkUi.column(this);completionBox.setVisibility(View.GONE);openFiles=DarkUi.button(this,"Abrir arquivo",true,false);openFolder=DarkUi.button(this,"Abrir pasta / local",false,false);completionBox.addView(openFiles);completionBox.addView(openFolder);activity.addView(completionBox);body.addView(activity);openFiles.setOnClickListener(v->openReceived());openFolder.setOnClickListener(v->openReceivedFolder());
        Button settings=DarkUi.button(this,"Conexão manual",false,false);body.addView(settings);settings.setOnClickListener(v->showManualSettings());
        pair=DarkUi.button(this,"Conectar manualmente",false,false);scan=DarkUi.button(this,"Escanear QR do Windows",false,false);pcIp=input("IP do computador");pcCode=input("Código do computador");pcCode.setInputType(android.text.InputType.TYPE_CLASS_NUMBER);pair.setOnClickListener(v->{if(pairing.get()!=null){pairing.clear();refreshPairing();showStatus("Conexão manual encerrada.");}else pairWindows(false);});scan.setOnClickListener(v->scanQr());
        setContentView(shell);choose.setOnClickListener(v->chooseReceiveFolder());String old=getPreferences(MODE_PRIVATE).getString("receiveFolder",null);if(old!=null)try{Uri uri=Uri.parse(old);boolean granted=getContentResolver().getPersistedUriPermissions().stream().anyMatch(p->p.getUri().equals(uri)&&p.isWritePermission());if(granted){folder=uri;folderView.setText("Salvar em: "+destinationName());choose.setText("Alterar pasta de recebimento");}}catch(Exception ignored){}if(folder!=null)startServer();
    }
    private void chooseReceiveFolder(){if(current!=null){Toast.makeText(this,"Aguarde terminar o recebimento.",Toast.LENGTH_SHORT).show();return;}resumeReception=false;stopServer();Intent pick=new Intent(Intent.ACTION_OPEN_DOCUMENT_TREE);pick.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION|Intent.FLAG_GRANT_WRITE_URI_PERMISSION|Intent.FLAG_GRANT_PERSISTABLE_URI_PERMISSION);openingExternal=true;startActivityForResult(pick,1);}
    private void showManualSettings(){LinearLayout content=DarkUi.card(this);content.addView(DarkUi.text(this,"Use somente se o aparelho não aparecer na lista.",12,DarkUi.MUTED,false));content.addView(DarkUi.text(this,"Meu IP: "+addresses()+"\nMeu código: "+(code==null?"escolha sua pasta primeiro":code),13,DarkUi.TEXT,false));for(View view:new View[]{pcIp,pcCode,pair,scan}){if(view.getParent() instanceof android.view.ViewGroup)((android.view.ViewGroup)view.getParent()).removeView(view);content.addView(view);}AlertDialog dialog=new AlertDialog.Builder(this).setTitle("Conexão manual").setView(content).setNegativeButton("Fechar",null).create();Button manualSend=DarkUi.button(this,"Enviar por conexão manual",false,false);content.addView(manualSend);manualSend.setOnClickListener(v->{if(pairing.get()==null){Toast.makeText(this,"Conecte manualmente primeiro.",Toast.LENGTH_SHORT).show();return;}selectedDevice=null;dialog.dismiss();pickFiles();});dialog.show();}
    private void pickFiles(){Intent pick=new Intent(Intent.ACTION_OPEN_DOCUMENT);pick.setType("*/*");pick.addCategory(Intent.CATEGORY_OPENABLE);pick.putExtra(Intent.EXTRA_ALLOW_MULTIPLE,true);pick.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);openingExternal=true;startActivityForResult(pick,2);}
    private void startDiscovery(){if(discovery!=null)return;discovery=new LocalDiscovery(this,discoveryId,Build.MODEL,new LocalDiscovery.Availability(){public boolean ready(){return running&&server!=null;}public String token(){return discoveryToken;}},new LocalDiscovery.Listener(){public void changed(List<LocalDiscovery.Device> devices){runOnUiThread(()->renderNearby(devices));}public void error(String message){runOnUiThread(()->nearbyHint.setText(message));}});discovery.start();}
    private void stopDiscovery(){if(discovery!=null){discovery.close();discovery=null;}}
    private void renderNearby(List<LocalDiscovery.Device> devices){
        if(isFinishing()||LocalDiscovery.sameSnapshot(nearbyDevices,devices))return;String chosen=selectedDevice==null?null:selectedDevice.id;nearbyDevices.clear();nearbyDevices.addAll(devices);selectedDevice=null;nearbyList.removeAllViews();
        for(LocalDiscovery.Device device:devices){if(device.id.equals(chosen))selectedDevice=device;Button button=DarkUi.button(this,(device.kind.equals("android")?"Celular":"Computador")+" · "+device.name+(device.ready?"":" · preparando recebimento"),false,false);button.setCompoundDrawablesWithIntrinsicBounds(new DeviceIcon(this,device.kind.equals("android")),null,null,null);button.setCompoundDrawablePadding(DarkUi.dp(this,10));button.setOnClickListener(v->{if(!device.ready){showStatus("O destinatário precisa escolher sua pasta de recebimento.");return;}selectedDevice=device;refreshPairing();if(sender==null&&current==null)pickFiles();});nearbyList.addView(button);DarkUi.gap(this,nearbyList,6);}
        nearbyHint.setText(devices.isEmpty()?"Abra Drop Local no outro aparelho. Se não aparecer, use IP / QR.":"Toque no destinatário para escolher arquivos. Ele aceita cada envio.");refreshPairing();
    }
    @Override protected void onActivityResult(int request, int result, Intent data) {
        super.onActivityResult(request,result,data);
        if (request == 1 && result == RESULT_OK && data != null && data.getData() != null) {
            folder = data.getData();try{int grants=0;if((data.getFlags()&Intent.FLAG_GRANT_READ_URI_PERMISSION)!=0)grants|=Intent.FLAG_GRANT_READ_URI_PERMISSION;if((data.getFlags()&Intent.FLAG_GRANT_WRITE_URI_PERMISSION)!=0)grants|=Intent.FLAG_GRANT_WRITE_URI_PERMISSION;getContentResolver().takePersistableUriPermission(folder,grants);getPreferences(MODE_PRIVATE).edit().putString("receiveFolder",folder.toString()).apply();}catch(Exception ignored){}  folderView.setText("Salvar em: "+destinationName());choose.setText("Alterar pasta de recebimento"); status.setText("Pasta pronta.");if(!running)startServer();
        }
        if(request==1&&folder!=null&&!running)startServer();
        if(request==2 && result==RESULT_OK && data!=null) { ArrayList<Uri> files=new ArrayList<>(); if(data.getClipData()!=null) { for(int index=0;index<data.getClipData().getItemCount();index++)files.add(data.getClipData().getItemAt(index).getUri()); } else if(data.getData()!=null)files.add(data.getData()); if(!files.isEmpty()){sendPicked(files);} }
        if(request==3 && result==RESULT_OK && data!=null) { try { Uri pair=Uri.parse(data.getStringExtra("pair")); String ip=PhoneSender.ipv4(pair.getQueryParameter("ip")); String verification=pair.getQueryParameter("token"); if(!"droplocal".equals(pair.getScheme()) || !"pair".equals(pair.getHost()) || !"45833".equals(pair.getQueryParameter("port")) || verification==null || !verification.matches("[0-9]{8}")) throw new IOException("QR inválido."); pcIp.setText(ip); pcCode.setText(verification); pairWindows(false); } catch(Exception e) { showStatus("Não foi possível parear: "+e.getMessage()); } }
    }
    private EditText input(String hint) {
        EditText view = new EditText(this); view.setHint(hint); view.setTextColor(DarkUi.TEXT); view.setHintTextColor(DarkUi.MUTED); view.setTextSize(15); view.setSingleLine(true); view.setBackground(DarkUi.shape(this,DarkUi.FIELD,DarkUi.BORDER,9)); view.setPadding(DarkUi.dp(this,12),DarkUi.dp(this,12),DarkUi.dp(this,12),DarkUi.dp(this,12)); LinearLayout.LayoutParams p = new LinearLayout.LayoutParams(-1,-2); p.topMargin=DarkUi.dp(this,10); p.bottomMargin=DarkUi.dp(this,8); view.setLayoutParams(p); return view;
    }
    private void scanQr() {
        if(android.os.Build.VERSION.SDK_INT >= 23 && checkSelfPermission(android.Manifest.permission.CAMERA) != android.content.pm.PackageManager.PERMISSION_GRANTED) { requestPermissions(new String[]{android.Manifest.permission.CAMERA},4); return; }
        openingExternal=true; startActivityForResult(new Intent(this,ScanActivity.class),3);
    }
    @Override public void onRequestPermissionsResult(int request,String[] permissions,int[] grants) { super.onRequestPermissionsResult(request,permissions,grants); if(request==4 && grants.length>0 && grants[0]==android.content.pm.PackageManager.PERMISSION_GRANTED) scanQr(); else showStatus("A câmera é opcional. Use IP e código manualmente."); }
    private void pairWindows(boolean quiet) {
        String ip=pcIp.getText().toString().trim(), verification=pcCode.getText().toString().trim();
        if(!running||server==null||folder==null){showStatus("Escolha a pasta de recebimento antes de conectar.");return;}
        if(ip.isEmpty()||!verification.matches("[0-9]{8}")){showStatus("Informe IP e código de oito dígitos, ou leia o QR.");return;}
        pair.setEnabled(false);outbound.execute(()->{try{new PhoneSender().pair(ip,verification,45832,pairing);runOnUiThread(()->{refreshPairing();});showStatus("Pareado. Ambos podem enviar e receber sem outro código.");}catch(Exception e){showStatus("Pareamento: "+e.getMessage());}finally{runOnUiThread(()->{pair.setEnabled(true);});}});
    }
    private void refreshPairing(){PairSession.Peer peer=pairing.get();sessionView.setText(selectedDevice!=null?"Destino: "+selectedDevice.name+" · cada envio exige aceitação":peer==null?"Escolha um aparelho · cada envio exige aceitação":"Pareado com "+peer.ip+" · enviar e receber");pair.setText(peer==null?"Parear":"Desconectar");}
    private void sendPicked(ArrayList<Uri> files) {
        if(current != null || sender != null) { showStatus("Aguarde a transferência atual antes de enviar."); return; }
        final PairSession.Peer peer=pairing.get();final LocalDiscovery.Device target=selectedDevice;if(target==null&&peer==null){showStatus("Escolha um aparelho antes de enviar.");return;}if(target!=null&&!target.ready){showStatus("O destinatário ainda precisa escolher a pasta de recebimento.");return;}
        PhoneSender task=new PhoneSender(); sender=task;  cancelSend.setEnabled(true); completionBox.setVisibility(View.GONE); progress.setVisibility(View.GONE);
        outbound.execute(() -> { try { PhoneSender.Progress report=(done,total,name) -> runOnUiThread(() -> { progress.setVisibility(View.VISIBLE); int percent=total==0?100:(int)(done*100.0/total); progress.setProgress(percent); status.setText("Enviando • "+percent+"%\n"+name+"\n"+DarkUi.size(done)+" de "+DarkUi.size(total)); });if(target!=null)task.sendNearby(this,files,target,report,this::showStatus);else task.send(this,files,peer,report,this::showStatus); runOnUiThread(() -> { progress.setVisibility(View.VISIBLE); progress.setProgress(100); }); } catch(Exception e) { showStatus(task.cancelled?"Envio cancelado.":"Falha no envio: "+e.getMessage()+" Reabra o outro app e escolha-o novamente na lista."); } finally { sender=null; runOnUiThread(() -> {  cancelSend.setEnabled(false); }); } });
    }
    private void openReceived() {
        if(receivedUris.size()==1)openReceivedAt(0);
        else if(!receivedUris.isEmpty()) { String[] labels=new String[receivedNames.size()];for(int i=0;i<labels.length;i++)labels[i]=(i+1)+". "+receivedNames.get(i)+" — "+DarkUi.size(receivedSizes.get(i));new AlertDialog.Builder(this).setTitle("Escolha o arquivo recebido").setItems(labels,(dialog,index)->openReceivedAt(index)).setNegativeButton("Voltar",null).show(); }
    }
    private void openReceivedAt(int index) {
        String name=receivedNames.get(index); int dot=name.lastIndexOf('.'); String type=dot>=0?android.webkit.MimeTypeMap.getSingleton().getMimeTypeFromExtension(name.substring(dot+1).toLowerCase(Locale.ROOT)):null;
        try { Intent intent=new Intent(Intent.ACTION_VIEW).setDataAndType(receivedUris.get(index),type==null?"*/*":type).addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION); intent.setClipData(ClipData.newRawUri("Arquivo recebido",receivedUris.get(index))); openingExternal=true; startActivity(intent); } catch(Exception e) { openingExternal=false; Toast.makeText(this,"Não há aplicativo para abrir este tipo de arquivo.",Toast.LENGTH_LONG).show(); }
    }
    private Intent folderIntent() { return new Intent(Intent.ACTION_VIEW).setDataAndType(DocumentsContract.buildDocumentUriUsingTree(receivedFolder,DocumentsContract.getTreeDocumentId(receivedFolder)),DocumentsContract.Document.MIME_TYPE_DIR).addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION); }
    private void openReceivedFolder() { if(receivedFolder==null)return; try { openingExternal=true; startActivity(folderIntent()); } catch(Exception e) { openingExternal=false; Toast.makeText(this,"Seu gerenciador não oferece abertura direta. Abra a pasta pelo aplicativo Arquivos.",Toast.LENGTH_LONG).show(); } }
    private void showStatus(String text) { runOnUiThread(() -> status.setText(text)); }
    private void receiverLabel(String text, boolean ready) {
        runOnUiThread(() -> { receiverState.setText(text); receiverState.setTextColor(ready ? DarkUi.ACCENT : DarkUi.MUTED); });
    }
    private String destinationName() {
        try {
            Uri doc = DocumentsContract.buildDocumentUriUsingTree(folder,DocumentsContract.getTreeDocumentId(folder));
            try (Cursor c = getContentResolver().query(doc,new String[]{DocumentsContract.Document.COLUMN_DISPLAY_NAME},null,null,null)) {
                if (c != null && c.moveToFirst()) return c.getString(0);
            }
        } catch (Exception ignored) {}
        return "Pasta selecionada";
    }
    private void startServer() {
        if(running||folder==null)return;
        if(code==null)code = String.format(Locale.US, "%08d", new SecureRandom().nextInt(100000000));
        final int generation=++serverGeneration;
        running = true;  choose.setEnabled(true); 
        receiverLabel("Iniciando receptor…",false);   progress.setProgress(0); progress.setVisibility(View.GONE);
        worker.execute(() -> {
            try (ServerSocket listener = new ServerSocket()) {
                listener.setReuseAddress(true); listener.bind(new InetSocketAddress(45832)); server = listener;
                if (running) {
                    
                    receiverLabel("Pronto para receber",true); showStatus("Aguardando um pedido de outro aparelho. Nenhum arquivo em transferência."); runOnUiThread(() -> {refreshPairing();});
                }
                while (running&&generation==serverGeneration) {
                    try (Socket socket = listener.accept()) {
                        current = socket;
                        if (!running) break;
                        socket.setSoTimeout(300000);
                        receive(socket,generation);
                    } catch (Exception e) { if (running&&generation==serverGeneration) showStatus("Falha: " + e.getMessage() + ". Aguardando novo pedido."); }
                    finally { current = null; if (running) receiverLabel("Pronto para receber",true); }
                }
            } catch (Exception e) { if (running) showStatus("Não foi possível abrir a porta: " + e.getMessage()); }
            finally { if(generation!=serverGeneration)return;server = null; running = false; runOnUiThread(() -> { receiverState.setText("Recebimento parado"); receiverState.setTextColor(DarkUi.MUTED);    choose.setEnabled(true);  }); }
        });
    }
    private void stopServer() {
        serverGeneration++;running = false;
        try { if (current != null) current.close(); } catch (IOException ignored) {}
        try { if (server != null) server.close(); } catch (IOException ignored) {}server=null;choose.setEnabled(true);
        if (requestDialog != null) requestDialog.dismiss();
        status.setText("Recebimento parado."); receiverLabel("Recebimento parado",false);   progress.setVisibility(View.GONE);
    }
    private void receive(Socket socket,int generation) throws Exception {
        InputStream in = new BufferedInputStream(socket.getInputStream()); OutputStream out = socket.getOutputStream();
        JSONObject offer = readJson(in);
        if(sender != null) { writeJson(out,new JSONObject().put("accepted",false)); return; }
        int version=offer.optInt("version");String ip=socket.getInetAddress().getHostAddress();String operation=offer.optString("operation");
        boolean isPair=operation.equals("pair");boolean valid=version==3&&!isPair?discoveryToken.equals(offer.optString("token")):version==2?(isPair?code.equals(offer.optString("token")):pairing.accepts(ip,offer.optString("token"))):version==1&&code.equals(offer.optString("token"));
        if(!valid){writeJson(out,new JSONObject().put("accepted",false).put("paired",false));return;}
        if(isPair){String remoteKey=offer.optString("peerToken");int peerPort=offer.optInt("peerPort");if(version!=2||!PairSession.validToken(remoteKey)||peerPort<1||peerPort>65535){writeJson(out,new JSONObject().put("paired",false));return;}
            CompletableFuture<Boolean> decision=new CompletableFuture<>();runOnUiThread(()->{if(!running){decision.complete(false);return;}requestDialog=new AlertDialog.Builder(this).setTitle("Parear aparelho?").setMessage("Aparelho "+ip+" solicita uma sessão bidirecional. Cada envio de arquivos ainda exige aceitação.").setPositiveButton("Aceitar",(d,w)->decision.complete(true)).setNegativeButton("Recusar",(d,w)->decision.complete(false)).setOnCancelListener(d->decision.complete(false)).create();requestDialog.setOnDismissListener(d->decision.complete(false));requestDialog.show();});boolean accepted;try{accepted=decision.get(120,TimeUnit.SECONDS);}catch(TimeoutException e){accepted=false;}finally{runOnUiThread(()->{if(requestDialog!=null){requestDialog.dismiss();requestDialog=null;}});}String localKey=PairSession.newToken();if(accepted&&(!running||generation!=serverGeneration))accepted=false;if(accepted)pairing.set(new PairSession.Peer(ip,peerPort,remoteKey,localKey));writeJson(out,new JSONObject().put("paired",accepted).put("sessionToken",accepted?localKey:JSONObject.NULL).put("expiresIn",0));runOnUiThread(()->{refreshPairing();});return;}
        if(operation.equals("ping")){writeJson(out,new JSONObject().put("paired",true));return;}
        JSONArray files = offer.getJSONArray("files");
        if (files.length() == 0 || files.length() > 1000) throw new IOException("Quantidade inválida de arquivos");
        long total = 0;
        for (int i=0; i<files.length(); i++) {
            JSONObject file = files.getJSONObject(i); String name = file.getString("name"); long size = file.getLong("size");
            if (name.isEmpty() || name.length() > 255 || name.contains("/") || name.contains("\\") || name.equals(".") || name.equals("..") || name.indexOf('\0') >= 0 || size < 0 || Long.MAX_VALUE-total < size) throw new IOException("Metadados inválidos");
            total += size;
        }
        final long offeredTotal = total;
        receiverLabel("Pedido recebido • confirme abaixo",true);
        showStatus("Aguardando sua aceitação. A transferência ainda não começou.");
        runOnUiThread(() -> { progress.setProgress(0); progress.setVisibility(View.GONE); });
        CompletableFuture<Boolean> decision = new CompletableFuture<>();
        runOnUiThread(() -> {
            if (!running || isFinishing()) { decision.complete(false); return; }
            LinearLayout card = DarkUi.card(this);
            card.addView(DarkUi.text(this,"↓   Pedido de transferência",19,DarkUi.TEXT,true)); DarkUi.gap(this,card,8);
            card.addView(DarkUi.text(this,files.length() + " arquivo(s) • " + DarkUi.size(offeredTotal),14,DarkUi.MUTED,false));
            card.addView(DarkUi.text(this,(offer.optString("senderKind").equals("windows")?"Computador: ":offer.optString("senderKind").equals("android")?"Celular: ":"Aparelho: ") + offer.optString("senderName","Aparelho").replaceAll("[\\p{Cntrl}]", "").substring(0,Math.min(64,offer.optString("senderName","Aparelho").replaceAll("[\\p{Cntrl}]", "").length())) + " (" + socket.getInetAddress().getHostAddress() + ")",12,DarkUi.MUTED,false));
            DarkUi.gap(this,card,12); card.addView(DarkUi.text(this,"Salvar em " + destinationName() + "?",13,DarkUi.TEXT,false)); DarkUi.gap(this,card,14);
            ScrollView listScroll = new ScrollView(this); LinearLayout fileList = DarkUi.column(this);
            for (int index=0; index<files.length(); index++) {
                JSONObject file = files.optJSONObject(index);
                fileList.addView(DarkUi.fileRow(this,file.optString("name"),file.optLong("size")));
            }
            listScroll.addView(fileList); card.addView(listScroll,new LinearLayout.LayoutParams(-1,DarkUi.dp(this,Math.min(240,files.length()*66)))); DarkUi.gap(this,card,16);
            LinearLayout actions = new LinearLayout(this);
            Button decline = DarkUi.button(this,"Recusar",false,true), accept = DarkUi.button(this,"Aceitar",true,false);
            LinearLayout.LayoutParams left = new LinearLayout.LayoutParams(0,DarkUi.dp(this,46),1); left.rightMargin=DarkUi.dp(this,10);
            actions.addView(decline,left); actions.addView(accept,new LinearLayout.LayoutParams(0,DarkUi.dp(this,46),1)); card.addView(actions);
            requestDialog = new AlertDialog.Builder(this).setView(card).setOnCancelListener(d -> decision.complete(false)).create();
            requestDialog.setOnDismissListener(d -> decision.complete(false));
            decline.setOnClickListener(v -> { decision.complete(false); if (requestDialog != null) requestDialog.dismiss(); });
            accept.setOnClickListener(v -> { decision.complete(true); if (requestDialog != null) requestDialog.dismiss(); });
            requestDialog.show();
            if (requestDialog.getWindow() != null) { requestDialog.getWindow().setBackgroundDrawable(new ColorDrawable(android.graphics.Color.TRANSPARENT)); requestDialog.getWindow().setLayout(getResources().getDisplayMetrics().widthPixels-DarkUi.dp(this,32),-2); }
        });
        boolean accepted;
        try { accepted = decision.get(120, TimeUnit.SECONDS); }
        catch (TimeoutException e) { accepted = false; }
        finally { runOnUiThread(() -> { if (requestDialog != null) { requestDialog.dismiss(); requestDialog = null; } }); }
        writeJson(out, new JSONObject().put("accepted", accepted));
        if (!accepted) { showStatus("Pedido recusado ou expirado."); return; }
        receiverLabel("Recebendo arquivos",true);
        ArrayList<Uri> savedUris = new ArrayList<>(); ArrayList<String> savedNames = new ArrayList<>(); ArrayList<Long> savedSizes=new ArrayList<>(); final Uri savedFolder=folder;
        runOnUiThread(() -> completionBox.setVisibility(View.GONE));
        long done = 0; byte[] buffer = new byte[65536]; long lastUpdate = 0;
        for (int i=0; i<files.length(); i++) {
            JSONObject file = files.getJSONObject(i); String name = file.getString("name"); long remaining = file.getLong("size");
            Uri parent = DocumentsContract.buildDocumentUriUsingTree(folder, DocumentsContract.getTreeDocumentId(folder));
            // Unique suffix avoids overwriting an existing document, even for providers that reuse names.
            String savedName = uniqueName(name);
            Uri target = DocumentsContract.createDocument(getContentResolver(), parent, "application/octet-stream", savedName);
            if (target == null) throw new IOException("Não foi possível criar " + name);
            boolean complete = false;
            try {
                try (OutputStream destination = getContentResolver().openOutputStream(target, "w")) {
                    if (destination == null) throw new IOException("Não foi possível abrir " + name);
                    while (remaining > 0) {
                        int n = in.read(buffer, 0, (int)Math.min(buffer.length, remaining));
                        if (n < 0) throw new EOFException("Conexão interrompida");
                        destination.write(buffer,0,n); remaining -= n; done += n;
                        long now = System.currentTimeMillis();
                        if (now-lastUpdate > 100 || remaining == 0) {
                            lastUpdate = now; int pct = total == 0 ? 100 : (int)(done*100.0/total); long transferred = done; long all = total;
                            runOnUiThread(() -> { progress.setVisibility(View.VISIBLE); progress.setProgress(pct); status.setText("Recebendo • " + pct + "%\n" + name + "\n" + DarkUi.size(transferred) + " de " + DarkUi.size(all)); });
                        }
                    }
                    destination.flush();
                }
                complete = true;
            } finally { if (!complete) { try { DocumentsContract.deleteDocument(getContentResolver(),target); } catch (Exception ignored) {} } }
            savedUris.add(target); savedNames.add(name); savedSizes.add(file.getLong("size"));
            writeJson(out,new JSONObject().put("ok",true));
        }
        runOnUiThread(() -> { progress.setVisibility(View.VISIBLE); progress.setProgress(100); receivedUris.clear(); receivedUris.addAll(savedUris); receivedNames.clear(); receivedNames.addAll(savedNames); receivedSizes.clear(); receivedSizes.addAll(savedSizes); receivedFolder=savedFolder; completionBox.setVisibility(View.VISIBLE); openFiles.setText(savedUris.size()==1?"Abrir arquivo":"Abrir arquivos ("+savedUris.size()+")"); openFolder.setEnabled(folderIntent().resolveActivity(getPackageManager())!=null); status.setText("Transferência concluída\n"+savedUris.size()+" arquivo(s) salvo(s).\nReceptor disponível para novos envios."); });
    }
    private static String uniqueName(String name) {
        int dot = name.lastIndexOf('.'); String suffix = "_" + UUID.randomUUID().toString();
        String base = dot > 0 ? name.substring(0,dot) : name;
        String ext = dot > 0 ? name.substring(dot) : "";
        // Keep the final name comfortably below typical filesystem byte limits.
        if (ext.length() > 12) ext = ext.substring(0,12);
        if (base.length() > 32) base = base.substring(0,32);
        return base + suffix + ext;
    }
    private static JSONObject readJson(InputStream in) throws Exception {
        ByteArrayOutputStream bytes = new ByteArrayOutputStream();
        for (int i=0; i<1048576; i++) { int b = in.read(); if (b < 0) throw new EOFException("Conexão fechada"); if (b == 10) return new JSONObject(bytes.toString("UTF-8")); bytes.write(b); }
        throw new IOException("Pedido muito grande");
    }
    private static void writeJson(OutputStream out, JSONObject json) throws IOException { out.write((json.toString()+"\n").getBytes(StandardCharsets.UTF_8)); out.flush(); }
    private String addresses() {
        try {
            List<String> result = new ArrayList<>();
            Enumeration<NetworkInterface> interfaces = NetworkInterface.getNetworkInterfaces();
            while (interfaces.hasMoreElements()) { NetworkInterface ni = interfaces.nextElement(); if (!ni.isUp() || ni.isLoopback()) continue;
                Enumeration<InetAddress> values = ni.getInetAddresses(); while (values.hasMoreElements()) { InetAddress a = values.nextElement(); if (a instanceof Inet4Address && a.isSiteLocalAddress()) result.add(a.getHostAddress()); }
            }
            return result.isEmpty() ? "nenhum IPv4 local; conecte ao Wi-Fi" : String.join(" / ",result);
        } catch (Exception e) { return "consulte as configurações do Wi-Fi"; }
    }
    @Override protected void onResume() { super.onResume(); openingExternal=false;startDiscovery();if(resumeReception&&folder!=null&&!running){resumeReception=false;startServer();} }
    @Override protected void onStop() { super.onStop(); if(!openingExternal){resumeReception=running;stopDiscovery();stopServer();if(pairing.get()!=null)sessionView.setText("Sessão preservada · receptor suspenso até voltar");} }
    @Override protected void onDestroy() { pairing.clear();stopDiscovery();stopServer(); if(sender!=null)sender.cancel(); outbound.shutdownNow(); worker.shutdownNow(); super.onDestroy(); }
}










