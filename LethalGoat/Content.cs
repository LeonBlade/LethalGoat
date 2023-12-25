using LethalLib.Modules;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Unity.Netcode.Components;
using UnityEngine.Video;

namespace LethalGoat
{
  public class Content
  {
    public static AssetBundle Assets;
    public static Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();
    public static VideoClip sonackToiletVideo;

    private static void TryLoadAssets()
    {
      if (Assets == null)
      {
        Assets = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "lethalgoat"));
      }
    }

    public static void Load()
    {
      TryLoadAssets();

      var bongoat = Assets.LoadAsset<Item>("Assets/Bongoat.asset");
      if (bongoat.spawnPrefab.GetComponent<NetworkTransform>() == null)
        bongoat.spawnPrefab.AddComponent<NetworkTransform>();
      Prefabs.Add("Bongoat", bongoat.spawnPrefab);
      NetworkPrefabs.RegisterNetworkPrefab(bongoat.spawnPrefab);
      Items.RegisterShopItem(bongoat, 69);

      sonackToiletVideo = Assets.LoadAsset<VideoClip>("Assets/toilet.mp4");

      Plugin.Log.LogInfo("Loaded assets");

      var types = Assembly.GetExecutingAssembly().GetTypes();
      foreach (var type in types)
      {
        var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        foreach (var method in methods)
        {
          var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
          if (attributes.Length > 0)
          {
            method.Invoke(null, null);
          }
        }
      }
    }

    public static void Unload()
    {
      // Assets.Unload(true);
    }
  }
}
