using System.Text.RegularExpressions;

namespace BatteryMonitor.Fetchers;

public sealed class RazerMouse : BatteryLevelFetcher
{
    const string LogFileLocation = @"\Razer\RazerAppEngine\User Data\Logs\background-manager.log";

    public override bool IsActive => true;
    public override int BatteryPercentage => ReadFromLogFile();

    private static int ReadFromLogFile()
    {
        var logFilePath = GetLogFilePath();

        if (!File.Exists(logFilePath))
            throw new FileNotFoundException("Log file not found.");

        // Safely read the lines in readonly mode
        string[] lines;
        using (FileStream fs = new(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (StreamReader reader = new(fs))
        {
            var allLines = reader.ReadToEnd();
            lines = allLines.Split(["\r\n", "\n"], StringSplitOptions.None);
        }

        // Reverse the list of lines and find the first one that contains the NoCharge_BatteryFull
        var lastEntry = lines.Reverse().FirstOrDefault(x => x.Contains("NoCharge_BatteryFull")) ?? string.Empty;
        var match = Regex.Match(lastEntry, "(?<=NoCharge_BatteryFull\\\\\",\\\\\"level\\\\\":)([0-9]*)(?=})");
        if (match.Success && int.TryParse(match.Groups[1].Value, out int batteryLevel))
        {
            return batteryLevel;
        }
        throw new Exception("No battery levels found");
    }

    private static string GetLogFilePath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Join(localAppData, LogFileLocation);
    }
}
