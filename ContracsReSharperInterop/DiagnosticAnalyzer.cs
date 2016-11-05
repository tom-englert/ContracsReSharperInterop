namespace ContracsReSharperInterop
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

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
                .OfType<InvocationExpressionSyntax>();

            // find all symbols that are part of a ContractRequires(...), e.g. ContractRequires(x != null) => x

            var notNullParameterSymbols = invocationExpressionSyntaxNodes
                .Where(item => IsContractRequiresExpression(context, item.Expression as MemberAccessExpressionSyntax)) // find all "ContractRequires(...)" 
                .Select(node => node.ArgumentList.Arguments) // get the arguments
                .Where(args => args.Count == 1) // ContractRequires has just one argument
                .Select(args => args.Single().Expression) // get the expression of the argument
                .Select(GetNotNullIdentifyerSyntax) // get the identifier syntax of the argument that is part of a not null check
                .Select(syntax => context.SemanticModel.GetSymbolInfo(syntax).Symbol) // get the parameter symbol 
                .OfType<IParameterSymbol>()
                .Where(item => item != null)
                .ToArray();

            foreach (var notNullParameterSymbol in notNullParameterSymbols)
            {
                var outerMethodSymbol = notNullParameterSymbol.ContainingSymbol as IMethodSymbol;
                if (outerMethodSymbol == null)
                    continue;

                if (outerMethodSymbol.MethodKind == MethodKind.PropertySet)
                {
                    var propertySymbol = outerMethodSymbol.AssociatedSymbol;

                    var propertySyntax = propertySymbol?.Locations
                        .Where(l => l.IsInSource)
                        .Select(l => l.SourceSpan)
                        .Select(s => root.FindNode(s))
                        .FirstOrDefault() as PropertyDeclarationSyntax;

                    if (propertySyntax?.AttributeLists.Any(attr => attr.Attributes.Any(a => (a.Name as IdentifierNameSyntax)?.Identifier.Text.Equals("NotNull") == true)) == true)
                        continue;

                    var diagnostic = Diagnostic.Create(_rule, propertySymbol.Locations.First(), propertySymbol.Name);
                    context.ReportDiagnostic(diagnostic);
                    continue;
                }

                var parameterSyntax = notNullParameterSymbol.Locations
                    .Where(l => l.IsInSource)
                    .Select(l => l.SourceSpan)
                    .Select(s => root.FindNode(s))
                    .FirstOrDefault() as ParameterSyntax;

                if (parameterSyntax != null)
                {
                    if (parameterSyntax.AttributeLists.Any(attr => attr.Attributes.Any(a => (a.Name as IdentifierNameSyntax)?.Identifier.Text.Equals("NotNull") == true)))
                        continue;

                    // now found ContractRequires.. without corresponding NotNull....
                    var diagnostic = Diagnostic.Create(_rule, parameterSyntax.GetLocation(), notNullParameterSymbol.Name);

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private static IdentifierNameSyntax GetNotNullIdentifyerSyntax(ExpressionSyntax argumentExpression)
        {
            var binaryArgumentExpression = argumentExpression as BinaryExpressionSyntax;
            if (binaryArgumentExpression?.Kind() == SyntaxKind.NotEqualsExpression)
                return GetArgumentSyntaxOfNotNullArgument(binaryArgumentExpression);

            var unaryArgumentExpression = argumentExpression as PrefixUnaryExpressionSyntax;
            if (unaryArgumentExpression?.Kind() == SyntaxKind.LogicalNotExpression)
                return GetIdentifyerSyntaxOfNotNullStringArgument(unaryArgumentExpression);

            return null;
        }

        private static IdentifierNameSyntax GetIdentifyerSyntaxOfNotNullStringArgument(PrefixUnaryExpressionSyntax unaryArgumentExpression)
        {
            var nullStringChecks = new[] { "string.IsNullOrEmpty", "string.IsNullOrWhitespace" };

            var invocationExpressionSyntax = unaryArgumentExpression.Operand as InvocationExpressionSyntax;
            if (invocationExpressionSyntax == null)
                return null;

            var expressionValue = invocationExpressionSyntax.Expression.ToString();

            if (!nullStringChecks.Any(item => string.Equals(expressionValue, item, StringComparison.OrdinalIgnoreCase)))
                return null;

            var arguments = invocationExpressionSyntax.ArgumentList.Arguments;
            if (arguments.Count != 1)
                return null;

            return arguments.Single().Expression as IdentifierNameSyntax;
        }

        private static bool IsContractRequiresExpression(SemanticModelAnalysisContext context, MemberAccessExpressionSyntax expressionSyntax)
        {
            if (expressionSyntax?.Name.ToString() != "Requires")
                return false;

            var contractRequiresMethodSymbol = context.SemanticModel.GetSymbolInfo(expressionSyntax).Symbol as IMethodSymbol;

            return contractRequiresMethodSymbol?.ToString().StartsWith("System.Diagnostics.Contracts.Contract.Requires", StringComparison.Ordinal) == true;
        }

        private static IdentifierNameSyntax GetArgumentSyntaxOfNotNullArgument(BinaryExpressionSyntax argumentExpression)
        {
            if (argumentExpression.Left.Kind() == SyntaxKind.NullLiteralExpression)
            {
                return argumentExpression.Right as IdentifierNameSyntax;
            }

            if (argumentExpression.Right.Kind() == SyntaxKind.NullLiteralExpression)
            {
                return argumentExpression.Left as IdentifierNameSyntax;
            }

            return null;
        }
    }
}
