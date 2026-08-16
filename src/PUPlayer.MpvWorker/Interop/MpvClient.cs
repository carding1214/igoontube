using System.Globalization;
using System.Runtime.InteropServices;
using PUPlayer.Core.Playback;
using PUPlayer.Core.Zoom;
using PUPlayer.Core.MediaTools;

namespace PUPlayer.MpvWorker.Interop;

public sealed class MpvClient : IMpvClient
{
    private nint context;
    private long originalAid = -1;
    private long externalAid = -1;

    public MpvClient(ulong windowId, string libraryPath)
    {
        MpvLibraryResolver.Register(libraryPath);
        context = MpvNative.mpv_create();
        if (context == nint.Zero) throw new InvalidOperationException("mpv_create failed.");
        try
        {
            SetOption("wid", unchecked((uint)windowId).ToString(CultureInfo.InvariantCulture));
            foreach (var (name, value) in MpvPlaybackOptions.Values) SetOption(name, value);
            Ensure(MpvNative.mpv_initialize(context), "mpv_initialize");
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void Load(string path)
    {
        originalAid = externalAid = -1;
        Command("loadfile", path, "replace");
    }
    public void SetPaused(bool value) => Command("set", "pause", value ? "yes" : "no");
    public void Seek(double seconds) => Command("seek", Format(seconds), "absolute+exact");
    public void SetVolume(double percent) => Command("set", "volume", Format(percent));
    public void SetSpeed(double value) => Command("set", "speed", Format(value));
    public void SetTransform(MpvTransform value)
    {
        Command("set", "video-zoom", Format(value.VideoZoom));
        Command("set", "video-pan-x", Format(value.VideoPanX));
        Command("set", "video-pan-y", Format(value.VideoPanY));
    }
    public void SetGeometry(VideoTransform value)
    {
        Command("set", "video-rotate", value.Rotation.ToString(CultureInfo.InvariantCulture));
        Command("set", "video-mirror", value.MirrorX ? "yes" : "no");
        Command("set", "video-flip", value.MirrorY ? "yes" : "no");
        var crop = value.Crop is { } c
            ? $"crop=iw*{Format(c.Width)}:ih*{Format(c.Height)}:iw*{Format(c.X)}:ih*{Format(c.Y)}"
            : "";
        Command("set", "vf", crop);
    }
    public void SetAudioFilter(string value) => Command("set", "af", value);
    public void LoadExternalAudio(string path)
    {
        originalAid = (long)GetDouble("aid", -1);
        Command("audio-add", path, "select");
        externalAid = (long)GetDouble("aid", -1);
    }
    public void UseOriginalAudio()
    {
        if (GetDouble("duration") > 0 && originalAid >= 0)
        {
            Command("set", "aid", originalAid.ToString(CultureInfo.InvariantCulture));
            if (externalAid >= 0) Command("audio-remove", externalAid.ToString(CultureInfo.InvariantCulture));
        }
        originalAid = externalAid = -1;
    }

    public VideoFrame CaptureFrame(int maxWidth)
    {
        var result = CommandNode("screenshot-raw", "video", "bgr0");
        try
        {
            if (result.Format != MpvFormat.NodeMap) throw new InvalidOperationException("libmpv did not return an image.");
            var list = Marshal.PtrToStructure<MpvNodeList>(result.Data.Pointer);
            var width = (int)MapInt64(list, "w");
            var height = (int)MapInt64(list, "h");
            var stride = (int)MapInt64(list, "stride");
            var bytes = Marshal.PtrToStructure<MpvByteArray>(MapNode(list, "data").Data.Pointer);
            return DownsampleBgr0(bytes.Data, width, height, stride, maxWidth);
        }
        finally { MpvNative.mpv_free_node_contents(ref result); }
    }

    public PlayerSnapshot ReadSnapshot() => new(
        GetDouble("time-pos"),
        GetDouble("duration"),
        GetFlag("pause"),
        GetDouble("speed", 1),
        GetDouble("volume", 100));

    public void Dispose()
    {
        if (context == nint.Zero) return;
        MpvNative.mpv_terminate_destroy(context);
        context = nint.Zero;
    }

    private void SetOption(string name, string value) =>
        Ensure(MpvNative.mpv_set_option_string(context, name, value), $"option {name}");

    private void Command(params string[] values)
    {
        ObjectDisposedException.ThrowIf(context == nint.Zero, this);
        var pointers = new nint[values.Length + 1];
        GCHandle handle = default;
        try
        {
            for (var i = 0; i < values.Length; i++) pointers[i] = Marshal.StringToCoTaskMemUTF8(values[i]);
            handle = GCHandle.Alloc(pointers, GCHandleType.Pinned);
            Ensure(MpvNative.mpv_command(context, handle.AddrOfPinnedObject()), values[0]);
        }
        finally
        {
            if (handle.IsAllocated) handle.Free();
            foreach (var pointer in pointers) if (pointer != nint.Zero) Marshal.FreeCoTaskMem(pointer);
        }
    }

    private MpvNode CommandNode(params string[] values)
    {
        var size = Marshal.SizeOf<MpvNode>();
        var nodes = Marshal.AllocHGlobal(size * values.Length);
        var listPointer = Marshal.AllocHGlobal(Marshal.SizeOf<MpvNodeList>());
        var strings = new nint[values.Length];
        try
        {
            for (var i = 0; i < values.Length; i++)
            {
                strings[i] = Marshal.StringToCoTaskMemUTF8(values[i]);
                Marshal.StructureToPtr(new MpvNode { Data = new() { Pointer = strings[i] }, Format = MpvFormat.String }, nodes + i * size, false);
            }
            Marshal.StructureToPtr(new MpvNodeList { Count = values.Length, Values = nodes }, listPointer, false);
            var args = new MpvNode { Data = new() { Pointer = listPointer }, Format = MpvFormat.NodeArray };
            var result = new MpvNode();
            Ensure(MpvNative.mpv_command_node(context, ref args, ref result), values[0]);
            return result;
        }
        finally
        {
            foreach (var pointer in strings) if (pointer != nint.Zero) Marshal.FreeCoTaskMem(pointer);
            Marshal.FreeHGlobal(listPointer);
            Marshal.FreeHGlobal(nodes);
        }
    }

    private static MpvNode MapNode(MpvNodeList list, string name)
    {
        var size = Marshal.SizeOf<MpvNode>();
        for (var i = 0; i < list.Count; i++)
        {
            var key = Marshal.PtrToStringUTF8(Marshal.ReadIntPtr(list.Keys, i * nint.Size));
            if (key == name) return Marshal.PtrToStructure<MpvNode>(list.Values + i * size);
        }
        throw new InvalidOperationException($"libmpv image is missing {name}.");
    }

    private static long MapInt64(MpvNodeList list, string name) => MapNode(list, name).Data.Int64;

    private static VideoFrame DownsampleBgr0(nint source, int width, int height, int stride, int maxWidth)
    {
        var outputWidth = Math.Min(width, Math.Max(1, maxWidth));
        var outputHeight = Math.Max(1, (int)Math.Round(height * outputWidth / (double)width));
        var rgb = new byte[outputWidth * outputHeight * 3];
        for (var y = 0; y < outputHeight; y++)
        {
            var sourceY = y * height / outputHeight;
            for (var x = 0; x < outputWidth; x++)
            {
                var sourceX = x * width / outputWidth;
                var pixel = source + sourceY * stride + sourceX * 4;
                var output = (y * outputWidth + x) * 3;
                rgb[output] = Marshal.ReadByte(pixel, 2);
                rgb[output + 1] = Marshal.ReadByte(pixel, 1);
                rgb[output + 2] = Marshal.ReadByte(pixel);
            }
        }
        return new(outputWidth, outputHeight, rgb);
    }

    private double GetDouble(string name, double fallback = 0)
    {
        var value = fallback;
        return MpvNative.mpv_get_double(context, name, MpvFormat.Double, ref value) >= 0 ? value : fallback;
    }

    private bool GetFlag(string name)
    {
        var value = 0;
        return MpvNative.mpv_get_flag(context, name, MpvFormat.Flag, ref value) >= 0 && value != 0;
    }

    private static string Format(double value) => value.ToString("G17", CultureInfo.InvariantCulture);
    private static void Ensure(int result, string operation)
    {
        if (result < 0) throw new InvalidOperationException($"libmpv failed: {operation} ({result}).");
    }
}
