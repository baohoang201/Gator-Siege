using System;
using System.Collections.Generic;
using UI.Event;
using UnityEngine;

namespace Gameplay.Manager {
    public enum SoundType {
        Click, Music, Bite, Explosion, Punch, Shoot, TowerShoot, WaterEffect, Stream, Victory
    }

    [Serializable]
    public class SoundData {
        public SoundType Type;
        public AudioClip Clip;
    }

    public class SoundManager : MonoBehaviour {
        public static SoundManager Instance { get; private set; }

        [SerializeField] AudioSource musicSource;
        [SerializeField] AudioSource soundFxSource;
        [SerializeField] List<SoundData> soundDatas;

        private void Awake() {
            if (Instance == null) {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else {
                Destroy(gameObject);
            }
        }

        private void Start() {
            PlayMusic(SoundType.Music);
        }

        public void PlayMusic(SoundType soundType) {
            AudioClip clip = GetSoundClip(soundType);
            if (clip != null) {
                musicSource.clip = clip;
                musicSource.Play();
            }
        }

        public void PlaySoundFX(SoundType soundType) {
            AudioClip clip = GetSoundClip(soundType);
            if (clip != null) {
                soundFxSource.PlayOneShot(clip);
            }
        }



        private AudioClip GetSoundClip(SoundType soundType) {
            foreach (var sound in soundDatas) {
                if (sound.Type == soundType) {
                    return sound.Clip;
                }
            }
            return null;
        }

        private void SetMusicValue(float value) {
            musicSource.volume = value;
        }

        private void SetSoundFXValue(float value) {
            soundFxSource.volume = value;
        }

        private void OnEnable() {
            UIEvent.OnChangeMusicEvent += SetMusicValue;
            UIEvent.OnChangeSoundFXEvent += SetSoundFXValue;
        }

        private void OnDisable() {
            UIEvent.OnChangeMusicEvent -= SetMusicValue;
            UIEvent.OnChangeSoundFXEvent -= SetSoundFXValue;
        }
    }
}