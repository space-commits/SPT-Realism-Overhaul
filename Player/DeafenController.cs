﻿using Comfort.Common;
using UnityEngine;

namespace RealismMod
{


    public static class HeadsetGainController
    {
        public static void IncreaseGain() 
        {
            if (Input.GetKeyDown(PluginConfig.IncGain.Value.MainKey) && DeafenController.HasHeadSet)
            {
                if (PluginConfig.HeadsetGain.Value < DeafenController.MaxGain)
                {
                    PluginConfig.HeadsetGain.Value += 1;
                    Singleton<BetterAudio>.Instance.PlayAtPoint(new Vector3(0, 0, 0), Plugin.DeviceAudioClips["beep.wav"], 0, BetterAudio.AudioSourceGroupType.Nonspatial, 100, 1.0f, EOcclusionTest.None, null, false);
                }
            }
        }

        public static void DecreaseGain() 
        {
            if (Input.GetKeyDown(PluginConfig.DecGain.Value.MainKey) && DeafenController.HasHeadSet)
            {
                if (PluginConfig.HeadsetGain.Value > DeafenController.MinGain)
                {
                    PluginConfig.HeadsetGain.Value -= 1;
                    Singleton<BetterAudio>.Instance.PlayAtPoint(new Vector3(0, 0, 0), Plugin.DeviceAudioClips["beep.wav"], 0, BetterAudio.AudioSourceGroupType.Nonspatial, 100, 1.0f, EOcclusionTest.None, null, false);
                }
            }
        }

        public static void AdjustHeadsetVolume()
        {
            IncreaseGain();
            DecreaseGain();
        }
    }

    public static class DeafenController
    {
        public const float AmbientOutVolume = 0f; //-0.91515f
        public const float AmbientInVolume = 0f; //-80f
        public const float WeaponMuffleBase = -80f;
        public const float BaseMainVolume = 0f;
        public const float LightHelmetDeafReduction = 0.95f;
        public const float HeavyHelmetDeafreduction = 0.8f;
        public const float MinHeadsetProtection = 0.7f;
        public const float MaxHeadsetProtection = 0.1f;
        public const int MaxGain = 15;
        public const int MinGain = -10;
        public const float MaxMuffle = 15f;
        public const float MaxDeafen = -20;
        public const float MaxVignette = 2.4f;

        public static PrismEffects PrismEffects { get; set; }
        public static float EarProtectionFactor { get; set; } = 1f;
        public static float AmmoDeafFactor { get; set; } = 1f;
        public static float GunDeafFactor { get; set; } = 1f;
        public static float BotFiringDeafFactor { get; set; } = 1f;
        public static float ExplosionDeafFactor { get; set; } = 1f;
        public static bool HasHeadSet { get; set; } = false;
        public static float HeadSetGain { get; set; } = PluginConfig.HeadsetGain.Value;
        public static bool IsBotFiring { get; set; } = false;
        public static bool GrenadeExploded { get; set; } = false;
        public static float BotTimer { get; set; } = 0f;
        public static float GrenadeTimer { get; set; } = 0f;

        private static float _mainVolumeReduction = 0f;
        private static float _mainVolumeReductionTarget = 0f;
        private static float _maxShootVolumeReduction = 0f;
        private static float _maxBotVolumeReduction = 0f;
        private static float _muffleTarget = WeaponMuffleBase;
        private static float _gunShotMuffle = WeaponMuffleBase;
        private static float _vignetteAmount = 0f;
        private static float _vignetteTarget = 0f;
        private static float _maxShootVignette = 0f; //relative to the specific gun
        private static float _maxBotShootVignette = 0f;
        private static float _maxExplosionVignette = 0f;

        public static float EnvironmentFactor
        {
            get
            {
                return PlayerValues.EnviroType == EnvironmentType.Indoor ? 2f : 1f;
            }
        }

        public static void IncreaseDeafeningShooting()
        {
            float factor = AmmoDeafFactor * GunDeafFactor * EarProtectionFactor * EnvironmentFactor;
            _mainVolumeReductionTarget += factor;
            _maxShootVolumeReduction += factor * PluginConfig.test1.Value;
            _muffleTarget += factor * 2f;
            _vignetteTarget += factor * 0.18f;
            _maxShootVignette = factor * 0.3f;

        }

