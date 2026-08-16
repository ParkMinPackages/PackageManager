using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace ParkMinPackages.PackageManager.Editor
{
	internal static class GitRestAPI
	{
		//GetOrganizationReposAsync
		public static async Awaitable<List<Repo>> GetOrganizationReposAsync(string personalAccessToken, string organization) {
			using (UnityWebRequest request = UnityWebRequest.Get($"https://api.github.com/orgs/{organization}/repos")) {
				string json = await SendBearerRequestAsync(request, personalAccessToken);
				List<Repo> list = JsonConvert.DeserializeObject<List<Repo>>(json);
				return list;
			}
		}
		public class Repo
		{
			public string name;
			public string default_branch;
			public string clone_url;
		}

		//GetOrganizationLastCommitHashAsync
		public static async Awaitable<string> GetOrganizationLastCommitHashAsync(string personalAccessToken, string organization, string repoName, string branchName) {
			string escapedBranchName = UnityWebRequest.EscapeURL(branchName);
			string url = $"https://api.github.com/repos/{organization}/{repoName}/branches/{escapedBranchName}";

			using (UnityWebRequest request = UnityWebRequest.Get(url)) {
				string json = await SendBearerRequestAsync(request, personalAccessToken);
				JObject obj = JObject.Parse(json);
				string sha = obj["commit"]?["sha"]?.ToString();
				return sha;
			}
		}

		//GetPackageJsonAsync
		public static async Awaitable<PackageJson> GetPackageJsonAsync(string personalAccessToken, string organization, string repoName, string branch) {
			string decodedJson = await GetRepositoryFileAsync(personalAccessToken, organization, repoName, branch, "package.json", false);
			PackageJson result = JsonConvert.DeserializeObject<PackageJson>(decodedJson);
			return result;
		}
		public static async Awaitable<PackageDependenciesJson> GetPackageDependenciesJsonAsync(string personalAccessToken, string organization, string repoName, string branch) {
			string decodedJson = await GetRepositoryFileAsync(personalAccessToken, organization, repoName, branch, "parkmin-dependencies.json", true);
			if (string.IsNullOrEmpty(decodedJson)) {
				return new PackageDependenciesJson();
			}

			PackageDependenciesJson result = JsonConvert.DeserializeObject<PackageDependenciesJson>(decodedJson);
			return result ?? new PackageDependenciesJson();
		}
		public class PackageJson
		{
			public string name;
			public string displayName;
		}
		public class PackageDependenciesJson
		{
			public int schemaVersion;
			public List<GitDependency> gitDependencies = new List<GitDependency>();
			public List<NuGetDependency> nugetDependencies = new List<NuGetDependency>();
		}


		//Internal
		static async Awaitable<string> GetRepositoryFileAsync(
			string personalAccessToken,
			string organization,
			string repoName,
			string branch,
			string fileName,
			bool allowNotFound
		) {
			string escapedBranch = UnityWebRequest.EscapeURL(branch);
			string escapedFileName = UnityWebRequest.EscapeURL(fileName);
			string url = $"https://api.github.com/repos/{organization}/{repoName}/contents/{escapedFileName}?ref={escapedBranch}";

			using (UnityWebRequest request = UnityWebRequest.Get(url)) {
				string json = await SendBearerRequestAsync(request, personalAccessToken, allowNotFound);
				if (string.IsNullOrEmpty(json)) {
					return null;
				}

				JObject obj = JObject.Parse(json);
				string base64 = obj["content"]?.ToString();
				if (string.IsNullOrEmpty(base64)) {
					throw new Exception($"{fileName} not found");
				}

				base64 = base64.Replace("\n", "").Replace("\r", "");
				byte[] bytes = Convert.FromBase64String(base64);
				return Encoding.UTF8.GetString(bytes);
			}
		}
		static async Awaitable<string> SendBearerRequestAsync(UnityWebRequest request, string personalAccessToken, bool allowNotFound = false) {
			if (!string.IsNullOrWhiteSpace(personalAccessToken))
				request.SetRequestHeader("Authorization", "Bearer " + personalAccessToken);
			request.SetRequestHeader("Accept", "application/vnd.github+json");
			request.SetRequestHeader("User-Agent", "ParkMinPackages PackageManager");

			await request.SendWebRequest();

			if (allowNotFound && request.responseCode == 404) {
				return null;
			}

			if (request.result != UnityWebRequest.Result.Success) {
				throw new Exception(
					"GitHub request failed. " +
					"URL: " + request.url + ", " +
					"HTTP: " + request.responseCode + ", " +
					"Error: " + request.error + ", " +
					"Body: " + request.downloadHandler.text);
			}

			return request.downloadHandler.text;
		}
	}
}
