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

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CreateContractClassCodeFixProvider)), Shared]
    public class CreateContractClassCodeFixProvider : CodeFixProvider
    {
        private static readonly string[] UsingDirectiveNames = { "System", "System.Diagnostics.Contracts" };
        private const string Title = "Add Contract Class";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CreateContractClassAnalyzer.DiagnosticId);

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

                var syntaxNode = root.FindNode(diagnosticSpan) as MemberDeclarationSyntax;
                if (syntaxNode == null)
                    return;

                var codeAction = CodeAction.Create(Title, c => AddContractClassAsync(context.Document, syntaxNode, c), Title);

                context.RegisterCodeFix(codeAction, diagnostic);
            }
        }

        private static async Task<Document> AddContractClassAsync([NotNull] Document document, [NotNull] MemberDeclarationSyntax memberDeclaration, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) as CompilationUnitSyntax;
            if (root == null)
                return document;

            var editor = await DocumentEditor.CreateAsync(document, cancellationToken);

            var missingUsingDirectives = UsingDirectiveNames.Where(dir => !root.HasUsingDirective(memberDeclaration, dir)).ToArray();

            var firstNode = root.DescendantNodes().OfType<UsingDirectiveSyntax>().FirstOrDefault();
            if (firstNode != null)
                editor.InsertBefore(firstNode, missingUsingDirectives.Select(dir => SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName(dir))));

            var classWithContractAttribute = AddContractClassAttribute(memberDeclaration);

            editor.ReplaceNode(memberDeclaration, classWithContractAttribute);

            var parent = memberDeclaration.Parent;

            var contractClassName = GetContractClassName(memberDeclaration);
            var baseClassName = GetIdentifierText(memberDeclaration);

            var contractClass = SyntaxFactory.ClassDeclaration(contractClassName)
                .AddAttributeLists(CreateContractClassForAttributeListSyntax(memberDeclaration))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.InternalKeyword), SyntaxFactory.Token(SyntaxKind.AbstractKeyword))
                .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(baseClassName)))
                .AddMembers(CreateContractClassMembers(memberDeclaration).ToArray());

            editor.AddMember(parent, contractClass);

            return editor.GetChangedDocument();
        }

        private static MemberDeclarationSyntax CreateOverride([NotNull] MethodDeclarationSyntax member)
        {
            return CreateImplementation(member).AddModifiers(SyntaxFactory.Token(SyntaxKind.OverrideKeyword));
        }

        private static MethodDeclarationSyntax CreateImplementation([NotNull] MethodDeclarationSyntax member)
        {
            var declaration = SyntaxFactory.MethodDeclaration(member.ReturnType, member.Identifier.Text)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(member.ParameterList.Parameters.Select(Clone).ToArray())
                .AddBodyStatements(CreateNotImplementedStatement());

            return declaration;
        }

        private static MemberDeclarationSyntax CreateOverride([NotNull] PropertyDeclarationSyntax member)
        {
            return CreateImplementation(member).AddModifiers(SyntaxFactory.Token(SyntaxKind.OverrideKeyword));
        }

        private static PropertyDeclarationSyntax CreateImplementation([NotNull] PropertyDeclarationSyntax member)
        {
            var declaration = SyntaxFactory.PropertyDeclaration(member.Type, member.Identifier.Text)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddAccessorListAccessors(member.AccessorList.Accessors.Select(Clone).ToArray());

            return declaration;
        }

        private static MemberDeclarationSyntax CreateOverride([NotNull] EventFieldDeclarationSyntax member)
        {
            return CreateImplementation(member).AddModifiers(SyntaxFactory.Token(SyntaxKind.OverrideKeyword));
        }

        private static EventFieldDeclarationSyntax CreateImplementation([NotNull] EventFieldDeclarationSyntax member)
        {
            var variables = member.Declaration.Variables.Select(v => SyntaxFactory.VariableDeclarator(v.Identifier.Text)).ToArray();
            var declaration = SyntaxFactory.EventFieldDeclaration(SyntaxFactory.VariableDeclaration(member.Declaration.Type).AddVariables(variables))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.AbstractKeyword));

            return declaration;
        }

        private static MemberDeclarationSyntax CreateOverride([NotNull] MemberDeclarationSyntax member)
        {
            return member.TryCast().Returning<MemberDeclarationSyntax>()
                .When<MethodDeclarationSyntax>(CreateOverride)
                .When<PropertyDeclarationSyntax>(CreateOverride)
                .When<EventFieldDeclarationSyntax>(CreateOverride)
                .ElseThrow("Unsupported abstract member: " + member);
        }

        private static MemberDeclarationSyntax CreateImplementation([NotNull] MemberDeclarationSyntax member)
        {
            return member.TryCast().Returning<MemberDeclarationSyntax>()
                .When<MethodDeclarationSyntax>(CreateImplementation)
                .When<PropertyDeclarationSyntax>(CreateImplementation)
                .When<EventFieldDeclarationSyntax>(CreateImplementation)
                .ElseThrow("Unsupported interface member: " + member);
        }

        private static IEnumerable<MemberDeclarationSyntax> CreateContractClassMembers([NotNull] MemberDeclarationSyntax baseMemberDeclaration)
        {
            return baseMemberDeclaration.TryCast().Returning<IEnumerable<MemberDeclarationSyntax>>()
                .When<ClassDeclarationSyntax>(CreateContractClassMembers)
                .When<InterfaceDeclarationSyntax>(CreateContractClassMembers)
                .ElseThrow("unsupported member: " + baseMemberDeclaration);
        }

        [NotNull]
        private static IEnumerable<MemberDeclarationSyntax> CreateContractClassMembers([NotNull] InterfaceDeclarationSyntax baseInterface)
        {
            return baseInterface.Members.Select(CreateImplementation);
        }

        [NotNull]
        private static IEnumerable<MemberDeclarationSyntax> CreateContractClassMembers([NotNull] ClassDeclarationSyntax baseClass)
        {
            return baseClass.Members.Where(m => m.IsAbstractMember()).Select(CreateOverride);
        }

        private static ThrowStatementSyntax CreateNotImplementedStatement()
        {
            return SyntaxFactory.ThrowStatement(SyntaxFactory.ParseExpression("new NotImplementedException()"));
        }

        private static ParameterSyntax Clone([NotNull] ParameterSyntax parameter)
        {
            return SyntaxFactory.Parameter(parameter.Identifier).WithType(parameter.Type);
        }

        private static AccessorDeclarationSyntax Clone([NotNull] AccessorDeclarationSyntax accessor)
        {
            return SyntaxFactory.AccessorDeclaration(accessor.Kind(), SyntaxFactory.Block(CreateNotImplementedStatement()))
                .WithModifiers(accessor.Modifiers);
        }

        private static MemberDeclarationSyntax AddContractClassAttribute([NotNull] MemberDeclarationSyntax memberDeclaration)
        {
            return memberDeclaration.WithAttribute(CreateContractClassAttributeListSyntax(memberDeclaration));
        }

        private static AttributeListSyntax CreateContractClassAttributeListSyntax([NotNull] MemberDeclarationSyntax memberDeclaration)
        {
            return CreateContractClassAttributeListSyntax(GetContractClassName(memberDeclaration), "ContractClass");
        }

        private static AttributeListSyntax CreateContractClassForAttributeListSyntax([NotNull] MemberDeclarationSyntax memberDeclaration)
        {
            return CreateContractClassAttributeListSyntax(GetIdentifierText(memberDeclaration), "ContractClassFor");
        }

        private static AttributeListSyntax CreateContractClassAttributeListSyntax([NotNull] string className, [NotNull] string attributeName)
        {
            var attributeArguments = $"(typeof({className}))";

            var arguments = SyntaxFactory.ParseAttributeArgumentList(attributeArguments).Arguments.ToArray();

            var attributeSyntax = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName(attributeName)).AddArgumentListArguments(arguments);

            var separatedSyntaxList = SyntaxFactory.SeparatedList(new[] { attributeSyntax });

            return SyntaxFactory.AttributeList(separatedSyntaxList);
        }

        [NotNull]
        private static string GetIdentifierText([NotNull] MemberDeclarationSyntax memberDeclaration)
        {
            return memberDeclaration.TryCast().Returning<string>()
                .When<ClassDeclarationSyntax>(m => m.Identifier.Text)
                .When<InterfaceDeclarationSyntax>(m => m.Identifier.Text)
                .ElseThrow("unsupported member: " + memberDeclaration);
        }

        [NotNull]
        private static string GetContractClassName([NotNull] MemberDeclarationSyntax memberDeclaration)
        {
            var baseName = memberDeclaration.TryCast().Returning<string>()
                .When<ClassDeclarationSyntax>(m => m.Identifier.Text)
                .When<InterfaceDeclarationSyntax>(m => StripInterfaceNamePrefix(m.Identifier.Text))
                .ElseThrow("unsupported member: " + memberDeclaration);

            return baseName + "Contract";
        }

        private static string StripInterfaceNamePrefix([NotNull] string name)
        {
            if ((name.Length > 0) && (name[0] == 'I'))
                return name.Substring(1);

            return name;
        }
    }
}