        public static void IncreaseDeafeningShotAt()
        {
            float factor = BotFiringDeafFactor * EnvironmentFactor;
            _mainVolumeReductionTarget += factor;
            _muffleTarget += factor * 2f;
            _vignetteTarget += factor * 0.18f;
            _maxBotShootVignette = Mathf.Max(factor * 0.3f, _maxBotShootVignette);
        }

        public static void IncreaseDeafeningExplosion()
        {
            float factor = ExplosionDeafFactor * EnvironmentFactor;
            _mainVolumeReductionTarget += factor * 0.2f; //0.2
            _muffleTarget += factor * 0.9f; //0.9
            _vignetteTarget += factor * 0.025f; // 0.025
            _maxExplosionVignette = _vignetteTarget; //0.035
        }

        public static void SetAudio(float mainVolume)
        {
            Singleton<BetterAudio>.Instance.Master.SetFloat("InGame", mainVolume + _mainVolumeReduction); //main volume
            //Singleton<BetterAudio>.Instance.Master.SetFloat("Tinnitus1", _gunShotMuffle); //higher = more muffled (reverby with headset) gunshots and all weapon sounds

            //ambient
            float ambientBase = HasHeadSet ? 10f : 5f;
            Singleton<BetterAudio>.Instance.Master.SetFloat("AmbientOutVolume", AmbientOutVolume + ambientBase + PluginConfig.AmbientMulti.Value); //outdoors
            Singleton<BetterAudio>.Instance.Master.SetFloat("AmbientInVolume", AmbientInVolume + ambientBase + PluginConfig.AmbientMulti.Value);
        }

        public static void DoVignette()
        {
            PrismEffects.SetVignetteStrength(_vignetteAmount);
            if (PrismEffects.useVignette && _vignetteAmount.ApproxEquals(0f))
            {
                PrismEffects.useVignette = false;
            }
        }

        public static void DoTimers()
        {
            if (IsBotFiring)
            {
                BotTimer += Time.deltaTime;
                if (BotTimer >= 0.5f)
                {
                    IsBotFiring = false;
                    BotTimer = 0f;
                }
            }

            if (GrenadeExploded)
            {
                GrenadeTimer += Time.deltaTime;
                if (GrenadeTimer >= 25f)
                {
                    GrenadeExploded = false;
                    GrenadeTimer = 0f;
                }
            }

        }

