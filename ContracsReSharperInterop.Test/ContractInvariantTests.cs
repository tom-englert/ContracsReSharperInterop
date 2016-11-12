namespace ContracsReSharperInterop.Test
{
    using Xunit;

    public class ContractInvariantTests : ConditionalForContractInvariantCodeFixVerifier
    {
        [Fact]
        public void ContractInvariantMethodWithoutConditionalAttributeGeneratesDiagnostic()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;
using System.Diagnostics.CodeAnalysis;

namespace Test
{
    class Class
    {
        private object _field;

        [ContractInvariantMethod]
        [SuppressMessage(""Microsoft.Performance"", ""CA1822: MarkMembersAsStatic"", Justification = ""Required for code contracts."")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_field != null);
        }
    }
}";

            var expected = new DiagnosticResult(13, 22, "ObjectInvariant");

            VerifyCSharpDiagnostic(originalCode, expected);

            const string fixedCode = @"
using System.Diagnostics.Contracts;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;

namespace Test
{
    class Class
    {
        private object _field;

        [ContractInvariantMethod]
        [SuppressMessage(""Microsoft.Performance"", ""CA1822: MarkMembersAsStatic"", Justification = ""Required for code contracts."")]
        [Conditional(""CONTRACTS_FULL"")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_field != null);
        }
    }
}";

            VerifyCSharpFix(originalCode, fixedCode, null, true);
        }

        [Fact]
        public void ContractInvariantMethodWithExistingDiscreteConditionalAttributeDoesNotGenerateDiagnostic()
        {
            const string originalCode = @"
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Diagnostics.CodeAnalysis;

namespace Test
{
    class Class
    {
        private object _field;

        [ContractInvariantMethod]
        [SuppressMessage(""Microsoft.Performance"", ""CA1822: MarkMembersAsStatic"", Justification = ""Required for code contracts."")]
        [Conditional(""CONTRACTS_FULL"")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_field != null);
        }
    }
}";

            VerifyCSharpDiagnostic(originalCode);
        }

        [Fact]
        public void ContractInvariantMethodWithExistingDiscreteFullQualifiedNameConditionalAttributeDoesNotGenerateDiagnostic()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;
using System.Diagnostics.CodeAnalysis;

namespace Test
{
    class Class
    {
        private object _field;

        [ContractInvariantMethod]
        [SuppressMessage(""Microsoft.Performance"", ""CA1822: MarkMembersAsStatic"", Justification = ""Required for code contracts."")]
        [System.Diagnostics.Conditional(""CONTRACTS_FULL"")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_field != null);
        }
    }
}";

            VerifyCSharpDiagnostic(originalCode);
        }

        [Fact]
        public void ContractInvariantMethodWithExistingCombinedConditionalAttributeDoesNotGenerateDiagnostic()
        {
            const string originalCode = @"
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Diagnostics.CodeAnalysis;

namespace Test
{
    class Class
    {
        private object _field;

        [ContractInvariantMethod, Conditional(""CONTRACTS_FULL"")]
        [SuppressMessage(""Microsoft.Performance"", ""CA1822: MarkMembersAsStatic"", Justification = ""Required for code contracts."")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_field != null);
        }
    }
}";

            VerifyCSharpDiagnostic(originalCode);
        }

        [Fact]
        public void ContractInvariantMethodWithExistingCombinedFullQualifiedNameConditionalAttributeDoesNotGenerateDiagnostic()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;
using System.Diagnostics.CodeAnalysis;

namespace Test
{
    class Class
    {
        private object _field;

        [ContractInvariantMethod, System.Diagnostics.Conditional(""CONTRACTS_FULL"")]
        [SuppressMessage(""Microsoft.Performance"", ""CA1822: MarkMembersAsStatic"", Justification = ""Required for code contracts."")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_field != null);
        }
    }
}";

            VerifyCSharpDiagnostic(originalCode);
        }
    }
}
