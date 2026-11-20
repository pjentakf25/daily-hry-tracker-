using System.Runtime.InteropServices;
using DailyHryTracker.Data;
using Microsoft.Xna.Framework;

namespace DailyHryTracker.Core;

/// <summary>
/// Webový prohlížeč vložený přímo do hlavního okna aplikace (nad plochu, kterou kreslí MonoGame).
/// macOS: systémový WKWebView, Windows: WebView2. Na ostatních systémech není k dispozici.
/// </summary>
public abstract class EmbeddedWeb
{
    public string Error { get; protected set; }

    /// <summary>Vytvoří prohlížeč pro tento systém, nebo null, když vložení neumíme.</summary>
    public static EmbeddedWeb Create(GameWindow window)
    {
        try
        {
            if (OperatingSystem.IsMacOS()) return new MacWebView(window);
            if (OperatingSystem.IsWindows()) return new WinWebView(window);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("Vestavěný prohlížeč: " + e);
        }
        return null;
    }

    public static bool Supported => OperatingSystem.IsMacOS() || OperatingSystem.IsWindows();

    /// <summary>Zobrazí web v obdélníku (souřadnice okna aplikace, počátek vlevo nahoře).</summary>
    public abstract void Open(string url, Rectangle area);
    public abstract void Reload();
    public abstract void GoBack();
    public abstract void Close();
    /// <summary>Volá se každý snímek (Windows potřebuje zpracovat asynchronní dokončení).</summary>
    public virtual void Pump() { }

    protected static IntPtr NativeWindow(GameWindow w) => Sdl.NativeWindow(w.Handle);
}

// =====================================================================================
//  macOS – WKWebView přes Objective-C runtime
// =====================================================================================

sealed class MacWebView : EmbeddedWeb
{
    const string ObjC = "/usr/lib/libobjc.A.dylib";

    [StructLayout(LayoutKind.Sequential)]
    struct CGRect
    {
        public double X, Y, W, H;
        public CGRect(double x, double y, double w, double h) { X = x; Y = y; W = w; H = h; }
    }

    [DllImport(ObjC)] static extern IntPtr objc_getClass(string name);
    [DllImport(ObjC)] static extern IntPtr sel_registerName(string name);
    [DllImport(ObjC)] static extern IntPtr objc_autoreleasePoolPush();
    [DllImport(ObjC)] static extern void objc_autoreleasePoolPop(IntPtr pool);
    [DllImport(ObjC, EntryPoint = "objc_msgSend")] static extern IntPtr Send(IntPtr r, IntPtr s);
    [DllImport(ObjC, EntryPoint = "objc_msgSend")] static extern IntPtr Send(IntPtr r, IntPtr s, IntPtr a);
    [DllImport(ObjC, EntryPoint = "objc_msgSend")] static extern IntPtr Send(IntPtr r, IntPtr s, IntPtr a, IntPtr b);
    [DllImport(ObjC, EntryPoint = "objc_msgSend")] static extern IntPtr Send(IntPtr r, IntPtr s, IntPtr a, IntPtr b, IntPtr c);
    [DllImport(ObjC, EntryPoint = "objc_msgSend")] static extern IntPtr SendBool(IntPtr r, IntPtr s, bool a);
    [DllImport(ObjC, EntryPoint = "objc_msgSend")] static extern IntPtr SendRect(IntPtr r, IntPtr s, CGRect rect);
    [DllImport(ObjC, EntryPoint = "objc_msgSend")] static extern IntPtr SendRectObj(IntPtr r, IntPtr s, CGRect rect, IntPtr a);
    [DllImport(ObjC, EntryPoint = "objc_msgSend")] static extern IntPtr SendLong(IntPtr r, IntPtr s, long a);
    [DllImport(ObjC, EntryPoint = "objc_msgSend")] static extern IntPtr SendObjLong(IntPtr r, IntPtr s, IntPtr a, long b);

    [DllImport("/usr/lib/libSystem.dylib")] static extern IntPtr dlopen(string path, int mode);

    static IntPtr Sel(string s) => sel_registerName(s);
    static IntPtr Cls(string s) => objc_getClass(s);

