using UnityEngine.InputSystem;


namespace LethalGoat
{
  public class Debug
  {
    public static void Load()
    {
      On.StartOfRound.Update += StartOfRound_Update;
    }

    public static void Unload()
    {
      On.StartOfRound.Update -= StartOfRound_Update;
    }

    private static void StartOfRound_Update(On.StartOfRound.orig_Update orig, StartOfRound self)
    {
      if (Keyboard.current[Key.F1].wasPressedThisFrame)
      {
        Utilities.LoadPrefab("Bongoat", self.localPlayerController.gameplayCamera.transform.position);
      }
      else if (Keyboard.current[Key.F2].wasPressedThisFrame)
      {
        UnityEngine.Object.FindObjectOfType<Terminal>().SyncGroupCreditsClientRpc(6969, 0);
      }
      else if (Keyboard.current[Key.F3].wasPressedThisFrame)
      {
        Plugin.TriggerBirthdayNext = !Plugin.TriggerBirthdayNext;
      }

      orig(self);
    }
  }
}
