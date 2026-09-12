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

    [LibraryImport(LibraryPath)]
    internal static partial IntPtr CFRetain(IntPtr reference);

    /// <summary>Creates an immutable copy of <paramref name="bytes"/>.</summary>
    [LibraryImport(LibraryPath)]
    internal static partial IntPtr CFDataCreate(IntPtr allocator, IntPtr bytes, nint length);

    [LibraryImport(LibraryPath)]
    internal static partial IntPtr CFDataCreateMutable(IntPtr allocator, nint capacity);

    [LibraryImport(LibraryPath)]
    internal static partial nint CFDataGetLength(IntPtr data);

    [LibraryImport(LibraryPath)]
    internal static partial IntPtr CFDataGetBytePtr(IntPtr data);

    [LibraryImport(LibraryPath)]
    internal static partial nint CFArrayGetCount(IntPtr array);

    [LibraryImport(LibraryPath)]
    internal static partial IntPtr CFArrayGetValueAtIndex(IntPtr array, nint index);

    [LibraryImport(LibraryPath)]
    internal static partial IntPtr CFDictionaryGetValue(IntPtr dictionary, IntPtr key);

    [LibraryImport(LibraryPath)]
    internal static partial IntPtr CFMachPortCreateRunLoopSource(
        IntPtr allocator,
        IntPtr port,
        nint order);

    [LibraryImport(LibraryPath)]
    internal static partial IntPtr CFRunLoopGetCurrent();

    [LibraryImport(LibraryPath)]
    internal static partial void CFRunLoopAddSource(IntPtr runLoop, IntPtr source, IntPtr mode);

    [LibraryImport(LibraryPath)]
    internal static partial void CFRunLoopRemoveSource(IntPtr runLoop, IntPtr source, IntPtr mode);

    [LibraryImport(LibraryPath)]
    internal static partial void CFRunLoopRun();

    [LibraryImport(LibraryPath)]
    internal static partial void CFRunLoopStop(IntPtr runLoop);

    /// <summary>The <c>kCFRunLoopCommonModes</c> constant.</summary>
    internal static IntPtr RunLoopCommonModes => CommonModes.Value;

    private static readonly Lazy<IntPtr> CommonModes = new(
        () => ReadGlobal("kCFRunLoopCommonModes"),
        LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>Value of <c>kCFNumberNSIntegerType</c>.</summary>
    private const int NSIntegerType = 15;

    [LibraryImport(LibraryPath)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial bool CFNumberGetValue(IntPtr number, int type, out nint value);

    /// <summary>Reads a <c>CFNumber</c>, answering false for any other kind of object.</summary>
    internal static bool TryReadInt(IntPtr number, out nint value)
    {
        value = 0;
        return number != IntPtr.Zero && CFNumberGetValue(number, NSIntegerType, out value);
    }

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
