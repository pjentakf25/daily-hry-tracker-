namespace DailyHryTracker;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Podproces pro okno s webem hry: DailyHryTracker --web <id-hry>
        if (args.Length >= 2 && args[0] == WebGames.Arg)
        {
            WebGames.RunWindow(args[1]);
            return;
        }
        using var app = new TrackerApp();
        app.Run();
    }
}
