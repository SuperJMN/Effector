using System;
using System.IO;
using System.Text;

namespace Effector.Compiz.Sample.App;

internal static class TransitionPreferenceStore
{
    private static readonly string DirectoryPath =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Effector");

    private static readonly string FilePath =
        Path.Combine(DirectoryPath, "compiz-transition-effect.txt");

    public static string? Load()
    {
        if (!File.Exists(FilePath))
        {
            return null;
        }

        var value = File.ReadAllText(FilePath, Encoding.UTF8).Trim();
        return value.Length == 0 ? null : value;
    }

    public static void Save(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        Directory.CreateDirectory(DirectoryPath);
        File.WriteAllText(FilePath, value.Trim(), Encoding.UTF8);
    }
}
