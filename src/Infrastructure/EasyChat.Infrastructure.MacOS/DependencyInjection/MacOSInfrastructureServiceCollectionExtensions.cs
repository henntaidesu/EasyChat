using EasyChat.Contracts.Platform;
using EasyChat.Infrastructure.MacOS.ApplicationStartup;
using EasyChat.Infrastructure.MacOS.Input;
using Microsoft.Extensions.DependencyInjection;

namespace EasyChat.Infrastructure.MacOS.DependencyInjection;

public static class MacOSInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddEasyChatMacOSInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        if (!OperatingSystem.IsMacOSVersionAtLeast(26))
            throw new PlatformNotSupportedException(
                "The macOS infrastructure module requires macOS 26 or later.");

        services.AddSingleton<IPlatformCapabilities, MacPlatformCapabilities>();
        services.AddSingleton<IPlatformPermissionRequester, MacPlatformPermissionRequester>();
        services.AddSingleton<IApplicationAutoStartService, MacApplicationAutoStartService>();
        services.AddSingleton<MacOwnedWindowBehavior>();

        // One pasteboard instance is shared by all three clipboard ports so a capture, a temporary
        // write and a restore can never interleave.
        services.AddSingleton<MacPasteboard>();
        services.AddSingleton<IClipboardSnapshots, MacClipboardSnapshots>();
        services.AddSingleton<IClipboardText, MacClipboardText>();
        services.AddSingleton<IClipboardImage, MacClipboardImage>();

        services.AddSingleton<IWindowFocus, MacWindowFocus>();
        services.AddSingleton<IRunningProcessCatalog, MacRunningProcessCatalog>();
        return services;
    }
}
