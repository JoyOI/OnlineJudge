namespace JoyOI.OnlineJudge.Models
{
    public static class Constants
    {
        // Languages
        public const string C = "C";
        public const string Cxx = "C++";
        public const string Python = "Python";
        public const string CSharp = "C#";
        public const string Pascal = "Pascal";
        public const string Nodejs = "JavaScript";
        public const string Java = "Java";
        public const string FSharp = "F#";
        public const string VB = "VB.NET";
        public static readonly string[] SupportedLanguages = new[] { C, Cxx, CSharp, Pascal, Java, VB, FSharp, Python };
        public static readonly string[] UnsupportedLanguages = new[] { Nodejs };
        public static readonly JudgeResult[] HackInvalidResults = new[] { JudgeResult.CompileError, JudgeResult.SystemError, JudgeResult.Pending, JudgeResult.Running };
        public static string GetSourceExtension(string language)
        {
            switch (language)
            {
                case "C":
                    return ".c";
                case "C++":
                    return ".cpp";
                case "Python":
                    return ".py";
                case "C#":
                    return ".cs";
                case "Pascal":
                    return ".pas";
                case "Node.js":
                    return ".js";
                case "Java":
                    return ".java";
                case "F#":
                    return ".fs";
                case "VB.NET":
                    return ".vb";
                default:
                    return null;
            }
        }
        public static string GetBinaryExtension(string language)
        {
            switch (language)
            {
                case "C":
                case "C++":
                case "Pascal":
                    return ".out";
                case "Python":
                    return ".py";
                case "C#":
                case "F#":
                case "VB.NET":
                    return ".dll";
                case "Node.js":
                    return ".js";
                case "Java":
                    return ".class";
                default:
                    return ".out";
            }
        }

        // Claims
        public const string MasterOrHigherRoles = "Root, Master";
        public const string ProblemEditPermission = "Edit Problem";
        public const string ContestEditPermission = "Edit Contest";
        public const string GroupEditPermission = "Edit Group";

        // StateMachine
        public const string CompileOnlyStateMachine = "CompileValidatorStateMachine";

        // Coins field
        public const string CoinField = "Coin";
    }
}
