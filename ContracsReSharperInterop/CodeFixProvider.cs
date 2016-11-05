namespace ContracsReSharperInterop
{
    using System;
    using System.Collections.Generic;
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
    using Microsoft.CodeAnalysis.Rename;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ContracsReSharperInteropCodeFixProvider)), Shared]
    public class ContracsReSharperInteropCodeFixProvider : CodeFixProvider
    {
        private const string UsingDirectiveName = "JetBrains.Annotations";
        private const string Title = "Add [NotNull] annotation";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ContracsReSharperInteropAnalyzer.DiagnosticId);

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

            var hasUsingDirective = HasUsingDirective(root, node);

            root = root.ReplaceNode(node, AddNotNullAttribute(node));

            if (!hasUsingDirective)
            {
                root = AddUsingDirective(root);
            }

            return document.WithSyntaxRoot(root);
        }

        private static CompilationUnitSyntax AddUsingDirective(CompilationUnitSyntax root)
        {
            var usingSyntax = new[] {SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName(UsingDirectiveName))};

            root = root.AddUsings(usingSyntax);
            return root;
        }

        private static bool HasUsingDirective(SyntaxNode root, SyntaxNode item)
        {
            var ancestors = new HashSet<SyntaxNode>(item.Ancestors());

            var hasUsingDirective = root.DescendantNodes()
                .Where(node => node.Kind() == SyntaxKind.UsingDirective)
                .OfType<UsingDirectiveSyntax>()
                .Where(x => ancestors.Contains(x.Parent))
                .Any(node => node.Name.ToString().Equals(UsingDirectiveName, StringComparison.Ordinal));
            return hasUsingDirective;
        }

        private static SyntaxNode AddNotNullAttribute(SyntaxNode node)
        {
            var parameter = node as ParameterSyntax;
            if (parameter != null)
            {
                return parameter.WithAttributeLists(parameter.AttributeLists.Add(CreateNotNullAttributeListSyntax()));
            }

            var property = node as PropertyDeclarationSyntax;
            if (property != null)
            {
                return property.WithAttributeLists(property.AttributeLists.Add(CreateNotNullAttributeListSyntax()));
            }

            return node;
        }

        private static AttributeListSyntax CreateNotNullAttributeListSyntax()
        {
            const string notnull = "NotNull";

            var separatedSyntaxList = SyntaxFactory.SeparatedList(new[] {SyntaxFactory.Attribute(SyntaxFactory.IdentifierName(notnull))});
            var attributeList = SyntaxFactory.AttributeList(separatedSyntaxList);
            return attributeList;
        }
    }
}