public interface ICorsiLevelManager
{
    void PlayerSelectedBlock(int blockID);
    void LogEvent(string eventType, string blockName);
}