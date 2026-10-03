using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        using var form = new MainForm();
        int navigationCheck=Array.IndexOf(args,"--verify-navigation");
        if(navigationCheck>=0 && navigationCheck+1<args.Length)
        {
            string destination=Path.GetFullPath(args[navigationCheck+1]);Directory.CreateDirectory(destination);int result=0;
            form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-30000,-30000);
            form.Shown+=async(_,_)=>{try{await form.VerifyNavigation(destination);File.WriteAllText(Path.Combine(destination,"result.txt"),"PASS single main window; tab state retained; receiver active while sending tab visible; modal acceptance; two files received; stop releases listener.");}catch(Exception e){result=1;File.WriteAllText(Path.Combine(destination,"result.txt"),e.ToString());}finally{form.Close();}};
            Application.Run(form);return result;
        }
        if (args.Contains("--verify-startup"))
        {
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA) throw new InvalidOperationException("A interface precisa iniciar em STA.");
            _ = form.Handle;
            return 0;
        }
        int receiverPreview=Array.IndexOf(args,"--render-receiver-preview");
        if(receiverPreview>=0 && receiverPreview+1<args.Length){form.ShowSection(true);form.DemoReceiverPreview();form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-30000,-30000);form.Show();Application.DoEvents();using var rendered=new Bitmap(form.Width,form.Height);form.DrawToBitmap(rendered,new Rectangle(Point.Empty,form.Size));rendered.Save(Path.GetFullPath(args[receiverPreview+1]));form.Close();return 0;}
        int preview = Array.IndexOf(args, "--render-preview");
        if (preview >= 0 && preview + 1 < args.Length)
        {
            form.AddFiles(args.Skip(preview + 2).ToArray());
            form.StartPosition = FormStartPosition.Manual; form.Location = new Point(-30000, -30000);
            form.Show(); Application.DoEvents();
            using var full = new Bitmap(form.Width, form.Height);
            form.DrawToBitmap(full, new Rectangle(Point.Empty, form.Size));
            full.Save(Path.GetFullPath(args[preview + 1]));
            form.Close(); return 0;
        }
        Application.Run(form); return 0;
    }
}

internal static class Palette
{
    public static readonly Color Background = Color.FromArgb(12, 24, 37);
    public static readonly Color Panel = Color.FromArgb(24, 41, 57);
    public static readonly Color Field = Color.FromArgb(31, 50, 68);
    public static readonly Color Border = Color.FromArgb(53, 77, 98);
    public static readonly Color Text = Color.FromArgb(239, 245, 251);
    public static readonly Color Muted = Color.FromArgb(163, 183, 203);
    public static readonly Color Accent = Color.FromArgb(18, 224, 222);
    public static GraphicsPath Round(RectangleF r, float radius)
    {
        var path = new GraphicsPath(); float d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
        path.AddArc(r.X, r.Y, d, d, 180, 90); path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); path.AddArc(r.X, r.Bottom - d, d, d, 90, 90); path.CloseFigure(); return path;
    }
    public static string Size(long bytes) => bytes >= 1073741824 ? $"{bytes / 1073741824.0:0.##} GB" : bytes >= 1048576 ? $"{bytes / 1048576.0:0.##} MB" : bytes >= 1024 ? $"{bytes / 1024.0:0.##} KB" : $"{bytes} B";
}

internal static class AppIdentity
{
    public static readonly Icon Icon = LoadIcon();
    public static readonly Bitmap Mark = LoadMark();
    static Icon LoadIcon()
    {
        using var stream = typeof(Program).Assembly.GetManifestResourceStream("DropLocal.AppIcon")
            ?? throw new InvalidOperationException("Ícone do aplicativo não encontrado.");
        using var source = new Icon(stream);
        return (Icon)source.Clone();
    }
    static Bitmap LoadMark()
    {
        using var stream = typeof(Program).Assembly.GetManifestResourceStream("DropLocal.AppMark")
            ?? throw new InvalidOperationException("Marca do aplicativo não encontrada.");
        using var source = new Bitmap(stream);
        return new Bitmap(source);
    }
}

