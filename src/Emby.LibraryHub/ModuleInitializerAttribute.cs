using System;

namespace System.Runtime.CompilerServices;

/// <summary>Lets the compiler emit a module initializer while targeting .NET Standard 2.0.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
internal sealed class ModuleInitializerAttribute : Attribute
{
}
