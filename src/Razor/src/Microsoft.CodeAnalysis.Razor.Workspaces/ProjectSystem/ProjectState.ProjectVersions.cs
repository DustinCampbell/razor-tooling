// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal sealed partial class ProjectState
{
    /// <param name="Version">
    ///  Gets the version of this project, INCLUDING content changes. The <see cref="ProjectState.Version"/> is
    ///  incremented for each new <see cref="ProjectState"/> instance created.
    /// </param>
    /// 
    /// <param name="Configuration">
    /// 
    /// </param>
    /// 
    /// <param name="DocumentCollection">
    ///  Gets the version of this project, NOT INCLUDING computed or content changes. The
    ///  <see cref="DocumentCollectionVersion"/> is incremented each time the configuration changes or
    ///  a document is added or removed.
    /// </param>
    ///
    /// <param name="ProjectWorkspaceState">
    ///  Gets the version of this project based on the project workspace state, NOT INCLUDING content
    ///  changes. The computed state is guaranteed to change when the configuration or tag helpers change.
    /// </param>
    private readonly record struct ProjectVersions(
        VersionStamp Version,
        VersionStamp Configuration,
        VersionStamp DocumentCollection,
        VersionStamp ProjectWorkspaceState)
    {
        public ProjectVersions ConfigurationChanged()
        {
            var newVersion = Version.GetNewerVersion();

            return new(newVersion, Configuration: newVersion, DocumentCollection: newVersion, ProjectWorkspaceState);
        }

        public ProjectVersions DocumentAdded()
        {
            var newVersion = Version.GetNewerVersion();

            return new(newVersion, Configuration, DocumentCollection: newVersion, ProjectWorkspaceState);
        }

        public ProjectVersions DocumentRemoved()
        {
            var newVersion = Version.GetNewerVersion();

            return new(newVersion, Configuration, DocumentCollection: newVersion, ProjectWorkspaceState);
        }

        public ProjectVersions DocumentChanged()
        {
            var newVersion = Version.GetNewerVersion();

            return new(newVersion, Configuration, DocumentCollection, ProjectWorkspaceState);
        }

        public ProjectVersions ProjectWorkspaceStateChanged()
        {
            var newVersion = Version.GetNewerVersion();

            return new(newVersion, Configuration, DocumentCollection, ProjectWorkspaceState: newVersion);
        }

        public static ProjectVersions Create()
        {
            var version = VersionStamp.Create();

            return new ProjectVersions(Version: version, Configuration: version, DocumentCollection: version, ProjectWorkspaceState: version);
        }

        public VersionStamp GetLatestVersion()
        {
            VersionStamp result = default;

            result = result.GetNewerVersion(Configuration);
            result = result.GetNewerVersion(ProjectWorkspaceState);
            result = result.GetNewerVersion(DocumentCollection);

            return result;
        }
    }
}
