using System;

namespace com.mutant.packagemanager.Editor
{
	public struct PackageData
	{
		public string RepoName;
		public string DisplayName;
		public string GitCloneURL;
		public string PackageName;
		public string RemoteCommitHash;
		public string CurrentCommitHash;
		public bool IsEmbed;
		public PackageState State
		{
			get
			{
				if (IsEmbed) {
					return PackageState.Installed;
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