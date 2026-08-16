using System.Collections.Generic;
using UnityEngine;

namespace ParkMinPackages.PackageManager.Editor
{
	//[CreateAssetMenu(fileName = "PublilcGitRepoData", menuName = "PublilcGitRepoData", order = 0)]
	internal class PublicGitRepoDatas : ScriptableObject
	{
		public IReadOnlyList<PublicGitRepoData> Value
		{
			get { return _value; }
		}

		[SerializeField] List<PublicGitRepoData> _value = new List<PublicGitRepoData>();
	}
}
