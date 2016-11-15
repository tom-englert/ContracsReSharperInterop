namespace ContracsReSharperInterop.Test
{
    using Xunit;

    public class ContractClassForTests : NotNullForContractCodeFixVerifier
    {
        [Fact]
        public void AnnotationForContractClassMethodRequiresIsDoneOnInterface()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;

namespace Test
{
    [ContractClass(typeof(InterfaceContract))]
    interface Interface
    {
        void Method(object arg);
    }

    [ContractClassFor(typeof(Interface))]
    abstract class InterfaceContract : Interface
    {
        public void Method(object argX)
        {
            Contract.Requires(argX != null);
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
    [ContractClass(typeof(InterfaceContract))]
    interface Interface
    {
        void Method([NotNull] object arg);
    }

    [ContractClassFor(typeof(Interface))]
    abstract class InterfaceContract : Interface
    {
        public void Method(object argX)
        {
            Contract.Requires(argX != null);
        }
    }
}";

            VerifyCSharpFix(originalCode, fixedCode, null, true);
        }

        [Fact]
        public void AnnotationForContractClassMethodRequiresWithExplicitImplementationIsDoneOnInterface()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;

namespace Test
{
    [ContractClass(typeof(InterfaceContract))]
    interface Interface
    {
        void Method(object arg);
    }

    [ContractClassFor(typeof(Interface))]
    abstract class InterfaceContract : Interface
    {
        void Interface.Method(object argX)
        {
            Contract.Requires(argX != null);
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
    [ContractClass(typeof(InterfaceContract))]
    interface Interface
    {
        void Method([NotNull] object arg);
    }

    [ContractClassFor(typeof(Interface))]
    abstract class InterfaceContract : Interface
    {
        void Interface.Method(object argX)
        {
            Contract.Requires(argX != null);
        }
    }
}";

            VerifyCSharpFix(originalCode, fixedCode, null, true);
        }

        [Fact]
        public void NoDiagnosticIsCreatedIfAnnotationForContractClassMethodRequiresIsAlreadyOnInterface()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    [ContractClass(typeof(InterfaceContract))]
    interface Interface
    {
        void Method([NotNull] object arg);
    }

    [ContractClassFor(typeof(Interface))]
    abstract class InterfaceContract : Interface
    {
        public void Method(object argX)
        {
            Contract.Requires(argX != null);
        }
    }
}";

            VerifyCSharpDiagnostic(originalCode);
        }

        [Fact]
        public void AnnotationForContractClassMethodRequiresIsDoneOnCorrectInterfaceMethod()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;

namespace Test
{
    [ContractClass(typeof(InterfaceContract))]
    interface Interface
    {
        void Method(object arg, object arg2);
        void Method<T>(T arg, object arg2) where T:class;
        void Method(object arg);
    }

    [ContractClassFor(typeof(Interface))]
    abstract class InterfaceContract : Interface
    {
        public void Method(object arg, object arg2)
        {
            Contract.Requires(arg2 != null);
        }

        public void Method<T>(T arg, object arg2) where T:class
        {
        }

        public void Method(object argX)
        {
            Contract.Requires(argX != null);
        }
    }
}";

            var expected1 = new DiagnosticResult(9, 40, "arg2");
            var expected2 = new DiagnosticResult(11, 28, "arg");

            VerifyCSharpDiagnostic(originalCode, expected1, expected2);

            const string fixedCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    [ContractClass(typeof(InterfaceContract))]
    interface Interface
    {
        void Method(object arg, [NotNull] object arg2);
        void Method<T>(T arg, object arg2) where T:class;
        void Method([NotNull] object arg);
    }

    [ContractClassFor(typeof(Interface))]
    abstract class InterfaceContract : Interface
    {
        public void Method(object arg, object arg2)
        {
            Contract.Requires(arg2 != null);
        }

        public void Method<T>(T arg, object arg2) where T:class
        {
        }

        public void Method(object argX)
        {
            Contract.Requires(argX != null);
        }
    }
}";

            VerifyCSharpFix(originalCode, fixedCode, null, true);
        }

        [Fact]
        public void AnnotationForContractClassMethodRequiresIsDoneOnAbstractBaseClass()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;

namespace Test
{
    [ContractClass(typeof(InterfaceContract))]
    abstract class Interface
    {
        public abstract void Method(object arg);
    }

    [ContractClassFor(typeof(Interface))]
    abstract class InterfaceContract : Interface
    {
        public override void Method(object arg)
        {
            Contract.Requires(arg != null);
        }
    }
}";

            var expected = new DiagnosticResult(9, 44, "arg");

            VerifyCSharpDiagnostic(originalCode, expected);

            const string fixedCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    [ContractClass(typeof(InterfaceContract))]
    abstract class Interface
    {
        public abstract void Method([NotNull] object arg);
    }

    [ContractClassFor(typeof(Interface))]
    abstract class InterfaceContract : Interface
    {
        public override void Method(object arg)
        {
            Contract.Requires(arg != null);
        }
    }
}";

            VerifyCSharpFix(originalCode, fixedCode, null, true);
        }

        [Fact]
        public void AnnotationForContractClassPropertyRequiresIsDoneOnInterface()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;

