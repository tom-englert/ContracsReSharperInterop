namespace ContracsReSharperInterop
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    using TomsToolbox.Core;

    internal static class ExtensionMethods
    {
        public static bool ContainsNotNullAttribute(this SyntaxList<AttributeListSyntax> attributeLists)
        {
            return attributeLists.SelectMany(attr => attr.Attributes)
                .Select(GetAttributeName)
                .Any(name => string.Equals("NotNull", name, StringComparison.Ordinal));
        }

        public static T GetSyntaxNode<T>(this ISymbol symbol, SyntaxNode root) where T : SyntaxNode
        {
            return symbol?.Locations
                .Where(l => l.IsInSource)
                .Select(l => l.SourceSpan)
                .Select(s => root.FindNode(s))
                .FirstOrDefault() as T;
        }

        public static bool IsContractExpression(this SemanticModelAnalysisContext context, MemberAccessExpressionSyntax expressionSyntax, ContractCategory category)
        {
            var name = category.ToString();

            if (expressionSyntax?.Name.ToString() != name)
                return false;

            var contractRequiresMethodSymbol = context.SemanticModel.GetSymbolInfo(expressionSyntax).Symbol as IMethodSymbol;

            return contractRequiresMethodSymbol?.ToString().StartsWith("System.Diagnostics.Contracts.Contract." + name, StringComparison.Ordinal) == true;
        }

        public static IEnumerable<T> GetNotNullIdentifierSyntax<T>(this IEnumerable<InvocationExpressionSyntax> nodes)
            where T : ExpressionSyntax
        {
            return nodes.Select(node => node.GetNotNullIdentifierSyntax<T>())
                .Where(node => node != null); 
        }

        public static T GetNotNullIdentifierSyntax<T>(this InvocationExpressionSyntax node)
            where T : ExpressionSyntax
        {
            var arguments = node.ArgumentList.Arguments;

            return arguments.Count == 1 // ContractRequires has just one argument
                ? arguments.Single().Expression.GetNotNullArgumentIdentifierSyntax<T>() 
                : null;
        }


        public static T GetNotNullArgumentIdentifierSyntax<T>(this ExpressionSyntax argumentExpression)
            where T : ExpressionSyntax
        {
            return argumentExpression.TryCast().Returning<T>()
                .When<BinaryExpressionSyntax>(GetIdentifyerSyntaxOfNotNullArgument<T>)
                .When<PrefixUnaryExpressionSyntax>(GetIdentifyerSyntaxOfNotNullStringArgument<T>)
                .Else(expr => null);
        }

        private static T GetIdentifyerSyntaxOfNotNullStringArgument<T>(PrefixUnaryExpressionSyntax unaryArgumentExpression)
            where T : ExpressionSyntax
        {
            if (unaryArgumentExpression?.Kind() != SyntaxKind.LogicalNotExpression)
                return null;

            var nullStringChecks = new[] { "string.IsNullOrEmpty", "string.IsNullOrWhitespace" };

            var invocationExpressionSyntax = unaryArgumentExpression.Operand as InvocationExpressionSyntax;
            if (invocationExpressionSyntax == null)
                return null;

            var expressionValue = invocationExpressionSyntax.Expression.ToString();

            if (!nullStringChecks.Any(item => String.Equals(expressionValue, item, StringComparison.OrdinalIgnoreCase)))
                return null;

            var arguments = invocationExpressionSyntax.ArgumentList.Arguments;
            if (arguments.Count != 1)
                return null;

            return arguments.Single().Expression as T;
        }

        private static T GetIdentifyerSyntaxOfNotNullArgument<T>(BinaryExpressionSyntax binaryArgumentExpression)
            where T : ExpressionSyntax
        {
            if (binaryArgumentExpression?.Kind() != SyntaxKind.NotEqualsExpression)
                return null;

            if (binaryArgumentExpression.Left.Kind() == SyntaxKind.NullLiteralExpression)
            {
                return binaryArgumentExpression.Right as T;
            }

            if (binaryArgumentExpression.Right.Kind() == SyntaxKind.NullLiteralExpression)
            {
                return binaryArgumentExpression.Left as T;
            }

            return null;
        }

        public static string GetAttributeName(this AttributeSyntax node)
        {
            var nodeName = node.Name.TryCast().Returning<string>()
                .When<IdentifierNameSyntax>(syntax => syntax.Identifier.Text)
                .When<QualifiedNameSyntax>(syntax => syntax.Right.Identifier.Text)
                .Else(_ => null);

            const string attributeKeyName = "Attribute";

            if (nodeName.EndsWith(attributeKeyName, StringComparison.Ordinal))
                nodeName = nodeName.Substring(0, nodeName.Length - attributeKeyName.Length);

            return nodeName;
        }

        public static CompilationUnitSyntax AddUsingDirective(this CompilationUnitSyntax root, string usingDirectiveName)
        {
            var usingSyntax = new[] { SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName(usingDirectiveName)) };

            return root.AddUsings(usingSyntax);
        }

        public static bool HasUsingDirective(this SyntaxNode root, SyntaxNode item, string usingDirectiveName)
        {
            var ancestors = new HashSet<SyntaxNode>(item.Ancestors());

            var hasUsingDirective = root.DescendantNodes()
                .Where(node => node.Kind() == SyntaxKind.UsingDirective)
                .OfType<UsingDirectiveSyntax>()
                .Where(x => ancestors.Contains(x.Parent))
                .Any(node => node.Name.ToString().Equals(usingDirectiveName, StringComparison.Ordinal));

            return hasUsingDirective;
        }

        public static bool HasAttributes(this SyntaxNode node)
        {
            return node.TryCast().Returning<bool>()
                .When<ParameterSyntax>(item => item.AttributeLists.Any())
                .When<PropertyDeclarationSyntax>(item => item.AttributeLists.Any())
                .When<MethodDeclarationSyntax>(item => item.AttributeLists.Any())
                .When<FieldDeclarationSyntax>(item => item.AttributeLists.Any())
                .When<ClassDeclarationSyntax>(item => item.AttributeLists.Any())
                .Else(item => false);
        }
    }
}
