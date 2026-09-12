using System.Runtime.InteropServices;

namespace EasyChat.Infrastructure.MacOS.Native;

/// <summary>Layout of <c>AudioObjectPropertyAddress</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct AudioPropertyAddress
{
    internal uint Selector;
    internal uint Scope;
    internal uint Element;
}

/// <summary>One CoreAudio input device, as much as enumeration reveals without any approval.</summary>
internal readonly record struct AudioInputDevice(
    uint DeviceId,
    string Uid,
    string Name,
    int InputChannelCount);

/// <summary>
/// CoreAudio device enumeration.
/// </summary>
/// <remarks>
/// Listing devices, their names and their channel layouts needs no microphone approval; only
/// actually reading samples does. That is why the source picker can be populated before EasyChat
/// ever asks the user for the microphone.
/// </remarks>
internal static partial class CoreAudioNative
{
    private const string LibraryPath =
        "/System/Library/Frameworks/CoreAudio.framework/CoreAudio";

    /// <summary>Value of <c>kAudioObjectSystemObject</c>.</summary>
    private const uint SystemObject = 1;

    /// <summary>Four-character code <c>dev#</c>, the value of <c>kAudioHardwarePropertyDevices</c>.</summary>
    private const uint DevicesSelector = 0x64657623;

    /// <summary>Four-character code <c>dIn </c>, the default input device.</summary>
    private const uint DefaultInputSelector = 0x64496E20;

    /// <summary>Four-character code <c>uid </c>, the device's persistent identifier.</summary>
    private const uint DeviceUidSelector = 0x75696420;

    /// <summary>Four-character code <c>lnam</c>, the device's human readable name.</summary>
    private const uint NameSelector = 0x6C6E616D;

    /// <summary>Four-character code <c>slay</c>, the stream channel layout.</summary>
    private const uint StreamConfigurationSelector = 0x736C6179;

    /// <summary>Four-character code <c>glob</c>.</summary>
    private const uint GlobalScope = 0x676C6F62;

    /// <summary>Four-character code <c>inpt</c>.</summary>
    private const uint InputScope = 0x696E7074;

    [LibraryImport(LibraryPath)]
    private static partial int AudioObjectGetPropertyDataSize(
        uint objectId,
        ref AudioPropertyAddress address,
        uint qualifierDataSize,
        IntPtr qualifierData,
        out uint dataSize);

    [LibraryImport(LibraryPath)]
    private static partial int AudioObjectGetPropertyData(
        uint objectId,
        ref AudioPropertyAddress address,
        uint qualifierDataSize,
        IntPtr qualifierData,
        ref uint dataSize,
        IntPtr data);

    /// <summary>Every device that can record, in the order CoreAudio reports them.</summary>
    internal static IReadOnlyList<AudioInputDevice> InputDevices()
    {
        var devices = new List<AudioInputDevice>();
        foreach (var deviceId in AllDevices())
        {
            var channels = InputChannelCount(deviceId);
            if (channels <= 0)
                continue;

            var uid = ReadString(deviceId, DeviceUidSelector);
            var name = ReadString(deviceId, NameSelector);
            if (uid is null)
                continue;

            devices.Add(new AudioInputDevice(deviceId, uid, name ?? uid, channels));
        }

        return devices;
    }

    /// <summary>The device the user has chosen as their default input, or null when there is none.</summary>
    internal static string? DefaultInputDeviceUid()
    {
        var address = new AudioPropertyAddress
        {
            Selector = DefaultInputSelector,
            Scope = GlobalScope,
            Element = 0
        };
        var size = (uint)sizeof(uint);
        var buffer = Marshal.AllocHGlobal((int)size);
        try
        {
            if (AudioObjectGetPropertyData(SystemObject, ref address, 0, IntPtr.Zero, ref size, buffer) != 0)
                return null;

            var deviceId = (uint)Marshal.ReadInt32(buffer);
            return deviceId == 0 ? null : ReadString(deviceId, DeviceUidSelector);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static IReadOnlyList<uint> AllDevices()
    {
        var address = new AudioPropertyAddress
        {
            Selector = DevicesSelector,
            Scope = GlobalScope,
            Element = 0
        };
        if (AudioObjectGetPropertyDataSize(SystemObject, ref address, 0, IntPtr.Zero, out var size) != 0
            || size == 0)
        {
            return [];
        }

        var buffer = Marshal.AllocHGlobal((int)size);
        try
        {
            if (AudioObjectGetPropertyData(SystemObject, ref address, 0, IntPtr.Zero, ref size, buffer) != 0)
                return [];

            var count = (int)(size / sizeof(uint));
            var devices = new uint[count];
            for (var index = 0; index < count; index++)
                devices[index] = (uint)Marshal.ReadInt32(buffer, index * sizeof(uint));
            return devices;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    /// <summary>
    /// Sums the channels of every input stream. A device with none is an output-only device, which
    /// is how a speaker is told apart from a microphone without guessing from its name.
    /// </summary>
    private static int InputChannelCount(uint deviceId)
    {
        var address = new AudioPropertyAddress
        {
            Selector = StreamConfigurationSelector,
            Scope = InputScope,
            Element = 0
        };
        if (AudioObjectGetPropertyDataSize(deviceId, ref address, 0, IntPtr.Zero, out var size) != 0
            || size < sizeof(uint))
        {
            return 0;
        }

        var buffer = Marshal.AllocHGlobal((int)size);
        try
        {
            if (AudioObjectGetPropertyData(deviceId, ref address, 0, IntPtr.Zero, ref size, buffer) != 0)
                return 0;

            var bufferCount = (uint)Marshal.ReadInt32(buffer);
            var channels = 0;
            // AudioBufferList is a UInt32 count followed by AudioBuffer records. AudioBuffer ends
            // in a pointer, so it is eight-byte aligned and the array starts after four bytes of
            // padding rather than immediately after the count.
            var firstBufferOffset = 2 * sizeof(uint);
            var bufferSize = sizeof(uint) + sizeof(uint) + IntPtr.Size;
            for (var index = 0; index < bufferCount; index++)
            {
                var offset = firstBufferOffset + (index * bufferSize);
                if (offset + sizeof(uint) > size)
                    break;
                channels += Marshal.ReadInt32(buffer, offset);
            }

            return channels;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static string? ReadString(uint deviceId, uint selector)
    {
        var address = new AudioPropertyAddress
        {
            Selector = selector,
            Scope = GlobalScope,
            Element = 0
        };
        var size = (uint)IntPtr.Size;
        var buffer = Marshal.AllocHGlobal((int)size);
        try
        {
            if (AudioObjectGetPropertyData(deviceId, ref address, 0, IntPtr.Zero, ref size, buffer) != 0)
                return null;

            var text = Marshal.ReadIntPtr(buffer);
            if (text == IntPtr.Zero)
                return null;

            try
            {
                return CoreFoundationNative.ReadString(text);
            }
            finally
            {
                CoreFoundationNative.CFRelease(text);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }
}
