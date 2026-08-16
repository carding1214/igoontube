using System.Runtime.InteropServices;

namespace PUPlayer.MpvWorker.Interop;

internal enum MpvFormat { String = 1, Flag = 3, Int64 = 4, Double = 5, NodeArray = 7, NodeMap = 8, ByteArray = 9 }

[StructLayout(LayoutKind.Explicit)]
internal struct MpvNodeData
{
    [FieldOffset(0)] internal nint Pointer;
    [FieldOffset(0)] internal long Int64;
    [FieldOffset(0)] internal double Double;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MpvNode { internal MpvNodeData Data; internal MpvFormat Format; }

[StructLayout(LayoutKind.Sequential)]
internal struct MpvNodeList { internal int Count; internal nint Values; internal nint Keys; }

[StructLayout(LayoutKind.Sequential)]
internal struct MpvByteArray { internal nint Data; internal nuint Size; }

internal static class MpvNative
{
    private const string Dll = "libmpv-2.dll";

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint mpv_create();

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int mpv_set_option_string(
        nint ctx,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string value);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int mpv_initialize(nint ctx);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int mpv_command(nint ctx, nint args);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int mpv_command_node(nint ctx, ref MpvNode args, ref MpvNode result);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void mpv_free_node_contents(ref MpvNode node);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "mpv_get_property")]
    internal static extern int mpv_get_double(
        nint ctx,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        MpvFormat format,
        ref double value);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "mpv_get_property")]
    internal static extern int mpv_get_flag(
        nint ctx,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        MpvFormat format,
        ref int value);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void mpv_terminate_destroy(nint ctx);
}
