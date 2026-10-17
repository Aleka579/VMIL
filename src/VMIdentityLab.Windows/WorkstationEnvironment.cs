using System.Security;
using Microsoft.Win32;

namespace VMIdentityLab.Windows;

public static class WorkstationEnvironment
{
    // Installed product metadata only: no scanning, process launch or registry mutation.
    public static Version? ReadInstalledVersion()
    {
        if (!OperatingSystem.IsWindows()) return null;
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            try
            {
                using var registry = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                using var key = registry.OpenSubKey(@"SOFTWARE\VMware, Inc.\VMware Workstation");
                if (key?.GetValue("ProductVersion") is string text && Version.TryParse(text, out var version))
                    return version;
            }
            catch (Exception error) when (error is UnauthorizedAccessException or SecurityException or IOException)
            { return null; }
        }
        return null;
    }
}
