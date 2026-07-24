using System;
using System.Collections.Generic;
using UnityEngine;

namespace com.parkminpackages.packagemanager.Editor
{
	//[CreateAssetMenu(fileName = "PublilcGitRepoData", menuName = "PublilcGitRepoData", order = 0)]
	internal class PublilcGitRepoData : ScriptableObject
	{
		public List<Data> Value
		{
			get { return _value; }
		}

		[SerializeField] List<Data> _value = new List<Data>();

		[Serializable]
		public class Data
		{
			public string DisplayName;
			[TextArea(4, 50)]
			public string CloneURL;
			public string PackageName;
		}
	}
}