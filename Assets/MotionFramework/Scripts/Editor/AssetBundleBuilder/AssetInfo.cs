﻿//--------------------------------------------------
// Motion Framework
// Copyright©2018-2021 何冠峰
// Licensed under the MIT license
//--------------------------------------------------
using UnityEditor;

namespace MotionFramework.Editor
{
	/// <summary>
	/// 资源信息类
	/// </summary>
	public class AssetInfo
	{
		/// <summary>
		/// 资源路径
		/// </summary>
		public string AssetPath { private set; get; }

		/// <summary>
		/// 资源类型
		/// </summary>
		public System.Type AssetType { private set; get; }

		/// <summary>
		/// 收集标记
		/// </summary>
		public bool IsCollectAsset { private set; get; }

		/// <summary>
		/// 被依赖次数
		/// </summary>
		public int DependCount = 0;

		/// <summary>
		/// AssetBundle标签
		/// </summary>
		public string AssetBundleLabel = null;

		/// <summary>
		/// AssetBundle变体
		/// </summary>
		public string AssetBundleVariant = null;


		public AssetInfo(string assetPath)
		{
			AssetPath = assetPath;
			AssetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
			IsCollectAsset = AssetBundleCollectorSettingData.IsCollectAsset(assetPath, AssetType);
		}

		/// <summary>
		/// 获取AssetBundle的完整名称
		/// </summary>
		public string GetAssetBundleFullName()
		{
			return AssetBundleBuilderHelper.MakeAssetBundleFullName(AssetBundleLabel, AssetBundleVariant);
		}
	}
}