namespace Test
{
    [ContractClass(typeof(InterfaceContract))]
    interface Interface
    {
        object Property { get; set; }
    }

    [ContractClassFor(typeof(Interface))]
    abstract class InterfaceContract : Interface
    {
        public object Property { 
            get { return null; } 
            set { Contract.Requires(value != null); } 
        }
    }
}";

            var expected = new DiagnosticResult(9, 16, "Property");

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
        object Property { get; set; }
    }

    [ContractClassFor(typeof(Interface))]
    abstract class InterfaceContract : Interface
    {
        public object Property { 
            get { return null; } 
            set { Contract.Requires(value != null); } 
        }
    }
}";

            VerifyCSharpFix(originalCode, fixedCode, null, true);
        }

        [Fact]
        public void AnnotationForContractClassPropertyRequiresIsDoneOnAbstractBaseClass()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;

namespace Test
{
    [ContractClass(typeof(InterfaceContract))]
    abstract class Interface
    {
        public abstract object Property { get; set; }
    }

    [ContractClassFor(typeof(Interface))]
    abstract class InterfaceContract : Interface
    {
        public override object Property { 
            get { return null; } 
            set { Contract.Requires(value != null); } 
        }
    }
}";

            var expected = new DiagnosticResult(9, 32, "Property");

            VerifyCSharpDiagnostic(originalCode, expected);

            const string fixedCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    [ContractClass(typeof(InterfaceContract))]
    abstract class Interface
    {
        [NotNull]
        public abstract object Property { get; set; }
    }

    [ContractClassFor(typeof(Interface))]
    abstract class InterfaceContract : Interface
    {
        public override object Property { 
            get { return null; } 
            set { Contract.Requires(value != null); } 
        }
    }
}";

            VerifyCSharpFix(originalCode, fixedCode, null, true);
        }

        [Fact]
        public void AnnotationForContractClassPropertyEnsuresIsDoneOnInterface()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;

namespace Test
{
    [ContractClass(typeof(InterfaceContract))]
    interface Interface
    {
        object Property { get; set; }
    }

    [ContractClassFor(typeof(Interface))]
    abstract class InterfaceContract : Interface
    {
        public object Property { 
            get { Contract.Ensures(Contract.Result<object>() != null); return null; } 
            set { } 
        }
    }
}";

            var expected = new DiagnosticResult(9, 16, "Property");

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
        object Property { get; set; }
    }

    [ContractClassFor(typeof(Interface))]
    abstract class InterfaceContract : Interface
    {
        public object Property { 
            get { Contract.Ensures(Contract.Result<object>() != null); return null; } 
            set { } 
        }
    }
}";

            VerifyCSharpFix(originalCode, fixedCode, null, true);
        }

        [Fact]
        public void AnnotationForContractClassPropertyEnsuresIsDoneOnAbstractBaseClass()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;

namespace Test
{
    [ContractClass(typeof(InterfaceContract))]
    abstract class Interface
    {
        public abstract object Property { get; set; }
    }

    [ContractClassFor(typeof(Interface))]
    abstract class InterfaceContract : Interface
    {
        public override object Property { 
            get { Contract.Ensures(Contract.Result<object>() != null); return null; } 
            set { } 
        }
    }
}";

            var expected = new DiagnosticResult(9, 32, "Property");

            VerifyCSharpDiagnostic(originalCode, expected);

            const string fixedCode = @"
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Test
{
    [ContractClass(typeof(InterfaceContract))]
    abstract class Interface
    {
        [NotNull]
        public abstract object Property { get; set; }
    }

    [ContractClassFor(typeof(Interface))]
    abstract class InterfaceContract : Interface
    {
        public override object Property { 
            get { Contract.Ensures(Contract.Result<object>() != null); return null; } 
            set { } 
        }
    }
}";

            VerifyCSharpFix(originalCode, fixedCode, null, true);
        }


        [Fact]
        public void NoDiagnosticIsCreatedIfAnnotationForContractClassPropertyEnsuresIsAlreadyOnInterface()
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
        object Property { get; set; }
    }

    [ContractClassFor(typeof(Interface))]
    abstract class InterfaceContract : Interface
    {
        public object Property { 
            get { Contract.Ensures(Contract.Result<object>() != null); return null; } 
            set { } 
        }
    }}";

            VerifyCSharpDiagnostic(originalCode);
        }

        [Fact]
        public void AnnotationForContractClassMethodEnsuresIsDoneOnInterface()
        {
            const string originalCode = @"
using System.Diagnostics.Contracts;

namespace Test
{
    [ContractClass(typeof(InterfaceContract))]
    interface Interface
    {
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

            var expected = new DiagnosticResult(9, 16, "Method");

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
        public void NoDiagnosticIsCreatedIfAnnotationForContractClassMethodEnsuresIsAlreadyOnInterface()
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
    }
}