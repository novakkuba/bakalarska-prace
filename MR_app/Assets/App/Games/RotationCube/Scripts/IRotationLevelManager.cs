public interface IRotationLevelManager
{
    void CheckAnswerButtonClicked(RotationButtonTrigger button);
    void LogEvent(string eventType, string details);
}