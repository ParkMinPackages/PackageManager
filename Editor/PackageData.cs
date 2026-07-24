using System;

namespace com.parkminpackages.packagemanager.Editor
{
	internal struct PackageData
	{
		public string RepoName;
		public string DisplayName;
		public string GitCloneURL;
		public string PackageName;
		public string RemoteVersion;
		public string CurrentVersion;
		public bool IsEmbed;

		public PackageState State
		{
			get
			{
				if (IsEmbed) return PackageState.Embedded;
				if (string.IsNullOrEmpty(CurrentVersion)) return PackageState.UnInstalled;
				return string.Equals(CurrentVersion, RemoteVersion, StringComparison.Ordinal)
					? PackageState.Installed
					: PackageState.Updateable;
			}
		}
	}
}