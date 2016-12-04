namespace ContracsReSharperInterop.Test
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;

    public class ContractForNotNullCodeFixVerifier : Framework.CodeFixVerifier
    {
        protected class DiagnosticResult : Framework.DiagnosticResult
        {
            public DiagnosticResult(int line, int column, string elementName)
            {
                Id = ContractForNotNullAnalyzer.DiagnosticId;
                Message = $"Element '{elementName}' has a [NotNull] attribute but does not have a corresponding not-null contract.";
                Severity = DiagnosticSeverity.Warning;
                Locations = new[] { new Framework.DiagnosticResultLocation("Test0.cs", line, column) };
            }
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new ContractForNotNullCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ContractForNotNullAnalyzer();
        }
    }
}
