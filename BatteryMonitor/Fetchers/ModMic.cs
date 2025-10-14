using Microsoft.Win32;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Diagnostics;
using System.Text.Json;
using System.Timers;

namespace BatteryMonitor.Fetchers;

public sealed class ModMic : BatteryLevelFetcher, IDisposable
{
    private const double BatteryLifeHours = 8.0;
    private const double DrainPerSecond = 100.0 / (BatteryLifeHours * 3600.0); // 8 hours = 36000 seconds
    private const string SaveFilePath = @"C:\BatteryMonitor\MicBatteryState.json";

    private System.Timers.Timer timer;
    private MMDeviceEnumerator enumerator;
    private MMDevice microphone;
    private WasapiCapture? capture;

    public MicrophoneInfo MicInfo { get; private set; }
    public override bool IsActive => MicInfo.IsInUse;
    public override int BatteryPercentage => Convert.ToInt32(MicInfo.EstimatedBattery);

    public ModMic()
    {
        MicInfo = LoadBatteryState();

        enumerator = new MMDeviceEnumerator();
        var modmic = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).Where(x => x.DeviceFriendlyName.Contains("Antlion Wireless Microphone")).FirstOrDefault();
        if (modmic == null)
        {
            Debug.WriteLine("Could not find modmic");
            return;
        }
        microphone = modmic;

        // Force access to AudioMeterInformation to ensure it supports it
        var _ = microphone.AudioMeterInformation;

        // Just so we can constently keep the mic awake
        capture = new WasapiCapture();
        capture.DataAvailable += (_, e) => { /* ignore data */ };
        capture.StartRecording();

        // Start recording in case there is no active consumer yet
        var waveIn = new WasapiLoopbackCapture();
        waveIn.StartRecording();

        // Need to do something with microphone otherwise weird errors
        Debug.WriteLine(microphone.DeviceFriendlyName);

        timer = new System.Timers.Timer(1000); // tick every second
        timer.Elapsed += Timer_Elapsed;
        timer.Start();

        SystemEvents.SessionEnding += (object sender, SessionEndingEventArgs e) => SaveBatteryState(MicInfo.EstimatedBattery);
    }

    public override void StartedCharging()
    {
        MicInfo = new()
        {
            EstimatedBattery = 100,
            IsInUse = false,
        };
    }

    private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        var micLevel = microphone.AudioMeterInformation.MasterPeakValue;
        MicInfo.IsInUse = MicActivity.IsMicConsistentlyOn(micLevel);
        Debug.WriteLine($"Level: {micLevel} Active: {micLevel > 0.0005f} Consistently: {MicInfo.IsInUse}");

        if (MicInfo.IsInUse)
        {
            MicInfo.EstimatedBattery -= DrainPerSecond;
            if (MicInfo.EstimatedBattery < 0) MicInfo.EstimatedBattery = 0;
        }
    }

    private static void SaveBatteryState(double estimatedBattery)
    {
        try
        {
            var state = new BatteryState { EstimatedBattery = estimatedBattery, Timestamp = DateTime.Now };
            string json = JsonSerializer.Serialize(state);
            Directory.CreateDirectory(Path.GetDirectoryName(SaveFilePath)!);
            File.WriteAllText(SaveFilePath, json);
        }
        catch { /* ignore errors */ }
    }

    private static MicrophoneInfo LoadBatteryState()
    {
        double? estimatedBattery = null;
        try
        {
            if (File.Exists(SaveFilePath))
            {
                string json = File.ReadAllText(SaveFilePath);
                var state = JsonSerializer.Deserialize<BatteryState>(json);
                estimatedBattery = state?.EstimatedBattery;
            }
        }
        catch { /* ignore errors */ }

        return new()
        {
            EstimatedBattery = estimatedBattery ?? 100,
            IsInUse = false,
        };
    }

    public void Dispose()
    {
        capture?.Dispose();
    }
}
public class BatteryState
{
    public double EstimatedBattery { get; set; }
    public DateTime Timestamp { get; set; }
}

public class MicrophoneInfo
{
    public required double EstimatedBattery { get; set; }
    public required bool IsInUse { get; set; }
}

public static class MicActivity
{
    public static int HistorySize => 10;
    private static readonly List<double> History = [];
    private static double ActiveVolumeThreshhold => 0.0005f;

    public static bool IsMicConsistentlyOn(double currentVolume)
    {
        History.Add(currentVolume);
        if (History.Count > HistorySize)
        {
            History.RemoveRange(0, History.Count - (HistorySize + 1));
        }

        var averageVolume = History.Average();
        return averageVolume > ActiveVolumeThreshhold;
    }
}