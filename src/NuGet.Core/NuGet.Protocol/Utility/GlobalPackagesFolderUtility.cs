// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Packaging.PackageExtraction;
using NuGet.Packaging.Signing;
using NuGet.Protocol.Core.Types;

namespace NuGet.Protocol
{
    public static class GlobalPackagesFolderUtility
    {
        private const int BufferSize = 8192;

        public static DownloadResourceResult GetPackage(PackageIdentity packageIdentity, string globalPackagesFolder)
        {
            if (packageIdentity == null)
            {
                throw new ArgumentNullException(nameof(packageIdentity));
            }

            if (globalPackagesFolder == null)
            {
                throw new ArgumentNullException(nameof(globalPackagesFolder));
            }

            var defaultPackagePathResolver = new VersionFolderPathResolver(globalPackagesFolder);

            var hashPath = defaultPackagePathResolver.GetHashPath(packageIdentity.Id, packageIdentity.Version);

            if (File.Exists(hashPath))
            {
                var installPath = defaultPackagePathResolver.GetInstallPath(
                    packageIdentity.Id,
                    packageIdentity.Version);

                var nupkgPath = defaultPackagePathResolver.GetPackageFilePath(
                    packageIdentity.Id,
                    packageIdentity.Version);

                Stream stream = null;
                PackageReaderBase packageReader = null;
                try
                {
                    stream = File.Open(nupkgPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    packageReader = new PackageFolderReader(installPath);
                    return new DownloadResourceResult(stream, packageReader, source: null) { SignatureVerified = true };
                }
                catch
                {
                    if (stream != null)
                    {
                        stream.Dispose();
                    }

                    if (packageReader != null)
                    {
                        packageReader.Dispose();
                    }

                    throw;
                }
            }

            return null;
        }

        public static async Task<DownloadResourceResult> AddPackageAsync(
            string source,
            PackageIdentity packageIdentity,
            Stream packageStream,
            string globalPackagesFolder,
            Guid parentId,
            ILogger logger,
            CancellationToken token)
        {
            if (packageIdentity == null)
            {
                throw new ArgumentNullException(nameof(packageIdentity));
            }

            if (packageStream == null)
            {
                throw new ArgumentNullException(nameof(packageStream));
            }

            if (globalPackagesFolder == null)
            {
                throw new ArgumentNullException(nameof(globalPackagesFolder));
            }

            var signedPackageVerifier = new PackageSignatureVerifier(
                          SignatureVerificationProviderFactory.GetSignatureVerificationProviders(),
                          SignedPackageVerifierSettings.Default);

            // The following call adds it to the global packages folder.
            // Addition is performed using ConcurrentUtils, such that,
            // multiple processes may add at the same time

            var packageExtractionContext = new PackageExtractionContext(
                PackageSaveMode.Defaultv3,
                PackageExtractionBehavior.XmlDocFileSaveMode,
                logger,
                signedPackageVerifier);

            var versionFolderPathResolver = new VersionFolderPathResolver(globalPackagesFolder);

            await PackageExtractor.InstallFromSourceAsync(
                source,
                packageIdentity,
                stream => packageStream.CopyToAsync(stream, BufferSize, token),
                versionFolderPathResolver,
                packageExtractionContext,
                token,
                parentId);

            var package = GetPackage(packageIdentity, globalPackagesFolder);

            Debug.Assert(package.PackageStream.CanSeek);
            Debug.Assert(package.PackageReader != null);

            return package;
        }
    }
}
