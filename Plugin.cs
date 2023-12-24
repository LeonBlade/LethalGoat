
using BepInEx;
using BepInEx.Logging;

namespace LethalGoat
{
  [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
  public class Plugin : BaseUnityPlugin
  {
    public static ManualLogSource Log;

    public static bool TriggerBirthdayNext = false;

    private void Awake()
    {
      // Create a new plugin source
      Log = Logger;

      Content.Load();
      // Debug.Load();

      // Plugin startup logic
      Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
    }
  }
}
