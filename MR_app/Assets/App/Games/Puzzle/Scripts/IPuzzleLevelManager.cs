public interface IPuzzleLevelManager
{
    void PiecePlaced();
    void LogEvent(string eventType, string pieceName);
}