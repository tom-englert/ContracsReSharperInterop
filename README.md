# CodeContracs R# Interop ![Badge](https://tom-englert.visualstudio.com/_apis/public/build/definitions/75bf84d2-d359-404a-a712-07c9f693f635/9/badge)

Download the stable release from the [Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=TomEnglert.CodeContracsRInterop)

Download the latest build from the [Open VSIX Gallery](http://vsixgallery.com/extension/ContracsReSharperInterop..6fae8f4c-22f3-40dc-bf36-ddb0c7c37ebf/)

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