using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

internal static class Program
{
    [STAThread] static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        var modes=new[]{"--verify-simple","--verify-stability","--verify-nearby","--verify-navigation"};string? mode=modes.FirstOrDefault(args.Contains);int index=mode==null?-1:Array.IndexOf(args,mode);
        using var form=new MainForm(args.Length==0,index>=0&&index+1<args.Length?Path.Combine(Path.GetFullPath(args[index+1]),"preferences"):null);
        if(index>=0&&index+1<args.Length){string root=Path.GetFullPath(args[index+1]);Directory.CreateDirectory(root);int result=0;form.Location=new Point(-30000,-30000);form.Shown+=async(_,_)=>{try{if(mode=="--verify-nearby")await form.VerifyNearbyUi(root);else if(mode=="--verify-stability")await form.VerifyStability(root);else await form.VerifySimpleReceive(root);File.WriteAllText(Path.Combine(root,"result.txt"),"PASS "+mode+": native single screen; automatic receiving without sender selection; stable device updates; per-lot consent and exact bytes.");}catch(Exception e){result=1;File.WriteAllText(Path.Combine(root,"result.txt"),e.ToString());}finally{form.Close();}};Application.Run(form);return result;}
        if(args.Contains("--verify-startup")){if(Thread.CurrentThread.GetApartmentState()!=ApartmentState.STA)throw new Exception("UI must run in STA");_ = form.Handle;return 0;}
        int preview=Array.FindIndex(args,a=>a=="--render-preview"||a=="--render-receiver-preview");if(preview>=0&&preview+1<args.Length){form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-30000,-30000);form.Show();form.PreviewNearby();Application.DoEvents();using var bitmap=new Bitmap(form.Width,form.Height);form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,form.Size));bitmap.Save(Path.GetFullPath(args[preview+1]));form.Close();return 0;}
        if(args.Length>0)return 2;
        Application.Run(form);return 0;
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

