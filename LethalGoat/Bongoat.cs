using System;
using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace LethalGoat
{
  public class Bongoat : GrabbableObject
  {
    public AudioSource audioSource;
    public BoxCollider boxCollider;

    [Header("Base SFX")]
    public AudioClip[] pickupSFX;
    public AudioClip[] dropSFX;
    public AudioClip[] interactSFX;
    public AudioClip[] showerSFX;
    public AudioClip rareSFX;

    [Header("Yuchi SFX")]
    public AudioClip[] pickupYuchiSFX;
    public AudioClip[] dropYuchiSFX;
    public AudioClip[] interactYuchiSFX;
    public AudioClip[] showerYuchiSFX;
    public AudioClip rareYuchiSFX;

    [Space(3f)]
    public AudioClip yuchiBdayStartSFX;
    public AudioClip yuchiBdayEndSFX;
    public GameObject partyHat;

    [Header("Misc SFX")]
    public AudioClip[] hoarderBugSFX;
    public AudioClip sellSFX;
    public AudioClip[] quotaFailSFX;

    [Header("Special SFX")]
    public AudioClip sonackSFX;
    public AudioClip sinSFX;
    public AudioClip leonSFX;

    [Header("Sonack Toilet")]
    public AudioClip[] sonackToiletSFX;


    private System.Random random;
    private int lastRandomNumber = -1;

    private bool pickedUpFirstTime = false;

    private bool tookShower = false;
    private bool didBirthdayEvent = false;
    private bool blockingAudioSource = false;

    private int flushCount = 0;
    private VideoPlayer videoPlayer;

    private enum Clips
    {
      Pickup,
      PickupYuchi,

      Interact,
      InteractYuchi,

      Shower,
      ShowerYuchi,

      Rare,
      RareYuchi,

      HoarderBug,
      Fail
    }

    private readonly Dictionary<Clips, AudioClip[]> clipMap = new();

    public override void Start()
    {
      base.Start();
      random = new(DateTime.Now.Millisecond);

      clipMap.Add(Clips.Interact, interactSFX);
      clipMap.Add(Clips.InteractYuchi, interactYuchiSFX);
      clipMap.Add(Clips.Pickup, pickupSFX);
      clipMap.Add(Clips.PickupYuchi, pickupYuchiSFX);
      clipMap.Add(Clips.Shower, showerSFX);
      clipMap.Add(Clips.ShowerYuchi, showerYuchiSFX);
      clipMap.Add(Clips.Fail, quotaFailSFX);
      clipMap.Add(Clips.HoarderBug, hoarderBugSFX);

      // Listen to the the game over state
      On.StartOfRound.playersFiredGameOver += OnPlayersFired;
      On.CleanPlayerBodyTrigger.OnTriggerStay += ShowerTriggerStay;
      On.InteractTrigger.Interact += OnInteractTrigger;
    }

    public override void OnDestroy()
    {
      base.OnDestroy();
      On.StartOfRound.playersFiredGameOver -= OnPlayersFired;
      On.CleanPlayerBodyTrigger.OnTriggerStay -= ShowerTriggerStay;
      On.InteractTrigger.Interact -= OnInteractTrigger;
    }

    private IEnumerator OnPlayersFired(On.StartOfRound.orig_playersFiredGameOver orig, StartOfRound self, bool abridgedVersion)
    {
      PlayRandomAudio(Clips.Fail);
      yield return orig(self, abridgedVersion);
    }

    private void ShowerTriggerStay(On.CleanPlayerBodyTrigger.orig_OnTriggerStay orig, CleanPlayerBodyTrigger self, Collider other)
    {
      if (IsOwner && other.gameObject.name == "Player")
      {
        if (other.gameObject == playerHeldBy.gameObject)
        {
          var enableCleaning = Traverse.Create(self).Field<bool>("enableCleaning").Value;
          if (enableCleaning)
            PlayShowerSFX();
          else
            tookShower = false;
        }
      }
      orig(self, other);
    }

    private void OnInteractTrigger(On.InteractTrigger.orig_Interact orig, InteractTrigger self, Transform playerTransform)
    {
      try
      {
        if (IsOwner && playerTransform != null && self.transform.parent != null && self.transform.parent.gameObject.name.Contains("Toilet"))
        {
          var player = playerTransform.gameObject.GetComponent<PlayerControllerB>();
          if (player != null && IsOwner && player == playerHeldBy && IsHeldBySonack())
            Invoke(nameof(ToiletFlushedServerRpc), 1.5f);
        }
      }
      catch (Exception e)
      {
        Plugin.Log.LogError(e);
      }
      orig(self, playerTransform);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ToiletFlushedServerRpc()
    {
      if (flushCount < sonackToiletSFX.Length - 2)
      {
        ToiletFlushedClientRpc(flushCount);
        flushCount++;
      }
      else
      {
        ToiletSequenceClientRpc();
        flushCount = 0;
      }
    }

    [ClientRpc] private void ToiletFlushedClientRpc(int count) => PlayAudio(sonackToiletSFX[count]);
    [ClientRpc]
    private void ToiletSequenceClientRpc()
    {
      if (IsOwner)
        StartCoroutine(ToiletSequence());
    }

    private IEnumerator ToiletSequence()
    {
      PlayAudio(sonackToiletSFX[9]);
      while (audioSource.isPlaying) yield return null;
      yield return new WaitForSeconds(0.5f);

      playerHeldBy.inSpecialInteractAnimation = true;

      videoPlayer = gameObject.AddComponent<VideoPlayer>();
      videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
      videoPlayer.clip = Content.sonackToiletVideo;
      videoPlayer.aspectRatio = VideoAspectRatio.FitOutside;
      videoPlayer.targetCamera = playerHeldBy.gameplayCamera;

      HUDManager.Instance.HideHUD(true);
      var playerCursor = GameObject.Find("PlayerCursor");
      playerCursor.SetActive(false);

      if (!videoPlayer.isPrepared)
      {
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared) yield return new WaitForSeconds(0.2f);
      }

      yield return new WaitForSeconds(0.1f);
      videoPlayer.Play();

      while (videoPlayer.isPlaying) yield return null;

      Destroy(videoPlayer);

      HUDManager.Instance.HideHUD(false);
      playerCursor.SetActive(true);

      playerHeldBy.inSpecialInteractAnimation = false;
      KillSonack();
      yield return null;
    }

    [ServerRpc(RequireOwnership = false)] private void KillSonack() => playerHeldBy.DamagePlayer(665, true, true, CauseOfDeath.Suffocation, 0, false, Vector3.up * 5f);

    private bool IsHeldByYuchi() => Utilities.IsYuchi(playerHeldBy?.playerSteamId);
    private bool IsHeldByLeon() => Utilities.IsLeon(playerHeldBy?.playerSteamId);
    private bool IsHeldBySonack() => Utilities.IsSonack(playerHeldBy?.playerSteamId);

    public override void GrabItem()
    {
      base.GrabItem();
      if (!pickedUpFirstTime && IsOwner)
      {
        if (IsHeldByYuchi())
          PlayRandomAudio(Clips.PickupYuchi);
        else if (IsHeldBySonack())
          PlayAudio(sonackSFX);
        else if (IsHeldByLeon() || IsHost)
          PlayAudio(leonSFX);
        else
          PlayRandomAudio(Clips.Pickup);
        pickedUpFirstTime = true;
      }
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
      base.ItemActivate(used, buttonDown);

      // We only care to trigger execution on the owner of the item
      if (!IsOwner)
        return;

      var isRare = random.Next(100) == 99;
      if (IsHeldByYuchi())
      {
        if (!didBirthdayEvent)
          if (TryBirthdayEvent())
            return;
        if (isRare)
          PlayAudio(rareYuchiSFX);
        else
          PlayRandomAudio(Clips.InteractYuchi);
      }
      else
      {
        if (isRare)
          PlayAudio(rareSFX);
        else
          PlayRandomAudio(Clips.Interact);
      }
    }

    private bool TryBirthdayEvent(bool force = false)
    {
      var date = DateTime.Now;
      if ((date.Month == 12 && date.Day == 29) || force)
      {
        BirthdayEventServerRpc();
        return true;
      }
      return false;
    }

    [ServerRpc(RequireOwnership = false)] private void BirthdayEventServerRpc() => BirthdayEventClientRpc();
    [ClientRpc] private void BirthdayEventClientRpc() => StartCoroutine(PlayBirthdayEvent());

    private IEnumerator PlayBirthdayEvent()
    {
      // Block other audio from playing, easier than trying to block other interactions
      blockingAudioSource = true;
      didBirthdayEvent = true;

      PlayAudio(yuchiBdayStartSFX, true);
      while (audioSource.isPlaying) yield return null;
      yield return new WaitForSeconds(0.5f);
      partyHat.SetActive(true);
      PlayAudio(yuchiBdayEndSFX, true);
      while (audioSource.isPlaying) yield return null;

      // Unblock the audio interactions
      blockingAudioSource = false;
    }

    public override void GrabItemFromEnemy(EnemyAI enemy)
    {
      base.GrabItemFromEnemy(enemy);
      if (enemy is HoarderBugAI)
        PlayRandomAudio(Clips.HoarderBug);
    }

    public void PlayShowerSFX()
    {
      if (!tookShower)
      {
        if (IsHeldByYuchi())
          PlayRandomAudio(Clips.ShowerYuchi);
        else
          PlayRandomAudio(Clips.Shower);
        tookShower = true;
      }
    }

    private void PlayRandomAudio(Clips clip)
    {
      // prevent immediate duplicate clips
      var clips = clipMap[clip];
      int r;
      do r = random.Next(0, clips.Length);
      while (r == lastRandomNumber);
      lastRandomNumber = r;

      PlayRandomClipServerRpc(clip, r);
    }

    private void PlayAudio(AudioClip clip, bool force = false)
    {
      if (clip == null || (blockingAudioSource && !force))
        return;

      audioSource.Stop(true);
      audioSource.PlayOneShot(clip, 1);
      RoundManager.Instance.PlayAudibleNoise(base.transform.position, audioSource.maxDistance, 1, 0, isInElevator && StartOfRound.Instance.hangarDoorsClosed);
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayRandomClipServerRpc(Clips clip, int i) => PlayRandomClipClientRpc(clip, i);
    [ClientRpc] private void PlayRandomClipClientRpc(Clips clip, int i) => PlayAudio(clipMap[clip][i]);
  }
}
