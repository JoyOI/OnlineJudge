namespace JoyOI.OnlineJudge.Models
{
    public enum JudgeResult { Accepted, PresentationError, WrongAnswer, OutputLimitExceeded, TimeLimitExceeded, MemoryLimitExceeded, RuntimeError, CompileError, SystemError, Hacked, Running, Pending, Hidden };
    public enum HackResult { Success, Failure, BadData, DatamakerError, SystemError, Running, Pending };
}
