using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ParkMinPackages.PackageManager.Editor
{
	[Serializable]
	internal class GitDependency
	{
		[JsonProperty("packageName")] public string PackageName;
		[JsonProperty("url")] public string URL;
	}

	[Serializable]
	internal class NuGetDependency
	{
		[JsonProperty("packageName")] public string PackageName;
		[JsonProperty("version")] public string Version;
	}

	internal enum PackageDependencyState
	{
		Installed,
		NotInstalled,
		VersionMismatch,
		Unavailable
	}

	internal struct PackageDependencyData
	{
		public string Name;
		public string Version;
		public string URL;
		public string InstalledVersion;
		public PackageDependencyState State;
	}

	internal struct PackageData
	{
		public string RepoName;
		public string DisplayName;
		public string GitCloneURL;
		public string PackageName;
		public string RemoteCommitHash;
		public string CurrentCommitHash;
		public bool IsEmbed;
		public IReadOnlyList<PackageDependencyData> GitDependencies;
		public IReadOnlyList<PackageDependencyData> NuGetDependencies;
		public PackageState State
		{
			get
			{
				if (IsEmbed) {
					return PackageState.Embedded;
				}

				if (CurrentCommitHash == null) {
					return PackageState.UnInstalled;
				}
				else if (CurrentCommitHash == RemoteCommitHash) {
					return PackageState.Installed;
				}
				else if (CurrentCommitHash != RemoteCommitHash) {
					return PackageState.Updateable;
				}

				throw new NotImplementedException();
			}
		}
	}
}
