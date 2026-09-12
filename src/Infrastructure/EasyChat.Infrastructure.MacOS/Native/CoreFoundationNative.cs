using System.Runtime.InteropServices;

namespace EasyChat.Infrastructure.MacOS.Native;

/// <summary>
/// Thin CoreFoundation ABI surface. Only allocation, release and constant lookup live here; no
/// EasyChat rule and no Contracts type is allowed to appear in this layer.
/// </summary>
internal static partial class CoreFoundationNative
{
    internal const string LibraryPath =
        "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    private const uint Utf8Encoding = 0x08000100;

    private static readonly Lazy<IntPtr> TrueValue = new(
        () => ReadGlobal("kCFBooleanTrue"),
        LazyThreadSafetyMode.ExecutionAndPublication);

    internal static IntPtr BooleanTrue => TrueValue.Value;

    [LibraryImport(LibraryPath, StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr CFStringCreateWithCString(
        IntPtr allocator,
        string value,
        uint encoding);

    [LibraryImport(LibraryPath)]
    private static partial IntPtr CFDictionaryCreate(
        IntPtr allocator,
        IntPtr[] keys,
        IntPtr[] values,
        nint count,
        IntPtr keyCallBacks,
        IntPtr valueCallBacks);

    [LibraryImport(LibraryPath)]
    internal static partial void CFRelease(IntPtr reference);

    internal static IntPtr CreateString(string value) =>
        CFStringCreateWithCString(IntPtr.Zero, value, Utf8Encoding);

    /// <summary>
    /// Creates a dictionary without retain/release callbacks. The caller owns every key and value
    /// for the whole lifetime of the returned dictionary.
    /// </summary>
    internal static IntPtr CreateDictionary(IntPtr[] keys, IntPtr[] values) =>
        CFDictionaryCreate(IntPtr.Zero, keys, values, keys.Length, IntPtr.Zero, IntPtr.Zero);

    /// <summary>
    /// Reads an exported CoreFoundation constant, which is a pointer-sized variable rather than a
    /// function, so it cannot be reached through <c>LibraryImport</c>.
    /// </summary>
    internal static IntPtr ReadGlobal(string symbol) =>
        Marshal.ReadIntPtr(NativeLibrary.GetExport(NativeLibrary.Load(LibraryPath), symbol));
}
