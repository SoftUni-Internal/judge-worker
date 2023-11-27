namespace OJS.Workers.Common.Models
{
    using System.ComponentModel;

    public enum PlagiarismDetectorType
    {
        [Description("C# code")]
        CSharpCompileDisassemble = 1,

        [Description("Java code")]
        JavaCompileDisassemble = 2,

        [Description("Text")]
        PlainText = 3,

        [Description("C# code (.NET Core)")]
        CSharpDotNetCoreCompileDisassemble = 4,
    }
}