using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet.Common;

namespace NuGet.Protocol.Plugins
{
    public static class PluginDiscoveryUtility
    {
        public static string InternalPluginDiscoveryRoot { get; set; } = null;

#if IS_DESKTOP
        private static string NuGetPluginPattern = "*NuGet.Plugin.exe";
#else
        private static string NuGetPluginPattern = "*NuGet.Plugin.dll";
#endif

        public static string GetInternalPlugins()
        {
            var rootDirectory = InternalPluginDiscoveryRoot;

            if (InternalPluginDiscoveryRoot == null)
            {
                rootDirectory = System.Reflection.Assembly.GetEntryAssembly().Location;
            }
            return Path.GetDirectoryName(rootDirectory);
        }

        public static string GetNuGetHomePluginsPath()
        {
            var nuGetHome = NuGetEnvironment.GetFolderPath(NuGetFolderPath.NuGetHome);

            return Path.Combine(nuGetHome,
                "plugins",
#if IS_DESKTOP
                "netframework"
#else
                "dotnet"
#endif
                );
        }

        public static IEnumerable<string> GetConventionBasedPlugins(IEnumerable<string> directories)
        {
            var paths = new List<string>();
            foreach (var directory in directories.Where(Directory.Exists))
            {
                paths.AddRange(Directory.EnumerateFiles(directory, NuGetPluginPattern, SearchOption.TopDirectoryOnly));
            }

            return paths;
        }
    }
}
