using System.Runtime.InteropServices;

namespace EasyChat.Infrastructure.MacOS.Native;

/// <summary>
/// Minimal Objective-C runtime surface for the few Apple capabilities that expose no C entry point.
/// Every message send is declared with its exact signature; no dynamic dispatch helper is provided.
/// </summary>
internal static partial class ObjectiveCNative
{
    private const string LibraryPath = "/usr/lib/libobjc.A.dylib";

    [LibraryImport(LibraryPath, EntryPoint = "objc_getClass", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial IntPtr GetClass(string name);

    [LibraryImport(LibraryPath, EntryPoint = "sel_registerName", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial IntPtr GetSelector(string name);

    [LibraryImport(LibraryPath, EntryPoint = "objc_msgSend")]
    internal static partial nint SendReturningNInt(IntPtr receiver, IntPtr selector, IntPtr argument);

    [LibraryImport(LibraryPath, EntryPoint = "objc_msgSend")]
    internal static partial void Send(IntPtr receiver, IntPtr selector, IntPtr first, IntPtr second);

    [LibraryImport(LibraryPath, EntryPoint = "objc_msgSend")]
    internal static partial IntPtr SendReturningHandle(IntPtr receiver, IntPtr selector);

    [LibraryImport(LibraryPath, EntryPoint = "objc_msgSend")]
    internal static partial nint SendReturningNInt(IntPtr receiver, IntPtr selector);

    [LibraryImport(LibraryPath, EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool SendReturningBool(IntPtr receiver, IntPtr selector, IntPtr argument);

    /// <summary>
    /// Reads an <c>NSString</c> as managed text. The returned buffer is owned by the autorelease
    /// pool, so it is copied immediately.
    /// </summary>
    internal static string? ReadString(IntPtr text)
    {
        if (text == IntPtr.Zero)
            return null;

        var utf8 = SendReturningHandle(text, GetSelector("UTF8String"));
        return utf8 == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(utf8);
    }
}
