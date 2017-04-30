namespace ContracsReSharperInterop
{
    using System.Collections.Immutable;
    using System.Linq;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CreateContractInvariantMethodAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CCRSI_CreateContractInvariantMethod";

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

            var fields = classDeclaration.ChildNodes()
                .OfType<FieldDeclarationSyntax>()
                .Where(f => f.Modifiers.All(m => m.Kind() != SyntaxKind.ReadOnlyKeyword) || f.Modifiers.All(m => m.Kind() != SyntaxKind.StaticKeyword));

            var hasNotNullFields = fields
                .Any(f => f.AttributeLists.ContainsNotNullAttribute());

            if (!hasNotNullFields)
                return;

            var hasInvariantMethod = classDeclaration.ChildNodes()
                .OfType<MethodDeclarationSyntax>()
                .Any(m => m.AttributeLists.ContainsAttribute("ContractInvariantMethod"));

            if (hasInvariantMethod)
                return;

            var identifier = classDeclaration.Identifier;

            context.ReportDiagnostic(Diagnostic.Create(_rule, identifier.GetLocation(), identifier.Text));
        }
    }
}
