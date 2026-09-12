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
}
