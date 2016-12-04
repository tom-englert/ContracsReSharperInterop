namespace ContracsReSharperInterop.Test
{
    using Xunit;

    public class RequiresTests : NotNullForContractCodeFixVerifier
    {
        [Fact]
        public void EmptyTextGeneratesNoFixes()
        {
            var code = string.Empty;

            VerifyCSharpDiagnostic(code);
        }

        [Fact]
        public void SimpleMethodWithNotNullArgument()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;

namespace Test
{
    class Class
    {
        void Method(object arg)
        {
            Contract.Requires(arg != null);
        }
    }
}";

            var expected = new DiagnosticResult(8, 28, "arg");

            VerifyCSharpDiagnostic(originalCode, expected);

            const string fixedCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        void Method([NotNull] object arg)
        {
            Contract.Requires(arg != null);
        }
    }
}";

            VerifyCSharpFix(originalCode, fixedCode, null, true);
        }

        [Fact]
        public void SimpleMethodWithNotNullArgumentThatAlreadyHasAnAttribute()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        void Method([NotNull] object arg)
        {
            Contract.Requires(arg != null);
        }
    }
}";

            VerifyCSharpDiagnostic(originalCode);
        }

        [Fact]
        public void SimpleMethodWithNotNullArgumentAndExistingNamespace()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        void Method(object arg)
        {
            Contract.Requires(arg != null);
        }
    }
}";

            var expected = new DiagnosticResult(9, 28, "arg");

            VerifyCSharpDiagnostic(originalCode, expected);

            const string fixedCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        void Method([NotNull] object arg)
        {
            Contract.Requires(arg != null);
        }
    }
}";

            VerifyCSharpFix(originalCode, fixedCode, null, true);
        }

        [Fact]
        public void SimpleMethodWithNonEmptyString()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;

namespace Test
{
    class Class
    {
        void Method(string arg)
        {
            Contract.Requires(!string.IsNullOrEmpty(arg));
        }
    }
}";

            var expected = new DiagnosticResult(8, 28, "arg");

            VerifyCSharpDiagnostic(originalCode, expected);

            const string fixedCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        void Method([NotNull] string arg)
        {
            Contract.Requires(!string.IsNullOrEmpty(arg));
        }
    }
}";

            VerifyCSharpFix(originalCode, fixedCode, null, true);
        }

        [Fact]
        public void SimpleMethodWithNonEmptyStringAlternateNotation()
        {
            const string originalCode = @"
using System;
using System.Diagnostics.Contracts;

namespace Test
{
    class Class
    {
        void Method(String arg)
        {
            Contract.Requires(!String.IsNullOrEmpty(arg));
        }
    }
}";

            var expected = new DiagnosticResult(9, 28, "arg");

            VerifyCSharpDiagnostic(originalCode, expected);

            const string fixedCode = @"
using System;
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        void Method([NotNull] String arg)
        {
            Contract.Requires(!String.IsNullOrEmpty(arg));
        }
    }
}";

            VerifyCSharpFix(originalCode, fixedCode, null, true);
        }

        [Fact]
        public void SimpleMethodWithNotNullArgumentAndReverseComparison()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;

namespace Test
{
    class Class
    {
        void Method(object arg)
        {
            Contract.Requires(null != arg);
        }
    }
}";

            var expected = new DiagnosticResult(8, 28, "arg");

            VerifyCSharpDiagnostic(originalCode, expected);

            const string fixedCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        void Method([NotNull] object arg)
        {
            Contract.Requires(null != arg);
        }
    }
}";

            VerifyCSharpFix(originalCode, fixedCode, null, true);
        }

        [Fact]
        public void SimpleMethodTwoNotNullArguments()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;

