using System.Diagnostics;
using System.Text;

namespace CentrED.Utils;

public class Metrics
{
    public Dictionary<string, TimeSpan> Timers = new();
    public Dictionary<string, long> Counters = new();
    private readonly Dictionary<string, long> starts = new();

    public TimeSpan this[string name]
    {
        set => Timers[name] = value;
    }

    public void Start(String name)
    {
        starts[name] = Stopwatch.GetTimestamp();
    }

    public void Stop(String name)
    {
        if (starts.TryGetValue(name, out var start))
            Timers[name] = Stopwatch.GetElapsedTime(start);
    }

    public void Measure(String name, Action callback)
    {
        Start(name);
        callback();
        Stop(name);
    }

    public void SetCounter(string name, long value)
    {
        Counters[name] = value;
    }

    #region frame profiler

    private const int MaxProfileFrames = 30_000;
    private const int FlushEveryFrames = 120;

    private StreamWriter? _profileWriter;
    private List<string>? _profileTimerCols;
    private List<string>? _profileCounterCols;
    private string? _profilePath;
    private long _lastFrameTimestamp;
    private int _profileFrames;

    public bool Profiling => _profileWriter != null;
    public int ProfiledFrames => _profileFrames;
    public string? ProfilePath => _profilePath;

    public void StartProfiling(string path)
    {
        StopProfiling();
        try
        {
            _profileWriter = new StreamWriter(path, append: false);
            _profilePath = path;
            _profileTimerCols = null;
            _profileCounterCols = null;
            _profileFrames = 0;
            _lastFrameTimestamp = 0;
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Metrics] Failed to open profile file {path}: {e.Message}");
            _profileWriter = null;
        }
    }

    public void CaptureFrame()
    {
        if (_profileWriter == null)
            return;

        var now = Stopwatch.GetTimestamp();
        var frameMs = _lastFrameTimestamp == 0
            ? 0.0
            : Stopwatch.GetElapsedTime(_lastFrameTimestamp, now).TotalMilliseconds;
        _lastFrameTimestamp = now;

        if (_profileTimerCols == null)
        {
            _profileTimerCols = Timers.Keys.OrderBy(k => k).ToList();
            _profileCounterCols = Counters.Keys.OrderBy(k => k).ToList();
            var header = new StringBuilder("frame,frameMs");
            foreach (var t in _profileTimerCols)
                header.Append(",t:").Append(t);
            foreach (var c in _profileCounterCols)
                header.Append(",c:").Append(c);
            _profileWriter.WriteLine(header.ToString());
        }

        var line = new StringBuilder();
        line.Append(_profileFrames).Append(',').Append(frameMs.ToString("F3"));
        foreach (var t in _profileTimerCols)
        {
            line.Append(',');
            line.Append(Timers.TryGetValue(t, out var v) ? v.TotalMilliseconds.ToString("F3") : "");
        }
        foreach (var c in _profileCounterCols!)
        {
            line.Append(',');
            line.Append(Counters.TryGetValue(c, out var v) ? v.ToString() : "");
        }
        _profileWriter.WriteLine(line.ToString());

        _profileFrames++;
        if (_profileFrames % FlushEveryFrames == 0)
            _profileWriter.Flush();
        if (_profileFrames >= MaxProfileFrames)
            StopProfiling();
    }

    public void StopProfiling()
    {
        if (_profileWriter != null)
        {
            try
            {
                _profileWriter.Flush();
                _profileWriter.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Metrics] Failed to finalize profile {_profilePath}: {e.Message}");
            }
            _profileWriter = null;
        }
        _profileTimerCols = null;
        _profileCounterCols = null;
    }

    #endregion
}
