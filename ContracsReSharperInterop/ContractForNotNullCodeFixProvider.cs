namespace ContracsReSharperInterop
{
    using System;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using JetBrains.Annotations;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    using TomsToolbox.Core;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NotNullForContractCodeFixProvider)), Shared]
    internal class ContractForNotNullCodeFixProvider : CodeFixProvider
    {
        private const string UsingDirectiveName = "System.Diagnostics.Contracts";
        private const string Title = "Add Contract for [NotNull] annotation";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ContractForNotNullAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var diagnosticSpan = diagnostic.Location.SourceSpan;

                var syntaxNode = root.FindNode(diagnosticSpan);
                if (syntaxNode == null)
                    return;

                var codeAction = CodeAction.Create(Title, c => AddContractAsync(context.Document, syntaxNode, c), Title);

                context.RegisterCodeFix(codeAction, diagnostic);
            }
        }

        private static async Task<Document> AddContractAsync([NotNull] Document document, [NotNull] SyntaxNode node, CancellationToken cancellationToken)
        {
            // ReSharper disable once PossibleNullReferenceException
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) as CompilationUnitSyntax;
            if (root == null)
                return document;

            // ReSharper disable once PossibleNullReferenceException
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var hasUsingDirective = root.HasUsingDirective(node, UsingDirectiveName);

            var newRoot = node.TryCast().Returning<CompilationUnitSyntax>()
                .When<ParameterSyntax>(syntax => AddRequires(root, semanticModel, syntax))
                .When<MethodDeclarationSyntax>(syntax => AddEnsures(root, semanticModel, syntax))
                .Else(syntax => root);

            Debug.Assert(newRoot != null);

            if (!hasUsingDirective)
            {
                newRoot = newRoot.AddUsingDirective(UsingDirectiveName);
            }

            return document.WithSyntaxRoot(newRoot);
        }

        private static CompilationUnitSyntax AddRequires(CompilationUnitSyntax root, SemanticModel semanticModel, [NotNull] ParameterSyntax parameterSyntax)
        {
            var methodSyntax = parameterSyntax.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (methodSyntax == null)
                return root;

            var parametersBefore = methodSyntax.ParameterList.Parameters.TakeWhile(p => p != parameterSyntax).Select(p => p.Identifier.Text).ToArray();

            var statements = methodSyntax.Body.Statements;

            var index = statements
                .Select(s => (s as ExpressionStatementSyntax)?.Expression as InvocationExpressionSyntax)
                .TakeWhile(s => (s?.Expression as MemberAccessExpressionSyntax).IsContractExpression(ContractCategory.Requires))
                .Select(p => p?.GetNotNullIdentifierSyntax<IdentifierNameSyntax>())
                .Select(p => semanticModel.GetSymbolInfo(p).Symbol)
                .Select(p => p?.Name)
                .TakeWhile(n => parametersBefore.Contains(n))
                .Count();

            var statementSyntax = SyntaxFactory.ParseStatement($"Contract.Requires({parameterSyntax.Identifier.Text} != null);\r\n")
                .WithLeadingTrivia(statements.FirstOrDefault()?.GetLeadingTrivia());

            statements = statements.Insert(index, statementSyntax);

            return root.ReplaceNode(methodSyntax.Body, methodSyntax.Body.WithStatements(statements));
        }

        private static CompilationUnitSyntax AddEnsures(CompilationUnitSyntax root, SemanticModel semanticModel, [NotNull] MethodDeclarationSyntax methodSyntax)
        {
            var statements = methodSyntax.Body.Statements;

            var index = statements
                .Select(s => (s as ExpressionStatementSyntax)?.Expression as InvocationExpressionSyntax)
                .TakeWhile(s => (s?.Expression as MemberAccessExpressionSyntax).IsContractExpression(ContractCategory.Requires))
                .Count();

            var statementSyntax = SyntaxFactory.ParseStatement($"Contract.Ensures(Contract.Result<{methodSyntax.ReturnType}>() != null);\r\n")
                .WithLeadingTrivia(statements.FirstOrDefault()?.GetLeadingTrivia());

            statements = statements.Insert(index, statementSyntax);

            return root.ReplaceNode(methodSyntax.Body, methodSyntax.Body.WithStatements(statements));
        }
    }
}