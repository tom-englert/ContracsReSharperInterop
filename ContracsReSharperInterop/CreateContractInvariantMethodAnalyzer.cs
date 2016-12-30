using System.Diagnostics.Contracts;
namespace ContracsReSharperInterop
{
    using System.Collections.Immutable;
    using System.Linq;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    using TomsToolbox.Core;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CreateContractInvariantMethodAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ContracsReSharperInterop_CreateContractInvariantMethod";

        private const string Title = "Missing Contract Invariant Method.";
        private const string MessageFormat = "Class '{0}' has [NotNull] annotations on fields but no contract invariant method.";
        private const string Description = "The class should have a contract invariant method.";

        private const string Category = "CodeContracts";

        private static readonly DiagnosticDescriptor _rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, true, Description, Utils.HelpLinkUri);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(_rule);

        public override void Initialize(AnalysisContext context)
        {
            context?.RegisterSyntaxNodeAction(AnalyzeClass, SyntaxKind.ClassDeclaration);
        }

        private static void AnalyzeClass(SyntaxNodeAnalysisContext context)
        {
            var classDeclaration = context.Node as ClassDeclarationSyntax;
            if (classDeclaration == null)
                return;

            var hasNotNullFields = classDeclaration.ChildNodes().OfType<FieldDeclarationSyntax>().Any(f => f.AttributeLists.ContainsNotNullAttribute());

            var hasInvariantMethod = classDeclaration.ChildNodes().OfType<MethodDeclarationSyntax>().Any(m => m.AttributeLists.ContainsAttribute("ContractInvariantMethod"));

            if (hasNotNullFields && !hasInvariantMethod)
            {
                context.ReportDiagnostic(Diagnostic.Create(_rule, classDeclaration.GetLocation(), classDeclaration.Identifier.Text));
            }
        }
    }
}
