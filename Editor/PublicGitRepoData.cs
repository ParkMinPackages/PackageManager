using System.Collections.Generic;
using UnityEngine;

namespace ParkMinPackages.PackageManager.Editor
{
	internal class PublicGitRepoData : ScriptableObject
	{
		public string DisplayName;
		public string Version;
		[TextArea(4, 50)] public string CloneURL;
		public string PackageName;
		public IReadOnlyList<GitDependency> GitDependencies
		{
			get { return _gitDependencies; }
		}
		public IReadOnlyList<NuGetDependency> NuGetDependencies
		{
			get { return _nuGetDependencies; }
		}

		[SerializeField] List<GitDependency> _gitDependencies = new List<GitDependency>();
		[SerializeField] List<NuGetDependency> _nuGetDependencies = new List<NuGetDependency>();
	}
}
