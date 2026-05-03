using System;
using System.IO;
using System.Linq;
using System.Threading;
using Avalonia.Media;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SkiaSharp;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: Effector.SelfWeaver <assembly-path>");
    return 1;
}

var assemblyPath = Path.GetFullPath(args[0]);
if (!File.Exists(assemblyPath))
{
    Console.Error.WriteLine($"Assembly was not found: {assemblyPath}");
    return 1;
}

using var assemblyLock = WaitForAssemblyLock(assemblyPath + ".effector.lock");

var pdbPath = Path.ChangeExtension(assemblyPath, ".pdb");
var readSymbols = File.Exists(pdbPath);
var resolver = new DefaultAssemblyResolver();
resolver.AddSearchDirectory(Path.GetDirectoryName(assemblyPath)!);
resolver.AddSearchDirectory(Path.GetDirectoryName(typeof(Effect).Assembly.Location)!);
resolver.AddSearchDirectory(Path.GetDirectoryName(typeof(SKImageFilter).Assembly.Location)!);
var readerParameters = new ReaderParameters
{
    AssemblyResolver = resolver,
    ReadSymbols = readSymbols,
    InMemory = true,
    SymbolReaderProvider = readSymbols ? new PortablePdbReaderProvider() : null
};

using var assembly = AssemblyDefinition.ReadAssembly(assemblyPath, readerParameters);
var module = assembly.MainModule;
var skiaEffectBase = module.Types.FirstOrDefault(static candidate => candidate.FullName == "Effector.SkiaEffectBase");
if (skiaEffectBase is null)
{
    Console.Error.WriteLine("Effector.SkiaEffectBase was not found.");
    return 1;
}

var effectBaseType = module.ImportReference(typeof(Effect));
var effectInterface = module.ImportReference(typeof(IEffect));

if (skiaEffectBase.BaseType?.FullName != effectBaseType.FullName)
{
    skiaEffectBase.BaseType = effectBaseType;
}

if (!skiaEffectBase.Interfaces.Any(candidate => candidate.InterfaceType.FullName == effectInterface.FullName))
{
    skiaEffectBase.Interfaces.Add(new InterfaceImplementation(effectInterface));
}

RewriteConstructor(module, skiaEffectBase);
RewriteInvalidateEffect(module, skiaEffectBase);

var writerParameters = new WriterParameters
{
    WriteSymbols = readSymbols,
    SymbolWriterProvider = readSymbols ? new PortablePdbWriterProvider() : null
};
var tempAssemblyPath = $"{assemblyPath}.{Environment.ProcessId}.{Guid.NewGuid():N}.effector.tmp";
var tempPdbPath = Path.ChangeExtension(tempAssemblyPath, ".pdb");

try
{
    assembly.Write(tempAssemblyPath, writerParameters);
    File.Move(tempAssemblyPath, assemblyPath, overwrite: true);

    if (readSymbols && File.Exists(tempPdbPath))
    {
        File.Move(tempPdbPath, pdbPath, overwrite: true);
    }
}
finally
{
    if (File.Exists(tempAssemblyPath))
    {
        File.Delete(tempAssemblyPath);
    }

    if (File.Exists(tempPdbPath))
    {
        File.Delete(tempPdbPath);
    }
}

return 0;

static FileStream WaitForAssemblyLock(string lockPath)
{
    var deadline = DateTime.UtcNow.AddMinutes(2);

    while (true)
    {
        try
        {
            return File.Open(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException) when (DateTime.UtcNow < deadline)
        {
            Thread.Sleep(50);
        }
        catch (UnauthorizedAccessException) when (DateTime.UtcNow < deadline)
        {
            Thread.Sleep(50);
        }
    }
}

static void RewriteConstructor(ModuleDefinition module, TypeDefinition skiaEffectBase)
{
    var constructor = skiaEffectBase.Methods.First(static candidate => candidate.IsConstructor && !candidate.IsStatic && candidate.Parameters.Count == 0);
    constructor.Body.Instructions.Clear();
    constructor.Body.ExceptionHandlers.Clear();
    constructor.Body.Variables.Clear();
    constructor.Body.InitLocals = false;

    var il = constructor.Body.GetILProcessor();
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Call, module.ImportReference(typeof(Effect).GetConstructor(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, binder: null, Type.EmptyTypes, modifiers: null)!));
    il.Emit(OpCodes.Ret);
}

static void RewriteInvalidateEffect(ModuleDefinition module, TypeDefinition skiaEffectBase)
{
    var invalidateEffect = skiaEffectBase.Methods.First(static candidate => candidate.Name == "InvalidateEffect" && candidate.Parameters.Count == 0);
    invalidateEffect.Body.Instructions.Clear();
    invalidateEffect.Body.ExceptionHandlers.Clear();
    invalidateEffect.Body.Variables.Clear();
    invalidateEffect.Body.InitLocals = false;

    var il = invalidateEffect.Body.GetILProcessor();
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Ldsfld, module.ImportReference(typeof(EventArgs).GetField(nameof(EventArgs.Empty))!));
    il.Emit(OpCodes.Call, module.ImportReference(typeof(Effect).GetMethod("RaiseInvalidated", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!));
    il.Emit(OpCodes.Ret);
}
