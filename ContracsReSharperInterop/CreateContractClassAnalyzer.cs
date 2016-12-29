namespace ContracsReSharperInterop
{
    using System.Collections.Immutable;
    using System.Linq;

    using Microsoft.CodeAnalysis;
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
            context?.RegisterSemanticModelAction(Analyze);
        }

        private static void Analyze(SemanticModelAnalysisContext context)
        {
            var root = context.SemanticModel?.SyntaxTree?.GetRoot(context.CancellationToken);
            if (root == null)
                return;

            var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
                .Where(ExtensionMethods.IsAbstractMember)
                .Where(declaration => !declaration.AttributeLists.ContainsAttribute("ContractClass"))
                .Where(declaration => declaration.DescendantNodes().Where(ExtensionMethods.IsAbstractMember).Any(HasNotNullAnntotations));

            foreach (var classDeclaration in classDeclarations)
            {
                context.ReportDiagnostic(Diagnostic.Create(_rule, classDeclaration.GetLocation(), classDeclaration.Identifier.Text));
            }

            var interfaceDeclarations = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>()
                .Where(declaration => !declaration.AttributeLists.ContainsAttribute("ContractClass"))
                .Where(declaration => declaration.DescendantNodes().Any(HasNotNullAnntotations));

            foreach (var interfaceDeclaration in interfaceDeclarations)
            {
                context.ReportDiagnostic(Diagnostic.Create(_rule, interfaceDeclaration.GetLocation(), interfaceDeclaration.Identifier.Text));
            }
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
