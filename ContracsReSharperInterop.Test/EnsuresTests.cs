namespace ContracsReSharperInterop.Test
{
    using Xunit;

    public class EnsuresTests : NotNullForContractCodeFixVerifier
    {
        [Fact]
        public void MethodEnsuresNotNull()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;

namespace Test
{
    class Class
    {
        object MyMethod(object arg)
        {
            Contract.Ensures(Contract.Result<object>() != null);
            return arg;
        }
    }
}";

            var expected = new DiagnosticResult(8, 16, "MyMethod");

            VerifyCSharpDiagnostic(originalCode, expected);

            const string fixedCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        [NotNull]
        object MyMethod(object arg)
        {
            Contract.Ensures(Contract.Result<object>() != null);
            return arg;
        }
    }
}";

            VerifyCSharpFix(originalCode, fixedCode, null, true);
        }

        [Fact]
        public void MethodEnsuresStringIsNotNull()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;

namespace Test
{
    class Class
    {
        string MyMethod(object arg)
        {
            Contract.Ensures(!string.IsNullOrEmpty(Contract.Result<string>()));
            return null;
        }
    }
}";

            var expected = new DiagnosticResult(8, 16, "MyMethod");

            VerifyCSharpDiagnostic(originalCode, expected);

            const string fixedCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        [NotNull]
        string MyMethod(object arg)
        {
            Contract.Ensures(!string.IsNullOrEmpty(Contract.Result<string>()));
            return null;
        }
    }
}";

            VerifyCSharpFix(originalCode, fixedCode, null, true);
        }

        [Fact]
        public void PropertyEnsuresNotNull()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;

namespace Test
{
    class Class
    {
        object MyProperty
        {
            get
            {
                Contract.Ensures(Contract.Result<object>() != null);
                return null;
            }
        }
    }
}";

            var expected = new DiagnosticResult(8, 16, "MyProperty");

            VerifyCSharpDiagnostic(originalCode, expected);

            const string fixedCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        [NotNull]
        object MyProperty
        {
            get
            {
                Contract.Ensures(Contract.Result<object>() != null);
                return null;
            }
        }
    }
}";

            VerifyCSharpFix(originalCode, fixedCode, null, true);
        }

        [Fact]
        public void MethodWithExistingArbitraryAttributeEnsuresNotNull()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;

namespace Test
{
    class Class
    {
        [System.Diagnostics.DebuggerHidden]
        object MyMethod(object arg)
        {
            Contract.Ensures(Contract.Result<object>() != null);
            return arg;
        }
    }
}";

            var expected = new DiagnosticResult(9, 16, "MyMethod");

            VerifyCSharpDiagnostic(originalCode, expected);

            const string fixedCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        [System.Diagnostics.DebuggerHidden]
        [NotNull]
        object MyMethod(object arg)
        {
            Contract.Ensures(Contract.Result<object>() != null);
            return arg;
        }
    }
}";

            VerifyCSharpFix(originalCode, fixedCode, null, true);
        }

    }
}
