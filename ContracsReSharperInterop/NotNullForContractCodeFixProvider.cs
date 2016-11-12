namespace ContracsReSharperInterop
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    using TomsToolbox.Core;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NotNullForContractCodeFixProvider)), Shared]
    internal class NotNullForContractCodeFixProvider : CodeFixProvider
    {
        private const string UsingDirectiveName = "JetBrains.Annotations";
        private const string Title = "Add [NotNull] annotation";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(NotNullForContractAnalyzer.DiagnosticId);

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

                var codeAction = CodeAction.Create(Title, c => AddNotNullAnnotationAsync(context.Document, syntaxNode, c), Title);

                context.RegisterCodeFix(codeAction, diagnostic);
            }
        }

        private static async Task<Document> AddNotNullAnnotationAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) as CompilationUnitSyntax;
            if (root == null)
                return document;

            var hasUsingDirective = root.HasUsingDirective(node, UsingDirectiveName);

            root = root.ReplaceNode(node, AddNotNullAttribute(node));

            if (!hasUsingDirective)
            {
                root = root.AddUsingDirective(UsingDirectiveName);
            }

            return document.WithSyntaxRoot(root);
        }

        private static SyntaxNode AddNotNullAttribute(SyntaxNode node)
        {
            // a workaround for https://github.com/dotnet/roslyn/issues/15191
            var leadingTrivia = GetLeadingNonWhitespaceTrivia(node);

            if (leadingTrivia.HasValue)
                node = node.WithLeadingTrivia();

            node = node.TryCast().Returning<SyntaxNode>()
                .When<ParameterSyntax>(item => item.WithAttributeLists(item.AttributeLists.Add(CreateNotNullAttributeListSyntax(leadingTrivia))))
                .When<PropertyDeclarationSyntax>(item => item.WithAttributeLists(item.AttributeLists.Add(CreateNotNullAttributeListSyntax(leadingTrivia))))
                .When<MethodDeclarationSyntax>(item => item.WithAttributeLists(item.AttributeLists.Add(CreateNotNullAttributeListSyntax(leadingTrivia))))
                .When<FieldDeclarationSyntax>(item => item.WithAttributeLists(item.AttributeLists.Add(CreateNotNullAttributeListSyntax(leadingTrivia))))
                .Else(item => item);

            return node;
        }

        private static SyntaxTriviaList? GetLeadingNonWhitespaceTrivia(SyntaxNode node)
        {
            if (!node.HasLeadingTrivia || node.HasAttributes())
                return null;

            var trivia = node.GetLeadingTrivia();

            if (trivia.Any(t => t.Kind() != SyntaxKind.WhitespaceTrivia))
            {
                return trivia;
            }

            return null;
        }

        private static AttributeListSyntax CreateNotNullAttributeListSyntax(SyntaxTriviaList? trivia)
        {
            const string notnull = "NotNull";

            var separatedSyntaxList = SyntaxFactory.SeparatedList(new[] { SyntaxFactory.Attribute(SyntaxFactory.IdentifierName(notnull)) });
            var attributeList = SyntaxFactory.AttributeList(separatedSyntaxList);

            if (trivia.HasValue)
                attributeList = attributeList.WithLeadingTrivia(trivia.Value);

            return attributeList;
        }
    }
}