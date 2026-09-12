using EasyChat.Contracts.Platform;
using EasyChat.Infrastructure.MacOS.Audio;
using EasyChat.Infrastructure.MacOS.Native;
using Microsoft.Extensions.Logging.Abstractions;

namespace EasyChat.Infrastructure.MacOS.Tests.Audio;

/// <summary>
/// No test here opens a real input device: doing so would put a microphone permission prompt in
/// front of whoever is running the suite.
/// </summary>
[TestClass]
public sealed class MacAudioSourceFactoryTests
{
    private static readonly PcmAudioFormat Format = PcmAudioFormat.SpeechRecognition;

    [TestMethod]
    public async Task SystemAudioSaysItIsNotImplementedRatherThanGoingSilent()
    {
        // Silence from a microphone and silence from a capture that never started look the same to
        // the user, so the unimplemented source has to announce itself.
        // OpenAsync rejects an unsupported source before returning a task, so the call itself has
        // to be inside the assertion.
        var failure = await Assert.ThrowsExactlyAsync<NotSupportedException>(async () =>
            await new MacAudioSourceFactory().OpenAsync(
                new MacAudioSource(AudioCaptureSourceKind.SystemOutput, string.Empty),
                Format,
                CancellationToken.None));

        StringAssert.Contains(failure.Message, "system audio");
    }

    [TestMethod]
    public async Task ApplicationAudioSaysItIsNotImplemented()
    {
        await Assert.ThrowsExactlyAsync<NotSupportedException>(async () =>
            await new MacAudioSourceFactory().OpenAsync(
                new MacAudioSource(AudioCaptureSourceKind.Application, "1234"),
                Format,
                CancellationToken.None));
    }

    [TestMethod]
    public async Task ACancelledOpenIsCancelledBeforeTouchingTheDevice()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () =>
            await new MacAudioSourceFactory().OpenAsync(
                new MacAudioSource(AudioCaptureSourceKind.Microphone, "device"),
                Format,
                cancellation.Token));
    }

    [TestMethod]
    public async Task AnUnavailableSourceEndsTheCaptureInsteadOfYieldingNothing()
    {
        var capture = new MacPcmAudioCapture(
            new MacAudioSourceFactory(),
            NullLogger<MacPcmAudioCapture>.Instance);

        await Assert.ThrowsExactlyAsync<NotSupportedException>(async () =>
        {
            await foreach (var _ in capture.CaptureAsync(
                               [MacAudioSourceTokens.ForSystemOutput()],
                               Format))
            {
            }
        });
    }

    [TestMethod]
    public void TheRecordingFormatDescribesSignedPackedPcmWithConsistentSizes()
    {
        // AudioQueue converts from the device's own format into this one, so getting the frame
        // arithmetic wrong here would silently halve or double the playback rate.
        var description = AudioQueueNative.DescribePcm(16_000, 1, 16);

        Assert.AreEqual(16_000d, description.SampleRate);
        Assert.AreEqual(0x6C70636Du, description.FormatId, "kAudioFormatLinearPCM");
        Assert.AreEqual(4u | 8u, description.FormatFlags, "signed integer and packed");
        Assert.AreEqual(1u, description.ChannelsPerFrame);
        Assert.AreEqual(16u, description.BitsPerChannel);
        Assert.AreEqual(2u, description.BytesPerFrame, "one 16-bit sample per mono frame");
        Assert.AreEqual(1u, description.FramesPerPacket, "PCM has one frame per packet");
        Assert.AreEqual(description.BytesPerFrame, description.BytesPerPacket);
    }

    [TestMethod]
    public void AStereoRecordingFormatScalesTheFrameSize()
    {
        var description = AudioQueueNative.DescribePcm(48_000, 2, 16);

        Assert.AreEqual(4u, description.BytesPerFrame, "two 16-bit samples per stereo frame");
        Assert.AreEqual(2u, description.ChannelsPerFrame);
    }
}
