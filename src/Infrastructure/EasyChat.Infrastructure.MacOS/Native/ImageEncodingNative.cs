using System.Runtime.InteropServices;

namespace EasyChat.Infrastructure.MacOS.Native;

/// <summary>
/// Encodes a BGRA32 frame as PNG with the system codecs. CoreGraphics and ImageIO are plain C, so no
/// Objective-C message send is involved, and the resulting <c>CFData</c> is toll-free bridged to
/// <c>NSData</c> for the pasteboard.
/// </summary>
internal static partial class ImageEncodingNative
{
    private const string CoreGraphicsPath =
        "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";

    private const string ImageIoPath = "/System/Library/Frameworks/ImageIO.framework/ImageIO";

    /// <summary>
    /// <c>kCGImageAlphaPremultipliedFirst | kCGBitmapByteOrder32Little</c>: a 32-bit little-endian
    /// word of ARGB is B, G, R, A in memory, which is exactly the contract's BGRA32 layout.
    /// </summary>
    private const uint Bgra32BitmapInfo = 2u | (2u << 12);

    private const int BitsPerComponent = 8;
    private const int BitsPerPixel = 32;

    /// <summary>Value of <c>kCGRenderingIntentDefault</c>.</summary>
    private const int DefaultRenderingIntent = 0;

    [LibraryImport(CoreGraphicsPath)]
    private static partial IntPtr CGColorSpaceCreateDeviceRGB();

    [LibraryImport(CoreGraphicsPath)]
    private static partial void CGColorSpaceRelease(IntPtr space);

    [LibraryImport(CoreGraphicsPath)]
    private static partial IntPtr CGDataProviderCreateWithCFData(IntPtr data);

    [LibraryImport(CoreGraphicsPath)]
    private static partial void CGDataProviderRelease(IntPtr provider);

    [LibraryImport(CoreGraphicsPath)]
    private static partial IntPtr CGImageCreate(
        nint width,
        nint height,
        nint bitsPerComponent,
        nint bitsPerPixel,
        nint bytesPerRow,
        IntPtr colorSpace,
        uint bitmapInfo,
        IntPtr provider,
        IntPtr decode,
        [MarshalAs(UnmanagedType.U1)] bool shouldInterpolate,
        int intent);

    [LibraryImport(CoreGraphicsPath)]
    private static partial void CGImageRelease(IntPtr image);

    [LibraryImport(ImageIoPath)]
    private static partial IntPtr CGImageDestinationCreateWithData(
        IntPtr data,
        IntPtr type,
        nint count,
        IntPtr options);

    [LibraryImport(ImageIoPath)]
    private static partial void CGImageDestinationAddImage(
        IntPtr destination,
        IntPtr image,
        IntPtr properties);

    [LibraryImport(ImageIoPath)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial bool CGImageDestinationFinalize(IntPtr destination);

    /// <summary>
    /// Encodes BGRA32 pixels as PNG, or throws with the stage that failed. The caller owns the
    /// returned bytes; no CoreFoundation handle escapes this method.
    /// </summary>
    internal static unsafe byte[] EncodePng(
        ReadOnlySpan<byte> pixels,
        int width,
        int height,
        int stride)
    {
        NativeLibrary.Load(ImageIoPath);

        IntPtr source = IntPtr.Zero;
        IntPtr provider = IntPtr.Zero;
        IntPtr colorSpace = IntPtr.Zero;
        IntPtr image = IntPtr.Zero;
        IntPtr target = IntPtr.Zero;
        IntPtr destination = IntPtr.Zero;
        IntPtr type = IntPtr.Zero;
        try
        {
            fixed (byte* pointer = pixels)
            {
                source = CoreFoundationNative.CFDataCreate(
                    IntPtr.Zero,
                    (IntPtr)pointer,
                    pixels.Length);
            }

            if (source == IntPtr.Zero)
                throw new InvalidOperationException("The pixel buffer could not be copied.");

            provider = CGDataProviderCreateWithCFData(source);
            colorSpace = CGColorSpaceCreateDeviceRGB();
            if (provider == IntPtr.Zero || colorSpace == IntPtr.Zero)
                throw new InvalidOperationException("CoreGraphics rejected the pixel buffer.");

            image = CGImageCreate(
                width,
                height,
                BitsPerComponent,
                BitsPerPixel,
                stride,
                colorSpace,
                Bgra32BitmapInfo,
                provider,
                IntPtr.Zero,
                false,
                DefaultRenderingIntent);
            if (image == IntPtr.Zero)
                throw new InvalidOperationException("CoreGraphics could not build the image.");

            target = CoreFoundationNative.CFDataCreateMutable(IntPtr.Zero, 0);
            type = CoreFoundationNative.CreateString(PasteboardType.Png);
            destination = CGImageDestinationCreateWithData(target, type, 1, IntPtr.Zero);
            if (destination == IntPtr.Zero)
                throw new InvalidOperationException("The PNG encoder could not be created.");

            CGImageDestinationAddImage(destination, image, IntPtr.Zero);
            if (!CGImageDestinationFinalize(destination))
                throw new InvalidOperationException("The PNG encoder failed to write the image.");

            var length = CoreFoundationNative.CFDataGetLength(target);
            var bytes = CoreFoundationNative.CFDataGetBytePtr(target);
            if (length <= 0 || bytes == IntPtr.Zero)
                throw new InvalidOperationException("The PNG encoder produced no data.");

            var encoded = new byte[length];
            Marshal.Copy(bytes, encoded, 0, (int)length);
            return encoded;
        }
        finally
        {
            if (destination != IntPtr.Zero)
                CoreFoundationNative.CFRelease(destination);
            if (type != IntPtr.Zero)
                CoreFoundationNative.CFRelease(type);
            if (target != IntPtr.Zero)
                CoreFoundationNative.CFRelease(target);
            if (image != IntPtr.Zero)
                CGImageRelease(image);
            if (colorSpace != IntPtr.Zero)
                CGColorSpaceRelease(colorSpace);
            if (provider != IntPtr.Zero)
                CGDataProviderRelease(provider);
            if (source != IntPtr.Zero)
                CoreFoundationNative.CFRelease(source);
        }
    }
}
