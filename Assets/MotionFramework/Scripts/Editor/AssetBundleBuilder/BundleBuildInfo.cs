﻿//--------------------------------------------------
// Motion Framework
// Copyright©2021-2021 何冠峰
// Licensed under the MIT license
//--------------------------------------------------
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace MotionFramework.Editor
{
	public class BundleBuildInfo
	{
		/// <summary>
		/// AssetBundle完整名称
		/// </summary>
		public string AssetBundleFullName { private set; get; }

		/// <summary>
		/// AssetBundle标签
		/// </summary>
		public string AssetBundleLabel { private set; get; }

		/// <summary>
		/// AssetBundle变体
		/// </summary>
		public string AssetBundleVariant { private set; get; }

		/// <summary>
		/// 收集标记
		/// </summary>
		public bool IsCollectBundle { private set; get; }

		/// <summary>
		/// 包含的资源列表
		/// </summary>
		public readonly List<AssetInfo> Assets = new List<AssetInfo>();


		public BundleBuildInfo(string bundleLabel, string bundleVariant)
		{
			AssetBundleLabel = bundleLabel;
			AssetBundleVariant = bundleVariant;
			AssetBundleFullName = AssetInfo.MakeAssetBundleFullName(bundleLabel, bundleVariant);
		}

		/// <summary>
		/// 添加一个打包资源
		/// </summary>
		public void PackAsset(AssetInfo assetInfo)
		{
			foreach (var asset in Assets)
			{
				if (asset.AssetPath == assetInfo.AssetPath)
					throw new System.Exception($"Asset info is existed : {assetInfo.AssetPath}");
			}
			Assets.Add(assetInfo);

			// 注意：只要有一个资源是主动收集的，我们就认为该Bundle文件同样为收集的。
			if (assetInfo.IsCollectAsset)
				IsCollectBundle = true;
		}

		/// <summary>
		/// 获取资源路径列表
		/// </summary>
		public string[] GetAssetPaths()
		{
			return Assets.Select(t => t.AssetPath).ToArray();
		}

		/// <summary>
		/// 创建AssetBundleBuild类
		/// </summary>
		public UnityEditor.AssetBundleBuild CreateAssetBundleBuild()
		{
			AssetBundleBuild build = new AssetBundleBuild();
			build.assetBundleName = AssetBundleLabel;
			build.assetBundleVariant = AssetBundleVariant;
			build.assetNames = Assets.Select(t => t.AssetPath).ToArray();
			return build;
		}
	}
}