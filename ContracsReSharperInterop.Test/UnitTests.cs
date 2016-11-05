namespace ContracsReSharperInterop.Test
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;

    using Xunit;

    public partial class UnitTest : Framework.CodeFixVerifier
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

            var expected = new DiagnosticResult(8, 21, "arg");

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

            var expected = new DiagnosticResult(9, 21, "arg");

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

            var expected = new DiagnosticResult(8, 21, "arg");

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

            var expected = new DiagnosticResult(8, 21, "arg");

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

            var expected1 = new DiagnosticResult(8, 21, "arg");
            var expected2 = new DiagnosticResult(8, 33, "arg2");

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
    }

    // ovverides & tools
    public partial class UnitTest
    {
        private class DiagnosticResult : Framework.DiagnosticResult
        {
            public DiagnosticResult(int line, int column, string elementName)
            {
                Id = "ContracsReSharperInterop";
                Message = $"Element '{elementName}' has a not-null contract but does not have a corresponding [NotNull] attribute.";
                Severity = DiagnosticSeverity.Warning;
                Locations = new[] { new Framework.DiagnosticResultLocation("Test0.cs", line, column) };
            }
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new ContracsReSharperInteropCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ContracsReSharperInteropAnalyzer();
        }
    }
}