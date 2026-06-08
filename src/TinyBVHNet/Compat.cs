// Polyfills for .NET Framework compatibility.
// These types are provided by the .NET 5+ runtime but not by .NET Framework.

#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices;

/// <summary>
/// Reserved for use by the compiler when emitting init-only property setters.
/// </summary>
internal static class IsExternalInit { }
#endif
