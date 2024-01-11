using LethalLib.Modules;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Unity.Netcode.Components;
using UnityEngine.Video;
using LethalLib.Extras;

namespace LethalGoat
{
  public class Content
  {
    public static AssetBundle Assets;
    public static Dictionary<string, GameObject> Prefabs = [];
    public static VideoClip sonackToiletVideo;

    private static void TryLoadAssets()
    {
      if (Assets == null)
        Assets = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "lethalgoat"));
    }

    public static void Load()
    {
      TryLoadAssets();

      var bongoat = Assets.LoadAsset<Item>("Assets/Bongoat.asset");
      var bongoatInfo = Assets.LoadAsset<TerminalNode>("Assets/BongoatTerminalNode.asset");
      if (bongoat.spawnPrefab.GetComponent<NetworkTransform>() == null)
      {
        var networkTransform = bongoat.spawnPrefab.AddComponent<NetworkTransform>();
        networkTransform.SlerpPosition = false;
        networkTransform.Interpolate = false;
        networkTransform.SyncPositionX = false;
        networkTransform.SyncPositionY = false;
        networkTransform.SyncPositionZ = false;
        networkTransform.SyncScaleX = false;
        networkTransform.SyncScaleY = false;
        networkTransform.SyncScaleZ = false;
        networkTransform.UseHalfFloatPrecision = true;
      }
      Prefabs.Add("Bongoat", bongoat.spawnPrefab);
      NetworkPrefabs.RegisterNetworkPrefab(bongoat.spawnPrefab);
      Items.RegisterShopItem(bongoat, null, null, bongoatInfo, 69);

      sonackToiletVideo = Assets.LoadAsset<VideoClip>("Assets/toilet.mp4");

      var yuchi = Assets.LoadAsset<UnlockableItemDef>("Assets/YuchiCutoutUnlockable.asset");
      var yuchiInfo = Assets.LoadAsset<TerminalNode>("Assets/YuchiCutoutInfo.asset");
      NetworkPrefabs.RegisterNetworkPrefab(yuchi.unlockable.prefabObject);
      Unlockables.RegisterUnlockable(yuchi, StoreType.Decor, null, null, yuchiInfo, 130);

      PrepareNetcodePatch();

      Plugin.Log.LogInfo("Loaded assets");
    }

    private static void PrepareNetcodePatch()
    {
      var types = Assembly.GetExecutingAssembly().GetTypes();
      foreach (var type in types)
      {
        var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        foreach (var method in methods)
        {
          var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
          if (attributes.Length > 0)
            method.Invoke(null, null);
        }
      }
    }
  }
}
