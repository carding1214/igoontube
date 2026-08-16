using System.Reflection;
using System.Runtime.InteropServices;

namespace PUPlayer.MpvWorker.Interop;

internal static class MpvLibraryResolver
{
    private static string? path;

    public static void Register(string fullPath)
    {
        if (!Path.IsPathFullyQualified(fullPath) || !File.Exists(fullPath))
            throw new FileNotFoundException("libmpv path must be an existing absolute file.", fullPath);
        path = fullPath;
        NativeLibrary.SetDllImportResolver(typeof(MpvNative).Assembly, Resolve);
    }

    private static nint Resolve(string name, Assembly _, DllImportSearchPath? __) =>
        name == "libmpv-2.dll" ? NativeLibrary.Load(path!) : nint.Zero;
}
