using System.Text.RegularExpressions;

namespace BatteryMonitor.Fetchers;

public sealed class RazerMouse : BatteryLevelFetcher
{
    const string LogFileLocation = @"\Razer\RazerAppEngine\User Data\Logs\";

    public override bool IsActive => true;
    public override int BatteryPercentage => ReadFromLogFiles();

    private static int ReadFromLogFiles()
    {
        var logFilePath = GetLogFilePath();
        var logFiles = Directory.EnumerateFiles(logFilePath, "*.log", SearchOption.TopDirectoryOnly)
            .Select(file => new FileInfo(file))
            .OrderByDescending(fileInfo => fileInfo.LastWriteTime)
            .ToList();

        foreach (var logFile in logFiles)
        {
            try
            {
                return ReadFromLogFile(logFile.FullName);
            }
            catch(Exception)
            {

            }
        }
        throw new Exception("No battery levels found in any log files");
    }

    private static int ReadFromLogFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Log file not found.");

        // Safely read the lines in readonly mode
        string[] lines;
        using (FileStream fs = new(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (StreamReader reader = new(fs))
        {
            var allLines = reader.ReadToEnd();
            lines = allLines.Split(["\r\n", "\n"], StringSplitOptions.None);
        }

        // Reverse the list of lines and find the first one that contains the NoCharge_BatteryFull
        var lastEntry = lines.Reverse().FirstOrDefault(x => x.Contains("NoCharge_BatteryFull") || x.Contains("chargingStatus\\\":\\\"Charging")) ?? string.Empty;

        var notchargingMatch = Regex.Match(lastEntry, "(?<=NoCharge_BatteryFull\\\\\",\\\\\"level\\\\\":)([0-9]*)(?=})");
        if (notchargingMatch.Success && int.TryParse(notchargingMatch.Groups[1].Value, out int batteryLevel))
        {
            return batteryLevel;
        }

        var chargingMatch = Regex.Match(lastEntry, "(?<=Charging\\\\\",\\\\\"level\\\\\":)([0-9]*)(?=})");
        if (chargingMatch.Success && int.TryParse(chargingMatch.Groups[1].Value, out int chargingLevel))
        {
            return chargingLevel;
        }
        throw new Exception("No battery levels found");
    }

    private static string GetLogFilePath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Join(localAppData, LogFileLocation);
    }
}
