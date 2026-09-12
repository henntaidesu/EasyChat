using EasyChat.Contracts.Platform;

namespace EasyChat.Infrastructure.MacOS.Audio;

/// <summary>
/// Opens the Apple audio source a decoded token names.
/// </summary>
/// <remarks>
/// System and application audio are not implemented yet and say so. Answering an empty stream
/// instead would be worse than failing: silence from a microphone and silence from a capture that
/// never started look identical to the user, and the workflow would report nothing at all.
/// </remarks>
internal sealed class MacAudioSourceFactory : IMacAudioSourceFactory
{
    public ValueTask<IMacAudioSourceStream> OpenAsync(
        MacAudioSource source,
        PcmAudioFormat format,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return source.Kind switch
        {
            AudioCaptureSourceKind.Microphone => ValueTask.FromResult<IMacAudioSourceStream>(
                MacMicrophoneAudioStream.Open(source.Identifier, format)),
            AudioCaptureSourceKind.SystemOutput => throw new NotSupportedException(
                "Capturing system audio on macOS is not implemented yet."),
            AudioCaptureSourceKind.Application => throw new NotSupportedException(
                "Capturing another application's audio on macOS is not implemented yet."),
            _ => throw new NotSupportedException($"Unsupported audio source: {source.Kind}.")
        };
    }
}
