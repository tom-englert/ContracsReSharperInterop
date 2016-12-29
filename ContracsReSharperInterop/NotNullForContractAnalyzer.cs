namespace ContracsReSharperInterop
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    using JetBrains.Annotations;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    using TomsToolbox.Core;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class NotNullForContractAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ContracsReSharperInterop_NotNullForContract";

        private const string Title = "Element with not-null contract does not have a corresponding [NotNull] attribute.";
        private const string MessageFormat = "Element '{0}' has a not-null contract but does not have a corresponding [NotNull] attribute.";
        private const string Description = "Elements with not-null contract should have a corresponding [NotNull] attribute.";

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

            var diags = new Analyzer(context.SemanticModel, root).Analyze();

            foreach (var diag in diags)
            {
                context.ReportDiagnostic(Diagnostic.Create(_rule, diag.Location, diag.Name));
            }
        }

        private class Analyzer
        {
            [NotNull]
            private readonly SemanticModel _semanticModel;
            [NotNull]
            private readonly SyntaxNode _root;
            [NotNull, ItemNotNull]
            private readonly IReadOnlyCollection<InvocationExpressionSyntax> _invocationExpressionSyntaxNodes;

            [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
            public Analyzer([NotNull] SemanticModel semanticModel, [NotNull] SyntaxNode root)
            {
                _semanticModel = semanticModel;
                _root = root;

                _invocationExpressionSyntaxNodes = root.DescendantNodesAndSelf()
                    .OfType<InvocationExpressionSyntax>()
                    .ToArray();
            }

            [NotNull, ItemNotNull]
            public IEnumerable<Diag> Analyze()
            {
                var diags = AnalyzeRequires()
                    .Concat(AnalyzeEnsures())
                    .Concat(AnalyzeInvariants())
                    .Where(diag => diag != null)
                    .Distinct()
                    .ToArray();

                // ReSharper disable once AssignNullToNotNullAttribute
                return diags;
            }

            private IEnumerable<Diag> AnalyzeRequires()
            {
                // find all symbols that are part of a Contract.Requires(...), e.g. Contract.Requires(x != null) => x

                const ContractCategory contractCategory = ContractCategory.Requires;

                var requiresExpressions = _invocationExpressionSyntaxNodes
                    .Where(item => (item.Expression as MemberAccessExpressionSyntax).IsContractExpression(contractCategory)); // find all "Contract.Requires(...)" 

                var notNullParameterSymbols = requiresExpressions
                    .GetNotNullArgumentIdentifierSyntaxNodes<IdentifierNameSyntax>()
                    .Select(syntax => _semanticModel.GetSymbolInfo(syntax).Symbol as IParameterSymbol)
                    .Where(item => item != null);

                foreach (var notNullParameterSymbol in notNullParameterSymbols)
                {
                    var parameterSymbol = notNullParameterSymbol.GetAnnotationTargetSymbol();

                    var outerMethodSymbol = parameterSymbol?.ContainingSymbol as IMethodSymbol;
                    if (outerMethodSymbol == null)
                        continue;

                    if (outerMethodSymbol.MethodKind == MethodKind.PropertySet)
                    {
                        var propertySymbol = outerMethodSymbol.AssociatedSymbol;
                        var propertySyntax = _root.GetSyntaxNode<PropertyDeclarationSyntax>(propertySymbol);

                        yield return GetDiagnostic(propertySyntax, contractCategory);
                    }
                    else
                    {
                        var parameterSyntax = _root.GetSyntaxNode<ParameterSyntax>(parameterSymbol);

                        yield return GetDiagnostic(parameterSyntax, contractCategory);
                    }
                }
            }

            private IEnumerable<Diag> AnalyzeInvariants()
            {
                // find all symbols that are part of a Contract.Invariant(...), e.g. Contract.Invariant(x != null) => x

                const ContractCategory contractCategory = ContractCategory.Invariant;

                var requiresExpressions = _invocationExpressionSyntaxNodes
                    .Where(item => (item?.Expression as MemberAccessExpressionSyntax).IsContractExpression(contractCategory)); // find all "Contract.Invariant(...)" 

                var notNullParameterSymbols = requiresExpressions.GetNotNullArgumentIdentifierSyntaxNodes<IdentifierNameSyntax>()
                    .Select(syntax => _semanticModel.GetSymbolInfo(syntax).Symbol); // get the parameter symbol 

                return notNullParameterSymbols
                    .Select(notNullParameterSymbol => _root.GetSyntaxNode<SyntaxNode>(notNullParameterSymbol))
                    .Select(parameterSyntax => parameterSyntax.TryCast().Returning<Diag>()
                        .When<PropertyDeclarationSyntax>(syntax => GetDiagnostic(syntax, contractCategory))
                        .When<VariableDeclaratorSyntax>(syntax => GetDiagnostic(syntax, contractCategory))
                        .Result);
            }

            private IEnumerable<Diag> AnalyzeEnsures()
            {
                // find all symbols that are part of a Contract.Ensures(...), e.g. Contract.Ensures(Contract.Result<T>() != null)

                const ContractCategory contractCategory = ContractCategory.Ensures;

                var ensuresExpressions = _invocationExpressionSyntaxNodes
                    .Where(item => (item.Expression as MemberAccessExpressionSyntax).IsContractExpression(contractCategory)) // find all "Contract.Ensures(...)" 
                    .Where(item => item.IsContractResultExpression());

                return ensuresExpressions
                    .Select(ensuresExpression => ensuresExpression.Ancestors().OfType<MemberDeclarationSyntax>().FirstOrDefault())
                    .Select(outerMember => outerMember.TryCast().Returning<Diag>()
                        .When<MethodDeclarationSyntax>(syntax => GetDiagnostic(syntax, contractCategory))
                        .When<PropertyDeclarationSyntax>(syntax => GetDiagnostic(syntax, contractCategory))
                        .Result);
            }

            private static Diag GetDiagnostic(ParameterSyntax syntax, ContractCategory contractCategory)
            {
                if (syntax?.AttributeLists.ContainsNotNullAttribute() != false)
                    return null;

                return new Diag(syntax.Identifier.GetLocation(), syntax.Identifier.Text, contractCategory);
            }

            private Diag GetDiagnostic(MethodDeclarationSyntax syntax, ContractCategory contractCategory)
            {
                if (syntax == null)
                    return null;

                syntax = FindDeclaringMemberOnBaseClass(syntax);

                if (syntax.AttributeLists.ContainsNotNullAttribute())
                    return null;

                return new Diag(syntax.Identifier.GetLocation(), syntax.Identifier.Text, contractCategory);
            }

            private static Diag GetDiagnostic(VariableDeclaratorSyntax syntax, ContractCategory contractCategory)
            {
                var fieldDeclarationSyntax = syntax?.Ancestors().OfType<FieldDeclarationSyntax>().FirstOrDefault();

                if (fieldDeclarationSyntax?.AttributeLists.ContainsNotNullAttribute() != false)
                    return null;

                return new Diag(fieldDeclarationSyntax.GetLocation(), syntax.ToString(), contractCategory);
            }

            private Diag GetDiagnostic(PropertyDeclarationSyntax syntax, ContractCategory contractCategory)
            {
                if (syntax == null)
                    return null;

                syntax = FindDeclaringMemberOnBaseClass(syntax);

                if (syntax.AttributeLists.ContainsNotNullAttribute())
                    return null;

                return new Diag(syntax.Identifier.GetLocation(), syntax.Identifier.Text, contractCategory);
            }

            [NotNull]
            private PropertyDeclarationSyntax FindDeclaringMemberOnBaseClass([NotNull] PropertyDeclarationSyntax propertySyntax)
            {
                var propertySymbol = _semanticModel.GetDeclaredSymbol(propertySyntax);

                var baseClass = propertySymbol?.GetContractClassFor();

                return _root.GetSyntaxNode<PropertyDeclarationSyntax>(baseClass?.FindDeclaringMemberOnBaseClass(propertySymbol)) ?? propertySyntax;
            }

            [NotNull]
            private MethodDeclarationSyntax FindDeclaringMemberOnBaseClass([NotNull] MethodDeclarationSyntax methodSyntax)
            {
                var methodSymbol = _semanticModel.GetDeclaredSymbol(methodSyntax);

                var baseClass = methodSymbol?.GetContractClassFor();

                return _root.GetSyntaxNode<MethodDeclarationSyntax>(baseClass?.FindDeclaringMemberOnBaseClass(methodSymbol)) ?? methodSyntax;
            }
        }

        private class Diag : IEquatable<Diag>
        {
            public Location Location { get; }
            public string Name { get; }
            public ContractCategory ContractCategory { get; }

            public Diag(Location location, string name, ContractCategory contractCategory)
            {
                Location = location;
                Name = name;
                ContractCategory = contractCategory;
            }

            #region IEquatable implementation

            /// <summary>
            /// Returns a hash code for this instance.
            /// </summary>
            /// <returns>
            /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
            /// </returns>
            public override int GetHashCode()
            {
                return Location.GetHashCode();
            }

            /// <summary>
            /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
            /// </summary>
            /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
            /// <returns><c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
            public override bool Equals(object obj)
            {
                return Equals(obj as Diag);
            }

            /// <summary>
            /// Determines whether the specified <see cref="Diag "/> is equal to this instance.
            /// </summary>
            /// <param name="other">The <see cref="Diag "/> to compare with this instance.</param>
            /// <returns><c>true</c> if the specified <see cref="Diag "/> is equal to this instance; otherwise, <c>false</c>.</returns>
            public bool Equals(Diag other)
            {
                return InternalEquals(this, other);
            }

            private static bool InternalEquals(Diag left, Diag right)
            {
                if (ReferenceEquals(left, right))
                    return true;
                if (ReferenceEquals(left, null))
                    return false;
                if (ReferenceEquals(right, null))
                    return false;

                return left.Location?.Equals(right.Location) == true;
            }

            /// <summary>
            /// Implements the operator ==.
            /// </summary>
            public static bool operator ==(Diag left, Diag right)
            {
                return InternalEquals(left, right);
            }
            /// <summary>
            /// Implements the operator !=.
            /// </summary>
            public static bool operator !=(Diag left, Diag right)
            {
                return !InternalEquals(left, right);
            }

            #endregion
        }
    }
}
