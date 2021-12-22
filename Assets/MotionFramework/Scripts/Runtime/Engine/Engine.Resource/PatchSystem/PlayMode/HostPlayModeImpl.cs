﻿//--------------------------------------------------
// Motion Framework
// Copyright©2019-2021 何冠峰
// Licensed under the MIT license
//--------------------------------------------------
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace MotionFramework.Resource
{
	internal class HostPlayModeImpl : IBundleServices
	{
		// 补丁清单
		internal PatchManifest AppPatchManifest;
		internal PatchManifest LocalPatchManifest;

		// 参数相关
		internal bool ClearCacheWhenDirty { private set; get; }
		internal bool IgnoreResourceVersion { private set; get; }
		private string _defaultHostServer;
		private string _fallbackHostServer;

		/// <summary>
		/// 异步初始化
		/// </summary>
		public InitializationOperation InitializeAsync(bool clearCacheWhenDirty, bool ignoreResourceVersion,
			 string defaultHostServer, string fallbackHostServer)
		{
			ClearCacheWhenDirty = clearCacheWhenDirty;
			IgnoreResourceVersion = ignoreResourceVersion;
			_defaultHostServer = defaultHostServer;
			_fallbackHostServer = fallbackHostServer;

			var operation = new HostPlayModeInitializationOperation(this);
			OperationUpdater.ProcessOperaiton(operation);
			return operation;
		}

		/// <summary>
		/// 异步更新补丁清单
		/// </summary>
		public UpdateManifestOperation UpdatePatchManifestAsync(int updateResourceVersion, int timeout)
		{
			var operation = new HostPlayModeUpdateManifestOperation(this, updateResourceVersion, timeout);
			OperationUpdater.ProcessOperaiton(operation);
			return operation;
		}

		/// <summary>
		/// 获取资源版本号
		/// </summary>
		public int GetResourceVersion()
		{
			if (LocalPatchManifest == null)
				return 0;
			return LocalPatchManifest.ResourceVersion;
		}

		/// <summary>
		/// 获取内置资源标记列表
		/// </summary>
		public string[] GetManifestBuildinTags()
		{
			if (LocalPatchManifest == null)
				return new string[0];
			return LocalPatchManifest.GetBuildinTags();
		}

		/// <summary>
		/// 创建补丁下载器
		/// </summary>
		public PatchDownloader CreateDLCDownloader(string[] dlcTags, int fileLoadingMaxNumber, int failedTryAgain)
		{
			List<AssetBundleInfo> downloadList = GetPatchDownloadList(dlcTags);
			PatchDownloader downlader = new PatchDownloader(this, downloadList, fileLoadingMaxNumber, failedTryAgain);
			return downlader;
		}
		private List<AssetBundleInfo> GetPatchDownloadList(string[] dlcTags)
		{
			List<PatchBundle> downloadList = new List<PatchBundle>(1000);
			foreach (var patchBundle in LocalPatchManifest.BundleList)
			{
				// 忽略缓存资源
				if (DownloadSystem.Cache.Contains(patchBundle.Hash))
				{
					string sandboxPath = PatchHelper.MakeSandboxCacheFilePath(patchBundle.Hash);
					if (File.Exists(sandboxPath))
						continue;
				}

				// 忽略APP资源
				// 注意：如果是APP资源并且哈希值相同，则不需要下载
				if (AppPatchManifest.Bundles.TryGetValue(patchBundle.BundleName, out PatchBundle appPatchBundle))
				{
					if (appPatchBundle.IsBuildin && appPatchBundle.Hash == patchBundle.Hash)
						continue;
				}

				// 如果是纯内置资源，则统一下载
				// 注意：可能是新增的或者变化的内置资源
				// 注意：可能是由热更资源转换的内置资源
				if (patchBundle.IsPureBuildin())
				{
					downloadList.Add(patchBundle);
				}
				else
				{
					// 查询DLC资源
					if (patchBundle.HasTag(dlcTags))
					{
						downloadList.Add(patchBundle);
					}
				}
			}

			return CacheAndFilterDownloadList(downloadList);
		}

		/// <summary>
		/// 创建补丁下载器
		/// </summary>
		public PatchDownloader CreateBundleDownloader(string[] locations, int fileLoadingMaxNumber, int failedTryAgain)
		{
			List<string> assetPaths = new List<string>(locations.Length);
			foreach (var location in locations)
			{
				string assetPath = AssetSystem.ConvertLocationToAssetPath(location);
				assetPaths.Add(assetPath);
			}

			List<AssetBundleInfo> downloadList = GetPatchDownloadList(assetPaths);
			PatchDownloader downlader = new PatchDownloader(this, downloadList, fileLoadingMaxNumber, failedTryAgain);
			return downlader;
		}
		private List<AssetBundleInfo> GetPatchDownloadList(List<string> assetPaths)
		{
			// 获取资源对象的资源包和所有依赖资源包
			List<PatchBundle> checkList = new List<PatchBundle>();
			foreach (var assetPath in assetPaths)
			{
				string mainBundleName = LocalPatchManifest.GetAssetBundleName(assetPath);
				if (string.IsNullOrEmpty(mainBundleName) == false)
				{
					if (LocalPatchManifest.Bundles.TryGetValue(mainBundleName, out PatchBundle mainBundle))
					{
						checkList.Add(mainBundle);
					}
				}

				string[] dependBundleNames = LocalPatchManifest.GetAllDependencies(assetPath);
				foreach (var dependBundleName in dependBundleNames)
				{
					if (LocalPatchManifest.Bundles.TryGetValue(dependBundleName, out PatchBundle dependBundle))
					{
						checkList.Add(dependBundle);
					}
				}
			}

			List<PatchBundle> downloadList = new List<PatchBundle>(1000);
			foreach (var patchBundle in checkList)
			{
				// 忽略缓存资源
				if (DownloadSystem.Cache.Contains(patchBundle.Hash))
				{
					string sandboxPath = PatchHelper.MakeSandboxCacheFilePath(patchBundle.Hash);
					if (File.Exists(sandboxPath))
						continue;
				}

				// 忽略APP资源
				// 注意：如果是APP资源并且哈希值相同，则不需要下载
				if (AppPatchManifest.Bundles.TryGetValue(patchBundle.BundleName, out PatchBundle appPatchBundle))
				{
					if (appPatchBundle.IsBuildin && appPatchBundle.Hash == patchBundle.Hash)
						continue;
				}

				downloadList.Add(patchBundle);
			}

			return CacheAndFilterDownloadList(downloadList);
		}

		// 缓存系统相关
		private List<AssetBundleInfo> CacheAndFilterDownloadList(List<PatchBundle> downloadList)
		{
			// 检测文件是否已经下载完毕
			// 注意：如果玩家在加载过程中强制退出，下次再进入的时候跳过已经下载的文件
			List<PatchBundle> cacheList = new List<PatchBundle>();
			for (int i = downloadList.Count - 1; i >= 0; i--)
			{
				var patchBundle = downloadList[i];
				if (CheckContentIntegrity(patchBundle))
				{
					cacheList.Add(patchBundle);
					downloadList.RemoveAt(i);
				}
			}

			// 缓存已经下载的有效文件
			if (cacheList.Count > 0)
				DownloadSystem.CacheDownloadPatchFiles(cacheList);

			List<AssetBundleInfo> result = new List<AssetBundleInfo>(downloadList.Count);
			foreach (var patchBundle in downloadList)
			{
				// 注意：资源版本号只用于确定下载路径
				string loadPath = PatchHelper.MakeSandboxCacheFilePath(patchBundle.Hash);
				string remoteURL = GetPatchDownloadURL(patchBundle.Version, patchBundle.Hash);
				string remoteFallbackURL = GetPatchDownloadFallbackURL(patchBundle.Version, patchBundle.Hash);
				AssetBundleInfo bundleInfo = new AssetBundleInfo(patchBundle, loadPath, remoteURL, remoteFallbackURL);
				result.Add(bundleInfo);
			}
			return result;
		}
		private bool CheckContentIntegrity(PatchBundle patchBundle)
		{
			string filePath = PatchHelper.MakeSandboxCacheFilePath(patchBundle.Hash);
			return DownloadSystem.CheckContentIntegrity(filePath, patchBundle.CRC, patchBundle.SizeBytes);
		}

		// 补丁清单相关
		internal void ParseAndSaveRemotePatchManifest(string content)
		{
			LocalPatchManifest = PatchManifest.Deserialize(content);

			// 注意：这里会覆盖掉沙盒内的补丁清单文件
			MotionLog.Log("Save remote patch manifest file.");
			string savePath = AssetPathHelper.MakePersistentLoadPath(ResourceSettingData.Setting.PatchManifestFileName);
			PatchManifest.Serialize(savePath, LocalPatchManifest);
		}

		// WEB相关
		internal string GetPatchDownloadURL(int resourceVersion, string fileName)
		{
			if (IgnoreResourceVersion)
				return $"{_defaultHostServer}/{fileName}";
			else
				return $"{_defaultHostServer}/{resourceVersion}/{fileName}";
		}
		internal string GetPatchDownloadFallbackURL(int resourceVersion, string fileName)
		{
			if (IgnoreResourceVersion)
				return $"{_fallbackHostServer}/{fileName}";
			else
				return $"{_fallbackHostServer}/{resourceVersion}/{fileName}";
		}

		#region IBundleServices接口
		AssetBundleInfo IBundleServices.GetAssetBundleInfo(string bundleName)
		{
			if (string.IsNullOrEmpty(bundleName))
				return new AssetBundleInfo(string.Empty, string.Empty);

			if (LocalPatchManifest.Bundles.TryGetValue(bundleName, out PatchBundle patchBundle))
			{
				// 查询APP资源
				if (AppPatchManifest.Bundles.TryGetValue(bundleName, out PatchBundle appPatchBundle))
				{
					if (appPatchBundle.IsBuildin && appPatchBundle.Hash == patchBundle.Hash)
					{
						string appLoadPath = AssetPathHelper.MakeStreamingLoadPath(appPatchBundle.Hash);
						AssetBundleInfo bundleInfo = new AssetBundleInfo(appPatchBundle, appLoadPath);
						return bundleInfo;
					}
				}

				// 查询沙盒资源
				string sandboxLoadPath = PatchHelper.MakeSandboxCacheFilePath(patchBundle.Hash);
				if (DownloadSystem.Cache.Contains(patchBundle.Hash))
				{
					if (File.Exists(sandboxLoadPath))
					{
						AssetBundleInfo bundleInfo = new AssetBundleInfo(patchBundle, sandboxLoadPath);
						return bundleInfo;
					}
					else
					{
						MotionLog.Error($"Cache file is missing : {sandboxLoadPath}, Bundle : {bundleName}");
					}
				}

				// 从服务端下载
				{
					// 注意：资源版本号只用于确定下载路径
					string remoteURL = GetPatchDownloadURL(patchBundle.Version, patchBundle.Hash);
					string remoteFallbackURL = GetPatchDownloadFallbackURL(patchBundle.Version, patchBundle.Hash);
					AssetBundleInfo bundleInfo = new AssetBundleInfo(patchBundle, sandboxLoadPath, remoteURL, remoteFallbackURL);
					return bundleInfo;
				}
			}
			else
			{
				MotionLog.Warning($"Not found bundle in patch manifest : {bundleName}");
				AssetBundleInfo bundleInfo = new AssetBundleInfo(bundleName, string.Empty);
				return bundleInfo;
			}
		}
		string IBundleServices.GetAssetBundleName(string assetPath)
		{
			return LocalPatchManifest.GetAssetBundleName(assetPath);
		}
		string[] IBundleServices.GetAllDependencies(string assetPath)
		{
			return LocalPatchManifest.GetAllDependencies(assetPath);
		}
		#endregion
	}
}