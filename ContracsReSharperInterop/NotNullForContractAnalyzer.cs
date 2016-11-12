namespace ContracsReSharperInterop
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

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

        private static readonly DiagnosticDescriptor _rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, true, Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSemanticModelAction(Analyze);
        }

        private static void Analyze(SemanticModelAnalysisContext context)
        {
            var root = context.SemanticModel.SyntaxTree.GetRoot(context.CancellationToken);

            var diags = new Analyzer(context, root).Analyze();

            foreach (var diag in diags)
            {
                context.ReportDiagnostic(Diagnostic.Create(_rule, diag.Location, diag.Name));
            }
        }

        private class Analyzer
        {
            private readonly SemanticModelAnalysisContext _context;
            private readonly SyntaxNode _root;
            private readonly ICollection<InvocationExpressionSyntax> _invocationExpressionSyntaxNodes;

            public Analyzer(SemanticModelAnalysisContext context, SyntaxNode root)
            {
                _context = context;
                _root = root;
                _invocationExpressionSyntaxNodes = root.DescendantNodesAndSelf()
                    .OfType<InvocationExpressionSyntax>()
                    .ToArray();
            }

            public IEnumerable<Diag> Analyze()
            {
                var diags = AnalyzeRequires()
                    .Concat(AnalyzeEnsures())
                    .Concat(AnalyzeInvariants())
                    .Where(diag => diag != null)
                    .Distinct()
                    .ToArray();

                return diags;
            }

            private IEnumerable<Diag> AnalyzeRequires()
            {
                // find all symbols that are part of a Contract.Requires(...), e.g. Contract.Requires(x != null) => x

                const ContractCategory contractCategory = ContractCategory.Requires;

                var requiresExpressions = _invocationExpressionSyntaxNodes
                    .Where(item => _context.IsContractExpression(item.Expression as MemberAccessExpressionSyntax, contractCategory)); // find all "Contract.Requires(...)" 

                var notNullParameterSymbols = requiresExpressions.GetNotNullIdentifierSyntax<IdentifierNameSyntax>()
                    .Select(syntax => _context.SemanticModel.GetSymbolInfo(syntax).Symbol); // get the parameter symbol 

                foreach (var notNullParameterSymbol in notNullParameterSymbols)
                {
                    var outerMethodSymbol = notNullParameterSymbol.ContainingSymbol as IMethodSymbol;
                    if (outerMethodSymbol == null)
                        continue;

                    if (outerMethodSymbol.MethodKind == MethodKind.PropertySet)
                    {
                        var propertySymbol = outerMethodSymbol.AssociatedSymbol;
                        var propertySyntax = propertySymbol.GetSyntaxNode<PropertyDeclarationSyntax>(_root);

                        yield return GetDiagnostic(propertySyntax, contractCategory);
                    }
                    else
                    {
                        var parameterSyntax = notNullParameterSymbol.GetSyntaxNode<ParameterSyntax>(_root);

                        yield return GetDiagnostic(parameterSyntax, contractCategory);
                    }
                }
            }

            private IEnumerable<Diag> AnalyzeInvariants()
            {
                // find all symbols that are part of a Contract.Invariant(...), e.g. Contract.Invariant(x != null) => x

                const ContractCategory contractCategory = ContractCategory.Invariant;

                var requiresExpressions = _invocationExpressionSyntaxNodes
                    .Where(item => _context.IsContractExpression(item.Expression as MemberAccessExpressionSyntax, contractCategory)); // find all "Contract.Invariant(...)" 

                var notNullParameterSymbols = requiresExpressions.GetNotNullIdentifierSyntax<IdentifierNameSyntax>()
                    .Select(syntax => _context.SemanticModel.GetSymbolInfo(syntax).Symbol); // get the parameter symbol 

                return notNullParameterSymbols
                    .Select(notNullParameterSymbol => notNullParameterSymbol.GetSyntaxNode<SyntaxNode>(_root))
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
                    .Where(item => _context.IsContractExpression(item.Expression as MemberAccessExpressionSyntax, contractCategory)) // find all "Contract.Ensures(...)" 
                    .Where(IsContractResultExpression);

                return ensuresExpressions
                    .Select(ensuresExpression => ensuresExpression.Ancestors().OfType<MemberDeclarationSyntax>().FirstOrDefault())
                    .Select(outerMember => outerMember.TryCast().Returning<Diag>()
                        .When<MethodDeclarationSyntax>(syntax => GetDiagnostic(syntax, contractCategory))
                        .When<PropertyDeclarationSyntax>(syntax => GetDiagnostic(syntax, contractCategory))
                        .Result);
            }

            private static bool IsContractResultExpression(InvocationExpressionSyntax item)
            {
                var expressionSyntax = item.GetNotNullIdentifierSyntax<InvocationExpressionSyntax>()?.Expression as MemberAccessExpressionSyntax;

                return expressionSyntax?.Name.Identifier.Text == "Result";
            }

            private static Diag GetDiagnostic(ParameterSyntax syntax, ContractCategory contractCategory)
            {
                if (syntax?.AttributeLists.ContainsNotNullAttribute() != false)
                    return null;

                return new Diag(syntax.Identifier.GetLocation(), syntax.Identifier.Text, contractCategory);
            }

            private static Diag GetDiagnostic(MethodDeclarationSyntax syntax, ContractCategory contractCategory)
            {
                if (syntax?.AttributeLists.ContainsNotNullAttribute() != false)
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

            private static Diag GetDiagnostic(PropertyDeclarationSyntax syntax, ContractCategory contractCategory)
            {
                if (syntax?.AttributeLists.ContainsNotNullAttribute() != false)
                    return null;

                return new Diag(syntax.Identifier.GetLocation(), syntax.Identifier.Text, contractCategory);
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
