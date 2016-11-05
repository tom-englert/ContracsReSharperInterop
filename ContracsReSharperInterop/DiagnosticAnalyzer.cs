namespace ContracsReSharperInterop
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    using TomsToolbox.Core;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ContracsReSharperInteropAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ContracsReSharperInterop";

        private const string Title = "Element with not-null contract does not have a corresponding [NotNull] attribute.";
        private const string MessageFormat = "Element '{0}' has a not-null contract but does not have a corresponding [NotNull] attribute.";
        private const string Description = "Elements with not-null contract should have a corresponding [NotNull] attribute.";

        private const string Category = "CodeContracts";

        private static readonly DiagnosticDescriptor _rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, true, Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSemanticModelAction(Analyze);
        }

        private static void Analyze(SemanticModelAnalysisContext context)
        {
            var root = context.SemanticModel.SyntaxTree.GetRoot(context.CancellationToken);

            var invocationExpressionSyntaxNodes = root.DescendantNodesAndSelf()
                .OfType<InvocationExpressionSyntax>()
                .ToArray();

            AnalyzeRequires(context, root, invocationExpressionSyntaxNodes);
            AnalyzeEnsures(context, root, invocationExpressionSyntaxNodes);
        }

        private static void AnalyzeRequires(SemanticModelAnalysisContext context, SyntaxNode root, IEnumerable<InvocationExpressionSyntax> invocationExpressionSyntaxNodes)
        {
            // find all symbols that are part of a ContractRequires(...), e.g. ContractRequires(x != null) => x

            var requiresExpressions = invocationExpressionSyntaxNodes
                .Where(item => context.IsContractExpression(item.Expression as MemberAccessExpressionSyntax, "Requires")); // find all "Contract.Requires(...)" 

            var notNullParameterSymbols = requiresExpressions.GetNotNullIdentifierSyntax<IdentifierNameSyntax>()
                .Select(syntax => context.SemanticModel.GetSymbolInfo(syntax).Symbol); // get the parameter symbol 

            foreach (var notNullParameterSymbol in notNullParameterSymbols)
            {
                var outerMethodSymbol = notNullParameterSymbol.ContainingSymbol as IMethodSymbol;
                if (outerMethodSymbol == null)
                    continue;

                if (outerMethodSymbol.MethodKind == MethodKind.PropertySet)
                {
                    var propertySymbol = outerMethodSymbol.AssociatedSymbol;
                    var propertySyntax = propertySymbol.GetSyntaxNode<PropertyDeclarationSyntax>(root);

                    if (propertySyntax == null)
                        continue;

                    if (propertySyntax.AttributeLists.ContainsNotNullAttribute())
                        continue;

                    var diagnostic = Diagnostic.Create(_rule, propertySymbol.Locations.First(), propertySymbol.Name);
                    context.ReportDiagnostic(diagnostic);
                    continue;
                }

                var parameterSyntax = notNullParameterSymbol.GetSyntaxNode<ParameterSyntax>(root);
                if (parameterSyntax != null)
                {
                    if (parameterSyntax.AttributeLists.ContainsNotNullAttribute())
                        continue;

                    // now found ContractRequires.. without corresponding NotNull....
                    var diagnostic = Diagnostic.Create(_rule, parameterSyntax.GetLocation(), notNullParameterSymbol.Name);

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private static void AnalyzeEnsures(SemanticModelAnalysisContext context, SyntaxNode root, IEnumerable<InvocationExpressionSyntax> invocationExpressionSyntaxNodes)
        {
            // find all symbols that are part of a ContractRequires(...), e.g. ContractRequires(x != null) => x

            var ensuresExpressions = invocationExpressionSyntaxNodes
                .Where(item => context.IsContractExpression(item.Expression as MemberAccessExpressionSyntax, "Ensures")) // find all "Contract.Ensures(...)" 
                .Where(item => (item.GetNotNullIdentifierSyntax<InvocationExpressionSyntax>().Expression as MemberAccessExpressionSyntax)?.Name.Identifier.Text == "Result")
                .ToArray();

            foreach (var ensuresExpression in ensuresExpressions)
            {
                var outerMember = ensuresExpression.Ancestors().OfType<MemberDeclarationSyntax>().FirstOrDefault();

                outerMember.TryCast()
                    .When<MethodDeclarationSyntax>(syntax =>
                    {
                        if (!syntax.AttributeLists.ContainsNotNullAttribute())
                            context.ReportDiagnostic(Diagnostic.Create(_rule, syntax.Identifier.GetLocation(), syntax.Identifier.Text));
                    })
                    .When<PropertyDeclarationSyntax>(syntax =>
                    {
                        if (!syntax.AttributeLists.ContainsNotNullAttribute())
                            context.ReportDiagnostic(Diagnostic.Create(_rule, syntax.Identifier.GetLocation(), syntax.Identifier.Text));
                    });
            }
        }
    }
}
