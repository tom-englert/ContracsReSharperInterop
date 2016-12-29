namespace ContracsReSharperInterop.Test
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;

    public class CreateContractClassCodeFixVerifier : Framework.CodeFixVerifier
    {
        protected class DiagnosticResult : Framework.DiagnosticResult
        {
            public DiagnosticResult(int line, int column, string elementName)
            {
                Id = CreateContractClassAnalyzer.DiagnosticId;
                Message = $"Interface or abstract class '{elementName}' has [NotNull] annotations but no contract class.";
                Severity = DiagnosticSeverity.Warning;
                Locations = new[] { new Framework.DiagnosticResultLocation("Test0.cs", line, column) };
            }
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new CreateContractClassCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CreateContractClassAnalyzer();
        }
    }
}
