namespace ContracsReSharperInterop
{
    using System;
    using System.Collections.Immutable;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Linq.Expressions;

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

            var requiresNodes = invocationExpressionSyntaxNodes
                .Select(item => new { Node = item, Expression = item.Expression as MemberAccessExpressionSyntax })
                .Where(item => item.Expression?.Name.ToString() == "Requires")
                .ToArray();

            foreach (var requiresNode in requiresNodes)
            {
                var contractRequiresExpression = requiresNode.Expression;
                var contractRequiresArguments = requiresNode.Node.ArgumentList.Arguments;

                if (contractRequiresArguments.Count != 1)
                    continue;

                string parameterName;
                ParameterSyntax parameterSyntax;

                if (GetNotNullParameter(context, root, contractRequiresExpression, contractRequiresArguments, out parameterName, out parameterSyntax))
                {
                    if (parameterSyntax.AttributeLists.Any(attr => attr.Attributes.Any(a => (a.Name as IdentifierNameSyntax)?.Identifier.Text.Equals("NotNull") == true)))
                        continue;

                    // now found ContractRequires.. without corresponding NotNull....
                    var diagnostic = Diagnostic.Create(_rule, parameterSyntax.GetLocation(), parameterName);

                    context.ReportDiagnostic(diagnostic);
                }



            }
        }

        private static bool GetNotNullParameter(SemanticModelAnalysisContext context, SyntaxNode root, 
            ExpressionSyntax contractRequiresExpression, SeparatedSyntaxList<ArgumentSyntax> arguments, 
            out string argumentName, out ParameterSyntax parameter)
        {
            argumentName = null;
            parameter = null;

            var argumentExpression = arguments.Single().Expression;

            var binaryArgumentExpression = argumentExpression as BinaryExpressionSyntax;
            if (binaryArgumentExpression?.Kind() == SyntaxKind.NotEqualsExpression)
                return GetNotNullParameterOfBinaryNotNullComparison(context, root, contractRequiresExpression, binaryArgumentExpression, ref argumentName, ref parameter);

            var unaryArgumentExpression = argumentExpression as PrefixUnaryExpressionSyntax;
            if (unaryArgumentExpression?.Kind() == SyntaxKind.LogicalNotExpression)
            {
                var invocationExpressionSyntax = unaryArgumentExpression.Operand as InvocationExpressionSyntax;
                if (invocationExpressionSyntax?.Expression.ToString().Equals("string.IsNullOrEmpty", StringComparison.OrdinalIgnoreCase) == true)
                {
                    var arguments1 = invocationExpressionSyntax.ArgumentList.Arguments;
                    if (arguments1.Count == 1)
                    {
                        var argument = arguments1.Single().Expression;

                        return GetParameterAndNameForArgument(context, root, contractRequiresExpression, argument, out argumentName, out parameter);
                    }
                }
            }

            return false;
        }

        private static bool GetNotNullParameterOfBinaryNotNullComparison(SemanticModelAnalysisContext context, SyntaxNode root, ExpressionSyntax contractRequiresExpression, BinaryExpressionSyntax argumentExpression, ref string argumentName, ref ParameterSyntax parameter)
        {
            var argumentSyntax = GetArgumentSyntaxOfNotNullArgument(argumentExpression);
            if (argumentSyntax == null)
                return false;

            return GetParameterAndNameForArgument(context, root, contractRequiresExpression, argumentSyntax, out argumentName, out parameter);
        }

        private static bool GetParameterAndNameForArgument(SemanticModelAnalysisContext context, SyntaxNode root, ExpressionSyntax contractRequiresExpression, ExpressionSyntax argumentSyntax, out string argumentName, out ParameterSyntax parameter)
        {
            argumentName = null;
            parameter = null;

            var methodSymbol = context.SemanticModel.GetSymbolInfo(contractRequiresExpression).Symbol as IMethodSymbol;
            if (methodSymbol?.ToString().StartsWith("System.Diagnostics.Contracts.Contract.Requires", StringComparison.Ordinal) != true)
                return false;

            var argumentSymbol = context.SemanticModel.GetSymbolInfo(argumentSyntax).Symbol as IParameterSymbol;
            if (argumentSymbol == null)
                return false;

            var outerMethodSymbol = argumentSymbol.ContainingSymbol as IMethodSymbol;

            var outerMethodSyntax = outerMethodSymbol?.Locations
                .Where(l => l.IsInSource)
                .Select(l => l.SourceSpan)
                .Select(s => root.FindNode(s))
                .FirstOrDefault() as BaseMethodDeclarationSyntax;

            var parameters = outerMethodSyntax?.ParameterList.Parameters;

            parameter = parameters?.FirstOrDefault(p => p.Identifier.Text.Equals(argumentSymbol.Name));

            argumentName = argumentSymbol.Name;

            return parameter != null;
        }

        private static ExpressionSyntax GetArgumentSyntaxOfNotNullArgument(BinaryExpressionSyntax argumentExpression)
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
