namespace ContracsReSharperInterop.Test
{
    using Xunit;

    public class CombinedTests : CodeFixVerifier
    {
        [Fact]
        public void AllInOne()
        {
            const string originalCode = @"
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace Test
{
    class Class
    {
        private IList<object> _test;

        public object Test { get; set; }

        public IList<object> MyMethod(object arg1, IList<object> arg2)
        {
            Contract.Requires(arg1 != null);
            Contract.Requires(arg2 != null);
            Contract.Requires(arg2.Any());
            Contract.Ensures(Contract.Result<IList<object>>() != null);
            Contract.Ensures(Contract.Result<IList<object>>().Any());

            return arg2;
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(Test != null);
            Contract.Invariant(_test != null);
            Contract.Invariant(_test.Any());
        }
    }
}";

            var expected1 = new DiagnosticResult(9, 9, "_test");
            var expected2 = new DiagnosticResult(11, 23, "Test");
            var expected3 = new DiagnosticResult(13, 30, "MyMethod");
            var expected4 = new DiagnosticResult(13, 46, "arg1");
            var expected5 = new DiagnosticResult(13, 66, "arg2");

            VerifyCSharpDiagnostic(originalCode, expected1, expected2, expected3, expected4, expected5);

            const string fixedCode = @"
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        [NotNull]
        private IList<object> _test;

        [NotNull]
        public object Test { get; set; }

        [NotNull]
        public IList<object> MyMethod([NotNull] object arg1, [NotNull] IList<object> arg2)
        {
            Contract.Requires(arg1 != null);
            Contract.Requires(arg2 != null);
            Contract.Requires(arg2.Any());
            Contract.Ensures(Contract.Result<IList<object>>() != null);
            Contract.Ensures(Contract.Result<IList<object>>().Any());

            return arg2;
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(Test != null);
            Contract.Invariant(_test != null);
            Contract.Invariant(_test.Any());
        }
    }
}";

            VerifyCSharpFix(originalCode, fixedCode, null, true);
        }
    }
}
