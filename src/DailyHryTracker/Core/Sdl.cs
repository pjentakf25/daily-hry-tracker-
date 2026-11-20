using System.Runtime.InteropServices;

namespace DailyHryTracker.Core;

/// <summary>Pár přímých volání SDL2 (knihovna, na které běží MonoGame DesktopGL).</summary>
public static class Sdl
{
    static IntPtr _lib;
    static bool _tried;

    static IntPtr Lib
    {
        get
        {
            if (_tried) return _lib;
            _tried = true;
            string[] names = OperatingSystem.IsWindows() ? new[] { "SDL2", "SDL2.dll" }
                : OperatingSystem.IsMacOS() ? new[] { "libSDL2-2.0.0.dylib", "libSDL2.dylib" }
                : new[] { "libSDL2-2.0.so.0", "libSDL2.so" };
            foreach (var n in names)
                if (NativeLibrary.TryLoad(n, typeof(Sdl).Assembly, null, out _lib)) break;
            return _lib;
        }
    }

    static T Fn<T>(string name) where T : Delegate
    {
        if (Lib == IntPtr.Zero || !NativeLibrary.TryGetExport(Lib, name, out var p)) return null;
        return Marshal.GetDelegateForFunctionPointer<T>(p);
    }

    // ---------------- nativní okno ----------------

    [StructLayout(LayoutKind.Sequential)]
    public struct SysWmInfo
    {
        public byte Major, Minor, Patch;
        public int Subsystem;
        public IntPtr Window; // NSWindow* (Cocoa) / HWND (Windows)
        public IntPtr Extra1, Extra2;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] Padding;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate int GetWmInfoFn(IntPtr window, ref SysWmInfo info);

    /// <summary>NSWindow* na macOS, HWND na Windows.</summary>
    public static IntPtr NativeWindow(IntPtr sdlWindow)
    {
        var fn = Fn<GetWmInfoFn>("SDL_GetWindowWMInfo") ?? throw new InvalidOperationException("SDL2 není k dispozici");
        var info = new SysWmInfo { Major = 2, Minor = 0, Patch = 22, Padding = new byte[64] };
        if (fn(sdlWindow, ref info) == 0 || info.Window == IntPtr.Zero)
            throw new InvalidOperationException("Nepodařilo se získat nativní okno");
        return info.Window;
    }

    // ---------------- kliknutí ----------------
    // Mouse.GetState() vidí jen stav na začátku snímku – rychlé ťuknutí (třeba tap na trackpadu),
    // kdy se tlačítko stiskne i pustí během jednoho snímku, by se ztratilo. Proto stisky
    // zachytáváme přímo z událostí SDL.

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate int EventFilter(IntPtr userdata, IntPtr ev);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void AddEventWatchFn(EventFilter filter, IntPtr userdata);

    static EventFilter _watch; // drží delegát naživu
    static int _leftDowns;

    const uint MouseButtonDown = 0x401;

    public static void WatchClicks()
    {
        if (_watch != null) return;
        var add = Fn<AddEventWatchFn>("SDL_AddEventWatch");
        if (add == null) return;
        _watch = (_, ev) =>
        {
            if ((uint)Marshal.ReadInt32(ev) == MouseButtonDown && Marshal.ReadByte(ev, 16) == 1) // button 1 = levé
                Interlocked.Increment(ref _leftDowns);
            return 1;
        };
        add(_watch, IntPtr.Zero);
    }

    /// <summary>Byl od minulého snímku stisknut levé tlačítko (i když už je zase puštěné)?</summary>
    public static bool TakeLeftDown() => Interlocked.Exchange(ref _leftDowns, 0) > 0;
}
