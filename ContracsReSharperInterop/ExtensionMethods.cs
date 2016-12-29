#pragma warning disable ContracsReSharperInterop_ContractForNotNull // Element with [NotNull] attribute does not have a corresponding not-null contract.

namespace ContracsReSharperInterop
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using JetBrains.Annotations;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

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
            return symbol?.DeclaringSyntaxReferences.FirstOrDefault(r => r.SyntaxTree.GetRoot() == root)?.GetSyntax() as T;
        }

        public static bool IsContractExpression(this ExpressionSyntax expressionSyntax, ContractCategory category)
        {
            return (expressionSyntax as MemberAccessExpressionSyntax)?.GetContractCategory() == category;
        }

        public static ContractCategory GetContractCategory(this MemberAccessExpressionSyntax expressionSyntax)
        {
            var parts = expressionSyntax?.ToString()?.Split('.').Reverse().Take(2).ToArray();

            if (parts?.Length != 2 || (parts[1] != "Contract"))
                return ContractCategory.Unknown;

            ContractCategory result;

            return Enum.TryParse(parts[0], out result) ? result : ContractCategory.Unknown;
        }

        public static bool IsContractResultExpression(this InvocationExpressionSyntax item)
        {
            var expressionSyntax = item?.GetNotNullArgumentIdentifierSyntax<InvocationExpressionSyntax>()?.Expression as MemberAccessExpressionSyntax;

            return expressionSyntax?.Name?.Identifier.Text == "Result";
        }

        [NotNull, ItemNotNull]
        public static IEnumerable<IdentifierNameSyntax> GetNotNullArgumentIdentifierSyntaxNodes([NotNull] this IEnumerable<InvocationExpressionSyntax> nodes)
        {
            return nodes.Select(node => node?.GetNotNullArgumentIdentifierSyntax<IdentifierNameSyntax>())
                .Where(item => item != null);
        }

        public static T GetNotNullArgumentIdentifierSyntax<T>([NotNull] this InvocationExpressionSyntax node)
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
                .When<BinaryExpressionSyntax>(GetNotNullArgumentIdentifyerSyntax<T>)
                .When<PrefixUnaryExpressionSyntax>(GetNotNullStringArgumentIdentifyerSyntax<T>)
                .Else(expr => null);
        }

        private static T GetNotNullStringArgumentIdentifyerSyntax<T>(PrefixUnaryExpressionSyntax unaryArgumentExpression)
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

        private static T GetNotNullArgumentIdentifyerSyntax<T>(BinaryExpressionSyntax binaryArgumentExpression)
            where T : ExpressionSyntax
        {
            if (binaryArgumentExpression == null)
                return null;

            // if the identifier is part of any member access expression, e.g. arg.Length > 0, it must be != null!
            if (binaryArgumentExpression.Left.Kind() == SyntaxKind.SimpleMemberAccessExpression)
            {
                var syntax = (MemberAccessExpressionSyntax)binaryArgumentExpression.Left;
                return syntax.Expression as T;
            }

            if (binaryArgumentExpression.Right.Kind() == SyntaxKind.SimpleMemberAccessExpression)
            {
                var syntax = (MemberAccessExpressionSyntax)binaryArgumentExpression.Right;
                return syntax.Expression as T;
            }

            switch (binaryArgumentExpression.Kind())
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

        public static IPropertySymbol FindImplementingMemberOnDerivedClass(this INamedTypeSymbol derivedClass, [NotNull] IPropertySymbol property)
        {
            if (derivedClass == null)
                return null;

            if (property.ContainingType?.TypeKind == TypeKind.Interface)
            {
                return derivedClass.FindImplementationForInterfaceMember(property) as IPropertySymbol;
            }

            return derivedClass.GetMembers().OfType<IPropertySymbol>().FirstOrDefault(p => PropertySignatureEquals(p, property));
        }

        public static IMethodSymbol FindImplementingMemberOnDerivedClass(this INamedTypeSymbol derivedClass, [NotNull] IMethodSymbol method)
        {
            if (derivedClass == null)
                return null;

            if (method.ContainingType?.TypeKind == TypeKind.Interface)
            {
                return derivedClass.FindImplementationForInterfaceMember(method) as IMethodSymbol;
            }

            return derivedClass.GetMembers().OfType<IMethodSymbol>().FirstOrDefault(m => MethodSignatureEquals(m, method));
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
            var containingType = symbol as INamedTypeSymbol ?? symbol.ContainingType;

            var contractClassForAttribute = containingType?
                .GetAttributes()
                .FirstOrDefault(a => a?.AttributeClass?.Name == "ContractClassForAttribute");

            var baseClass = contractClassForAttribute?.ConstructorArguments.FirstOrDefault().Value as INamedTypeSymbol;

            if (baseClass?.IsGenericType == true)
            {
                baseClass = symbol.GetBaseClassAndInterfaces()
                    .FirstOrDefault(x => x.IsGenericType
                        && x.Name == baseClass.Name
                        && x.TypeArguments.Length == baseClass.TypeArguments.Length);
            }

            return baseClass;
        }

        public static INamedTypeSymbol GetContractClass([NotNull] this ISymbol symbol)
        {
            var containingType = symbol as INamedTypeSymbol ?? symbol.ContainingType;

            return containingType?.ContainingNamespace?.GetTypeMembers().FirstOrDefault(type => Equals(type?.GetContractClassFor(), containingType));
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

        public static IParameterSymbol GetTargetSymbolForAnnotation(this IParameterSymbol parameterSymbol)
        {
            var baseClass = parameterSymbol?.GetContractClassFor();
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

        [NotNull]
        public static PropertyDeclarationSyntax FindDeclaringMemberOnBaseClass([NotNull] this PropertyDeclarationSyntax propertySyntax, [NotNull] SemanticModel semanticModel, [NotNull] SyntaxNode root)
        {
            var propertySymbol = semanticModel.GetDeclaredSymbol(propertySyntax);

            var baseClass = propertySymbol?.GetContractClassFor();

            return root.GetSyntaxNode<PropertyDeclarationSyntax>(baseClass?.FindDeclaringMemberOnBaseClass(propertySymbol)) ?? propertySyntax;
        }

        [NotNull]
        public static MethodDeclarationSyntax FindDeclaringMemberOnBaseClass([NotNull] this MethodDeclarationSyntax methodSyntax, [NotNull] SemanticModel semanticModel, [NotNull] SyntaxNode root)
        {
            var methodSymbol = semanticModel.GetDeclaredSymbol(methodSyntax);

            var baseClass = methodSymbol?.GetContractClassFor();

            return root.GetSyntaxNode<MethodDeclarationSyntax>(baseClass?.FindDeclaringMemberOnBaseClass(methodSymbol)) ?? methodSyntax;
        }
    }
}
