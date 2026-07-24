using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEditor.PackageManager;
using UnityEngine;

namespace com.parkminpackages.packagemanager.Editor
{
	internal static class PackageDataManager
	{
		// public static async Awaitable<List<PackageData>> asd(string gitURL, CancellationToken cancellationToken) {
		// 	
		// 	
		// }


		public static async Awaitable<List<PackageData>> RequestToOrganizationAsync(string personalAccessToken, string organization, string[] exceptRepos, CancellationToken cancellationToken) {
			List<PackageData> packageDatas = new List<PackageData>();

			HashSet<string> exceptRepoSet = new HashSet<string>(exceptRepos);

			PackageCollection unityPackageCollection = await ClientUtility.ListAsync();
			if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException();

			List<GitRestAPI.Repo> repoList = await GitRestAPI.GetOrganizationReposAsync(personalAccessToken, organization);
			if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException();

			foreach (GitRestAPI.Repo repo in repoList) {
				if (exceptRepoSet.Contains(repo.name))
					continue;

				GitRestAPI.PackageJson remotePackageJson = await GitRestAPI.GetPackageJsonAsync(personalAccessToken, organization, repo.name, repo.default_branch);
				if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException();

				string remoteLastCommitHash = await GitRestAPI.GetOrganizationLastCommitHashAsync(personalAccessToken, organization, repo.name, repo.default_branch);
				if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException();

				PackageInfo unityPackageInfo = unityPackageCollection.FirstOrDefault(unityPackageInfo => unityPackageInfo.name == remotePackageJson.name);


				PackageData packageData = new PackageData();
				packageData.RepoName = repo.name;
				packageData.DisplayName = remotePackageJson.displayName;
				packageData.GitCloneURL = repo.clone_url;
				packageData.PackageName = remotePackageJson.name;
				packageData.CurrentCommitHash = unityPackageInfo == null || unityPackageInfo.git == null ? null : unityPackageInfo.git.hash;
				packageData.RemoteCommitHash = remoteLastCommitHash;
				packageData.IsEmbed = unityPackageInfo == null ? false : unityPackageInfo.source == PackageSource.Embedded;

				packageDatas.Add(packageData);
			}


			return packageDatas;
		}
	}
}