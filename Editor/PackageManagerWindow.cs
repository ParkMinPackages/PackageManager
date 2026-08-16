using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UIElements;

namespace ParkMinPackages.PackageManager.Editor
{
	internal class PackageManagerWindow : EditorWindow
	{
		const string _showDependenciesEditorPrefsKey = "ParkMinPackages.PackageManager.ShowDependencies";

		async Awaitable CreateGUI() {
			if (_cts != null) {
				_cts.Cancel();
				_cts.Dispose();
				_cts = null;
			}

			_cts = new CancellationTokenSource();

			//초기화
			string personalAccessToken = PersonalAccessTokenManager.LoadToken();
			string organization = "ParkMinPackages";
			string[] exceptRepos = new string[] { "Package-Dev" };

			VisualTreeAsset mainTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
				"Packages/com.parkminpackages.packagemanager/Editor/PackageManagerWindow.uxml"
			);

			VisualTreeAsset itemTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
				"Packages/com.parkminpackages.packagemanager/Editor/PackageManagerWindow.Item.uxml"
			);

			PublicGitRepoDatas publicGitRepoDatas = AssetDatabase.LoadAssetAtPath<PublicGitRepoDatas>(
				"Packages/com.parkminpackages.packagemanager/PublicGitRepoDatas/PublicGitRepoDatas.asset"
			);

			rootVisualElement.Clear();
			rootVisualElement.Add(mainTreeAsset.CloneTree());

			Button pacakgesFolderButton = rootVisualElement.Q<Button>("PacakgesFolderButton");
			Foldout personalAccessTokenFoldout = rootVisualElement.Q<Foldout>("PersonalAccessTokenFoldout");
			TextField personalAccessTokenTextField = rootVisualElement.Q<TextField>("PersonalAccessTokenTextField");
			Button installSelectedButton = rootVisualElement.Q<Button>("InstallSelectedButton");
			Button removeSelectedButton = rootVisualElement.Q<Button>("RemoveSelectedButton");
			Button refreshButton = rootVisualElement.Q<Button>("RefreshButton");
			Toggle showDependenciesToggle = rootVisualElement.Q<Toggle>("ShowDependenciesToggle");
			ScrollView scrollView = rootVisualElement.Q<ScrollView>();
			VisualElement publicGitPackagesContainer = scrollView.Q<VisualElement>("PublicGitPackagesContainer");
			VisualElement parkMinPackagesContainer = scrollView.Q<VisualElement>("ParkMinPackagesContainer");
			Label refreshStateLabel = rootVisualElement.Q<Label>("RefreshStateLabel");
			List<GitItemUI> itemUiList = new List<GitItemUI>();
			bool showDependencies = EditorPrefs.GetBool(_showDependenciesEditorPrefsKey, false);
			showDependenciesToggle.SetValueWithoutNotify(showDependencies);

			//pacakgesFolderButton 구현
			pacakgesFolderButton.clicked += async () =>
			{
				UnityEngine.Object folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Packages/com.parkminpackages.packagemanager/UnityPackages");
				Selection.activeObject = folder;
				EditorUtility.FocusProjectWindow();
				EditorGUIUtility.PingObject(folder);
				AssetDatabase.OpenAsset(folder);
			};

			//PersonalAccessToken UI 구현
			personalAccessTokenTextField.value = PersonalAccessTokenManager.LoadToken();
			personalAccessTokenTextField.RegisterValueChangedCallback(evt =>
			{
				PersonalAccessTokenManager.SaveToken(evt.newValue);
			});
			showDependenciesToggle.RegisterValueChangedCallback(evt =>
			{
				showDependencies = evt.newValue;
				EditorPrefs.SetBool(_showDependenciesEditorPrefsKey, showDependencies);
				foreach (GitItemUI itemUI in itemUiList) {
					itemUI.SetDependenciesVisible(showDependencies);
				}
			});

			//_installSelectedButton 구현
			installSelectedButton.clicked += async () =>
			{
				await ClientUtility.AddAndRemoveAsyncWithProgressBar(itemUiList.Where(ui => ui.Checked).Select(ui => ui.GitURL).ToArray(), null);
				CreateGUI();
			};

			//_removeSelectedButton 구현
			removeSelectedButton.clicked += async () =>
			{
				await ClientUtility.AddAndRemoveAsyncWithProgressBar(null, itemUiList.Where(ui => ui.Checked).Select(ui => ui.PackageName).ToArray());
				CreateGUI();
			};

			//_refreshButton 구현
			refreshButton.clicked += () =>
			{
				CreateGUI();
			};