        public static void DoDeafening()
        {
            float baseMainVolume = 0f;
            if (IsBotFiring || GrenadeExploded || ShootController.IsFiringDeafen)
            {
                if (HasHeadSet)
                {
                    baseMainVolume = Mathf.Min(PluginConfig.HeadsetNoiseReduction.Value, PluginConfig.HeadsetGain.Value - 5f);
                }

                float maxVolumeReduction = Mathf.Min(MaxDeafen, _maxShootVolumeReduction + _maxBotVolumeReduction);
                _mainVolumeReduction = Mathf.Lerp(_mainVolumeReduction, Mathf.Max(-_mainVolumeReductionTarget, maxVolumeReduction), 0.01f); //0.01f
                _gunShotMuffle = Mathf.Lerp(_gunShotMuffle, Mathf.Min(_muffleTarget, MaxMuffle), 0.035f);

                float maxVignette = Mathf.Min(MaxVignette, _maxShootVignette + _maxBotShootVignette + _maxExplosionVignette);
                _vignetteAmount = Mathf.Lerp(_vignetteAmount, Mathf.Min(_vignetteTarget, maxVignette), 0.1f);

                PrismEffects.useVignette = true;
                PrismEffects.vignetteStart = 1.5f;
                PrismEffects.vignetteEnd = 0.1f;
            }
            else
            {
                if (HasHeadSet)
                {
                    baseMainVolume = PluginConfig.HeadsetGain.Value;
                }
                _mainVolumeReductionTarget = 0f;
                _muffleTarget = WeaponMuffleBase;
                _vignetteTarget = 0f;
                _maxShootVignette = 0f;
                _maxBotShootVignette = 0f;
                _maxExplosionVignette = 0f;

                _mainVolumeReduction = Mathf.Lerp(_mainVolumeReduction, BaseMainVolume, 0.001f); //0.001f
                _gunShotMuffle = Mathf.Lerp(_gunShotMuffle, WeaponMuffleBase, 0.0015f);
                _vignetteAmount = Mathf.Lerp(_vignetteAmount, 0f, 0.01f);
            }

            SetAudio(baseMainVolume);
            DoVignette();
            DoTimers();

            //do not use
            /*      if (PluginConfig    3.Value != 1f) Singleton<BetterAudio>.Instance.Master.SetFloat("ReverbVolume", PluginConfig.test3.Value); //unused
                  if (PluginConfig.test6.Value != 1f) Singleton<BetterAudio>.Instance.Master.SetFloat("WorldVolume", PluginConfig.test6.Value); //overall volume while wearing headset, doesn't seem to do anything without a headset..tried again and it did seem to affect all volume with or without headset...
                  if (PluginConfig.test7.Value != 1f) Singleton<BetterAudio>.Instance.Master.SetFloat("AmbientOccluded", PluginConfig.test7.Value); //also affects ambient in some way
                  if (PluginConfig.test1.Value != 1f) Singleton<BetterAudio>.Instance.Master.SetFloat("MainVolume", PluginConfig.test1.Value); //outdoor ambient, main volume, not gunshots or green flare. Doesn't affect headset
                  if (PluginConfig.test1.Value != 1f) Singleton<BetterAudio>.Instance.Master.SetFloat("Tinnitus2", PluginConfig.test1.Value); //not sure, does affect ambient volume
                  if (PluginConfig.test2.Value != 1f) Singleton<BetterAudio>.Instance.Master.SetFloat("WorldReverbLevel", PluginConfig.test2.Value); //no noticable effect
                  if (PluginConfig.test4.Value != 1f) Singleton<BetterAudio>.Instance.Master.SetFloat("GunCompressorAttack", PluginConfig.test4.Value); //no noticable ffect
                  if (PluginConfig.test5.Value != 1f) Singleton<BetterAudio>.Instance.Master.SetFloat("GunCompressorRelease", PluginConfig.test5.Value);//no noticable ffect
                  if (PluginConfig.test6.Value != 1f) Singleton<BetterAudio>.Instance.Master.SetFloat("GunCompressorThreshold", PluginConfig.test6.Value);//no noticable ffect
                  if (PluginConfig.test7.Value != 1f) Singleton<BetterAudio>.Instance.Master.SetFloat("GunCompressorDistortion", PluginConfig.test7.Value); //all gun noise distortion with headset, don't mess with it
                  if (PluginConfig.test9.Value != 1f) Singleton<BetterAudio>.Instance.Master.SetFloat("GunsLowPassFilter", PluginConfig.test9.Value); //nothing
                  if (PluginConfig.test1.Value != 1f) Singleton<BetterAudio>.Instance.Master.SetFloat("EffectsReturnsGroupVolume", PluginConfig.test1.Value); //not sure, does affect ambient volume
                  if (PluginConfig.test2.Value != 1f) Singleton<BetterAudio>.Instance.Master.SetFloat("HeadphonesMixerVolume", PluginConfig.test2.Value); //no noticable effect
               Singleton<BetterAudio>.Instance.Master.GetFloat("GunsCompressorSendLevel", out float sendlevel); //affected all gun volume with headset
                    //might have to use too, not sure
            if (PluginConfig.test2.Value != 1f) Singleton<BetterAudio>.Instance.Master.SetFloat("GunsVolume", PluginConfig.test2.Value); //just gun volume...also affects all gun itneraction volume because BSG is fucking retarded. Doesn't affect it properly when wearing headest
            if (PluginConfig.test3.Value != 1f) Singleton<BetterAudio>.Instance.Master.SetFloat("GunCompressorGain", PluginConfig.test3.Value);  //all gun volume using headset
             */
            //maybe use
            //if (PluginConfig.test8.Value != 1f) Singleton<BetterAudio>.Instance.Master.SetFloat("OutEnvironmentVolume", PluginConfig.test8.Value); //not sure
        }
    }
}
