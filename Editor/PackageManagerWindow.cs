using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UIElements;

namespace com.mutant.packagemanager.Editor
{
	public class PackageManagerWindow : EditorWindow
	{
		async Awaitable CreateGUI() {
			if (_cts != null) {
				_cts.Cancel();
				_cts.Dispose();
				_cts = null;
			}

			_cts = new CancellationTokenSource();

			//초기화
			string personalAccessToken = PersonalAccessTokenManager.LoadToken();
			string organization = "Mutant-UnityPackages";
			string[] exceptRepos = new string[] { "Package-Dev" };

			VisualTreeAsset mainTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
				"Packages/com.mutant.packagemanager/Editor/PackageManagerWindow.uxml"
			);

			VisualTreeAsset itemTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
				"Packages/com.mutant.packagemanager/Editor/PackageManagerWindow.Item.uxml"
			);

			PublilcGitRepoData publicGitRepoData = AssetDatabase.LoadAssetAtPath<PublilcGitRepoData>(
				"Packages/com.mutant.packagemanager/Editor/PublilcGitRepoData.asset"
			);

			rootVisualElement.Clear();
			rootVisualElement.Add(mainTreeAsset.CloneTree());

			Button pacakgesFolderButton = rootVisualElement.Q<Button>("PacakgesFolderButton");
			Foldout personalAccessTokenFoldout = rootVisualElement.Q<Foldout>("PersonalAccessTokenFoldout");
			TextField personalAccessTokenTextField = rootVisualElement.Q<TextField>("PersonalAccessTokenTextField");
			Button installSelectedButton = rootVisualElement.Q<Button>("InstallSelectedButton");
			Button removeSelectedButton = rootVisualElement.Q<Button>("RemoveSelectedButton");
			Button refreshButton = rootVisualElement.Q<Button>("RefreshButton");
			ScrollView scrollView = rootVisualElement.Q<ScrollView>();
			Label refreshStateLabel = rootVisualElement.Q<Label>("RefreshStateLabel");
			List<GitItemUI> itemUiList = new List<GitItemUI>();

			//pacakgesFolderButton 구현
			pacakgesFolderButton.clicked += async () =>
			{
				UnityEngine.Object folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Packages/com.mutant.packagemanager/UnityPackages");
				Selection.activeObject = folder;
				EditorUtility.FocusProjectWindow();
				EditorGUIUtility.PingObject(folder);
				AssetDatabase.OpenAsset(folder);
			};

			//PersonalAccessToken UI 구현
			UpdatePersonalAccessTokenFoldout(personalAccessTokenFoldout);
			personalAccessTokenTextField.value = PersonalAccessTokenManager.LoadToken();
			personalAccessTokenTextField.RegisterValueChangedCallback(evt =>
			{
				PersonalAccessTokenManager.SaveToken(evt.newValue);
				UpdatePersonalAccessTokenFoldout(personalAccessTokenFoldout);
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

				PackageCollection packageCollection = await ClientUtility.ListAsync();
				if (_cts.IsCancellationRequested) throw new OperationCanceledException();

				//Public Repo
				foreach (PublilcGitRepoData.Data data in publicGitRepoData.Value) {
					PublicGitItemUI publicGitItemUI = new PublicGitItemUI(packageCollection, itemTreeAsset, scrollView,
						data.DisplayName,
						data.CloneURL,
						data.PackageName,
						afterButtonClickAction
					);
					itemUiList.Add(publicGitItemUI);
				}

				//Private Organization Repo
				List<PackageData> requestAsync = await PackageDataManager.RequestToOrganizationAsync(personalAccessToken, organization, exceptRepos, _cts.Token);

				foreach (PackageData packageData in requestAsync) {
					GitItemUI eachGitItemUI = new GitItemUI(itemTreeAsset, scrollView, packageData.DisplayName, packageData.GitCloneURL, packageData.PackageName, afterButtonClickAction);
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
		void UpdatePersonalAccessTokenFoldout(Foldout foldout) {
			if (string.IsNullOrEmpty(PersonalAccessTokenManager.LoadToken())) {
				foldout.text = "Git Personal Access Token을 입력해주세요.";
				foldout.style.color = Color.red;
			}
			else {
				foldout.text = "Git Personal Access Token";
				foldout.style.color = new StyleColor(StyleKeyword.Null);
			}
		}


		//Type
		class PublicGitItemUI : GitItemUI
		{
			public PublicGitItemUI(PackageCollection packageCollection, VisualTreeAsset itemTreeAsset, VisualElement parent, string displayName, string gitURL, string packageName, Action afterButtonClickAction) : base(itemTreeAsset, parent, displayName, gitURL, packageName, afterButtonClickAction) {
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
			public GitItemUI(VisualTreeAsset itemTreeAsset, VisualElement parent, string displayName, string gitURL, string packageName, Action afterButtonClickAction) : base(itemTreeAsset, parent) {
				_gitURL = gitURL;
				_packageName = packageName;
				DisplayName = displayName;

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
				_installButton = templateContainer.Q<Button>("InstallButton");
				_removeButton = templateContainer.Q<Button>("RemoveButton");
				_embedButton = templateContainer.Q<Button>("EmbedButton");

				State = PackageState.UnInstalled;

				parent.Add(templateContainer);
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
			protected Button _installButton;
			protected Button _removeButton;
			protected Button _embedButton;
			PackageState _state;
			string _displayName;
		}
	}
}