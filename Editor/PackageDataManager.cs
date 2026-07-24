using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace com.parkminpackages.packagemanager.Editor
{
	internal static class PackageDataManager
	{
		const string CatalogCacheJsonKey = "ParkMinPackages.PackageManager.Catalog.Json";
		const string CatalogCacheTicksKey = "ParkMinPackages.PackageManager.Catalog.Ticks";
		static readonly TimeSpan CatalogCacheDuration = TimeSpan.FromMinutes(15);

		public static async Awaitable<List<PackageData>> RequestFromCatalogAsync(string personalAccessToken, bool forceRefresh, CancellationToken cancellationToken)
		{
			PackageCollection unityPackageCollection = await ClientUtility.ListAsync();
			if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException();

			PackageCatalog catalog = await GetCatalogAsync(personalAccessToken, forceRefresh, cancellationToken);
			List<PackageData> packageDatas = new List<PackageData>();

			foreach (PackageCatalogEntry entry in catalog.packages ?? new List<PackageCatalogEntry>()) {
				if (string.IsNullOrWhiteSpace(entry.packageName) || string.IsNullOrWhiteSpace(entry.gitUrl)) continue;

				UnityEditor.PackageManager.PackageInfo unityPackageInfo = unityPackageCollection.FirstOrDefault(info => info.name == entry.packageName);
				packageDatas.Add(new PackageData {
					RepoName = entry.repository,
					DisplayName = string.IsNullOrWhiteSpace(entry.displayName) ? entry.packageName : entry.displayName,
					GitCloneURL = entry.gitUrl,
					PackageName = entry.packageName,
					RemoteVersion = entry.version,
					CurrentVersion = unityPackageInfo?.version,
					IsEmbed = unityPackageInfo != null && unityPackageInfo.source == PackageSource.Embedded,
				});
			}

			return packageDatas;
		}

		static async Awaitable<PackageCatalog> GetCatalogAsync(string personalAccessToken, bool forceRefresh, CancellationToken cancellationToken)
		{
			if (!forceRefresh && TryLoadCachedCatalog(false, out PackageCatalog cachedCatalog)) return cachedCatalog;

			try {
				string catalogJson = await GitRestAPI.GetPackageCatalogJsonAsync(personalAccessToken);
				if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException();
				PackageCatalog catalog = JsonConvert.DeserializeObject<PackageCatalog>(catalogJson);
				if (catalog?.packages == null) throw new Exception("Package catalog is invalid.");
				EditorPrefs.SetString(CatalogCacheJsonKey, catalogJson);
				EditorPrefs.SetString(CatalogCacheTicksKey, DateTime.UtcNow.Ticks.ToString());
				return catalog;
			}
			catch (Exception exception) when (TryLoadCachedCatalog(true, out PackageCatalog fallbackCatalog)) {
				Debug.LogWarning($"Failed to refresh the package catalog. Using cached data instead. {exception.Message}");
				return fallbackCatalog;
			}
		}

		static bool TryLoadCachedCatalog(bool allowExpired, out PackageCatalog catalog)
		{
			catalog = null;
			string catalogJson = EditorPrefs.GetString(CatalogCacheJsonKey, string.Empty);
			string ticksText = EditorPrefs.GetString(CatalogCacheTicksKey, string.Empty);
			if (string.IsNullOrWhiteSpace(catalogJson) || !long.TryParse(ticksText, out long ticks)) return false;
			if (!allowExpired && DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc) > CatalogCacheDuration) return false;
			catalog = JsonConvert.DeserializeObject<PackageCatalog>(catalogJson);
			return catalog?.packages != null;
		}
	}

	[Serializable]
	internal class PackageCatalog { public List<PackageCatalogEntry> packages; }

	[Serializable]
	internal class PackageCatalogEntry
	{
		public string repository;
		public string displayName;
		public string packageName;
		public string gitUrl;
		public string version;
	}
}