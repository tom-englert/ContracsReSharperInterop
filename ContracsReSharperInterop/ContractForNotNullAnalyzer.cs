namespace ContracsReSharperInterop
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using JetBrains.Annotations;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    using TomsToolbox.Core;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class ContractForNotNullAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ContracsReSharperInterop_ContractForNotNull";

        private const string Title = "Element with [NotNull] attribute does not have a corresponding not-null contract.";
        private const string MessageFormat = "Element '{0}' has a [NotNull] attribute but does not have a corresponding not-null contract.";
        private const string Description = "Elements with [NotNull] attribute should have a corresponding not-null contract.";

        private const string Category = "CodeContracts";

        private static readonly DiagnosticDescriptor _rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, true, Description);

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
            private readonly SemanticModel _semanticModel;
            [NotNull]
            private readonly SyntaxNode _root;
            [NotNull, ItemNotNull]
            private readonly ICollection<InvocationExpressionSyntax> _invocationExpressionSyntaxNodes;
            [NotNull, ItemNotNull]
            private readonly ICollection<MethodDeclarationSyntax> _methodDeclarationSyntaxNodes;

            public Analyzer(SemanticModel semanticModel, [NotNull] SyntaxNode root)
            {
                _semanticModel = semanticModel;
                _root = root;
                // ReSharper disable once AssignNullToNotNullAttribute
                _invocationExpressionSyntaxNodes = root.DescendantNodesAndSelf()
                    .OfType<InvocationExpressionSyntax>()
                    .ToArray();
                // ReSharper disable once AssignNullToNotNullAttribute
                _methodDeclarationSyntaxNodes = root.DescendantNodesAndSelf()
                    .OfType<MethodDeclarationSyntax>()
                    .ToArray();
            }

            [ItemNotNull]
            [NotNull]
            public IEnumerable<Diag> Analyze()
            {
                var diags = AnalyzeMethodParameters()
                    .Concat(AnalyzeMethodEnsures())
                    .Where(diag => diag != null)
                    .Distinct()
                    .ToArray();

                // ReSharper disable once AssignNullToNotNullAttribute
                return diags;
            }

            private IEnumerable<Diag> AnalyzeMethodParameters()
            {
                const ContractCategory contractCategory = ContractCategory.Requires;

                var requiresExpressions = _invocationExpressionSyntaxNodes
                    .Where(item => (item.Expression as MemberAccessExpressionSyntax).IsContractExpression(contractCategory)); // find all "Contract.Requires(...)" 

                var parametersWithNotNullContract = requiresExpressions
                    .GetNotNullIdentifierSyntax<IdentifierNameSyntax>()
                    .Select(syntax => _semanticModel.GetSymbolInfo(syntax).Symbol as IParameterSymbol) // get the parameter symbol 
                    .Select(symbol => symbol?.GetAnnotationTargetSymbol())
                    .Select(symbol => _root.GetSyntaxNode<ParameterSyntax>(symbol))
                    .Where(parameter => parameter != null);

                var parametersWithNotNullAnnotation = _methodDeclarationSyntaxNodes
                    .SelectMany(node => node.ParameterList.Parameters)
                    .Where(parameter => parameter?.AttributeLists.ContainsNotNullAttribute() == true);

                var parametersWithMissingContracts = parametersWithNotNullAnnotation.Except(parametersWithNotNullContract);

                foreach (var parameterSyntax in parametersWithMissingContracts)
                {
                    yield return GetDiagnostic(parameterSyntax, contractCategory);
                }
            }

            private IEnumerable<Diag> AnalyzeMethodEnsures()
            {
                // find all symbols that are part of a Contract.Ensures(...), e.g. Contract.Ensures(Contract.Result<T>() != null)

                const ContractCategory contractCategory = ContractCategory.Ensures;

                var ensuresExpressions = _invocationExpressionSyntaxNodes
                    .Where(item => (item.Expression as MemberAccessExpressionSyntax).IsContractExpression(contractCategory)) // find all "Contract.Ensures(...)" 
                    .Where(item => item.IsContractResultExpression());

                var methodsWithContractEnsures = ensuresExpressions
                    .Select(ensuresExpression => ensuresExpression.Ancestors().OfType<MemberDeclarationSyntax>().FirstOrDefault() as MethodDeclarationSyntax)
                    .Select(FindDeclaringMemberOnBaseClass)
                    .Where(method => method != null);

                var methodsWithNotNullAnnotation = _methodDeclarationSyntaxNodes
                    .Where(method => method.AttributeLists.ContainsNotNullAttribute());

                var methodsWithMissingContractEnsures = methodsWithNotNullAnnotation
                    .Except(methodsWithContractEnsures);

                foreach (var methodSyntax in methodsWithMissingContractEnsures)
                {
                    yield return GetDiagnostic(methodSyntax, contractCategory);
                }
            }

            [NotNull]
            private MethodDeclarationSyntax FindDeclaringMemberOnBaseClass([NotNull] MethodDeclarationSyntax methodSyntax)
            {
                var methodSymbol = _semanticModel.GetDeclaredSymbol(methodSyntax);

                var baseClass = methodSymbol?.GetContractClassFor();

                return _root.GetSyntaxNode<MethodDeclarationSyntax>(baseClass?.FindDeclaringMemberOnBaseClass(methodSymbol)) ?? methodSyntax;
            }

            private static Diag GetDiagnostic(ParameterSyntax syntax, ContractCategory contractCategory)
            {
                if (syntax == null)
                    return null;

                return new Diag(syntax.Identifier.GetLocation(), syntax.Identifier.Text, contractCategory);
            }

            private static Diag GetDiagnostic(MethodDeclarationSyntax syntax, ContractCategory contractCategory)
            {
                if (syntax == null)
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
