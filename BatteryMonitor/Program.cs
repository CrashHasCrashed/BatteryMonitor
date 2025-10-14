using BatteryMonitor.Fetchers;
using BatteryMonitor.TrayIcons;
using BatteryMonitor.Warnings;

namespace BatteryMonitor;

static class Program
{
    private static Dictionary<string, BatteryLevelFetcher> BatteryLevelFetchers = [];

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var mouseFetcher = new RazerMouse();
        using var micFetcher = new ModMic();
        BatteryLevelFetchers = new()
        {
            { "Death Adder", mouseFetcher },
            { "ModMic", micFetcher },
        };

        _ = new OnDisabledWarning(micFetcher, 50);

        using var trayManager = new TrayIconManager(BatteryLevelFetchers);
        Application.Run();
    }
}
