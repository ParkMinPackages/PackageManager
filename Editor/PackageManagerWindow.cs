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


			rootVisualElement.Clear();
			rootVisualElement.Add(mainTreeAsset.CloneTree());

			Foldout personalAccessTokenFoldout = rootVisualElement.Q<Foldout>("PersonalAccessTokenFoldout");
			TextField personalAccessTokenTextField = rootVisualElement.Q<TextField>("PersonalAccessTokenTextField");
			Button installSelectedButton = rootVisualElement.Q<Button>("InstallSelectedButton");
			Button removeSelectedButton = rootVisualElement.Q<Button>("RemoveSelectedButton");
			Button refreshButton = rootVisualElement.Q<Button>("RefreshButton");
			ScrollView scrollView = rootVisualElement.Q<ScrollView>();
			Label refreshStateLabel = rootVisualElement.Q<Label>("RefreshStateLabel");
			List<GitItemUI> itemUiList = new List<GitItemUI>();

			//PersonalAccessToken UI 구현
			UpdatePersonalAccessTokenFoldout(personalAccessTokenFoldout);
			personalAccessTokenTextField.value = PersonalAccessTokenManager.LoadToken();
			personalAccessTokenTextField.RegisterValueChangedCallback(evt =>
			{
				PersonalAccessTokenManager.SaveToken(evt.newValue);
				UpdatePersonalAccessTokenFoldout(personalAccessTokenFoldout);
			});

			//_installSelectedButton 구현
			installSelectedButton.clicked += () =>
			{
				ClientUtility.AddAndRemoveAsyncWithProgressBar(itemUiList.Where(ui => ui.Checked).Select(ui => ui.GitURL).ToArray(), null);
			};

			//_removeSelectedButton 구현
			removeSelectedButton.clicked += () =>
			{
				ClientUtility.AddAndRemoveAsyncWithProgressBar(null, itemUiList.Where(ui => ui.Checked).Select(ui => ui.PackageName).ToArray());
			};

			//_refreshButton 구현
			refreshButton.clicked += () =>
			{
				CreateGUI();
			};

			//아이템 채우기 구현
			try {
				PackageCollection packageCollection = await ClientUtility.ListAsync();
				if (_cts.IsCancellationRequested) throw new OperationCanceledException();

				//Public Repo
				PublicGitItemUI uniTaskItemUI = new PublicGitItemUI(packageCollection, itemTreeAsset, scrollView,
					"Cysharp.UniTask",
					"https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
					"com.cysharp.unitask"
				);
				itemUiList.Add(uniTaskItemUI);

				PublicGitItemUI eflatunSceneReferenceItemUI = new PublicGitItemUI(packageCollection, itemTreeAsset, scrollView,
					"Eflatun.SceneReference",
					"git+https://github.com/starikcetin/Eflatun.SceneReference.git#upm",
					"com.eflatun.scenereference"
				);
				itemUiList.Add(eflatunSceneReferenceItemUI);

				//Private Organization Repo
				List<PackageData> requestAsync = await PackageDataManager.RequestToOrganizationAsync(personalAccessToken, organization, exceptRepos, _cts.Token);

				foreach (PackageData packageData in requestAsync) {
					GitItemUI eachGitItemUI = new GitItemUI(itemTreeAsset, scrollView, packageData.DisplayName, packageData.GitCloneURL, packageData.PackageName);
					eachGitItemUI.State = packageData.State;
					itemUiList.Add(eachGitItemUI);
				}

				refreshStateLabel.style.display = DisplayStyle.None;
			}
			catch (OperationCanceledException) { }
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
			public PublicGitItemUI(PackageCollection packageCollection, VisualTreeAsset itemTreeAsset, VisualElement parent, string displayName, string gitURL, string packageName) : base(itemTreeAsset, parent, displayName, gitURL, packageName) {
				if (packageCollection.Any(info => info.name == packageName))
					State = PackageState.Installed;
				else
					State = PackageState.UnInstalled;
			}
		}

		class GitItemUI : ItemUI
		{
			public GitItemUI(VisualTreeAsset itemTreeAsset, VisualElement parent, string displayName, string gitURL, string packageName) : base(itemTreeAsset, parent) {
				_gitURL = gitURL;
				_packageName = packageName;
				DisplayName = displayName;

				_installButton.clicked += () =>
				{
					ClientUtility.AddAsyncWithProgressBar(gitURL);
				};
				_removeButton.clicked += () =>
				{
					ClientUtility.RemoveAsyncWithProgressBar(packageName);
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
							_stateLabel.style.color = new StyleColor(StyleKeyword.Null);
							break;
						case PackageState.Updateable:
							_stateLabel.text = "업데이트가능";
							_stateLabel.style.color = new StyleColor(Color.yellow);
							break;
						case PackageState.Installed:
							_stateLabel.text = "설치됨";
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
			PackageState _state;
			string _displayName;
		}
	}
}