    static IntPtr NSStr(string s)
    {
        var p = Marshal.StringToCoTaskMemUTF8(s);
        try { return Send(Cls("NSString"), Sel("stringWithUTF8String:"), p); }
        finally { Marshal.FreeCoTaskMem(p); }
    }

    readonly IntPtr _window;
    IntPtr _web;
    static bool _menuDone;

    public MacWebView(GameWindow w)
    {
        dlopen("/System/Library/Frameworks/WebKit.framework/WebKit", 1);
        if (Cls("WKWebView") == IntPtr.Zero) throw new InvalidOperationException("WebKit není k dispozici");
        _window = NativeWindow(w);
        EnsureEditMenu();
    }

    CGRect ToNative(Rectangle r)
    {
        // contentView macOS má počátek vlevo dole
        return new CGRect(r.X, Ui.Height - r.Bottom, r.Width, r.Height);
    }

    public override void Open(string url, Rectangle area)
    {
        var pool = objc_autoreleasePoolPush();
        try
        {
            Close();
            var cfg = Send(Send(Cls("WKWebViewConfiguration"), Sel("alloc")), Sel("init"));
            // trvalé úložiště (cookies, přihlášení, statistiky webů)
            Send(cfg, Sel("setWebsiteDataStore:"), Send(Cls("WKWebsiteDataStore"), Sel("defaultDataStore")));
            _web = SendRectObj(Send(Cls("WKWebView"), Sel("alloc")), Sel("initWithFrame:configuration:"), ToNative(area), cfg);
            Send(cfg, Sel("release"));
            Send(_web, Sel("setCustomUserAgent:"),
                NSStr("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.0 Safari/605.1.15"));
            SendBool(_web, Sel("setAllowsBackForwardNavigationGestures:"), true);

            var content = Send(_window, Sel("contentView"));
            Send(content, Sel("addSubview:"), _web);

            var nsurl = Send(Cls("NSURL"), Sel("URLWithString:"), NSStr(url));
            var req = Send(Cls("NSURLRequest"), Sel("requestWithURL:"), nsurl);
            Send(_web, Sel("loadRequest:"), req);
            Send(_window, Sel("makeFirstResponder:"), _web);
            Error = null;
        }
        catch (Exception e)
        {
            Error = e.Message;
        }
        finally
        {
            objc_autoreleasePoolPop(pool);
        }
    }

    public override void Reload()
    {
        if (_web != IntPtr.Zero) Send(_web, Sel("reload"));
    }

    public override void GoBack()
    {
        if (_web != IntPtr.Zero) Send(_web, Sel("goBack"));
    }

    public override void Close()
    {
        if (_web == IntPtr.Zero) return;
        var pool = objc_autoreleasePoolPush();
        Send(_web, Sel("stopLoading"));
        Send(_web, Sel("removeFromSuperview"));
        Send(_web, Sel("release"));
        _web = IntPtr.Zero;
        // klávesnice zpátky aplikaci
        Send(_window, Sel("makeFirstResponder:"), Send(_window, Sel("contentView")));
        objc_autoreleasePoolPop(pool);
    }

    /// <summary>SDL nemá menu Úpravy, bez něj ve webu nefunguje Cmd+C / Cmd+V / Cmd+A.</summary>
    static void EnsureEditMenu()
    {
        if (_menuDone) return;
        _menuDone = true;
        var pool = objc_autoreleasePoolPush();
        try
        {
            var app = Send(Cls("NSApplication"), Sel("sharedApplication"));
            var mainMenu = Send(app, Sel("mainMenu"));
            if (mainMenu == IntPtr.Zero) return;
            var edit = Send(Send(Cls("NSMenu"), Sel("alloc")), Sel("initWithTitle:"), NSStr("Úpravy"));
            (string title, string sel, string key)[] items =
            {
                ("Zpět", "undo:", "z"), ("Vyjmout", "cut:", "x"), ("Kopírovat", "copy:", "c"),
                ("Vložit", "paste:", "v"), ("Vybrat vše", "selectAll:", "a"),
            };
            foreach (var (title, sel, key) in items)
            {
                var item = Send(Send(Cls("NSMenuItem"), Sel("alloc")), Sel("initWithTitle:action:keyEquivalent:"),
                    NSStr(title), Sel(sel), NSStr(key));
                Send(edit, Sel("addItem:"), item);
                Send(item, Sel("release"));
            }
            var holder = Send(Send(Cls("NSMenuItem"), Sel("alloc")), Sel("init"));
            Send(holder, Sel("setSubmenu:"), edit);
            SendObjLong(mainMenu, Sel("insertItem:atIndex:"), holder, 1);
            Send(holder, Sel("release"));
            Send(edit, Sel("release"));
        }
        finally
        {
            objc_autoreleasePoolPop(pool);
        }
    }
}

