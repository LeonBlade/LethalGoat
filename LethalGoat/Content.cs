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
      var bongoatInfo = Assets.LoadAsset<TerminalNode>("Assets/BongoatTerminalNode.asset");
      Prefabs.Add("Bongoat", bongoat.spawnPrefab);
      NetworkPrefabs.RegisterNetworkPrefab(bongoat.spawnPrefab);
      Items.RegisterShopItem(bongoat, null, null, bongoatInfo, 69);

      sonackToiletVideo = Assets.LoadAsset<VideoClip>("Assets/toilet.mp4");

      // var yuchi = Assets.LoadAsset<UnlockableItemDef>("Assets/YuchiCutoutUnlockable.asset");
      // var yuchiInfo = Assets.LoadAsset<TerminalNode>("Assets/YuchiCutoutInfo.asset");
      // yuchi.unlockable.alwaysInStock = true;
      // NetworkPrefabs.RegisterNetworkPrefab(yuchi.unlockable.prefabObject);
      // Unlockables.RegisterUnlockable(yuchi, yuchi.storeType, null, null, yuchiInfo, 69);

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
