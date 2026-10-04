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
    private TextView info, status, codeView, receiverState, folderView;
    private ProgressBar progress;
    private Button choose, start, stop;
    private Uri folder;
    private EditText pcIp, pcCode;
    private Button sendFiles, cancelSend, openFiles, openFolder;
    private LinearLayout completionBox;
    private LinearLayout receivePage, sendPage;
    private Button sendTab, receiveTab;
    private boolean receivingTab;
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
    private final ExecutorService worker = Executors.newSingleThreadExecutor();

    @Override public void onCreate(Bundle state) {
        super.onCreate(state);
        getWindow().addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
        getWindow().getDecorView().setSystemUiVisibility(0);
        LinearLayout shell=DarkUi.column(this);int pad=DarkUi.dp(this,20);shell.setPadding(pad,pad,pad,pad);shell.setBackgroundColor(DarkUi.BG);
        shell.setOnApplyWindowInsetsListener((v,insets)->{shell.setPadding(pad+insets.getSystemWindowInsetLeft(),pad+insets.getSystemWindowInsetTop(),pad+insets.getSystemWindowInsetRight(),pad+insets.getSystemWindowInsetBottom());return insets;});
        ScrollView scroll=new ScrollView(this);scroll.setFillViewport(true);scroll.setBackgroundColor(DarkUi.BG);
        LinearLayout layout=DarkUi.column(this);
        LinearLayout header = new LinearLayout(this); header.setGravity(Gravity.CENTER_VERTICAL);
        ImageView icon = new ImageView(this); icon.setImageResource(com.droplocal.R.drawable.ic_drop); icon.setContentDescription("Drop Local");
        header.addView(icon,new LinearLayout.LayoutParams(DarkUi.dp(this,48),DarkUi.dp(this,48)));
        LinearLayout heading = DarkUi.column(this); LinearLayout.LayoutParams headingParams = new LinearLayout.LayoutParams(0,-2,1); headingParams.leftMargin=DarkUi.dp(this,14); heading.setLayoutParams(headingParams);
        heading.addView(DarkUi.text(this,"Drop Local",24,DarkUi.TEXT,true)); heading.addView(DarkUi.text(this,"Seus arquivos, pela sua rede · 0.6.0",12,DarkUi.MUTED,false));heading.addView(DarkUi.text(this,"by Joseph David",11,DarkUi.MUTED,false));header.addView(heading);shell.addView(header);sessionView=DarkUi.text(this,"Sessão não pareada",12,DarkUi.ACCENT,true);shell.addView(sessionView);Button help=DarkUi.button(this,"Como usar",false,false);help.setOnClickListener(v->beginTutorial());shell.addView(help);DarkUi.gap(this,shell,8);
        LinearLayout navigation=new LinearLayout(this);
        sendTab=DarkUi.button(this,"Arquivos",false,false);receiveTab=DarkUi.button(this,"Conexão",false,false);
        LinearLayout.LayoutParams sendParams=new LinearLayout.LayoutParams(0,DarkUi.dp(this,44),1);sendParams.rightMargin=DarkUi.dp(this,6);navigation.addView(sendTab,sendParams);
        LinearLayout.LayoutParams receiveParams=new LinearLayout.LayoutParams(0,DarkUi.dp(this,44),1);receiveParams.leftMargin=DarkUi.dp(this,6);navigation.addView(receiveTab,receiveParams);
        shell.addView(navigation);DarkUi.gap(this,shell,12);shell.addView(scroll,new LinearLayout.LayoutParams(-1,0,1));
        receivePage=DarkUi.column(this);sendPage=DarkUi.column(this);layout.addView(receivePage);layout.addView(sendPage);
        sendTab.setOnClickListener(v->{selectPage(false);guide("files-tab");});receiveTab.setOnClickListener(v->{selectPage(true);guide("connection");});

        LinearLayout reception = DarkUi.card(this);
        reception.addView(DarkUi.text(this,"Meu IP e código",17,DarkUi.TEXT,true));
        receiverState = DarkUi.text(this,"Recebimento parado",13,DarkUi.MUTED,false); DarkUi.gap(this,reception,5); reception.addView(receiverState); DarkUi.gap(this,reception,18);
        reception.addView(DarkUi.text(this,"IP do Android",12,DarkUi.MUTED,false));
        info = DarkUi.text(this,"Inicie o recebimento para mostrar o IP",15,DarkUi.TEXT,true); info.setTextIsSelectable(true); DarkUi.gap(this,reception,4); reception.addView(info); DarkUi.gap(this,reception,14);
        reception.addView(DarkUi.text(this,"Código de sessão",12,DarkUi.MUTED,false));
        codeView = DarkUi.text(this,"—",24,DarkUi.ACCENT,true); codeView.setTypeface(Typeface.MONOSPACE,Typeface.BOLD); codeView.setLetterSpacing(0.12f); codeView.setTextIsSelectable(true);
        codeView.setPadding(DarkUi.dp(this,12),DarkUi.dp(this,10),DarkUi.dp(this,12),DarkUi.dp(this,10)); codeView.setBackground(DarkUi.shape(this,DarkUi.FIELD,DarkUi.BORDER,9));
        DarkUi.gap(this,reception,5); reception.addView(codeView); DarkUi.gap(this,reception,10); reception.addView(DarkUi.text(this,"Informe estes dados no programa Windows.",12,DarkUi.MUTED,false)); receivePage.addView(reception);
        LinearLayout destination = DarkUi.card(this); destination.addView(DarkUi.text(this,"▣   Pasta de destino",16,DarkUi.TEXT,true));
        folderView = DarkUi.text(this,"Nenhuma pasta escolhida",14,DarkUi.MUTED,false); DarkUi.gap(this,destination,7); destination.addView(folderView); DarkUi.gap(this,destination,16);
        choose = DarkUi.button(this,"Escolher pasta",false,false); destination.addView(choose); receivePage.addView(destination);
        start = DarkUi.button(this,"Ativar sessão",true,false); start.setEnabled(false); receivePage.addView(start); DarkUi.gap(this,receivePage,10);
        stop = DarkUi.button(this,"Parar / cancelar",false,true); stop.setEnabled(false); receivePage.addView(stop); DarkUi.gap(this,receivePage,12); receivePage.addView(DarkUi.text(this,"O recebimento continua ativo ao mudar de aba.",12,DarkUi.MUTED,false));
        LinearLayout transfer = DarkUi.card(this); transfer.addView(DarkUi.text(this,"Atividade",15,DarkUi.TEXT,true)); DarkUi.gap(this,transfer,8);
        status = DarkUi.text(this,"Conecte ao Windows para enviar ou escolha Receber.",13,DarkUi.MUTED,false); transfer.addView(status); DarkUi.gap(this,transfer,12);
        progress = new ProgressBar(this,null,android.R.attr.progressBarStyleHorizontal); progress.setMax(100); progress.setProgressTintList(ColorStateList.valueOf(DarkUi.ACCENT)); progress.setProgressBackgroundTintList(ColorStateList.valueOf(DarkUi.FIELD)); progress.setVisibility(View.GONE);
        transfer.addView(progress,new LinearLayout.LayoutParams(-1,DarkUi.dp(this,10)));
        boolean compact=getResources().getConfiguration().screenHeightDp<640 || getResources().getConfiguration().fontScale>=1.4f;
        if(compact)layout.addView(transfer);else shell.addView(transfer);
        completionBox = DarkUi.column(this); completionBox.setVisibility(View.GONE);
        openFiles = DarkUi.button(this,"Abrir arquivo",true,false); completionBox.addView(openFiles); DarkUi.gap(this,completionBox,8);
        openFolder = DarkUi.button(this,"Abrir pasta / local",false,false); completionBox.addView(openFolder); transfer.addView(completionBox);
        openFiles.setOnClickListener(v -> openReceived()); openFolder.setOnClickListener(v -> openReceivedFolder());
        LinearLayout sending = DarkUi.card(this); sending.addView(DarkUi.text(this,"Parear outro aparelho",17,DarkUi.TEXT,true)); DarkUi.gap(this,sending,10);
        sending.addView(DarkUi.text(this,"Ative sua sessão. No PC, abra Conexão / QR e confirme o pareamento.",12,DarkUi.MUTED,false));
        pcIp = input("IP do computador"); pcCode = input("Código de verificação do Windows"); pcCode.setInputType(android.text.InputType.TYPE_CLASS_NUMBER); sending.addView(pcIp); sending.addView(pcCode);
        scan = DarkUi.button(this,"Escanear QR do Windows",false,false); sending.addView(scan); scan.setOnClickListener(v -> scanQr()); DarkUi.gap(this,sending,8);
        pair = DarkUi.button(this,"Parear",false,false); sending.addView(pair); pair.setOnClickListener(v -> {if(pairing.get()!=null){pairing.clear();refreshPairing();}else pairWindows(false);}); DarkUi.gap(this,sending,12);
        sendFiles = DarkUi.button(this,"Enviar arquivo",true,false); LinearLayout fileActions=DarkUi.card(this);fileActions.addView(sendFiles); sendFiles.setOnClickListener(v -> {
            if(current != null || sender != null) { Toast.makeText(this,"Aguarde a transferência atual.",Toast.LENGTH_SHORT).show(); return; }
            try { if(pairing.get()==null)throw new IOException("Pareie primeiro na aba Conexão."); }
            catch(Exception e) { showStatus(e.getMessage()); return; }
            Intent pick = new Intent(Intent.ACTION_OPEN_DOCUMENT); pick.setType("*/*"); pick.addCategory(Intent.CATEGORY_OPENABLE); pick.putExtra(Intent.EXTRA_ALLOW_MULTIPLE,true); pick.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION); openingExternal=true; startActivityForResult(pick,2);
        }); DarkUi.gap(this,sending,8);
        cancelSend = DarkUi.button(this,"Cancelar envio",false,true); cancelSend.setEnabled(false); fileActions.addView(cancelSend); cancelSend.setOnClickListener(v -> { if(sender != null) sender.cancel(); }); receivePage.addView(sending);sendPage.addView(fileActions);
        scroll.addView(layout); setContentView(shell);selectPage(state==null || state.getBoolean("receivingTab",true));
        choose.setOnClickListener(v -> { Intent i = new Intent(Intent.ACTION_OPEN_DOCUMENT_TREE); i.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION | Intent.FLAG_GRANT_WRITE_URI_PERMISSION); openingExternal=true; startActivityForResult(i, 1); });
        start.setOnClickListener(v -> startServer()); stop.setOnClickListener(v -> {resumeReception=false;pairing.clear();code=null;stopServer();refreshPairing();});
        initTutorial(shell);
    }
    private void selectPage(boolean receiving) {
        receivingTab=receiving;receivePage.setVisibility(receiving?View.VISIBLE:View.GONE);sendPage.setVisibility(receiving?View.GONE:View.VISIBLE);
        styleTab(sendTab,!receiving);styleTab(receiveTab,receiving);
        if(!running && sender==null && receivedUris.isEmpty() && (status.getText().toString().startsWith("Conecte ao Windows") || status.getText().toString().startsWith("Escolha uma pasta")))status.setText(receiving?"Escolha uma pasta para receber do Windows.":"Conecte ao Windows para enviar arquivos.");
    }
    private void styleTab(Button tab,boolean selected) {
        tab.setSelected(selected);tab.setTextColor(selected?DarkUi.BG:DarkUi.ACCENT);tab.setBackground(DarkUi.shape(this,selected?DarkUi.ACCENT:DarkUi.PANEL,selected?DarkUi.ACCENT:DarkUi.BORDER,10));
        tab.setContentDescription(tab.getText()+(selected?", aba selecionada":", aba"));
    }
    @Override protected void onSaveInstanceState(Bundle state) {super.onSaveInstanceState(state);state.putBoolean("receivingTab",receivingTab);}
    @Override protected void onActivityResult(int request, int result, Intent data) {
        super.onActivityResult(request,result,data);
        if (request == 1 && result == RESULT_OK && data != null && data.getData() != null) {
            folder = data.getData(); start.setEnabled(true); folderView.setText(destinationName());guide("folder"); status.setText("Pasta pronta. Toque em Iniciar recebimento.");
        }
        if(request==2 && result==RESULT_OK && data!=null) { ArrayList<Uri> files=new ArrayList<>(); if(data.getClipData()!=null) { for(int index=0;index<data.getClipData().getItemCount();index++)files.add(data.getClipData().getItemAt(index).getUri()); } else if(data.getData()!=null)files.add(data.getData()); if(!files.isEmpty()){guide("select");sendPicked(files);} }
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
        if(!running||server==null||folder==null){showStatus("Escolha a pasta e ative sua sessão antes de parear.");return;}
        if(ip.isEmpty()||!verification.matches("[0-9]{8}")){showStatus("Informe IP e código de oito dígitos, ou leia o QR.");return;}
        pair.setEnabled(false);outbound.execute(()->{try{new PhoneSender().pair(ip,verification,45832,pairing);runOnUiThread(()->{refreshPairing();guide("pair");});showStatus("Pareado. Ambos podem enviar e receber sem outro código.");}catch(Exception e){showStatus("Pareamento: "+e.getMessage());}finally{runOnUiThread(()->{pair.setEnabled(true);applyGuide();});}});
    }
    private void refreshPairing(){PairSession.Peer peer=pairing.get();sessionView.setText(peer==null?"Sessão não pareada":"Pareado com "+peer.ip+" · enviar e receber");pair.setText(peer==null?"Parear":"Desconectar");}
    private void sendPicked(ArrayList<Uri> files) {
        if(current != null || sender != null) { showStatus("Aguarde a transferência atual antes de enviar."); return; }
        final PairSession.Peer peer=pairing.get();if(peer==null){showStatus("Pareie antes de enviar.");return;}guide("send");
        PhoneSender task=new PhoneSender(); sender=task; sendFiles.setEnabled(false); cancelSend.setEnabled(true); completionBox.setVisibility(View.GONE); progress.setVisibility(View.GONE);
        outbound.execute(() -> { try { task.send(this,files,peer,(done,total,name) -> runOnUiThread(() -> { progress.setVisibility(View.VISIBLE); int percent=total==0?100:(int)(done*100.0/total); progress.setProgress(percent); status.setText("Enviando • "+percent+"%\n"+name+"\n"+DarkUi.size(done)+" de "+DarkUi.size(total)); }),this::showStatus); runOnUiThread(() -> { progress.setVisibility(View.VISIBLE); progress.setProgress(100); }); } catch(Exception e) { showStatus(task.cancelled?"Envio cancelado.":"Falha no envio: "+e.getMessage()+" Se o outro app reiniciou, desconecte e pareie novamente."); } finally { sender=null; runOnUiThread(() -> { sendFiles.setEnabled(true); cancelSend.setEnabled(false); }); } });
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
        runOnUiThread(() -> { receiverState.setText(text); receiverState.setTextColor(ready ? DarkUi.ACCENT : DarkUi.MUTED);receiveTab.setText(running?"Conexão • ativa":"Conexão"); });
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
        if(code==null)code = String.format(Locale.US, "%08d", new SecureRandom().nextInt(100000000));
        final int generation=++serverGeneration;
        running = true; start.setEnabled(false); choose.setEnabled(false); stop.setEnabled(true);
        receiverLabel("Iniciando receptor…",false); info.setText("—"); codeView.setText("—"); progress.setProgress(0); progress.setVisibility(View.GONE);
        worker.execute(() -> {
            try (ServerSocket listener = new ServerSocket()) {
                listener.setReuseAddress(true); listener.bind(new InetSocketAddress(45832)); server = listener;
                if (running) {
                    runOnUiThread(() -> { if (running) { info.setText(addresses()); codeView.setText(code); } });
                    receiverLabel("Pronto para receber",true); showStatus("Aguardando um pedido do Windows. Nenhum arquivo em transferência."); runOnUiThread(() -> {guide("start");refreshPairing();});
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
            finally { if(generation!=serverGeneration)return;server = null; running = false; runOnUiThread(() -> { receiverState.setText("Recebimento parado"); receiverState.setTextColor(DarkUi.MUTED); info.setText("—"); codeView.setText("—"); start.setEnabled(folder != null); choose.setEnabled(true); stop.setEnabled(false);receiveTab.setText("Conexão"); }); }
        });
    }
    private void stopServer() {
        serverGeneration++;running = false;
        try { if (current != null) current.close(); } catch (IOException ignored) {}
        try { if (server != null) server.close(); } catch (IOException ignored) {}server=null;start.setEnabled(folder!=null);choose.setEnabled(true);stop.setEnabled(false);
        if (requestDialog != null) requestDialog.dismiss();
        status.setText("Recebimento parado."); receiverLabel("Recebimento parado",false); info.setText("—"); codeView.setText("—"); progress.setVisibility(View.GONE);
    }
    private void receive(Socket socket,int generation) throws Exception {
        InputStream in = new BufferedInputStream(socket.getInputStream()); OutputStream out = socket.getOutputStream();
        JSONObject offer = readJson(in);
        if(sender != null) { writeJson(out,new JSONObject().put("accepted",false)); return; }
        int version=offer.optInt("version");String ip=socket.getInetAddress().getHostAddress();String operation=offer.optString("operation");
        boolean isPair=operation.equals("pair");boolean valid=version==2?(isPair?code.equals(offer.optString("token")):pairing.accepts(ip,offer.optString("token"))):version==1&&code.equals(offer.optString("token"));
        if(!valid){writeJson(out,new JSONObject().put("accepted",false).put("paired",false));return;}
        if(isPair){String remoteKey=offer.optString("peerToken");int peerPort=offer.optInt("peerPort");if(version!=2||!PairSession.validToken(remoteKey)||peerPort<1||peerPort>65535){writeJson(out,new JSONObject().put("paired",false));return;}
            CompletableFuture<Boolean> decision=new CompletableFuture<>();runOnUiThread(()->{if(!running){decision.complete(false);return;}requestDialog=new AlertDialog.Builder(this).setTitle("Parear aparelho?").setMessage("Aparelho "+ip+" solicita uma sessão bidirecional. Cada envio de arquivos ainda exige aceitação.").setPositiveButton("Aceitar",(d,w)->decision.complete(true)).setNegativeButton("Recusar",(d,w)->decision.complete(false)).setOnCancelListener(d->decision.complete(false)).create();requestDialog.setOnDismissListener(d->decision.complete(false));requestDialog.show();});boolean accepted;try{accepted=decision.get(120,TimeUnit.SECONDS);}catch(TimeoutException e){accepted=false;}finally{runOnUiThread(()->{if(requestDialog!=null){requestDialog.dismiss();requestDialog=null;}});}String localKey=PairSession.newToken();if(accepted&&(!running||generation!=serverGeneration))accepted=false;if(accepted)pairing.set(new PairSession.Peer(ip,peerPort,remoteKey,localKey));writeJson(out,new JSONObject().put("paired",accepted).put("sessionToken",accepted?localKey:JSONObject.NULL).put("expiresIn",0));runOnUiThread(()->{refreshPairing();if(pairing.get()!=null)guide("pair");});return;}
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
            card.addView(DarkUi.text(this,"Do Windows: " + socket.getInetAddress().getHostAddress(),12,DarkUi.MUTED,false));
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
    private LinearLayout tourPanel;private TextView tourText;private Button tourSkip,tourDemo;private final TutorialState tour=new TutorialState();private boolean renderingTour;
    private final java.util.IdentityHashMap<View,android.graphics.drawable.Drawable> tourBackgrounds=new java.util.IdentityHashMap<>();private final java.util.IdentityHashMap<View,CharSequence> tourDescriptions=new java.util.IdentityHashMap<>();
    private final java.util.IdentityHashMap<View,Boolean> tourEnabled=new java.util.IdentityHashMap<>();
    private static final String[] TOUR_EVENTS=TutorialState.EVENTS;
    private static final String[] TOUR_TEXT={"1/7 · Abra Conexão para preparar os dois aparelhos.","2/7 · Escolha a pasta onde deseja salvar os arquivos.","3/7 · Ative sua sessão. IP e código ficam disponíveis.","4/7 · Leia o QR do PC ou preencha IP/código e toque Parear. Confirme no outro aparelho.","5/7 · Abra Arquivos. A sessão permite enviar nos dois sentidos.","6/7 · Toque Enviar arquivo e escolha o lote.","7/7 · O destino confirma cada lote. Depois de receber, use Abrir arquivo ou Abrir pasta."};
    private void initTutorial(LinearLayout shell){tourPanel=DarkUi.card(this);tourText=DarkUi.text(this,"",13,DarkUi.TEXT,true);tourPanel.addView(tourText);LinearLayout actions=new LinearLayout(this);tourSkip=DarkUi.button(this,"Pular tutorial",false,false);tourDemo=DarkUi.button(this,"Demonstração",true,false);actions.addView(tourSkip,new LinearLayout.LayoutParams(0,-2,1));actions.addView(tourDemo,new LinearLayout.LayoutParams(0,-2,1));tourPanel.addView(actions);shell.addView(tourPanel);tourPanel.setVisibility(View.GONE);tourSkip.setOnClickListener(v->finishTutorial());tourDemo.setOnClickListener(v->{showStatus(tour.step==3?"Demonstração: os dois aparelhos aceitam a sessão. Nenhum aparelho conectado.":"Demonstração: lote aceito, salvo e aberto. Nenhum arquivo enviado.");guide(TOUR_EVENTS[tour.step]);});if(!getPreferences(MODE_PRIVATE).getBoolean("tutorialDone",false)){tour.begin();applyGuide();}}
    private void beginTutorial(){if(sender!=null||current!=null){showStatus("Aguarde a operação atual antes do tutorial.");return;}restoreGuide();tour.begin();applyGuide();}
    private void restoreGuide(){for(java.util.Map.Entry<View,android.graphics.drawable.Drawable> item:tourBackgrounds.entrySet())item.getKey().setBackground(item.getValue());tourBackgrounds.clear();for(java.util.Map.Entry<View,CharSequence> item:tourDescriptions.entrySet())item.getKey().setContentDescription(item.getValue());tourDescriptions.clear();for(java.util.Map.Entry<View,Boolean> item:tourEnabled.entrySet())item.getKey().setEnabled(item.getValue());tourEnabled.clear();choose.setEnabled(!running);start.setEnabled(folder!=null&&!running);stop.setEnabled(running);}
    private void finishTutorial(){restoreGuide();tour.finish();tourPanel.setVisibility(View.GONE);getPreferences(MODE_PRIVATE).edit().putBoolean("tutorialDone",true).apply();showStatus("Tutorial concluído ou pulado. Reabra em Como usar.");}
    private void guide(String event){if(!renderingTour&&tour.action(event)){restoreGuide();if(tour.done())finishTutorial();else applyGuide();}}
    private void disableGuideActions(View root,java.util.List<View> allowed){if(root==tourPanel)return;if(root instanceof Button||root instanceof EditText){tourEnabled.put(root,root.isEnabled());root.setEnabled(root.isEnabled()&&allowed.contains(root));if(allowed.contains(root)){tourBackgrounds.put(root,root.getBackground());tourDescriptions.put(root,root.getContentDescription());root.setContentDescription(TOUR_TEXT[tour.step]);root.setBackground(DarkUi.shape(this,DarkUi.FIELD,DarkUi.ACCENT,9));}}if(root instanceof android.view.ViewGroup){android.view.ViewGroup group=(android.view.ViewGroup)root;for(int i=0;i<group.getChildCount();i++)disableGuideActions(group.getChildAt(i),allowed);}}
    private void applyGuide(){if(tourPanel==null||tour.step<0)return;renderingTour=true;restoreGuide();selectPage(tour.step<=3);java.util.List<View> allowed=new ArrayList<>();switch(tour.step){case 0:allowed.add(receiveTab);break;case 1:allowed.add(choose);break;case 2:allowed.add(start);break;case 3:allowed.add(pcIp);allowed.add(pcCode);allowed.add(pair);allowed.add(scan);break;case 4:allowed.add(sendTab);break;default:allowed.add(sendFiles);}disableGuideActions(getWindow().getDecorView(),allowed);tourText.setText(TOUR_TEXT[tour.step]);tourDemo.setVisibility(tour.step>=3?View.VISIBLE:View.GONE);tourPanel.setVisibility(View.VISIBLE);if(!allowed.isEmpty()){View target=allowed.get(0);target.requestFocus();target.post(()->target.requestRectangleOnScreen(new android.graphics.Rect(0,0,target.getWidth(),target.getHeight()),true));}renderingTour=false;}
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
    @Override protected void onResume() { super.onResume(); openingExternal=false;if(resumeReception&&folder!=null&&!running){resumeReception=false;startServer();} }
    @Override protected void onStop() { super.onStop(); if(!openingExternal){resumeReception=running;stopServer();if(pairing.get()!=null)sessionView.setText("Sessão preservada · receptor suspenso até voltar");} }
    @Override protected void onDestroy() { pairing.clear();stopServer(); if(sender!=null)sender.cancel(); outbound.shutdownNow(); worker.shutdownNow(); super.onDestroy(); }
}










