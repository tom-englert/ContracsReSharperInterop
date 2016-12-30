﻿using System.Diagnostics;

namespace ContracsReSharperInterop.Test
{
    using Xunit;

    public class CreateContractInvariantMethodTest : CreateContractInvariantMethodCodeFixVerifier
    {
        [Fact]
        public void NoDiagnosticIsGeneratedIfClassDoesNotHaveNotNullFieldAttributes()
        {
            const string originalCode = @"
namespace Test
{
    class Class
    {
        private int _field;

        void Method(object arg, object arg2) 
        {
        }
    }
}";

            VerifyCSharpDiagnostic(originalCode);
        }

        [Fact]
        public void NoDiagnosticIsGeneratedIfClassDoesHaveNotNullFieldAttributesAndContractInvariantMethod()
        {
            const string originalCode = @"
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        [NotNull] private int _field;

        void Method(object arg, object arg2) 
        {
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
        }
    }
}";

            VerifyCSharpDiagnostic(originalCode);
        }

        [Fact]
        public void DiagnosticIsGeneratedIfClassDoesHaveNotNullFieldAttributesButNoContractInvariantMethod()
        {
            const string originalCode = @"
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        [NotNull] private int _field;

        void Method(object arg, object arg2) 
        {
        }
    }
}";

            var expected = new DiagnosticResult(8, 5, "Class");

            VerifyCSharpDiagnostic(originalCode, expected);

            const string fixedCode = @"
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        [NotNull] private int _field;

        void Method(object arg, object arg2) 
        {
        }

        [ContractInvariantMethod]
        [SuppressMessage(""Microsoft.Performance"", ""CA1822: MarkMembersAsStatic"", Justification = ""Required for code contracts."")]
        [Conditional(""CONTRACTS_FULL"")]
        private void ObjectInvariant()
        {
        }
    }
}";

            VerifyCSharpFix(originalCode, fixedCode, null, true);
        }
    }
}