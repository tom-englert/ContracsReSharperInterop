using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("ContracsReSharperInterop")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("tom-englert.de")]
[assembly: AssemblyProduct("ContracsReSharperInterop")]
[assembly: AssemblyCopyright("Copyright © 2017 tom-englert.de")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]

[assembly: AssemblyVersion(Product.Version)]
[assembly: AssemblyFileVersion(Product.Version)]

[assembly: InternalsVisibleTo("ContracsReSharperInterop.Test")]

// ReSharper disable once CheckNamespace
internal class Product
{
    public const string Version = "1.1.0.0";
}
