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
    internal class CreateContractClassAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ContracsReSharperInterop_CreateContractClass";

        private const string Title = "Missing Contract Class.";
        private const string MessageFormat = "Interface or abstract class '{0}' has [NotNull] annotations but no contract class.";
        private const string Description = "The Interface or abstract class should have contract class.";

        private const string Category = "CodeContracts";

        private static readonly DiagnosticDescriptor _rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, true, Description, Utils.HelpLinkUri);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(_rule);

        public override void Initialize(AnalysisContext context)
        {
            if (context == null)
                return;

            context.RegisterSyntaxNodeAction(AnalyzeClass, SyntaxKind.ClassDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeInterface, SyntaxKind.InterfaceDeclaration);
        }

        private static void AnalyzeClass(SyntaxNodeAnalysisContext context)
        {
            var classDeclaration = context.Node as ClassDeclarationSyntax;
            if (classDeclaration == null)
                return;

            if (!classDeclaration.IsAbstractMember())
                return;

            if (classDeclaration.AttributeLists.ContainsAttribute("ContractClass"))
                return;

            if (!classDeclaration.DescendantNodes().Where(node => node.IsAbstractMember()).Any(HasNotNullAnntotations))
                return;

            var identifier = classDeclaration.Identifier;

            context.ReportDiagnostic(Diagnostic.Create(_rule, identifier.GetLocation(), identifier.Text));
        }

        private static void AnalyzeInterface(SyntaxNodeAnalysisContext context)
        {
            var interfaceDeclaration = context.Node as InterfaceDeclarationSyntax;
            if (interfaceDeclaration == null)
                return;

            if (interfaceDeclaration.AttributeLists.ContainsAttribute("ContractClass"))
                return;

            if (!interfaceDeclaration.DescendantNodes().Any(HasNotNullAnntotations))
                return;

            var identifier = interfaceDeclaration.Identifier;

            context.ReportDiagnostic(Diagnostic.Create(_rule, identifier.GetLocation(), identifier.Text));
        }

        private static bool HasNotNullAnntotations(SyntaxNode node)
        {
            return node?.TryCast().Returning<bool>()
                .When<MethodDeclarationSyntax>(m => m.AttributeLists.ContainsNotNullAttribute() || m.ParameterList.Parameters.Any(p => p.AttributeLists.ContainsNotNullAttribute()))
                .When<PropertyDeclarationSyntax>(p => p.AttributeLists.ContainsNotNullAttribute())
                .Result ?? false;
        }
    }
}
