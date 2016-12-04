namespace ContracsReSharperInterop
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using JetBrains.Annotations;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    using TomsToolbox.Core;

    internal static class ExtensionMethods
    {
        public static bool ContainsNotNullAttribute(this SyntaxList<AttributeListSyntax> attributeLists)
        {
            return attributeLists.ContainsAttribute("NotNull");
        }

        public static bool ContainsAttribute(this SyntaxList<AttributeListSyntax> attributeLists, string attributeName)
        {
            return attributeLists.SelectMany(attr => (attr?.Attributes).GetValueOrDefault())
                .Select(GetAttributeName)
                .Any(name => string.Equals(attributeName, name, StringComparison.Ordinal));
        }

        public static T GetSyntaxNode<T>([NotNull] this SyntaxNode root, ISymbol symbol) where T : SyntaxNode
        {
            return symbol?.Locations
                .Where(l => l?.IsInSource ?? false)
                .Select(l => l?.SourceSpan)
                .WhereItemNotNull()
                .Select(s => root.FindNode(s.GetValueOrDefault()))
                .FirstOrDefault() as T;
        }

        public static bool IsContractExpression(this MemberAccessExpressionSyntax expressionSyntax, ContractCategory category)
        {
            if (expressionSyntax == null)
                return false;

            var expected = new[] { category.ToString(), "Contract" };

            return expressionSyntax.ToString().Split('.').Reverse().Take(2).SequenceEqual(expected);
        }

        [ItemNotNull]
        [NotNull]
        public static IEnumerable<T> GetNotNullIdentifierSyntax<T>([NotNull] this IEnumerable<InvocationExpressionSyntax> nodes)
            where T : ExpressionSyntax
        {
            return nodes.Select(node => node?.GetNotNullIdentifierSyntax<T>())
                .WhereItemNotNull();
        }

        public static T GetNotNullIdentifierSyntax<T>([NotNull] this InvocationExpressionSyntax node)
            where T : ExpressionSyntax
        {
            var arguments = node.ArgumentList.Arguments;

            return arguments.Count == 1 // ContractRequires has just one argument
                ? arguments.Single()?.Expression.GetNotNullArgumentIdentifierSyntax<T>()
                : null;
        }

        private static T GetNotNullArgumentIdentifierSyntax<T>([NotNull] this ExpressionSyntax argumentExpression)
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

            var expressionValue = invocationExpressionSyntax.Expression?.ToString();

            if (!nullStringChecks.Any(item => string.Equals(expressionValue, item, StringComparison.OrdinalIgnoreCase)))
                return null;

            var arguments = invocationExpressionSyntax.ArgumentList?.Arguments ?? new SeparatedSyntaxList<ArgumentSyntax>();
            if (arguments.Count != 1)
                return null;

            return arguments.Single()?.Expression as T;
        }

        private static T GetIdentifyerSyntaxOfNotNullArgument<T>(BinaryExpressionSyntax binaryArgumentExpression)
            where T : ExpressionSyntax
        {
            switch (binaryArgumentExpression?.Kind())
            {
                case SyntaxKind.NotEqualsExpression:
                case SyntaxKind.GreaterThanExpression:
                case SyntaxKind.LessThanExpression:
                    break;

                default:
                    return null;
            }

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

        public static string GetNodeName([NotNull] this TypeSyntax node)
        {
            var nodeName = node.TryCast().Returning<string>()
                // ReSharper disable PossibleNullReferenceException
                .When<IdentifierNameSyntax>(syntax => syntax.Identifier.Text)
                .When<QualifiedNameSyntax>(syntax => syntax.Right.Identifier.Text)
                .When<PredefinedTypeSyntax>(syntax => syntax.Keyword.Text)
                // ReSharper restore PossibleNullReferenceException
                .Else(_ => null);

            return nodeName;
        }

        public static string GetAttributeName(this AttributeSyntax node)
        {
            var nodeName = node?.Name?.GetNodeName();

            if (nodeName == null)
                return null;

            const string attributeKeyName = "Attribute";

            if (nodeName.EndsWith(attributeKeyName, StringComparison.Ordinal))
                nodeName = nodeName.Substring(0, nodeName.Length - attributeKeyName.Length);

            return nodeName;
        }

        public static CompilationUnitSyntax AddUsingDirective([NotNull] this CompilationUnitSyntax root, string usingDirectiveName)
        {
            var usingSyntax = new[] { SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName(usingDirectiveName)) };

            return root.AddUsings(usingSyntax);
        }

        public static bool HasUsingDirective([NotNull] this SyntaxNode root, [NotNull] SyntaxNode item, string usingDirectiveName)
        {
            var ancestors = new HashSet<SyntaxNode>(item.Ancestors());

            var hasUsingDirective = root.DescendantNodes()
                .Where(node => node.Kind() == SyntaxKind.UsingDirective)
                .OfType<UsingDirectiveSyntax>()
                .Where(x => ancestors.Contains(x?.Parent))
                .Any(node => string.Equals(node?.Name.ToString(), usingDirectiveName, StringComparison.Ordinal));

            return hasUsingDirective;
        }

        public static bool HasAttributes([NotNull] this SyntaxNode node)
        {
            return node.TryCast().Returning<bool?>()
                .When<ParameterSyntax>(item => item?.AttributeLists.Any())
                .When<PropertyDeclarationSyntax>(item => item?.AttributeLists.Any())
                .When<MethodDeclarationSyntax>(item => item?.AttributeLists.Any())
                .When<FieldDeclarationSyntax>(item => item?.AttributeLists.Any())
                .When<ClassDeclarationSyntax>(item => item?.AttributeLists.Any())
                .Else(item => null) ?? false;
        }

        public static SyntaxNode WithAttribute([NotNull] this SyntaxNode node, AttributeListSyntax attributeListSyntax)
        {
            return node.TryCast().Returning<SyntaxNode>()
                .When<ParameterSyntax>(item => item.WithAttributeLists(item.AttributeLists.Add(attributeListSyntax)))
                .When<PropertyDeclarationSyntax>(item => item.WithAttributeLists(item.AttributeLists.Add(attributeListSyntax)))
                .When<MethodDeclarationSyntax>(item => item.WithAttributeLists(item.AttributeLists.Add(attributeListSyntax)))
                .When<FieldDeclarationSyntax>(item => item.WithAttributeLists(item.AttributeLists.Add(attributeListSyntax)))
                .When<ClassDeclarationSyntax>(item => item.WithAttributeLists(item.AttributeLists.Add(attributeListSyntax)))
                .Else(item => item);
        }

        public static IPropertySymbol FindDeclaringMemberOnBaseClass(this INamedTypeSymbol baseClass, [NotNull] IPropertySymbol property)
        {
            if (baseClass == null)
                return null;

            if (baseClass.TypeKind == TypeKind.Interface)
            {
                return baseClass.GetMembers().OfType<IPropertySymbol>()
                    .FirstOrDefault(m => property.ContainingType.FindImplementationForInterfaceMember(m)?.Equals(property) == true);
            }

            return baseClass.GetMembers().OfType<IPropertySymbol>().FirstOrDefault(p => PropertySignatureEquals(p, property));
        }

        private static bool PropertySignatureEquals(IPropertySymbol baseProperty, [NotNull] IPropertySymbol property)
        {
            if (!property.IsOverride)
                return false;

            if (baseProperty == null)
                return false;

            if (baseProperty.Name != property.Name)
                return false;

            if (!baseProperty.IsAbstract)
                return false;

            return baseProperty.Type?.Equals(property.Type) ?? false;
        }

        public static IMethodSymbol FindDeclaringMemberOnBaseClass(this INamedTypeSymbol baseClass, [NotNull] IMethodSymbol method)
        {
            if (baseClass == null)
                return null;

            if (baseClass.TypeKind == TypeKind.Interface)
            {
                return baseClass.GetMembers().OfType<IMethodSymbol>()
                    .FirstOrDefault(m => method.ContainingType.FindImplementationForInterfaceMember(m)?.Equals(method) == true);
            }

            return baseClass.GetMembers().OfType<IMethodSymbol>().FirstOrDefault(m => MethodSignatureEquals(m, method));
        }

        private static bool MethodSignatureEquals(IMethodSymbol baseMethod, [NotNull] IMethodSymbol method)
        {
            if (!method.IsOverride)
                return false;

            if (baseMethod == null)
                return false;

            if (baseMethod.Name != method.Name)
                return false;

            if (!baseMethod.IsAbstract)
                return false;

            if (baseMethod.ReturnType?.Equals(method.ReturnType) != true)
                return false;

            if (!baseMethod.TypeArguments.SequenceEqual(method.TypeArguments))
                return false;

            if (!baseMethod.Parameters.Select(p => p?.Type).SequenceEqual(method.Parameters.Select(p => p?.Type)))
                return false;

            return true;
        }

        public static INamedTypeSymbol GetContractClassFor([NotNull] this ISymbol symbol)
        {
            var contractClassForAttribute = symbol
                .ContainingType?
                .GetAttributes()
                .FirstOrDefault(a => a?.AttributeClass.Name == "ContractClassForAttribute");

            var baseClass = contractClassForAttribute?.ConstructorArguments.FirstOrDefault().Value as INamedTypeSymbol;

            if (baseClass?.IsGenericType == true)
            {
                baseClass = symbol.GetBaseClassAndInterfaces()
                    .FirstOrDefault(x => x.IsGenericType
                        && (x.Name == baseClass.Name)
                        && x.TypeArguments.Length == baseClass.TypeArguments.Length);
            }

            return baseClass;
        }

        [ItemNotNull]
        private static IEnumerable<INamedTypeSymbol> GetBaseClassAndInterfaces([NotNull] this ISymbol symbol)
        {
            var type = symbol.ContainingType;

            if (type?.BaseType == null)
                yield break;

            yield return type.BaseType;

            foreach (var @interface in type.Interfaces)
            {
                yield return @interface;
            }
        }

        [ItemNotNull]
        [NotNull]
        public static IEnumerable<T> WhereItemNotNull<T>(this IEnumerable<T> items)
        {
            if (items == null)
                yield break;

            foreach (var item in items)
            {
                if (item == null)
                    continue;

                yield return item;
            }
        }

        public static IParameterSymbol GetAnnotationTargetSymbol([NotNull] this IParameterSymbol parameterSymbol)
        {
            var baseClass = parameterSymbol.GetContractClassFor();
            if (baseClass == null)
                return parameterSymbol;

            var outerMethodSymbol = parameterSymbol.ContainingSymbol as IMethodSymbol;
            if (outerMethodSymbol == null)
                return parameterSymbol;

            var baseMethod = baseClass.FindDeclaringMemberOnBaseClass(outerMethodSymbol);
            if (baseMethod == null)
                return parameterSymbol;

            return baseMethod.Parameters[outerMethodSymbol.Parameters.IndexOf(parameterSymbol)];
        }

        public static bool IsContractResultExpression(this InvocationExpressionSyntax item)
        {
            var expressionSyntax = item.GetNotNullIdentifierSyntax<InvocationExpressionSyntax>()?.Expression as MemberAccessExpressionSyntax;

            return expressionSyntax?.Name?.Identifier.Text == "Result";
        }
    }
}
