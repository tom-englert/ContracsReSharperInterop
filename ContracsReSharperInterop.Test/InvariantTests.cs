﻿using System.Diagnostics.Contracts;
namespace ContracsReSharperInterop.Test
{
    using Xunit;

    public class InvariantTests : CodeFixVerifier
    {
        [Fact]
        public void InvariantField()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;

namespace Test
{
    class Class
    {
        private readonly object _test;

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(_test != null);
        }
    }
}";

            var expected = new DiagnosticResult(8, 9, "_test");

            VerifyCSharpDiagnostic(originalCode, expected);

            const string fixedCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        [NotNull]
        private readonly object _test;

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(_test != null);
        }
    }
}";

            VerifyCSharpFix(originalCode, fixedCode, null, true);
        }
        [Fact]
        public void InvariantProperty()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;

namespace Test
{
    class Class
    {
        public object Test { get; set; }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(Test != null);
        }
    }
}";

            var expected = new DiagnosticResult(8, 23, "Test");

            VerifyCSharpDiagnostic(originalCode, expected);

            const string fixedCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        [NotNull]
        public object Test { get; set; }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(Test != null);
        }
    }
}";

            VerifyCSharpFix(originalCode, fixedCode, null, true);
        }
    }
}