			//아이템 채우기 구현
			try {
				Action afterButtonClickAction = () =>
				{
					CreateGUI();
				};

				PackageCollection packageCollection = await ClientUtility.ListAsync(true);
				if (_cts.IsCancellationRequested) throw new OperationCanceledException();
				PackageDependencyResolver dependencyResolver = new PackageDependencyResolver(packageCollection);

				//Public Repo
				foreach (PublicGitRepoData data in publicGitRepoDatas.Value) {
					PublicGitItemUI publicGitItemUI = new PublicGitItemUI(packageCollection, itemTreeAsset, publicGitPackagesContainer,
						data.DisplayName,
						data.Version,
						data.CloneURL,
						data.PackageName,
						afterButtonClickAction
					);
					publicGitItemUI.SetDependencies(
						dependencyResolver.ResolveGit(data.GitDependencies),
						dependencyResolver.ResolveNuGet(data.NuGetDependencies)
					);
					publicGitItemUI.SetDependenciesVisible(showDependencies);
					itemUiList.Add(publicGitItemUI);
				}

				//Private Organization Repo
				List<PackageData> requestAsync = await PackageDataManager.RequestToOrganizationAsync(
					personalAccessToken,
					organization,
					exceptRepos,
					packageCollection,
					dependencyResolver,
					_cts.Token
				);

				foreach (PackageData packageData in requestAsync) {
					GitItemUI eachGitItemUI = new GitItemUI(
						itemTreeAsset,
						parkMinPackagesContainer,
						packageData.DisplayName,
						packageData.Version,
						packageData.GitCloneURL,
						packageData.PackageName,
						afterButtonClickAction
					);
					eachGitItemUI.SetDependencies(packageData.GitDependencies, packageData.NuGetDependencies);
					eachGitItemUI.SetDependenciesVisible(showDependencies);
					eachGitItemUI.State = packageData.State;
					itemUiList.Add(eachGitItemUI);
				}

				refreshStateLabel.style.display = DisplayStyle.None;
			}
			catch (OperationCanceledException) { }
			catch (Exception e) {
				Debug.LogException(e);
				throw;
			}
		}
		void OnDisable() {
			if (_cts != null) {
				_cts.Cancel();
				_cts.Dispose();
				_cts = null;
			}
		}

		CancellationTokenSource _cts;

		//Type
		class PublicGitItemUI : GitItemUI
		{
			public PublicGitItemUI(
				PackageCollection packageCollection,
				VisualTreeAsset itemTreeAsset,
				VisualElement parent,
				string displayName,
				string version,
				string gitURL,
				string packageName,
				Action afterButtonClickAction
			) : base(itemTreeAsset, parent, displayName, version, gitURL, packageName, afterButtonClickAction) {
				UnityEditor.PackageManager.PackageInfo packageInfo = packageCollection.FirstOrDefault(info => info.name == packageName);
				if (packageInfo != null) {
					if (packageInfo.source == PackageSource.Embedded) {
						State = PackageState.Embedded;
					}
					else {
						State = PackageState.Installed;
					}
				}
				else
					State = PackageState.UnInstalled;
			}
		}

		class GitItemUI : ItemUI
		{
			public GitItemUI(
				VisualTreeAsset itemTreeAsset,
				VisualElement parent,
				string displayName,
				string version,
				string gitURL,
				string packageName,
				Action afterButtonClickAction
			) : base(itemTreeAsset, parent) {
				_gitURL = gitURL;
				_packageName = packageName;
				DisplayName = $"{displayName} #{(string.IsNullOrWhiteSpace(version) ? "최신" : version)}";

				_installButton.clicked += async () =>
				{
					await ClientUtility.AddAsyncWithProgressBar(gitURL);
					afterButtonClickAction?.Invoke();
				};
				_removeButton.clicked += async () =>
				{
					await ClientUtility.RemoveAsyncWithProgressBar(packageName);
					afterButtonClickAction?.Invoke();
				};
				_embedButton.clicked += async () =>
				{
					await ClientUtility.EmbedAsyncWithProgressBar(packageName);
					afterButtonClickAction?.Invoke();
				};
			}
			public string GitURL
			{
				get { return _gitURL; }
			}
			public string PackageName
			{
				get { return _packageName; }
			}
			readonly string _gitURL;
			readonly string _packageName;
		}

		class ItemUI
		{
			public ItemUI(VisualTreeAsset itemTreeAsset, VisualElement parent) {
				TemplateContainer templateContainer = itemTreeAsset.CloneTree();
				_toggle = templateContainer.Q<Toggle>();
				_displayNameLabel = templateContainer.Q<Label>("DisplayNameLabel");
				_stateLabel = templateContainer.Q<Label>("StateLabel");
				_dependenciesContainer = templateContainer.Q<VisualElement>("DependenciesContainer");
				_installButton = templateContainer.Q<Button>("InstallButton");
				_removeButton = templateContainer.Q<Button>("RemoveButton");
				_embedButton = templateContainer.Q<Button>("EmbedButton");

				State = PackageState.UnInstalled;

				parent.Add(templateContainer);
			}

			public void SetDependencies(
				IReadOnlyList<PackageDependencyData> gitDependencies,
				IReadOnlyList<PackageDependencyData> nuGetDependencies
			) {
				_dependenciesContainer.Clear();
				AddDependencyRows("Git", gitDependencies);
				AddDependencyRows("NuGet", nuGetDependencies);
				UpdateDependenciesVisibility();
			}
			public void SetDependenciesVisible(bool visible) {
				_showDependencies = visible;
				UpdateDependenciesVisibility();
			}
			public PackageState State
			{
				get { return _state; }
				set
				{
					_state = value;

					switch (_state) {
						case PackageState.UnInstalled:
							_stateLabel.text = "설치안됨";
							_installButton.enabledSelf = true;
							_removeButton.enabledSelf = false;
							_embedButton.enabledSelf = false;
							_stateLabel.style.color = new StyleColor(StyleKeyword.Null);
							break;
						case PackageState.Updateable:
							_stateLabel.text = "업데이트가능";
							_installButton.enabledSelf = true;
							_removeButton.enabledSelf = true;
							_embedButton.enabledSelf = true;
							_stateLabel.style.color = new StyleColor(Color.yellow);
							break;
						case PackageState.Installed:
							_stateLabel.text = "설치됨";
							_installButton.enabledSelf = true;
							_removeButton.enabledSelf = true;
							_embedButton.enabledSelf = true;
							_stateLabel.style.color = new StyleColor(Color.green);
							break;
						case PackageState.Embedded:
							_stateLabel.text = "Embed";
							_installButton.enabledSelf = false;
							_removeButton.enabledSelf = false;
							_embedButton.enabledSelf = false;
							_stateLabel.style.color = new StyleColor(Color.green);
							break;
					}
				}
			}
			public string DisplayName
			{
				get { return _displayName; }
				set
				{
					_displayName = value;
					_displayNameLabel.text = value;
				}
			}
			public bool Checked
			{
				get { return _toggle.value; }
				set { _toggle.value = value; }
			}

			protected Toggle _toggle;
			protected Label _displayNameLabel;
			protected Label _stateLabel;
			protected VisualElement _dependenciesContainer;
			protected Button _installButton;
			protected Button _removeButton;
			protected Button _embedButton;
			PackageState _state;
			string _displayName;
			bool _showDependencies;

			void AddDependencyRows(
				string category,
				IReadOnlyList<PackageDependencyData> dependencies
			) {
				if (dependencies == null || dependencies.Count == 0) {
					return;
				}

				foreach (PackageDependencyData dependency in dependencies) {
					VisualElement dependencyRow = new VisualElement();
					dependencyRow.style.flexDirection = FlexDirection.Row;
					dependencyRow.style.justifyContent = Justify.SpaceBetween;

					string version = string.IsNullOrWhiteSpace(dependency.Version) ? string.Empty : $" {dependency.Version}";
					Label dependencyNameLabel = new Label($"{category} · {dependency.Name}{version}");
					dependencyNameLabel.style.flexGrow = 1;
					dependencyNameLabel.tooltip = dependency.URL;

					Label dependencyStateLabel = new Label(GetDependencyStateText(dependency));
					dependencyStateLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
					dependencyStateLabel.style.color = GetDependencyStateColor(dependency.State);

					dependencyRow.Add(dependencyNameLabel);
					dependencyRow.Add(dependencyStateLabel);
					_dependenciesContainer.Add(dependencyRow);
				}
			}
			void UpdateDependenciesVisibility() {
				_dependenciesContainer.style.display = _showDependencies && _dependenciesContainer.childCount > 0
					? DisplayStyle.Flex
					: DisplayStyle.None;
			}
			static string GetDependencyStateText(PackageDependencyData dependency) {
				switch (dependency.State) {
					case PackageDependencyState.Installed:
						return "설치됨";
					case PackageDependencyState.NotInstalled:
						return "설치안됨";
					case PackageDependencyState.VersionMismatch:
						return string.IsNullOrWhiteSpace(dependency.InstalledVersion)
							? "버전 불일치"
							: $"버전 불일치 ({dependency.InstalledVersion})";
					case PackageDependencyState.Unavailable:
						return "확인 불가";
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			static StyleColor GetDependencyStateColor(PackageDependencyState state) {
				switch (state) {
					case PackageDependencyState.Installed:
						return new StyleColor(Color.green);
					case PackageDependencyState.NotInstalled:
					case PackageDependencyState.VersionMismatch:
						return new StyleColor(Color.yellow);
					case PackageDependencyState.Unavailable:
						return new StyleColor(StyleKeyword.Null);
					default:
						throw new ArgumentOutOfRangeException(nameof(state), state, null);
				}
			}
		}
	}
}
