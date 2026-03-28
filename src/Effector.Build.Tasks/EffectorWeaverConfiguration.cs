using System;

namespace Effector.Build.Tasks;

internal sealed class EffectorWeaverConfiguration
{
    public EffectorWeaverConfiguration(
        string assemblyPath,
        bool strict,
        bool verbose,
        string projectDirectory,
        string[] referencePaths,
        string supportedAvaloniaVersion)
    {
        AssemblyPath = assemblyPath;
        Strict = strict;
        Verbose = verbose;
        ProjectDirectory = projectDirectory;
        ReferencePaths = referencePaths;
        SupportedAvaloniaVersion = supportedAvaloniaVersion;
    }

    public string AssemblyPath { get; }

    public bool Strict { get; }

    public bool Verbose { get; }

    public string ProjectDirectory { get; }

    public string[] ReferencePaths { get; }

    public string SupportedAvaloniaVersion { get; }
}