namespace Test
{
    class Class
    {
        void Method(object arg, string arg2)
        {
            Contract.Requires(arg != null);
            Contract.Requires(arg2 != null);
        }
    }
}";

            var expected1 = new DiagnosticResult(8, 28, "arg");
            var expected2 = new DiagnosticResult(8, 40, "arg2");

            VerifyCSharpDiagnostic(originalCode, expected1, expected2);

            const string fixedCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        void Method([NotNull] object arg, [NotNull] string arg2)
        {
            Contract.Requires(arg != null);
            Contract.Requires(arg2 != null);
        }
    }
}";

            VerifyCSharpFix(originalCode, fixedCode, null, true);
        }

        [Fact]
        public void SimpleMethodTwoNotNullArgumentsWhereOneHasAlreadyAnAttribute()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        void Method([NotNull] object arg, string arg2)
        {
            Contract.Requires(arg != null);
            Contract.Requires(arg2 != null);
        }
    }
}";

            var expected2 = new DiagnosticResult(9, 50, "arg2");

            VerifyCSharpDiagnostic(originalCode, expected2);

            const string fixedCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        void Method([NotNull] object arg, [NotNull] string arg2)
        {
            Contract.Requires(arg != null);
            Contract.Requires(arg2 != null);
        }
    }
}";

            VerifyCSharpFix(originalCode, fixedCode, null, true);
        }

        [Fact]
        public void SimplePropertyWithNotNullArgument()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;

namespace Test
{
    class Class
    {
        object Property
        {
            get
            {
                Contract.Ensures(Contract.Result<object>() != null);
                return default(object);
            }
            set
            {
                Contract.Requires(value != null);
            }
        }
    }
}";

            var expected = new DiagnosticResult(8, 16, "Property");

            VerifyCSharpDiagnostic(originalCode, expected);

            const string fixedCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        [NotNull]
        object Property
        {
            get
            {
                Contract.Ensures(Contract.Result<object>() != null);
                return default(object);
            }
            set
            {
                Contract.Requires(value != null);
            }
        }
    }
}";

            VerifyCSharpFix(originalCode, fixedCode, null, true);
        }

        [Fact]
        public void SimplePropertyWithNotNullArgumentAndExplicitTypeNames()
        {
            const string originalCode = @"
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        object Property
        {
            get
            {
                System.Diagnostics.Contracts.Contract.Ensures(System.Diagnostics.Contracts.Contract.Result<object>() != null);
                return default(object);
            }
            set
            {
                System.Diagnostics.Contracts.Contract.Requires(value != null);
            }
        }
    }
}";

            var expected = new DiagnosticResult(8, 16, "Property");

            VerifyCSharpDiagnostic(originalCode, expected);

            const string fixedCode = @"
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        [NotNull]
        object Property
        {
            get
            {
                System.Diagnostics.Contracts.Contract.Ensures(System.Diagnostics.Contracts.Contract.Result<object>() != null);
                return default(object);
            }
            set
            {
                System.Diagnostics.Contracts.Contract.Requires(value != null);
            }
        }
    }
}";

            VerifyCSharpFix(originalCode, fixedCode, null, true);
        }

        [Fact]
        public void SimplePropertyWithNotNullArgumentThatAlreadyHasAnAttribute()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        [NotNull]
        object Property
        {
            get
            {
                Contract.Ensures(Contract.Result<object>() != null);
                return default(object);
            }
            set
            {
                Contract.Requires(value != null);
            }
        }
    }
}";

            VerifyCSharpDiagnostic(originalCode);
        }

        [Fact]
        public void SimplePropertyWithNotNullArgumentThatAlreadyHasAFullQualifiedAttribute()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;

namespace Test
{
    class Class
    {
        [JetBrains.Annotations.NotNull]
        object Property
        {
            get
            {
                Contract.Ensures(Contract.Result<object>() != null);
                return default(object);
            }
            set
            {
                Contract.Requires(value != null);
            }
        }
    }
}";

            VerifyCSharpDiagnostic(originalCode);
        }
    }
}

namespace Test
{
    using System;
    using System.Diagnostics.Contracts;

    class Class
    {
        void Method(String arg)
        {
            Contract.Requires(!String.IsNullOrEmpty(arg));
        }
    }
}