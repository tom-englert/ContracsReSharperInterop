namespace ContracsReSharperInterop
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using JetBrains.Annotations;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;

    using TomsToolbox.Core;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CreateContractInvariantMethodCodeFixProvider)), Shared]
    public class CreateContractInvariantMethodCodeFixProvider : CodeFixProvider
    {
        private static readonly string[] UsingDirectiveNames = { "System.Diagnostics", "System.Diagnostics.Contracts", "System.Diagnostics.CodeAnalysis" };
        private const string Title = "Add Contract Invariant Method";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CreateContractInvariantMethodAnalyzer.DiagnosticId);

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

                var syntaxNode = root.FindNode(diagnosticSpan) as ClassDeclarationSyntax;
                if (syntaxNode == null)
                    return;

                var codeAction = CodeAction.Create(Title, c => AddContractInvariantMethodAsync(context.Document, syntaxNode, c), Title);

                context.RegisterCodeFix(codeAction, diagnostic);
            }
        }

        private static async Task<Document> AddContractInvariantMethodAsync([NotNull] Document document, [NotNull] ClassDeclarationSyntax classDeclaration, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken);

            var root = editor.OriginalRoot;

            var missingUsingDirectives = UsingDirectiveNames.Where(dir => !root.HasUsingDirective(classDeclaration, dir)).ToArray();

            var firstNode = root.DescendantNodes().OfType<UsingDirectiveSyntax>().FirstOrDefault();
            if (firstNode != null)
                editor.InsertBefore(firstNode, missingUsingDirectives.Select(dir => SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName(dir))));

            var invariantMethod = SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), "ObjectInvariant")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
                .AddAttributeLists(CreateAttributeListSyntax("ContractInvariantMethod"))
                .AddAttributeLists(CreateAttributeListSyntax("SuppressMessage", @"(""Microsoft.Performance"", ""CA1822: MarkMembersAsStatic"", Justification = ""Required for code contracts."")"))
                .AddAttributeLists(CreateAttributeListSyntax("Conditional", @"(""CONTRACTS_FULL"")"))
                .AddBodyStatements();

            editor.AddMember(classDeclaration, invariantMethod);

            return editor.GetChangedDocument();
        }

        private static AttributeListSyntax CreateAttributeListSyntax([NotNull] string attributeName, string arguments = null)
        {
            var attributeSyntax = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName(attributeName));

            if (arguments != null)
            {
                var argumentList = SyntaxFactory.ParseAttributeArgumentList(arguments).Arguments.ToArray();

                attributeSyntax = attributeSyntax.AddArgumentListArguments(argumentList);
            }

            var separatedSyntaxList = SyntaxFactory.SeparatedList(new[] { attributeSyntax });

            return SyntaxFactory.AttributeList(separatedSyntaxList);
        }
    }
}