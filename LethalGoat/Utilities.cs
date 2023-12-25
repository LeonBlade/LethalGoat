using Unity.Netcode;
using UnityEngine;

namespace LethalGoat
{
  public class Utilities
  {
    public static void LoadPrefab(string name, Vector3 position)
    {
      if (Content.Prefabs.ContainsKey(name))
      {
        Plugin.Log.LogInfo($"Loading prefab {name}");
        var item = UnityEngine.Object.Instantiate(Content.Prefabs[name], position, Quaternion.identity);
        item.GetComponent<NetworkObject>().Spawn();
      }
      else
      {
        Plugin.Log.LogWarning($"Prefab {name} not found!");
      }
    }

    public static bool IsYuchi(ulong? steamId) => steamId == 76561198879298270;
    public static bool IsLeon(ulong? steamId) => steamId == 76561197998258984;
    public static bool IsSonack(ulong? steamId) => steamId == 76561198311162717;
  }
}