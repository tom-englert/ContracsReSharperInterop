namespace ContracsReSharperInterop.Test
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;

    public class CodeFixVerifier : Framework.CodeFixVerifier
    {
        protected class DiagnosticResult : Framework.DiagnosticResult
        {
            public DiagnosticResult(int line, int column, string elementName)
            {
                Id = "ContracsReSharperInterop";
                Message = $"Element '{elementName}' has a not-null contract but does not have a corresponding [NotNull] attribute.";
                Severity = DiagnosticSeverity.Warning;
                Locations = new[] { new Framework.DiagnosticResultLocation("Test0.cs", line, column) };
            }
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new ContracsReSharperInteropCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ContracsReSharperInteropAnalyzer();
        }

    }
}
