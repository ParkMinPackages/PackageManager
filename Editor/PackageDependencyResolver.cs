using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor.PackageManager;
using UnityEngine;

namespace ParkMinPackages.PackageManager.Editor
{
	internal sealed class PackageDependencyResolver
	{
		static IReadOnlyDictionary<string, string> GetInstalledNuGetPackageVersions() {
			string packagesConfigPath = Path.Combine(Application.dataPath, "packages.config");
			if (!File.Exists(packagesConfigPath)) {
				return null;
			}

			XDocument document = XDocument.Load(packagesConfigPath);
			Dictionary<string, string> installedPackageVersions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			if (document.Root == null) {
				return installedPackageVersions;
			}

			foreach (XElement packageElement in document.Root.Elements("package")) {
				XAttribute idAttribute = packageElement.Attribute("id");
				XAttribute versionAttribute = packageElement.Attribute("version");
				if (idAttribute == null || versionAttribute == null) {
					continue;
				}

				installedPackageVersions[idAttribute.Value] = versionAttribute.Value;
			}

			return installedPackageVersions;
		}

		public PackageDependencyResolver(PackageCollection unityPackageCollection) {
			_unityPackageCollection = unityPackageCollection;
			_installedNuGetPackageVersions = GetInstalledNuGetPackageVersions();
		}

		public IReadOnlyList<PackageDependencyData> ResolveGit(IReadOnlyList<GitDependency> dependencies) {
			if (dependencies == null) {
				return Array.Empty<PackageDependencyData>();
			}

			return dependencies
				.Select(dependency => {
					PackageInfo installedPackage = _unityPackageCollection.FirstOrDefault(
						packageInfo => string.Equals(packageInfo.name, dependency.PackageName, StringComparison.OrdinalIgnoreCase)
					);

					return new PackageDependencyData {
						Name = dependency.PackageName,
						Version = dependency.Version,
						URL = dependency.URL,
						InstalledVersion = installedPackage?.version,
						State = installedPackage == null
							? PackageDependencyState.NotInstalled
							: string.IsNullOrWhiteSpace(dependency.Version) || string.Equals(installedPackage.version, dependency.Version, StringComparison.OrdinalIgnoreCase)
								? PackageDependencyState.Installed
								: PackageDependencyState.VersionMismatch
					};
				})
				.ToArray();
		}
		public IReadOnlyList<PackageDependencyData> ResolveNuGet(IReadOnlyList<NuGetDependency> dependencies) {
			if (dependencies == null) {
				return Array.Empty<PackageDependencyData>();
			}

			List<PackageDependencyData> dependencyDatas = new List<PackageDependencyData>();
			foreach (NuGetDependency dependency in dependencies) {
				PackageDependencyData dependencyData = new PackageDependencyData();
				dependencyData.Name = dependency.PackageName;
				dependencyData.Version = dependency.Version;

				if (_installedNuGetPackageVersions == null) {
					dependencyData.State = PackageDependencyState.Unavailable;
				}
				else if (!_installedNuGetPackageVersions.TryGetValue(dependency.PackageName, out string installedVersion)) {
					dependencyData.State = PackageDependencyState.NotInstalled;
				}
				else {
					dependencyData.InstalledVersion = installedVersion;
					dependencyData.State = string.Equals(installedVersion, dependency.Version, StringComparison.OrdinalIgnoreCase)
						? PackageDependencyState.Installed
						: PackageDependencyState.VersionMismatch;
				}

				dependencyDatas.Add(dependencyData);
			}

			return dependencyDatas;
		}

		readonly PackageCollection _unityPackageCollection;
		readonly IReadOnlyDictionary<string, string> _installedNuGetPackageVersions;
	}
}
