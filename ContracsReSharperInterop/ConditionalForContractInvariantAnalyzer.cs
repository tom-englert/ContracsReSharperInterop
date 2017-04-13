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
    internal class ConditionalForContractInvariantAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ContracsReSharperInterop_ConditionalForContractInvariant";

        private const string Title = "Missing [Conditional(\"CONTRACTS_FULL\")] attribute.";
        private const string MessageFormat = "Method '{0}' is the contract invariant method but does not have a [Conditional(\"CONTRACTS_FULL\")] attribute.";
        private const string Description = "The contract invariant method should have a [Conditional(\"CONTRACTS_FULL\")] attribute.";

        private const string Category = "CodeContracts";

        private static readonly DiagnosticDescriptor _rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, true, Description, Utils.HelpLinkUri);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.Attribute);
        }

        private static void Analyze(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node as AttributeSyntax;
            if (node == null)
                return;

            var nodeName = node.GetAttributeName();

            if (!string.Equals("ContractInvariantMethod", nodeName, StringComparison.Ordinal))
                return;

            var method = node.Parent.Parent as MethodDeclarationSyntax;
            if (method == null)
                return;

            if (HasConditionalAttribute(method))
                return;

            context.ReportDiagnostic(Diagnostic.Create(_rule, method.Identifier.GetLocation(), method.Identifier.Text));
        }

        private static bool HasConditionalAttribute(MethodDeclarationSyntax method)
        {
            var conditionalAttributes = method.AttributeLists
                .SelectMany(list => list.Attributes)
                .Where(attr => string.Equals("Conditional", ExtensionMethods.GetAttributeName(attr), StringComparison.Ordinal));

            foreach (var conditionalAttribute in conditionalAttributes)
            {
                var arguments = conditionalAttribute.ArgumentList.Arguments;
                if (arguments.Count != 1)
                    continue;

                var argumentExpression = arguments[0]?.Expression as LiteralExpressionSyntax;
                if (argumentExpression?.Kind() != SyntaxKind.StringLiteralExpression)
                    continue;

                if (argumentExpression.ToString() == "\"CONTRACTS_FULL\"")
                    return true;
            }

            return false;
        }
    }
}
