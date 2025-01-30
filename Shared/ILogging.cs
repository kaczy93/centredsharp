namespace CentrED;

public interface ILogging
{
    void LogInfo(string message);

    void LogWarn(string message);

    void LogError(string message);

    void LogDebug(string message);
}