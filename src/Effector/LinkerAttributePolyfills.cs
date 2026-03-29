#if NETSTANDARD2_0
using System;

namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Constructor |
    AttributeTargets.Interface | AttributeTargets.Delegate | AttributeTargets.Field | AttributeTargets.Property |
    AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter,
    Inherited = false)]
internal sealed class DynamicallyAccessedMembersAttribute : Attribute
{
    public DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes memberTypes)
    {
        MemberTypes = memberTypes;
    }

    public DynamicallyAccessedMemberTypes MemberTypes { get; }
}

[AttributeUsage(
    AttributeTargets.Constructor | AttributeTargets.Field | AttributeTargets.Method,
    AllowMultiple = true,
    Inherited = false)]
internal sealed class DynamicDependencyAttribute : Attribute
{
    public DynamicDependencyAttribute(DynamicallyAccessedMemberTypes memberTypes, string typeName, string assemblyName)
    {
        MemberTypes = memberTypes;
        TypeName = typeName;
        AssemblyName = assemblyName;
    }

    public DynamicallyAccessedMemberTypes MemberTypes { get; }

    public string TypeName { get; }

    public string AssemblyName { get; }
}

[Flags]
internal enum DynamicallyAccessedMemberTypes
{
    None = 0,
    PublicConstructors = 0x0003,
    NonPublicConstructors = 0x0004,
    PublicMethods = 0x0008,
    NonPublicMethods = 0x0010,
    PublicFields = 0x0020,
    NonPublicFields = 0x0040,
    PublicProperties = 0x200,
    NonPublicProperties = 0x0400,
    PublicParameterlessConstructor = 0x0001
}
#endif
