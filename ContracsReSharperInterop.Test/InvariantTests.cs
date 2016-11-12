namespace ContracsReSharperInterop.Test
{
    using Xunit;

    public class InvariantTests : NotNullForContractCodeFixVerifier
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

        [Fact]
        public void InvariantPropertyWithRegularComment()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;

namespace Test
{
    class Class
    {
        // this is a test
        public object Test { get; set; }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(Test != null);
        }
    }
}";

            var expected = new DiagnosticResult(9, 23, "Test");

            VerifyCSharpDiagnostic(originalCode, expected);

            const string fixedCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        // this is a test
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

        [Fact]
        public void InvariantPropertyWithXmlComment()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;

namespace Test
{
    class Class
    {
        /// <summary>
        /// This is a Test!
        /// </summary>
        public object Test { get; set; }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(Test != null);
        }
    }
}";

            var expected = new DiagnosticResult(11, 23, "Test");

            VerifyCSharpDiagnostic(originalCode, expected);

            const string fixedCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        /// <summary>
        /// This is a Test!
        /// </summary>
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

        [Fact]
        public void InvariantPropertyWithExistingAttributeAndXmlComment()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;

namespace Test
{
    class Class
    {
        /// <summary>
        /// This is a Test!
        /// </summary>
        [System.Diagnostics.DebuggerHidden]
        public object Test { get; set; }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(Test != null);
        }
    }
}";

            var expected = new DiagnosticResult(12, 23, "Test");

            VerifyCSharpDiagnostic(originalCode, expected);

            const string fixedCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        /// <summary>
        /// This is a Test!
        /// </summary>
        [System.Diagnostics.DebuggerHidden]
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
