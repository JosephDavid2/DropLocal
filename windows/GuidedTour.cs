internal sealed partial class MainForm
{
    ComboBox portChoice=null!;ActionButton pairButton=null!; Label pairingStatus=null!; System.Windows.Forms.Timer sessionTimer=null!;
    Panel? guidePanel; int guideStep=-1; bool applyingGuide;
    readonly Dictionary<Control,bool> guideEnabled=new();readonly Dictionary<Control,Color> guideColors=new();readonly Dictionary<ActionButton,bool> guidePrimary=new();
    readonly string tutorialDirectory;string TutorialPath=>Path.Combine(tutorialDirectory,"tutorial-v1.done");
    static readonly string[] GuideEvents={"connection","folder","start","pair","files-tab","select","send"};
    static readonly string[] GuideMessages={"1/7 · Abra Conexão / QR para preparar o pareamento.","2/7 · Escolha onde os arquivos recebidos serão salvos.","3/7 · Ative sua sessão para mostrar IP, código e QR.","4/7 · No outro aparelho, confirme o pareamento. Ou informe o IP e código dele e toque Parear.","5/7 · Abra Arquivos. A mesma sessão funciona nos dois sentidos.","6/7 · Selecione arquivos para o aparelho pareado.","7/7 · Envie o lote. O destino sempre confirma. Ao receber, use Abrir pasta recebida."};
    void RefreshConnection()
    {
        var peer=receiveView.Pairing.Peer;pairButton.Text=peer==null?"Parear":"Desconectar";
        pairingStatus.Text=peer==null?"Sessão não pareada · cada envio exige aceitação":$"Pareado com {peer.Address} · enviar e receber";
        if(peer!=null){connection.Text="Sessão em memória · até encerrar ou fechar";Guide("pair");}
    }
    IEnumerable<Control> AllControls(Control root){foreach(Control c in root.Controls){yield return c;foreach(var child in AllControls(c))yield return child;}}
    void RestoreGuide(){foreach(var item in guideColors)if(!item.Key.IsDisposed)item.Key.BackColor=item.Value;guideColors.Clear();foreach(var item in guidePrimary)if(!item.Key.IsDisposed){item.Key.Primary=item.Value;item.Key.Invalidate();}guidePrimary.Clear();foreach(var item in guideEnabled)if(!item.Key.IsDisposed)item.Key.Enabled=item.Value;guideEnabled.Clear();guidePanel?.Dispose();guidePanel=null;receiveView.ApplyAvailability();}
    void BeginTutorial(){if(active!=null){status.Text="Conclua ou cancele o envio antes do tutorial.";return;}RestoreGuide();guideStep=0;ShowGuide();}
    void Guide(string action){if(!applyingGuide&&guideStep>=0&&GuideEvents[guideStep]==action){RestoreGuide();guideStep++;if(guideStep==GuideEvents.Length)FinishGuide();else ShowGuide();}}
    void FinishGuide(){RestoreGuide();guideStep=-1;Directory.CreateDirectory(Path.GetDirectoryName(TutorialPath)!);File.WriteAllText(TutorialPath,"done");status.Text="Tutorial concluído ou pulado. Reabra em Como usar.";}
    void ShowGuide()
    {
        applyingGuide=true;
        if(guideStep<=3)ShowSection(true);else ShowSection(false);
        var allowed=guideStep switch {0=>new Control[]{receiveTab},1=>new Control[]{receiveView.chooseFolder},2=>new Control[]{receiveView.StartControl},3=>new Control[]{address,token,portChoice,pairButton},4=>new Control[]{sendTab},5=>new Control[]{choose},_=>new Control[]{send}};
        // Disable actions individually: containers stay enabled so the highlighted real control remains usable.
        foreach(var c in AllControls(this).Where(c=>c is Button||c is TextBox||c is ComboBox||c is ListBox||c is LinkLabel)) {guideEnabled[c]=c.Enabled;c.Enabled=allowed.Contains(c)&&c.Enabled;}
        guidePanel=new Panel{Dock=DockStyle.Bottom,Height=112,BackColor=Palette.Field,Padding=new Padding(12),AccessibleName="Tutorial guiado"};
        var text=new Label{Text=GuideMessages[guideStep],Dock=DockStyle.Top,Height=48,ForeColor=Palette.Text,Font=new Font("Segoe UI",10)};guidePanel.Controls.Add(text);
        var skip=new ActionButton{Text="Pular tutorial",Dock=DockStyle.Right,Width=140};skip.Click+=(_,_)=>FinishGuide();guidePanel.Controls.Add(skip);
        var demo=new ActionButton{Text="Demonstração sem enviar",Dock=DockStyle.Left,Width=240,Primary=true};demo.Click+=(_,_)=>{status.Text=guideStep==3?"Demonstração: os dois aparelhos aceitam a sessão. Nenhum aparelho foi conectado.":"Demonstração: lote selecionado, pedido aceito e arquivos abertos. Nenhum arquivo foi enviado.";Guide(GuideEvents[guideStep]);};guidePanel.Controls.Add(demo);demo.Visible=guideStep>=3;
        Controls.Add(guidePanel);guidePanel.BringToFront();allowed.FirstOrDefault()?.Focus();foreach(var c in allowed){guideColors[c]=c.BackColor;c.BackColor=Palette.Field;if(c is ActionButton button){guidePrimary[button]=button.Primary;button.Primary=true;button.Invalidate();}}foreach(var c in allowed)c.AccessibleDescription="Etapa atual do tutorial: "+GuideMessages[guideStep];applyingGuide=false;
    }
    public void PreviewTutorial(){ShowSection(true);DemoReceiverPreview();BeginTutorial();}
    public async Task VerifyTutorial(string destination)
    {
        BeginTutorial();if(guideStep!=0||choose.Enabled||pairButton.Enabled)throw new Exception("Tutorial did not gate actions");receiveTab.PerformClick();if(guideStep!=1||!receiveView.chooseFolder.Enabled)throw new Exception("Connection step failed");Guide("folder");receiveView.StartForCheck(Path.Combine(destination,"received"));for(int i=0;i<100&&guideStep==2;i++)await Task.Delay(20);if(guideStep!=3)throw new Exception("Real receiver activation did not advance tutorial");
        if(receiveView.Pairing.Peer!=null)throw new Exception("Unexpected peer before demonstration");guidePanel!.Controls.OfType<ActionButton>().Single(b=>b.Text=="Demonstração sem enviar").PerformClick();if(guideStep!=4||receiveView.Pairing.Peer!=null)throw new Exception("Demo created a peer");sendTab.PerformClick();string sample=Path.Combine(destination,"demo.txt");File.WriteAllText(sample,"tutorial");AddFiles(new[]{sample});if(guideStep!=6||!send.Enabled)throw new Exception("Real selection did not advance");guidePanel!.Controls.OfType<ActionButton>().Single(b=>b.Text=="Demonstração sem enviar").PerformClick();if(guideStep!=-1||!File.Exists(TutorialPath)||Directory.GetFiles(Path.Combine(destination,"received")).Length!=0)throw new Exception("Tour completion or no-send guarantee failed");
        receiveView.Stop();for(int i=0;i<100&&receiveView.IsListening;i++)await Task.Delay(20);using var reopened=new MainForm(true,tutorialDirectory);reopened.Location=new Point(-30000,-30000);reopened.Show();Application.DoEvents();if(reopened.guideStep!=-1)throw new Exception("Tour repeated after completion");reopened.BeginTutorial();if(reopened.guideStep!=0)throw new Exception("Help could not reopen tutorial");reopened.guidePanel!.Controls.OfType<ActionButton>().Single(b=>b.Text=="Pular tutorial").PerformClick();if(reopened.guideStep!=-1)throw new Exception("Skip failed");reopened.Close();
    }}