internal sealed class Card : Panel
{
    [System.ComponentModel.DefaultValue(false)] public bool Dashed { get; set; }
    public Card() { DoubleBuffered = true; BackColor = Palette.Background; Padding = new Padding(18); }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        if (Width < 4 || Height < 4) return;
        using var path = Palette.Round(new RectangleF(1, 1, Width - 3, Height - 3), 14);
        using var fill = new SolidBrush(Palette.Panel); using var pen = new Pen(Palette.Border) { DashStyle = Dashed ? DashStyle.Dash : DashStyle.Solid };
        e.Graphics.FillPath(fill, path); e.Graphics.DrawPath(pen, path);
    }
}

internal sealed class ActionButton : Button
{
    [System.ComponentModel.DefaultValue(false)] public bool Primary { get; set; }
    [System.ComponentModel.DefaultValue(false)] public bool Danger { get; init; }
    bool hover;
    public ActionButton() { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true); Cursor = Cursors.Hand; FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0; Height = 42; Font = new Font("Segoe UI", 10, FontStyle.Bold); }
    protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; e.Graphics.Clear(Parent?.BackColor ?? Palette.Background);
        using var shape = Palette.Round(new RectangleF(1, 1, Width - 3, Height - 3), 9);
        var fg = !Enabled ? Palette.Muted : Primary ? Palette.Background : Danger ? Color.FromArgb(255, 112, 128) : Palette.Accent;
        var bg = !Enabled ? Palette.Field : Primary ? (hover ? Color.FromArgb(93, 243, 237) : Palette.Accent) : hover ? Palette.Field : Palette.Panel;
        using var brush = new SolidBrush(bg); using var pen = new Pen(!Enabled ? Palette.Border : Primary ? bg : fg);
        e.Graphics.FillPath(brush, shape); e.Graphics.DrawPath(pen, shape);
        TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, fg, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        if (Focused) { using var focus = new Pen(fg) { DashStyle = DashStyle.Dot }; e.Graphics.DrawRectangle(focus, 5, 5, Width - 11, Height - 11); }
    }
}

internal sealed class TransferProgress : Control
{
    int value;
    [System.ComponentModel.DefaultValue(0)] public int Value { get => value; set { this.value = Math.Clamp(value, 0, 100); Invalidate(); } }
    public TransferProgress() { DoubleBuffered = true; Height = 10; }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var track = Palette.Round(new RectangleF(0, 0, Width, Height), 5); using var bg = new SolidBrush(Palette.Field); e.Graphics.FillPath(bg, track);
        if (value > 0) { using var fill = Palette.Round(new RectangleF(0, 0, Math.Max(10, Width * value / 100f), Height), 5); using var accent = new SolidBrush(Palette.Accent); e.Graphics.FillPath(accent, fill); }
    }
}

internal sealed class InputFrame : Panel
{
    public InputFrame() { DoubleBuffered = true; BackColor = Palette.Panel; }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var shape = Palette.Round(new RectangleF(1, 1, Width - 3, Height - 3), 8);
        using var fill = new SolidBrush(Palette.Field); using var border = new Pen(Palette.Border);
        e.Graphics.FillPath(fill, shape); e.Graphics.DrawPath(border, shape);
    }
}

internal sealed class WifiBadge : Control
{
    public WifiBadge() { Size = new Size(54, 54); DoubleBuffered = true; }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        e.Graphics.DrawImage(AppIdentity.Mark, ClientRectangle);
    }
}

