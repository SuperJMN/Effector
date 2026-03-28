using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace Effector.Build.Tasks;

internal sealed class EffectorMetadataScanner
{
    private const string SkiaEffectAttributeTypeName = "Effector.SkiaEffectAttribute";

    public EffectorMetadataScanResult Scan(string assemblyPath)
    {
        using var stream = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var peReader = new PEReader(stream, PEStreamOptions.Default);
        var metadata = peReader.GetMetadataReader();
        var result = new List<EffectCandidate>();
        var inspectedTypes = 0;

        foreach (var typeHandle in metadata.TypeDefinitions)
        {
            inspectedTypes++;
            var definition = metadata.GetTypeDefinition(typeHandle);

            foreach (var attributeHandle in definition.GetCustomAttributes())
            {
                var attribute = metadata.GetCustomAttribute(attributeHandle);

                if (TryGetAttributeTypeName(metadata, attribute.Constructor, out var attributeTypeName) &&
                    attributeTypeName == SkiaEffectAttributeTypeName)
                {
                    result.Add(
                        new EffectCandidate(
                            MetadataTokens.GetToken(typeHandle),
                            GetTypeDefinitionFullName(metadata, typeHandle)));
                    break;
                }
            }
        }

        return new EffectorMetadataScanResult(inspectedTypes, result);
    }

    private static bool TryGetAttributeTypeName(MetadataReader metadata, EntityHandle constructorHandle, out string? attributeTypeName)
    {
        attributeTypeName = null;

        if (constructorHandle.Kind == HandleKind.MemberReference)
        {
            var member = metadata.GetMemberReference((MemberReferenceHandle)constructorHandle);
            attributeTypeName = GetTypeHandleFullName(metadata, member.Parent);
            return attributeTypeName is not null;
        }

        if (constructorHandle.Kind == HandleKind.MethodDefinition)
        {
            var method = metadata.GetMethodDefinition((MethodDefinitionHandle)constructorHandle);
            attributeTypeName = GetTypeDefinitionFullName(metadata, method.GetDeclaringType());
            return true;
        }

        return false;
    }

    private static string? GetTypeHandleFullName(MetadataReader metadata, EntityHandle handle)
    {
        return handle.Kind switch
        {
            HandleKind.TypeReference => GetTypeReferenceFullName(metadata, (TypeReferenceHandle)handle),
            HandleKind.TypeDefinition => GetTypeDefinitionFullName(metadata, (TypeDefinitionHandle)handle),
            _ => null
        };
    }

    private static string GetTypeDefinitionFullName(MetadataReader metadata, TypeDefinitionHandle handle)
    {
        var definition = metadata.GetTypeDefinition(handle);
        var ns = metadata.GetString(definition.Namespace);
        var name = metadata.GetString(definition.Name);
        return string.IsNullOrEmpty(ns) ? name : ns + "." + name;
    }

    private static string GetTypeReferenceFullName(MetadataReader metadata, TypeReferenceHandle handle)
    {
        var reference = metadata.GetTypeReference(handle);
        var ns = metadata.GetString(reference.Namespace);
        var name = metadata.GetString(reference.Name);
        return string.IsNullOrEmpty(ns) ? name : ns + "." + name;
    }
}
