﻿//--------------------------------------------------
// Motion Framework
// Copyright©2020-2021 何冠峰
// Licensed under the MIT license
//--------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MotionFramework.Utility;

namespace MotionFramework.Resource
{
	[Serializable]
	public class PatchCache
	{
		/// <summary>
		/// 缓存的APP内置版本
		/// </summary>
		public string CacheAppVersion = string.Empty;

		/// <summary>
		/// 读取缓存文件
		/// 注意：如果文件不存在则创建新的缓存文件
		/// </summary>
		public static PatchCache LoadCache()
		{
			if (PatchHelper.CheckSandboxCacheFileExist())
			{
				MotionLog.Log("Load patch cache from disk.");
				string filePath = PatchHelper.GetSandboxCacheFilePath();
				string jsonData = FileUtility.ReadFile(filePath);
				return JsonUtility.FromJson<PatchCache>(jsonData);
			}
			else
			{
				MotionLog.Log($"Create patch cache to disk : {Application.version}");
				PatchCache cache = new PatchCache();
				cache.CacheAppVersion = Application.version;
				string filePath = PatchHelper.GetSandboxCacheFilePath();
				string jsonData = JsonUtility.ToJson(cache);
				FileUtility.CreateFile(filePath, jsonData);
				return cache;
			}
		}

		/// <summary>
		/// 更新缓存文件
		/// </summary>
		public static void UpdateCache()
		{
			MotionLog.Log($"Update patch cache to disk : {Application.version}");
			PatchCache cache = new PatchCache();
			cache.CacheAppVersion = Application.version;
			string filePath = PatchHelper.GetSandboxCacheFilePath();
			string jsonData = JsonUtility.ToJson(cache);
			FileUtility.CreateFile(filePath, jsonData);
		}
	}
}