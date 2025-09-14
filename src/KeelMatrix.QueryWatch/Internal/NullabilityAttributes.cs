#if NETSTANDARD2_0
// Minimal shim so we can use [AllowNull] in multi-targeted libraries.
namespace System.Diagnostics.CodeAnalysis {
    [System.AttributeUsage(
        System.AttributeTargets.Field |
        System.AttributeTargets.Parameter |
        System.AttributeTargets.Property |
        System.AttributeTargets.ReturnValue,
        Inherited = false)]
    internal sealed class AllowNullAttribute : System.Attribute { }
}
#endif
