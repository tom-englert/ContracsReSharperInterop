# CodeContracs R# Interop

Code fixes to automate adding R#'s `[NotNull]` attributes for the corrseponding `Contract.Requires`, `Contract.Ensures` or `Contract.Invariant`.

This can be useful to have both R# and CodeContract generate (mostly) the same warnings, or to migrate from CodeContracts to R# at all.

![screenshot](https://github.com/tom-englert/ContracsReSharperInterop/blob/master/Assets/Screenshot.png)

e.g.

```
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
}
```

gets

```
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
}
```

It handles the cases `arg != null` as well as `!string.IsNullOrEmpty(arg)` 