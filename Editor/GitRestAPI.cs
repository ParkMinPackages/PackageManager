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
			string url = $"https://api.github.com/repos/{organization}/{repoName}/branches/{branchName}";

			using (UnityWebRequest request = UnityWebRequest.Get(url)) {
				string json = await SendBearerRequestAsync(request, personalAccessToken);
				JObject obj = JObject.Parse(json);
				string sha = obj["commit"]?["sha"]?.ToString();
				return sha;
			}
		}

		//GetPackageJsonAsync
		public static async Awaitable<PackageJson> GetPackageJsonAsync(string personalAccessToken, string organization, string repoName, string branch) {
			string url = $"https://api.github.com/repos/{organization}/{repoName}/contents/package.json?ref={UnityWebRequest.EscapeURL(branch)}";

			using (UnityWebRequest request = UnityWebRequest.Get(url)) {
				string json = await SendBearerRequestAsync(request, personalAccessToken);

				// 1. content(base64) 추출
				JObject obj = JObject.Parse(json);
				string base64 = obj["content"]?.ToString();

				if (string.IsNullOrEmpty(base64))
					throw new Exception("package.json not found");

				// 2. base64 디코딩
				base64 = base64.Replace("\n", "");
				byte[] bytes = Convert.FromBase64String(base64);
				string decodedJson = Encoding.UTF8.GetString(bytes);

				// 3. DTO로 변환
				PackageJson result = JsonConvert.DeserializeObject<PackageJson>(decodedJson);

				return result;
			}
		}
		public class PackageJson
		{
			public string name;
			public string displayName;
		}


		//Internal
		static async Awaitable<string> SendBearerRequestAsync(UnityWebRequest request, string personalAccessToken) {
			if (!string.IsNullOrWhiteSpace(personalAccessToken))
				request.SetRequestHeader("Authorization", "Bearer " + personalAccessToken);
			request.SetRequestHeader("Accept", "application/vnd.github+json");
			request.SetRequestHeader("User-Agent", "ParkMinPackages PackageManager");

			await request.SendWebRequest();

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