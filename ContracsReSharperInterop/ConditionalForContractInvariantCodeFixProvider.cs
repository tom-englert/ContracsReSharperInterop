namespace ContracsReSharperInterop
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConditionalForContractInvariantCodeFixProvider)), Shared]
    public class ConditionalForContractInvariantCodeFixProvider : CodeFixProvider
    {
        private const string UsingDirectiveName = "System.Diagnostics";
        private const string Title = "Add [Conditional(\"CONTRACTS_FULL\")] attribute";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ConditionalForContractInvariantAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var diagnosticSpan = diagnostic.Location.SourceSpan;

                var syntaxNode = root.FindNode(diagnosticSpan) as MethodDeclarationSyntax;
                if (syntaxNode == null)
                    return;

                var codeAction = CodeAction.Create(Title, c => AddConditionalAttributeAsync(context.Document, syntaxNode, c), Title);

                context.RegisterCodeFix(codeAction, diagnostic);
            }
        }

        private static async Task<Document> AddConditionalAttributeAsync(Document document, MethodDeclarationSyntax node, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) as CompilationUnitSyntax;
            if (root == null)
                return document;

            var hasUsingDirective = root.HasUsingDirective(node, UsingDirectiveName);

            root = root.ReplaceNode(node, AddConditionalAttribute(node));

            if (!hasUsingDirective)
            {
                root = root.AddUsingDirective(UsingDirectiveName);
            }

            return document.WithSyntaxRoot(root);
        }

        private static SyntaxNode AddConditionalAttribute(MethodDeclarationSyntax node)
        {
            return node.WithAttributeLists(node.AttributeLists.Add(CreateConditionalAttributeListSyntax()));
        }

        private static AttributeListSyntax CreateConditionalAttributeListSyntax()
        {
            const string notnull = "Conditional";

            var separatedSyntaxList = SyntaxFactory.SeparatedList(new[]
            {
                SyntaxFactory.Attribute(SyntaxFactory.IdentifierName(notnull))
                    .AddArgumentListArguments(SyntaxFactory.AttributeArgument(
                        SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression)
                            .WithToken(SyntaxFactory.ParseToken("\"CONTRACTS_FULL\""))))
            });

            return SyntaxFactory.AttributeList(separatedSyntaxList);
        }
    }
}