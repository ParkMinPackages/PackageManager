using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace com.parkminpackages.packagemanager.Editor
{
	internal static class ClientUtility
	{
		static bool _isBusy;
		const string _progressBarTitle = "ParkMinPackages Package Manager";

		// =========================
		// Add
		// =========================
		public static Awaitable<UnityEditor.PackageManager.PackageInfo> AddAsync(string identifier) {
			if (string.IsNullOrWhiteSpace(identifier))
				throw new ArgumentException("identifier is null or empty.", nameof(identifier));

			EnsureNotBusy();

			_isBusy = true;

			AddRequest request = Client.Add(identifier);
			AwaitableCompletionSource<UnityEditor.PackageManager.PackageInfo> tcs =
				new AwaitableCompletionSource<UnityEditor.PackageManager.PackageInfo>();

			EditorApplication.update += Update;
			return tcs.Awaitable;

			void Update() {
				if (!request.IsCompleted)
					return;

				EditorApplication.update -= Update;
				_isBusy = false;

				if (request.Status == StatusCode.Success) {
					tcs.TrySetResult(request.Result);
					return;
				}

				string msg = request.Error != null
					? request.Error.message
					: "Add failed.";

				tcs.TrySetException(new Exception(msg));
			}
		}

		public static async Awaitable<UnityEditor.PackageManager.PackageInfo> AddAsyncWithProgressBar(string identifier) {
			EditorUtility.DisplayProgressBar(_progressBarTitle, "설치 중..", 0.9f);

			try {
				return await AddAsync(identifier);
			}
			finally {
				EditorUtility.ClearProgressBar();
			}
		}

		// =========================
		// Remove
		// =========================
		public static Awaitable<bool> RemoveAsync(string packageName) {
			if (string.IsNullOrWhiteSpace(packageName))
				throw new ArgumentException("packageName is null or empty.", nameof(packageName));

			EnsureNotBusy();

			_isBusy = true;

			RemoveRequest request = Client.Remove(packageName);
			AwaitableCompletionSource<bool> tcs = new AwaitableCompletionSource<bool>();

			EditorApplication.update += Update;
			return tcs.Awaitable;

			void Update() {
				if (!request.IsCompleted)
					return;

				EditorApplication.update -= Update;
				_isBusy = false;

				if (request.Status == StatusCode.Success) {
					tcs.TrySetResult(true);
					return;
				}

				string msg = request.Error != null
					? request.Error.message
					: "Remove failed.";

				tcs.TrySetException(new Exception(msg));
			}
		}

		public static async Awaitable RemoveAsyncWithProgressBar(string packageName) {
			EditorUtility.DisplayProgressBar(_progressBarTitle, "제거 중..", 0.9f);

			try {
				await RemoveAsync(packageName);
			}
			finally {
				EditorUtility.ClearProgressBar();
			}
		}

		// =========================
		// AddAndRemove
		// =========================
		public static Awaitable<bool> AddAndRemoveAsync(
			IList<string> addList,
			IList<string> removeList,
			bool dryRun = false) {
			if (addList == null)
				addList = Array.Empty<string>();

			if (removeList == null)
				removeList = Array.Empty<string>();

			EnsureNotBusy();

			_isBusy = true;

			AddAndRemoveRequest request = Client.AddAndRemove(
				new List<string>(addList).ToArray(),
				new List<string>(removeList).ToArray(),
				dryRun
			);

			AwaitableCompletionSource<bool> tcs = new AwaitableCompletionSource<bool>();

			EditorApplication.update += Update;
			return tcs.Awaitable;

			void Update() {
				if (!request.IsCompleted)
					return;

				EditorApplication.update -= Update;
				_isBusy = false;

				if (request.Status == StatusCode.Success) {
					tcs.TrySetResult(true);
					return;
				}

				string msg = request.Error != null
					? request.Error.message
					: "AddAndRemove failed.";

				tcs.TrySetException(new Exception(msg));
			}
		}

		public static async Awaitable AddAndRemoveAsyncWithProgressBar(
			IList<string> addList,
			IList<string> removeList,
			bool dryRun = false) {
			EditorUtility.DisplayProgressBar(_progressBarTitle, "패키지 변경 중..", 0.5f);

			try {
				await AddAndRemoveAsync(addList, removeList, dryRun);
			}
			finally {
				EditorUtility.ClearProgressBar();
			}
		}

		// =========================
		// Embed
		// =========================
		public static Awaitable<UnityEditor.PackageManager.PackageInfo> EmbedAsync(string packageName) {
			if (string.IsNullOrWhiteSpace(packageName))
				throw new ArgumentException("packageName is null or empty.", nameof(packageName));

			EnsureNotBusy();

			_isBusy = true;

			EmbedRequest request = Client.Embed(packageName);
			AwaitableCompletionSource<UnityEditor.PackageManager.PackageInfo> tcs =
				new AwaitableCompletionSource<UnityEditor.PackageManager.PackageInfo>();

			EditorApplication.update += Update;
			return tcs.Awaitable;

			void Update() {
				if (!request.IsCompleted)
					return;

				EditorApplication.update -= Update;
				_isBusy = false;

				if (request.Status == StatusCode.Success) {
					tcs.TrySetResult(request.Result);
					return;
				}

				string msg = request.Error != null
					? request.Error.message
					: "Embed failed.";

				tcs.TrySetException(new Exception(msg));
			}
		}

		public static async Awaitable<UnityEditor.PackageManager.PackageInfo> EmbedAsyncWithProgressBar(string packageName) {
			EditorUtility.DisplayProgressBar(_progressBarTitle, "로컬 패키지로 변환 중..", 0.9f);

			try {
				return await EmbedAsync(packageName);
			}
			finally {
				EditorUtility.ClearProgressBar();
			}
		}

		// =========================
		// List
		// =========================
		public static Awaitable<UnityEditor.PackageManager.PackageCollection> ListAsync(bool includeIndirect = false) {
			EnsureNotBusy();

			_isBusy = true;

			ListRequest request = Client.List(includeIndirect);
			AwaitableCompletionSource<UnityEditor.PackageManager.PackageCollection> tcs =
				new AwaitableCompletionSource<UnityEditor.PackageManager.PackageCollection>();

			EditorApplication.update += Update;
			return tcs.Awaitable;

			void Update() {
				if (!request.IsCompleted)
					return;

				EditorApplication.update -= Update;
				_isBusy = false;

				if (request.Status == StatusCode.Success) {
					tcs.TrySetResult(request.Result);
					return;
				}

				string msg = request.Error != null
					? request.Error.message
					: "List failed.";

				tcs.TrySetException(new Exception(msg));
			}
		}

		// =========================
		// 공통
		// =========================
		static void EnsureNotBusy() {
			if (_isBusy)
				throw new InvalidOperationException("이미 패키지 작업이 진행 중입니다.");
		}
	}
}