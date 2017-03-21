namespace ContracsReSharperInterop.Test
{
    using Xunit;

    public class ContractForNotNullTest : ContractForNotNullCodeFixVerifier
    {
        [Fact]
        public void NoDiagnosticIsGeneratedIfAllNotNullParametersHaveContracts()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        void Method([NotNull] object arg, [NotNull] object arg2)
        {
            Contract.Requires(arg != null);
            Contract.Requires(arg2 != null);
        }
    }
}";

            VerifyCSharpDiagnostic(originalCode);
        }

        [Fact]
        public void DiagnosticIsGeneratedIfNotAllNotNullParametersHaveContracts()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        void Method([NotNull] object arg, [NotNull] object arg2)
        {
            Contract.Requires(arg != null);

            arg = arg2;
        }
    }
}";

            var expected = new DiagnosticResult(9, 60, "arg2");

            VerifyCSharpDiagnostic(originalCode, expected);

            const string fixedCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        void Method([NotNull] object arg, [NotNull] object arg2)
        {
            Contract.Requires(arg != null);
            Contract.Requires(arg2 != null);

            arg = arg2;
        }
    }
}";

            VerifyCSharpFix(originalCode, fixedCode, null, true);
        }

        [Fact]
        public void DiagnosticIsGeneratedIfNotAllNotNullParametersOfConstructorHaveContracts()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        Class([NotNull] object arg, [NotNull] object arg2)
        {
            Contract.Requires(arg != null);

            arg = arg2;
        }
    }
}";

            var expected = new DiagnosticResult(9, 54, "arg2");

            VerifyCSharpDiagnostic(originalCode, expected);

            const string fixedCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        Class([NotNull] object arg, [NotNull] object arg2)
        {
            Contract.Requires(arg != null);
            Contract.Requires(arg2 != null);

            arg = arg2;
        }
    }
}";

            VerifyCSharpFix(originalCode, fixedCode, null, true);
        }

        [Fact]
        public void DiagnosticIsGeneratedIfAllNotNullParametersHaveNoContracts()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        void Method([NotNull] object arg, [NotNull] object arg2)
        {
            arg = arg2;
        }
    }
}";

            var expected1 = new DiagnosticResult(9, 38, "arg");
            var expected2 = new DiagnosticResult(9, 60, "arg2");

            VerifyCSharpDiagnostic(originalCode, expected1, expected2);

            const string fixedCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        void Method([NotNull] object arg, [NotNull] object arg2)
        {
            Contract.Requires(arg != null);
            Contract.Requires(arg2 != null);
            arg = arg2;
        }
    }
}";

            VerifyCSharpFix(originalCode, fixedCode, null, true);
        }

        [Fact]
        public void DiagnosticIsGeneratedIfResultHasNoContract()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        [NotNull]
        object Method([NotNull] object arg, [NotNull] object arg2)
        {
            Contract.Requires(arg != null);
            Contract.Requires(arg2 != null);

            arg = arg2;
            return null;
        }
    }
}";

            var expected = new DiagnosticResult(10, 16, "Method");

            VerifyCSharpDiagnostic(originalCode, expected);

            const string fixedCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        [NotNull]
        object Method([NotNull] object arg, [NotNull] object arg2)
        {
            Contract.Requires(arg != null);
            Contract.Requires(arg2 != null);
            Contract.Ensures(Contract.Result<object>() != null);

            arg = arg2;
            return null;
        }
    }
}";

            VerifyCSharpFix(originalCode, fixedCode, null, true);
        }

        [Fact]
        public void DiagnosticIsGeneratedOnContractClassIfResultHasNoContract()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    [ContractClass(typeof(InterfaceContract))]
    interface Interface
    {
        [NotNull]
        object Method();
    }

    [ContractClassFor(typeof(Interface))]
    abstract class InterfaceContract : Interface
    {
        public object Method()
        {
            return null;
        }
    }
}";

            var expected = new DiagnosticResult(11, 16, "Method");

            VerifyCSharpDiagnostic(originalCode, expected);

            const string fixedCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    [ContractClass(typeof(InterfaceContract))]
    interface Interface
    {
        [NotNull]
        object Method();
    }

    [ContractClassFor(typeof(Interface))]
    abstract class InterfaceContract : Interface
    {
        public object Method()
        {
            Contract.Ensures(Contract.Result<object>() != null);
            return null;
        }
    }
}";

            VerifyCSharpFix(originalCode, fixedCode, null, true);
        }

        [Fact]
        public void NoDiagnosticIsGeneratedOnContractClassIfResultHasAlreadyContract()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    [ContractClass(typeof(InterfaceContract))]
    interface Interface
    {
        [NotNull]
        object Method();
    }

    [ContractClassFor(typeof(Interface))]
    abstract class InterfaceContract : Interface
    {
        public object Method()
        {
            Contract.Ensures(Contract.Result<object>() != null);
            return null;
        }
    }
}";

            VerifyCSharpDiagnostic(originalCode);
        }

        [Fact]
        public void DiagnosticIsGeneratedIfGenericResultHasNoContract()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        [NotNull]
        public System.Collections.Generic.IEnumerable<object> Method([NotNull] object arg, [NotNull] object arg2)
        {
            Contract.Requires(arg != null);
            Contract.Requires(arg2 != null);

            arg = arg2;
            return null;
        }
    }
}";

            var expected = new DiagnosticResult(10, 63, "Method");

            VerifyCSharpDiagnostic(originalCode, expected);

            const string fixedCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        [NotNull]
        public System.Collections.Generic.IEnumerable<object> Method([NotNull] object arg, [NotNull] object arg2)
        {
            Contract.Requires(arg != null);
            Contract.Requires(arg2 != null);
            Contract.Ensures(Contract.Result<System.Collections.Generic.IEnumerable<object>>() != null);

            arg = arg2;
            return null;
        }
    }
}";

            VerifyCSharpFix(originalCode, fixedCode, null, true);
        }

        [Fact]
        public void NoDiagnosticIsGeneratedIfInterfaceHasNoContractClass()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    interface IClass
    {
        [NotNull]
        System.Collections.Generic.IEnumerable<object> Method([NotNull] object arg, [NotNull] object arg2);
    }
}";

            VerifyCSharpDiagnostic(originalCode);
        }

        [Fact]
        public void NoDiagnosticIsGeneratedIfAbstractClassHasNoContractClass()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    abstract Class
    {
        [NotNull]
        public abstract System.Collections.Generic.IEnumerable<object> Method([NotNull] object arg, [NotNull] object arg2);
    }
}";

            VerifyCSharpDiagnostic(originalCode);
        }

        [Fact]
        public void DiagnosticFixForInterfaceMethodEnsuresIsAppliedToContractClass()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    [ContractClass(typeof(InterfaceContract))]
    interface IInterface
    {
        [NotNull]
        System.Collections.Generic.IEnumerable<object> Method([NotNull] object arg, [NotNull] object arg2);
    }

    [ContractClassFor(typeof(IInterface))]
    abstract class InterfaceContract : IInterface
    {
        public System.Collections.Generic.IEnumerable<object> Method(object arg, object arg2)
        {
            Contract.Requires(arg != null);
            Contract.Requires(arg2 != null);

            return null;
        }
    }
}";

            var expected = new DiagnosticResult(11, 56, "Method");
            VerifyCSharpDiagnostic(originalCode, expected);

            const string fixedCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    [ContractClass(typeof(InterfaceContract))]
    interface IInterface
    {
        [NotNull]
        System.Collections.Generic.IEnumerable<object> Method([NotNull] object arg, [NotNull] object arg2);
    }

    [ContractClassFor(typeof(IInterface))]
    abstract class InterfaceContract : IInterface
    {
        public System.Collections.Generic.IEnumerable<object> Method(object arg, object arg2)
        {
            Contract.Requires(arg != null);
            Contract.Requires(arg2 != null);
            Contract.Ensures(Contract.Result<System.Collections.Generic.IEnumerable<object>>() != null);

            return null;
        }
    }
}";
            VerifyCSharpFix(originalCode, fixedCode, null, true);
        }

        [Fact]
        public void DiagnosticFixesForInterfaceMethodAreAppliedToContractClass()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    [ContractClass(typeof(InterfaceContract))]
    interface IInterface
    {
        [NotNull]
        System.Collections.Generic.IEnumerable<object> Method([NotNull] object arg, [NotNull] object arg2);
    }

    [ContractClassFor(typeof(IInterface))]
    abstract class InterfaceContract : IInterface
    {
        public System.Collections.Generic.IEnumerable<object> Method(object arg, object arg2)
        {
            return null;
        }
    }
}";

            var expected1 = new DiagnosticResult(11, 56, "Method");
            var expected2 = new DiagnosticResult(11, 80, "arg");
            var expected3 = new DiagnosticResult(11, 102, "arg2");
            VerifyCSharpDiagnostic(originalCode, expected1, expected2, expected3);

            const string fixedCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    [ContractClass(typeof(InterfaceContract))]
    interface IInterface
    {
        [NotNull]
        System.Collections.Generic.IEnumerable<object> Method([NotNull] object arg, [NotNull] object arg2);
    }

    [ContractClassFor(typeof(IInterface))]
    abstract class InterfaceContract : IInterface
    {
        public System.Collections.Generic.IEnumerable<object> Method(object arg, object arg2)
        {
            Contract.Requires(arg != null);
            Contract.Requires(arg2 != null);
            Contract.Ensures(Contract.Result<System.Collections.Generic.IEnumerable<object>>() != null);
            return null;
        }
    }
}";
            VerifyCSharpFix(originalCode, fixedCode, null, true);
        }

        [Fact]
        public void DiagnosticFixesForAbstractClassMethodAreAppliedToContractClass()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    [ContractClass(typeof(ClassContract))]
    abstract class Class
    {
        [NotNull]
        public abstract System.Collections.Generic.IEnumerable<object> Method([NotNull] object arg, [NotNull] object arg2);
    }

    [ContractClassFor(typeof(Class))]
    abstract class ClassContract : Class
    {
        public override System.Collections.Generic.IEnumerable<object> Method(object arg, object arg2)
        {
            return null;
        }
    }
}";

            var expected1 = new DiagnosticResult(11, 72, "Method");
            var expected2 = new DiagnosticResult(11, 96, "arg");
            var expected3 = new DiagnosticResult(11, 118, "arg2");
            VerifyCSharpDiagnostic(originalCode, expected1, expected2, expected3);

            const string fixedCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    [ContractClass(typeof(ClassContract))]
    abstract class Class
    {
        [NotNull]
        public abstract System.Collections.Generic.IEnumerable<object> Method([NotNull] object arg, [NotNull] object arg2);
    }

    [ContractClassFor(typeof(Class))]
    abstract class ClassContract : Class
    {
        public override System.Collections.Generic.IEnumerable<object> Method(object arg, object arg2)
        {
            Contract.Requires(arg != null);
            Contract.Requires(arg2 != null);
            Contract.Ensures(Contract.Result<System.Collections.Generic.IEnumerable<object>>() != null);
            return null;
        }
    }
}";
            VerifyCSharpFix(originalCode, fixedCode, null, true);
        }

        [Fact]
        public void NoDiagnosticIsGeneratedIfClassHasNotNullFieldsButNoContractInvariantMethod()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        [NotNull]
        private int _field;

        void Method(object arg, object arg2)
        {
        }
    }
}";

            VerifyCSharpDiagnostic(originalCode);
        }

        [Fact]
        public void NoDiagnosticIsGeneratedIfClassHasNotNullFieldsWithContractInvariant()
        {
            const string originalCode = @"
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        [NotNull]
        private object _field;
        [NotNull]
        private readonly object _field2;

        void Method(object arg, object arg2)
        {
        }

        [ContractInvariantMethod]
        [SuppressMessage(""Microsoft.Performance"", ""CA1822: MarkMembersAsStatic"", Justification = ""Required for code contracts."")]
        [Conditional(""CONTRACTS_FULL"")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_field != null);
            Contract.Invariant(_field2 != null);
        }
    }
}";

            VerifyCSharpDiagnostic(originalCode);
        }

        [Fact]
        public void DiagnosticIsGeneratedIfClassHasNotNullFieldsAndContractInvariantMethod()
        {
            const string originalCode = @"
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        [NotNull]
        private int _field;

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
            var expected = new DiagnosticResult(10, 9, "_field");

            VerifyCSharpDiagnostic(originalCode, expected);

            const string fixedCode = @"
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        [NotNull]
        private int _field;

        void Method(object arg, object arg2)
        {
        }

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
        public void DiagnosticGenerationDoesNotFailIfComplexContractsAreAlreadyPresent()
        {
            const string originalCode = @"
namespace Test
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using JetBrains.Annotations;

    class Client
    {
        public Client([NotNull] object socket, TimeSpan timeout, [NotNull] AsyncCallback socketDataReceived, Action timeoutHandler)
        {
            Contract.Requires(socket != null);
            Contract.Requires((timeout <= TimeSpan.Zero) || (timeoutHandler != null));
        }
    }
}";
            var expected = new DiagnosticResult(11, 90, "socketDataReceived");

            VerifyCSharpDiagnostic(originalCode, expected);

            const string fixedCode = @"
namespace Test
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using JetBrains.Annotations;

    class Client
    {
        public Client([NotNull] object socket, TimeSpan timeout, [NotNull] AsyncCallback socketDataReceived, Action timeoutHandler)
        {
            Contract.Requires(socket != null);
            Contract.Requires(socketDataReceived != null);
            Contract.Requires((timeout <= TimeSpan.Zero) || (timeoutHandler != null));
        }
    }
}";

            VerifyCSharpFix(originalCode, fixedCode, null, true);
        }
    }
}