internal sealed class MainForm : Form
{
    readonly TextBox address = new() { PlaceholderText = "192.168.1.20", AccessibleName = "IP do Android" };
    readonly TextBox token = new() { PlaceholderText = "Código de oito dígitos", AccessibleName = "Código de sessão", MaxLength = 8 };
    readonly ListBox list = new() { DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 50, BorderStyle = BorderStyle.None, BackColor = Palette.Background, ForeColor = Palette.Text, IntegralHeight = false, AccessibleName = "Arquivos selecionados" };
    readonly ActionButton send = new() { Text = "↑   Enviar arquivos", Primary = true, Dock = DockStyle.Fill };
    readonly ActionButton cancel = new() { Text = "Cancelar", Danger = true, Enabled = false, Width = 105 };
    readonly ActionButton choose = new() { Text = "Selecionar arquivos", Width = 200 };
    readonly ActionButton clear = new() { Text = "Limpar lista", Width = 110, Height = 30 };
    readonly TransferProgress progress = new() { Dock = DockStyle.Fill, Visible = false };
    readonly Label status = Label("Escolha os arquivos e informe os dados do Android.", 9, Palette.Muted);
    readonly Label summary = Label("Nenhum arquivo selecionado", 10, Palette.Muted);
    readonly Label connection = Label("Use os dados exibidos no aplicativo Android", 9, Palette.Muted);
    CancellationTokenSource? active;
    readonly ReceiveView receiveView;
    readonly Panel pages = new() { Dock = DockStyle.Fill, BackColor = Palette.Background, Margin = Padding.Empty };
    readonly ActionButton sendTab = new() { Text = "↑   Enviar", Dock = DockStyle.Fill, Primary = true };
    readonly ActionButton receiveTab = new() { Text = "↓   Receber / QR", Dock = DockStyle.Fill };
    Control sendPage = null!;
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
    public MainForm()
    {
        Text = "Drop Local"; Icon = AppIdentity.Icon; BackColor = Palette.Background; ForeColor = Palette.Text; Font = new Font("Segoe UI", 10);
        ClientSize = new Size(740, 820); MinimumSize = new Size(720, 790); StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        var shell = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 3, BackColor = Palette.Background };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));shell.RowStyles.Add(new RowStyle(SizeType.Absolute,76));shell.RowStyles.Add(new RowStyle(SizeType.Absolute,56));shell.RowStyles.Add(new RowStyle(SizeType.Percent,100));Controls.Add(shell);
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = Padding.Empty, Margin = Padding.Empty, ColumnCount = 1, RowCount = 7, BackColor = Palette.Background };
        sendPage=root;
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        foreach (var height in new float[] { 0, 160, 152, 36 }) root.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        shell.Controls.Add(pages,0,2);pages.Controls.Add(root);
        receiveView=new ReceiveView((ip,code)=>{address.Text=ip.ToString();token.Text=code;connection.Text="Pareado • pronto para enviar um pedido";}) { Dock=DockStyle.Fill,Visible=false };
        pages.Controls.Add(receiveView);
        var navigation=new TableLayoutPanel { Dock=DockStyle.Fill,Margin=new Padding(0,0,0,12),ColumnCount=2,RowCount=1,BackColor=Palette.Background };
        navigation.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));navigation.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));navigation.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        sendTab.Margin=new Padding(0,0,6,0);receiveTab.Margin=new Padding(6,0,0,0);navigation.Controls.Add(sendTab,0,0);navigation.Controls.Add(receiveTab,1,0);shell.Controls.Add(navigation,0,1);
        sendTab.Click+=(_,_)=>ShowSection(false);receiveTab.Click+=(_,_)=>ShowSection(true);
        receiveView.ListeningChanged+=listening=>{receiveTab.Text=listening?"↓   Receber / QR • ativo":"↓   Receber / QR";receiveTab.Invalidate();};
        var header = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty, BackColor = Palette.Background };
        header.Controls.Add(new WifiBadge { Location = new Point(0, 0) });
        header.Controls.Add(Label("Drop Local", 22, Palette.Text, true, new Point(70, -2)));
        header.Controls.Add(Label("Seus arquivos, pela sua rede · 0.5.1", 10, Palette.Muted, false, new Point(72, 38)));
        var credit=Label("by Joseph David",9,Palette.Muted);credit.Anchor=AnchorStyles.Top|AnchorStyles.Right;header.Controls.Add(credit);header.Resize+=(_,_)=>credit.Location=new Point(header.ClientSize.Width-credit.Width,16);shell.Controls.Add(header, 0, 0);
        var connect = new Card { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 12) };
        var connectContent = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, BackColor = Palette.Panel };
        connectContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); connectContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        connectContent.RowStyles.Add(new RowStyle(SizeType.Absolute, 25)); connectContent.RowStyles.Add(new RowStyle(SizeType.Absolute, 25)); connectContent.RowStyles.Add(new RowStyle(SizeType.Absolute, 22)); connectContent.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var title = Label("Conectar ao celular", 12, Palette.Text, true); connectContent.Controls.Add(title, 0, 0); connectContent.SetColumnSpan(title, 2);
        connection.Dock = DockStyle.Fill; connectContent.Controls.Add(connection, 0, 1); connectContent.SetColumnSpan(connection, 2);
        connectContent.Controls.Add(Label("IP do Android", 9, Palette.Text), 0, 2); connectContent.Controls.Add(Label("Código de sessão", 9, Palette.Text), 1, 2);
        connectContent.Controls.Add(Field(address), 0, 3); connectContent.Controls.Add(Field(token), 1, 3); connect.Controls.Add(connectContent); root.Controls.Add(connect, 0, 1);
        var drop = new Card { Dock = DockStyle.Fill, Dashed = true, Margin = new Padding(0, 0, 0, 8) };
        var dropContent = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, BackColor = Palette.Panel };
        dropContent.RowStyles.Add(new RowStyle(SizeType.Absolute, 26)); dropContent.RowStyles.Add(new RowStyle(SizeType.Absolute, 24)); dropContent.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var dragTitle = Label("↑   Arraste seus arquivos aqui", 13, Palette.Text, true); dragTitle.Dock = DockStyle.Fill; dragTitle.TextAlign = ContentAlignment.MiddleCenter;
        var hint = Label("ou clique para selecionar", 9, Palette.Muted); hint.Dock = DockStyle.Fill; hint.TextAlign = ContentAlignment.MiddleCenter;
        var centered = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = Palette.Panel, FlowDirection = FlowDirection.LeftToRight };
        centered.Controls.Add(choose); centered.Resize += (_, _) => centered.Padding = new Padding(Math.Max(0, (centered.Width - choose.Width) / 2), 4, 0, 0);
        dropContent.Controls.Add(dragTitle, 0, 0); dropContent.Controls.Add(hint, 0, 1); dropContent.Controls.Add(centered, 0, 2); drop.Controls.Add(dropContent); root.Controls.Add(drop, 0, 2);
        var listHeader = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty, BackColor = Palette.Background };
        summary.Dock = DockStyle.Fill; clear.Dock = DockStyle.Right; listHeader.Controls.Add(summary); listHeader.Controls.Add(clear); root.Controls.Add(listHeader, 0, 3);
        list.Dock = DockStyle.Fill; list.Margin = new Padding(0, 0, 0, 8); root.Controls.Add(list, 0, 4); root.Controls.Add(send, 0, 5); send.Margin = new Padding(0, 4, 0, 8);
        var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Margin = Padding.Empty, BackColor = Palette.Background };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 115)); footer.RowStyles.Add(new RowStyle(SizeType.Absolute, 44)); footer.RowStyles.Add(new RowStyle(SizeType.Absolute, 10));
        status.Dock = DockStyle.Fill; status.AutoSize = false; status.TextAlign = ContentAlignment.MiddleLeft; footer.Controls.Add(status, 0, 0); footer.Controls.Add(cancel, 1, 0); footer.Controls.Add(progress, 0, 1); footer.SetColumnSpan(progress, 2); root.Controls.Add(footer, 0, 6);
        choose.Click += (_, _) => SelectFiles(); clear.Click += (_, _) => { if (active == null) { list.Items.Clear(); UpdateSummary(); } };
        list.DrawItem += DrawFile;
        list.MouseDown += (_, e) => { int index = list.IndexFromPoint(e.Location); if (active == null && index >= 0 && e.X > list.ClientSize.Width - 36) { list.Items.RemoveAt(index); UpdateSummary(); } };
        list.KeyDown += (_, e) => { if (e.KeyCode == Keys.Delete && active == null && list.SelectedIndex >= 0) { list.Items.RemoveAt(list.SelectedIndex); UpdateSummary(); } };
        RegisterDrop(this); RegisterDrop(root); RegisterDrop(drop); RegisterDrop(dropContent); RegisterDrop(dragTitle); RegisterDrop(hint); RegisterDrop(centered); RegisterDrop(list);
        cancel.Click += (_, _) => active?.Cancel(); FormClosing += (_, _) => {active?.Cancel();receiveView.Stop();}; send.Click += async (_, _) => await Send();
        Shown += (_, _) => { int dark = 1; DwmSetWindowAttribute(Handle, 20, ref dark, sizeof(int)); };
    }
    public void ShowSection(bool receiving)
    {
        sendPage.Visible=!receiving;receiveView.Visible=receiving;(receiving?(Control)receiveView:sendPage).BringToFront();
        sendTab.Primary=!receiving;receiveTab.Primary=receiving;sendTab.Invalidate();receiveTab.Invalidate();
        sendTab.AccessibleDescription=receiving?"Aba enviar":"Aba enviar selecionada";receiveTab.AccessibleDescription=receiving?"Aba receber selecionada":"Aba receber";
    }
    public void DemoReceiverPreview()=>receiveView.DemoPreview();
    public async Task VerifyNavigation(string destination)
    {
        address.Text="192.168.0.42";token.Text="12345678";ShowSection(true);string code=receiveView.VerificationCode;
        receiveView.StartForCheck(Path.Combine(destination,"received"));ShowSection(false);ShowSection(true);ShowSection(false);
        if(Application.OpenForms.Count!=1||address.Text!="192.168.0.42"||token.Text!="12345678"||receiveView.VerificationCode!=code||!receiveView.IsListening)throw new InvalidOperationException("Navigation did not retain state in the same window.");
        string first=Path.Combine(destination,"sample.bin"),second=Path.Combine(destination,"empty.txt");byte[] bytes=Enumerable.Range(0,70000).Select(n=>(byte)(n%251)).ToArray();File.WriteAllBytes(first,bytes);File.WriteAllBytes(second,[]);
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var accept=new System.Windows.Forms.Timer { Interval=50 };int accepted=0;
        accept.Tick+=(_,_)=>{var dialog=Application.OpenForms.Cast<Form>().FirstOrDefault(f=>f.Text=="Drop Local — autorização");if(dialog!=null){if(dialog.Owner!=this)throw new InvalidOperationException("Approval has a different owner.");var button=dialog.Controls.Cast<Control>().SelectMany(c=>c.Controls.Cast<Control>()).OfType<ActionButton>().First(b=>b.Text=="Aceitar");accepted++;button.PerformClick();}};accept.Start();
        await LocalTransfer.SendAsync(System.Net.IPAddress.Loopback,code,[first,second],(_,_,_)=>{},_=>{},timeout.Token,45833);accept.Stop();
        string[] saved=Directory.GetFiles(Path.Combine(destination,"received"));if(accepted!=1||saved.Length!=2||!saved.Any(p=>File.ReadAllBytes(p).SequenceEqual(bytes))||!saved.Any(p=>new FileInfo(p).Length==0))throw new InvalidOperationException("Integrated receive failed.");
        receiveView.Stop();for(int i=0;i<100&&receiveView.IsListening;i++)await Task.Delay(20);if(receiveView.IsListening)throw new InvalidOperationException("Receiver did not stop.");
    }
    static Label Label(string text, float size, Color color, bool bold = false, Point? location = null) => new() { Text = text, AutoSize = true, ForeColor = color, BackColor = Color.Transparent, Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular), Location = location ?? Point.Empty, Margin = Padding.Empty };
    static Panel Field(TextBox box)
    {
        var panel = new InputFrame { Dock = DockStyle.Fill, Padding = new Padding(10, 7, 10, 7), Margin = new Padding(0, 0, 12, 0) };
        box.BorderStyle = BorderStyle.None; box.BackColor = Palette.Field; box.ForeColor = Palette.Text; box.Font = new Font("Segoe UI", 11); box.Dock = DockStyle.Fill; panel.Controls.Add(box); return panel;
    }
    void SelectFiles() { using var dialog = new OpenFileDialog { Multiselect = true, Title = "Escolher arquivos para o Android" }; if (dialog.ShowDialog(this) == DialogResult.OK) AddFiles(dialog.FileNames); }
    void RegisterDrop(Control control)
    {
        control.AllowDrop = true;
        control.DragEnter += (_, e) => e.Effect = active == null && e.Data?.GetDataPresent(DataFormats.FileDrop) == true ? DragDropEffects.Copy : DragDropEffects.None;
        control.DragDrop += (_, e) => { if (e.Data?.GetData(DataFormats.FileDrop) is string[] paths) AddFiles(paths); };
    }
    public void AddFiles(string[] paths) { if (active != null) return; foreach (var path in paths) if (File.Exists(path) && !list.Items.Contains(path)) list.Items.Add(path); UpdateSummary(); }
    void UpdateSummary()
    {
        long total = list.Items.Cast<string>().Sum(p => { try { return new FileInfo(p).Length; } catch { return 0L; } });
        summary.Text = list.Items.Count == 0 ? "Nenhum arquivo selecionado" : $"{list.Items.Count} arquivo(s) • {Palette.Size(total)}";
        clear.Enabled = list.Items.Count > 0 && active == null;
    }
    void DrawFile(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; using var bg = new SolidBrush(Palette.Background); e.Graphics.FillRectangle(bg, e.Bounds);
        var row = new Rectangle(e.Bounds.X, e.Bounds.Y + 2, e.Bounds.Width - 2, e.Bounds.Height - 5);
        using var shape = Palette.Round(row, 9); using var fill = new SolidBrush((e.State & DrawItemState.Selected) != 0 ? Palette.Field : Palette.Panel); e.Graphics.FillPath(fill, shape);
        var path = (string)list.Items[e.Index]; var ext = Path.GetExtension(path).ToLowerInvariant();
        var color = ext is ".mp4" or ".mov" or ".avi" ? Color.FromArgb(141, 103, 239) : ext is ".mp3" or ".wav" ? Color.FromArgb(239, 100, 148) : Palette.Accent;
        using var iconShape = Palette.Round(new Rectangle(row.X + 10, row.Y + 8, 29, 29), 6); using var iconFill = new SolidBrush(color); e.Graphics.FillPath(iconFill, iconShape);
        TextRenderer.DrawText(e.Graphics, ext is ".mp4" or ".mov" ? "▶" : ext is ".mp3" or ".wav" ? "♪" : "↓", Font, new Rectangle(row.X + 10, row.Y + 8, 29, 29), Palette.Background, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        TextRenderer.DrawText(e.Graphics, Path.GetFileName(path), Font, new Rectangle(row.X + 50, row.Y, Math.Max(0, row.Width - 185), row.Height), Palette.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        string size; try { size = Palette.Size(new FileInfo(path).Length); } catch { size = "Indisponível"; }
        TextRenderer.DrawText(e.Graphics, size, Font, new Rectangle(row.Right - 135, row.Y, 90, row.Height), Palette.Muted, TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
        TextRenderer.DrawText(e.Graphics, "×", Font, new Rectangle(row.Right - 34, row.Y, 28, row.Height), active == null ? Palette.Muted : Palette.Border, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
        if ((e.State & DrawItemState.Focus) != 0) e.DrawFocusRectangle();
    }
    async Task Send()
    {
        if (list.Items.Count == 0 || !System.Net.IPAddress.TryParse(address.Text.Trim(), out var ip) || token.Text.Trim().Length == 0) { status.Text = "Selecione arquivos, informe um IP válido e o código do Android."; return; }
        active = new(); var ct = active.Token; send.Enabled = choose.Enabled = clear.Enabled = address.Enabled = token.Enabled = false; cancel.Enabled = true; progress.Value = 0; progress.Visible = false;
        try
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct); deadline.CancelAfter(TimeSpan.FromMinutes(5));
            await LocalTransfer.SendAsync(ip, token.Text.Trim(), list.Items.Cast<string>().ToArray(),
                (done,total,name) => { if (IsDisposed) return; connection.Text = "Transferindo para o Android"; progress.Visible = true; progress.Value = total == 0 ? 100 : (int)(done * 100.0 / total); status.Text = $"Enviando • {progress.Value}%   {Palette.Size(done)} de {Palette.Size(total)}\n{name}"; },
                text => { if (!IsDisposed) { status.Text = text; connection.Text = text.StartsWith("Aguardando") ? "Pedido enviado • confirme no Android" : "Conectando ao Android…"; } }, deadline.Token);
            if (!IsDisposed) { progress.Visible = true; progress.Value = 100; status.Text = "Concluído • arquivos salvos no Android."; connection.Text = "Envio concluído • conexão encerrada"; }
        }
        catch (OperationCanceledException) { if (!IsDisposed) status.Text = ct.IsCancellationRequested ? "Envio cancelado." : "Tempo limite atingido. Envie lotes menores."; }
        catch (Exception ex) { if (!IsDisposed) status.Text = "Falha: " + ex.Message; }
        finally
        {
            active.Dispose(); active = null;
            if (!IsDisposed) { send.Enabled = choose.Enabled = address.Enabled = token.Enabled = true; cancel.Enabled = false; if (progress.Value != 100) connection.Text = "Informe os dados exibidos no Android para tentar novamente"; UpdateSummary(); list.Invalidate(); }
        }
    }
}