// =====================================================================================
//  Windows – WebView2 (Edge) jako podokno okna SDL
// =====================================================================================

sealed class WinWebView : EmbeddedWeb
{
    /// <summary>Kontext, který dokončení asynchronních volání WebView2 spouští v hlavní smyčce hry.</summary>
    sealed class FrameContext : SynchronizationContext
    {
        readonly Queue<(SendOrPostCallback, object)> _q = new();
        public override void Post(SendOrPostCallback d, object state)
        {
            lock (_q) _q.Enqueue((d, state));
        }
        public void Run()
        {
            while (true)
            {
                (SendOrPostCallback d, object s) item;
                lock (_q)
                {
                    if (_q.Count == 0) return;
                    item = _q.Dequeue();
                }
                item.d(item.s);
            }
        }
    }

    readonly IntPtr _hwnd;
    readonly FrameContext _ctx = new();
    object _controller; // Microsoft.Web.WebView2.Core.CoreWebView2Controller
    object _env;
    int _generation;

    public WinWebView(GameWindow w)
    {
        _hwnd = NativeWindow(w);
    }

    public override void Pump() => _ctx.Run();

    public override void Open(string url, Rectangle area)
    {
        Close();
        Error = null;
        int gen = ++_generation;
        var prev = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(_ctx);
        try
        {
            _ = OpenAsync(url, area, gen);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(prev);
        }
    }

    async Task OpenAsync(string url, Rectangle area, int gen)
    {
        try
        {
            await WinImpl.Open(this, url, area, gen);
        }
        catch (Exception e)
        {
            Error = "WebView2 se nepodařilo spustit: " + e.Message;
        }
    }

    // oddělená třída, aby se typy WebView2 načítaly jen na Windows
    static class WinImpl
    {
        public static async Task Open(WinWebView self, string url, Rectangle area, int gen)
        {
            var dataDir = Path.Combine(SaveStore.Folder, "Web");
            Directory.CreateDirectory(dataDir);
            var env = (Microsoft.Web.WebView2.Core.CoreWebView2Environment)self._env;
            if (env == null)
            {
                env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(null, dataDir);
                self._env = env;
            }
            var ctrl = await env.CreateCoreWebView2ControllerAsync(self._hwnd);
            if (gen != self._generation)
            {
                ctrl.Close(); // mezitím se zavřelo
                return;
            }
            ctrl.Bounds = new System.Drawing.Rectangle(area.X, area.Y, area.Width, area.Height);
            ctrl.IsVisible = true;
            ctrl.CoreWebView2.Navigate(url);
            ctrl.MoveFocus(Microsoft.Web.WebView2.Core.CoreWebView2MoveFocusReason.Programmatic);
            self._controller = ctrl;
        }

        public static void Close(object c) => ((Microsoft.Web.WebView2.Core.CoreWebView2Controller)c).Close();
        public static void Reload(object c) => ((Microsoft.Web.WebView2.Core.CoreWebView2Controller)c).CoreWebView2.Reload();
        public static void Back(object c)
        {
            var w = ((Microsoft.Web.WebView2.Core.CoreWebView2Controller)c).CoreWebView2;
            if (w.CanGoBack) w.GoBack();
        }
    }

    public override void Reload()
    {
        if (_controller != null) WinImpl.Reload(_controller);
    }

    public override void GoBack()
    {
        if (_controller != null) WinImpl.Back(_controller);
    }

    public override void Close()
    {
        _generation++;
        if (_controller == null) return;
        try { WinImpl.Close(_controller); } catch { /* už zavřené */ }
        _controller = null;
    }
}
