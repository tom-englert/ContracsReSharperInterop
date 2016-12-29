namespace ContracsReSharperInterop.Test
{
    using Xunit;

    public class CreateContractClassParameterTest : CreateContractClassCodeFixVerifier
    {
        [Fact]
        public void NoDiagnosticIsGeneratedIfAbstractClassDoesNotHaveNotNullAttributes()
        {
            const string originalCode = @"
namespace Test
{
    abstract class Class
    {
        abstract void Method(object arg, object arg2);
    }
}";

            VerifyCSharpDiagnostic(originalCode);
        }

        [Fact]
        public void NoDiagnosticIsGeneratedIfClassIsNotAbstract()
        {
            const string originalCode = @"
using JetBrains.Annotations;

namespace Test
{
    class Class
    {
        void Method([NotNull] object arg, object arg2)
        {
        }
    }
}";

            VerifyCSharpDiagnostic(originalCode);
        }

        [Fact]
        public void NoDiagnosticIsGeneratedIfClassIsAbstractButMethodNot()
        {
            const string originalCode = @"
using JetBrains.Annotations;

namespace Test
{
    abstract class Class
    {
        void Method([NotNull] object arg, object arg2)
        {
        }
    }
}";

            VerifyCSharpDiagnostic(originalCode);
        }

        [Fact]
        public void NoDiagnosticIsGeneratedIfClassHasContractClassAttribure()
        {
            const string originalCode = @"
using JetBrains.Annotations;

namespace Test
{
    [ContractClass(typeof(Class))]
    abstract class Class
    {
        void Method([NotNull] object arg, object arg2)
        {
        }
    }
}";

            VerifyCSharpDiagnostic(originalCode);
        }

        [Fact]
        public void DiagnosticIsGeneratedIfAbstractMethodHasNotNullAttributeOnParameter()
        {
            const string originalCode = @"
using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Test
{
    abstract class Class
    {
        public abstract void Method([NotNull] object arg, object arg2);

        public void Method2(object arg) { }

        public abstract object Property { get; set; }

        public abstract IEnumerable<object> ReadOnlyProperty { get; }

        public object NotAbstractProperty { get; set; }

        public event EventHandler<EventArgs> Event;

        public abstract event EventHandler<EventArgs> AbstractEvent;
    }
}";
            var expected = new DiagnosticResult(8, 5, "Class");

            VerifyCSharpDiagnostic(originalCode, expected);

            const string fixedCode = @"
using System.Diagnostics.Contracts;
using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Test
{
    [ContractClass(typeof(ClassContract))]
    abstract class Class
    {
        public abstract void Method([NotNull] object arg, object arg2);

        public void Method2(object arg) { }

        public abstract object Property { get; set; }

        public abstract IEnumerable<object> ReadOnlyProperty { get; }

        public object NotAbstractProperty { get; set; }

        public event EventHandler<EventArgs> Event;

        public abstract event EventHandler<EventArgs> AbstractEvent;
    }

    [ContractClassFor(typeof(Class))]
    internal abstract class ClassContract : Class
    {
        public override void Method(object arg, object arg2)
        {
            throw new NotImplementedException();
        }

        public override object Property
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public override IEnumerable<object> ReadOnlyProperty
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public abstract override event EventHandler<EventArgs> AbstractEvent;
    }
}";

            VerifyCSharpFix(originalCode, fixedCode, null, true);
        }

        [Fact]
        public void DiagnosticIsGeneratedIfInterfaceHasNotNullAttributeOnParameter()
        {
            const string originalCode = @"
using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Test
{
    interface IClass
    {
        void Method([NotNull] object arg, object arg2);

        object Property { get; set; }

        IEnumerable<object> ReadOnlyProperty { get; }

        event EventHandler<EventArgs> Event;
    }
}";
            var expected = new DiagnosticResult(8, 5, "IClass");

            VerifyCSharpDiagnostic(originalCode, expected);

            const string fixedCode = @"
using System.Diagnostics.Contracts;
using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Test
{
    [ContractClass(typeof(ClassContract))]
    interface IClass
    {
        void Method([NotNull] object arg, object arg2);

        object Property { get; set; }

        IEnumerable<object> ReadOnlyProperty { get; }

        event EventHandler<EventArgs> Event;
    }

    [ContractClassFor(typeof(IClass))]
    internal abstract class ClassContract : IClass
    {
        public void Method(object arg, object arg2)
        {
            throw new NotImplementedException();
        }

        public object Property
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public IEnumerable<object> ReadOnlyProperty
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public abstract event EventHandler<EventArgs> Event;
    }
}";

            VerifyCSharpFix(originalCode, fixedCode, null, true);
        }
    }
}

namespace Test2
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;

    using JetBrains.Annotations;

    [ContractClass(typeof(ClassContract))]
    interface IClass
    {
        void Method([NotNull] object arg, object arg2);

        object Property { get; set; }

        IEnumerable<object> ReadOnlyProperty { get; }

        event EventHandler<EventArgs> Event;
    }

    [ContractClassFor(typeof(IClass))]
    internal abstract class ClassContract : IClass
    {
        public void Method(object arg, object arg2)
        {
            throw new NotImplementedException();
        }

        public object Property
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public IEnumerable<object> ReadOnlyProperty
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public abstract event EventHandler<EventArgs> Event;
    }
}
