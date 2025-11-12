#if NETSTANDARD2_0
// Minimal shim so we can use [AllowNull] in multi-targeted libraries.
namespace System.Diagnostics.CodeAnalysis {
    [System.AttributeUsage(
        System.AttributeTargets.Field |
        System.AttributeTargets.Parameter |
        System.AttributeTargets.Property |
        System.AttributeTargets.ReturnValue,
        Inherited = false)]
#pragma warning disable RCS1251 // Remove unnecessary braces
    internal sealed class AllowNullAttribute : System.Attribute { }
#pragma warning restore  RCS1251
}
#endif
