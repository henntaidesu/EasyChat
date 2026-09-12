using Avalonia;
using EasyChat.Desktop.MacOS.ApplicationLifecycle;
using EasyChat.Desktop.MacOS.Capture;
using EasyChat.Desktop.MacOS.DependencyInjection;
using EasyChat.Infrastructure.MacOS.DependencyInjection;
using EasyChat.Presentation.Shared.Controls;

namespace EasyChat.Desktop.MacOS;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Length >= 2 && string.Equals(
                args[0],
                "--screenshot-worker",
                StringComparison.Ordinal))
        {
            MacScreenshotWorker.Run(args[1]);
            return;
        }

        // Shortcuts are stored with platform-neutral names, so the host says how they are written.
        // A Mac user expects the symbols from the keys and the menu bar, not "Win + Shift + A".
        KeyGlyphs.Convention = KeyGlyphConvention.MacSymbols;

        DesktopApplication.Run(
            args,
            new MacOSDesktopInstanceCoordinator(),
            services =>
            {
                services.AddEasyChatMacOSInfrastructure();
                services.AddEasyChatMacOSDesktop();
            },
            configureAppBuilder: builder => builder
                .With(new SkiaOptions
                {
                    MaxGpuResourceSizeBytes = 16L * 1024 * 1024
                }));
    }
}
