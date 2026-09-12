using System.Runtime.InteropServices;

namespace EasyChat.Infrastructure.MacOS.Native;

/// <summary>
/// Accessibility (TCC) trust checks. <see cref="IsProcessTrusted"/> never prompts;
/// <see cref="PromptForTrust"/> is the only entry point allowed to surface a system dialog.
/// </summary>
internal static partial class AccessibilityNative
{
    private const string LibraryPath =
        "/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices";

    /// <summary>Documented literal value of <c>kAXTrustedCheckOptionPrompt</c>.</summary>
    private const string TrustedCheckOptionPrompt = "AXTrustedCheckOptionPrompt";

    [LibraryImport(LibraryPath)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial bool AXIsProcessTrusted();

    [LibraryImport(LibraryPath)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial bool AXIsProcessTrustedWithOptions(IntPtr options);

    internal static bool IsProcessTrusted() => AXIsProcessTrusted();

    /// <summary>
    /// Asks macOS to show the "open System Settings" prompt and reports the trust state observed at
    /// that moment. macOS answers <see langword="false"/> while the user is still deciding, so the
    /// caller must re-check rather than treat the prompt itself as consent.
    /// </summary>
    internal static bool PromptForTrust()
    {
        var key = CoreFoundationNative.CreateString(TrustedCheckOptionPrompt);
        if (key == IntPtr.Zero)
            return AXIsProcessTrusted();

        var options = IntPtr.Zero;
        try
        {
            options = CoreFoundationNative.CreateDictionary(
                [key],
                [CoreFoundationNative.BooleanTrue]);
            return options == IntPtr.Zero
                ? AXIsProcessTrusted()
                : AXIsProcessTrustedWithOptions(options);
        }
        finally
        {
            if (options != IntPtr.Zero)
                CoreFoundationNative.CFRelease(options);
            CoreFoundationNative.CFRelease(key);
        }
    }
}
