using System;
using UnityEngine;
using UnityEngine.Networking;

namespace com.parkminpackages.packagemanager.Editor
{
	internal static class GitRestAPI
	{
		const string PackageCatalogUrl = "https://raw.githubusercontent.com/ParkMinPackages/Package-Dev/main/package-catalog.json";

		public static async Awaitable<string> GetPackageCatalogJsonAsync(string personalAccessToken)
		{
			using (UnityWebRequest request = UnityWebRequest.Get(PackageCatalogUrl)) {
				return await SendRequestAsync(request, personalAccessToken);
			}
		}

		static async Awaitable<string> SendRequestAsync(UnityWebRequest request, string personalAccessToken)
		{
			if (!string.IsNullOrWhiteSpace(personalAccessToken)) request.SetRequestHeader("Authorization", "Bearer " + personalAccessToken);
			request.SetRequestHeader("Accept", "application/json");
			request.SetRequestHeader("User-Agent", "ParkMinPackages PackageManager");
			await request.SendWebRequest();

			if (request.result != UnityWebRequest.Result.Success) {
				string remaining = request.GetResponseHeader("X-RateLimit-Remaining");
				if (request.responseCode == 403 && remaining == "0") throw new Exception("GitHub API rate limit reached. Enter a Personal Access Token or try again later.");
				throw new Exception($"GitHub request failed. HTTP: {request.responseCode}, Error: {request.error}");
			}

			return request.downloadHandler.text;
		}
	}
}