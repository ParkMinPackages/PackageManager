using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEditor.PackageManager;
using UnityEngine;

namespace ParkMinPackages.PackageManager.Editor
{
	internal static class PackageDataManager
	{
		// public static async Awaitable<List<PackageData>> asd(string gitURL, CancellationToken cancellationToken) {
		// 	
		// 	
		// }


		public static async Awaitable<List<PackageData>> RequestToOrganizationAsync(
			string personalAccessToken,
			string organization,
			string[] exceptRepos,
			PackageCollection unityPackageCollection,
			PackageDependencyResolver dependencyResolver,
			CancellationToken cancellationToken
		) {
			List<PackageData> packageDatas = new List<PackageData>();
			HashSet<string> exceptRepoSet = new HashSet<string>(exceptRepos);

			List<GitRestAPI.Repo> repoList = await GitRestAPI.GetOrganizationReposAsync(personalAccessToken, organization);
			if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException();

			foreach (GitRestAPI.Repo repo in repoList) {
				if (exceptRepoSet.Contains(repo.name))
					continue;

				GitRestAPI.PackageDependenciesJson remoteDependenciesJson = await GitRestAPI.GetPackageDependenciesJsonAsync(personalAccessToken, organization, repo.name, repo.default_branch);
				if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException();

				string[] packagePathSegments = string.IsNullOrWhiteSpace(remoteDependenciesJson.packagePath)
					? Array.Empty<string>()
					: remoteDependenciesJson.packagePath.Replace('\\', '/').Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
				if (packagePathSegments.Any(segment => segment == "." || segment == "..")) {
					throw new InvalidOperationException($"Invalid packagePath in {repo.name}/parkmin-dependencies.json");
				}

				string packagePath = string.Join("/", packagePathSegments);
				GitRestAPI.PackageJson remotePackageJson = await GitRestAPI.GetPackageJsonAsync(personalAccessToken, organization, repo.name, repo.default_branch, packagePath);
				if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException();

				string remoteLastCommitHash = await GitRestAPI.GetOrganizationLastCommitHashAsync(personalAccessToken, organization, repo.name, repo.default_branch);
				if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException();

				PackageInfo unityPackageInfo = unityPackageCollection.FirstOrDefault(unityPackageInfo => unityPackageInfo.name == remotePackageJson.name);


				PackageData packageData = new PackageData();
				packageData.RepoName = repo.name;
				packageData.DisplayName = remotePackageJson.displayName;
				packageData.Version = remotePackageJson.version;
				packageData.GitCloneURL = string.IsNullOrEmpty(packagePath)
					? repo.clone_url
					: $"{repo.clone_url}?path=/{string.Join("/", packagePathSegments.Select(Uri.EscapeDataString))}";
				packageData.PackageName = remotePackageJson.name;
				packageData.CurrentCommitHash = unityPackageInfo == null || unityPackageInfo.git == null ? null : unityPackageInfo.git.hash;
				packageData.RemoteCommitHash = remoteLastCommitHash;
				packageData.IsEmbed = unityPackageInfo == null ? false : unityPackageInfo.source == PackageSource.Embedded;
				packageData.GitDependencies = dependencyResolver.ResolveGit(remoteDependenciesJson.gitDependencies);
				packageData.NuGetDependencies = dependencyResolver.ResolveNuGet(remoteDependenciesJson.nugetDependencies);

				packageDatas.Add(packageData);
			}


			return packageDatas;
		}
	}
}
