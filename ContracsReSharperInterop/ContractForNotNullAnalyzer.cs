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
            private readonly ICollection<InvocationExpressionSyntax> _invocationExpressionSyntaxNodes;
            [NotNull, ItemNotNull]
            private readonly ICollection<BaseMethodDeclarationSyntax> _methodDeclarationSyntaxNodes;

            public Analyzer([NotNull] SemanticModel semanticModel, [NotNull] SyntaxNode root)
            {
                _semanticModel = semanticModel;
                _root = root;
                // ReSharper disable once AssignNullToNotNullAttribute
                _invocationExpressionSyntaxNodes = root.DescendantNodesAndSelf()
                    .OfType<InvocationExpressionSyntax>()
                    .ToArray();
                // ReSharper disable once AssignNullToNotNullAttribute
                _methodDeclarationSyntaxNodes = root.DescendantNodesAndSelf()
                    .OfType<BaseMethodDeclarationSyntax>()
                    .ToArray();
            }

            [NotNull, ItemNotNull]
            public IEnumerable<Diag> Analyze()
            {
                var diags = AnalyzeFieldInvariants()
                    .Concat(AnalyzeMethodEnsures())
                    .Concat(AnalyzeMethodParameters())
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
                    .GetNotNullArgumentIdentifierSyntaxNodes()
                    .Select(syntax => _semanticModel.GetSymbolInfo(syntax).Symbol as IParameterSymbol) // get the parameter symbol
                    .Select(symbol => symbol?.GetTargetSymbolForAnnotation())
                    .Select(symbol => _root.GetSyntaxNode<ParameterSyntax>(symbol))
                    .Where(parameter => parameter != null);

                var parametersWithNotNullAnnotation = _methodDeclarationSyntaxNodes
                    .Where(CanAddContracts)
                    .SelectMany(node => node.ParameterList.Parameters)
                    .Where(parameter => parameter.AttributeLists.ContainsNotNullAttribute());

                var parametersWithMissingContracts = parametersWithNotNullAnnotation.Except(parametersWithNotNullContract, new DelegateEqualityComparer<ParameterSyntax>(p => p.GetLocation()));

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
                    .Select(method => method?.FindDeclaringMemberOnBaseClass(_semanticModel, _root))
                    .Where(method => method != null);

                var methodsWithNotNullAnnotation = _methodDeclarationSyntaxNodes
                    .Where(CanAddContracts)
                    .Where(method => method.AttributeLists.ContainsNotNullAttribute());

                var methodsWithMissingContractEnsures = methodsWithNotNullAnnotation
                    .Except(methodsWithContractEnsures);

                foreach (var methodSyntax in methodsWithMissingContractEnsures)
                {
                    yield return GetDiagnostic(methodSyntax, contractCategory);
                }
            }

            private IEnumerable<Diag> AnalyzeFieldInvariants()
            {
                var classDeclarations = _root.DescendantNodes()
                    .OfType<ClassDeclarationSyntax>();

                foreach (var classDeclaration in classDeclarations)
                {
                    var notNullFields = classDeclaration.ChildNodes()
                        .OfType<FieldDeclarationSyntax>()
                        .Where(f => f.AttributeLists.ContainsNotNullAttribute())
                        .SelectMany(f => f.Declaration.Variables)
                        .ToArray();

                    if (!notNullFields.Any())
                        continue;

                    var invariantMethod = classDeclaration.ChildNodes()
                        .OfType<MethodDeclarationSyntax>()
                        .FirstOrDefault(m => m.AttributeLists.ContainsAttribute("ContractInvariantMethod"));

                    if (invariantMethod == null)
                        continue;

                    var invariantExpressions = invariantMethod.Body?.Statements.OfType<ExpressionStatementSyntax>()
                        .Select(s => s.Expression)
                        .OfType<InvocationExpressionSyntax>()
                        .Where(item => item.Expression.IsContractExpression(ContractCategory.Invariant)) // find all "Contract.Invariant(...)"
                        .ToArray();

                    var invariantNotNullFields = invariantExpressions?
                        .GetNotNullArgumentIdentifierSyntaxNodes()
                        .Select(syntax => _semanticModel.GetSymbolInfo(syntax).Symbol) // get the variable symbol
                        .Select(notNullParameterSymbol => _root.GetSyntaxNode<SyntaxNode>(notNullParameterSymbol))
                        .OfType<VariableDeclaratorSyntax>()
                        .ToArray() ?? new VariableDeclaratorSyntax[0];

                    var fieldsWithoutContracts = notNullFields.Except(invariantNotNullFields);

                    foreach (var field in fieldsWithoutContracts)
                    {
                        yield return GetDiagnostic(field, ContractCategory.Invariant);
                    }

                }

                yield break;
            }

            private static Diag GetDiagnostic(ParameterSyntax syntax, ContractCategory contractCategory)
            {
                if (syntax == null)
                    return null;

                return new Diag(syntax.Identifier.GetLocation(), syntax.Identifier.Text, contractCategory);
            }

            private static Diag GetDiagnostic(BaseMethodDeclarationSyntax syntax, ContractCategory contractCategory)
            {
                if (syntax == null)
                    return null;

                return syntax.TryCast().Returning<Diag>()
                    .When<MethodDeclarationSyntax>(s => new Diag(s.Identifier.GetLocation(), s.Identifier.Text, contractCategory))
                    .When<ConstructorDeclarationSyntax>(s => new Diag(s.Identifier.GetLocation(), s.Identifier.Text, contractCategory))
                    .Result;
            }

            private static Diag GetDiagnostic(VariableDeclaratorSyntax syntax, ContractCategory contractCategory)
            {
                return new Diag(syntax.Identifier.GetLocation(), syntax.Identifier.Text, contractCategory);
            }

            private static bool CanAddContracts(BaseMethodDeclarationSyntax method)
            {
                if (method == null)
                    return false;

                if (method.Body != null)
                {
                    return method.Modifiers.All(token => token.Kind() != SyntaxKind.OverrideKeyword); 
                }

                if (method.Parent.ContainsAttribute("ContractClass"))
                    return true;

                return false;
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
