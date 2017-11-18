namespace JoyOI.OnlineJudge.WebApi.Models
{
    public static class Constants
    {
        // Languages
        public const string C = "C";
        public const string Cxx = "C++";
        public const string Cxx11 = "C++ 11";
        public const string Cxx14 = "C++ 14";
        public const string Python = "Python";
        public const string CSharp = "C#";
        public const string Pascal = "Pascal";
        public const string Nodejs = "Node.js";
        public const string Java = "Java";
        public static readonly string[] CompileNeededLanguages = new[] { C, Cxx, Cxx11, Cxx14, CSharp, Pascal, Java };
        public static readonly string[] ScriptLanguages = new[] { Python, Nodejs };
        public static string GetExtension(string language)
        {
            switch (language)
            {
                case "C":
                    return ".c";
                case "C++":
                    return ".cpp";
                case "C++ 11":
                    return "11.cpp";
                case "C++ 14":
                    return "14.cpp";
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
                default:
                    return null;
            }
        }

        // Claims
        public const string MasterOrHigherRoles = "Root, Master";
        public const string ProblemEditPermission = "Edit Problem";
        public const string ContestEditPermission = "Edit Contest";
        public const string GroupEditPermission = "Edit Group";

        // StateMachine
        public const string CompileOnlyStateMachine = "CompileValidatorStateMachine";
    }
}
