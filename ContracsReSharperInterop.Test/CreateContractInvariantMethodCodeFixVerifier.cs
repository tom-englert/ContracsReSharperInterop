namespace ContracsReSharperInterop.Test
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;

    public class CreateContractInvariantMethodCodeFixVerifier : Framework.CodeFixVerifier
    {
        protected class DiagnosticResult : Framework.DiagnosticResult
        {
            public DiagnosticResult(int line, int column, string elementName)
            {
                Id = CreateContractInvariantMethodAnalyzer.DiagnosticId;
                Message = $"Class '{elementName}' has [NotNull] annotations on fields but no contract invariant method.";
                Severity = DiagnosticSeverity.Warning;
                Locations = new[] { new Framework.DiagnosticResultLocation("Test0.cs", line, column) };
            }
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new CreateContractInvariantMethodCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CreateContractInvariantMethodAnalyzer();
        }
    }
}
