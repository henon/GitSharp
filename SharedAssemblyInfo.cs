using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyProduct("GitSharp")]

[assembly: AssemblyCompany("The Git Development Community")]
[assembly: AssemblyCopyright("Copyright © 2009 The GitSharp Team")]
[assembly: ComVisible(false)]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
