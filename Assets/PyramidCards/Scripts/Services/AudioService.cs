using UnityEngine;

namespace PyramidCards
{
    /// <summary>Maps game events to sounds. Purely a listener — it never drives the game, so removing it
    /// (or leaving its library empty) changes nothing but the audio. Extend by adding <see cref="GameSfx"/>
    /// values and reacting to more events here.</summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioService : MonoBehaviour
    {
        AudioLibrary library;
        GameManager gm;
        AudioSource sfx;
        AudioSource music;

        public void Bind(GameManager manager, AudioLibrary lib)
        {
            Unbind();

            gm = manager;
            library = lib;

            sfx = GetComponent<AudioSource>();
            sfx.playOnAwake = false;

            if (gm != null)
            {
                gm.CardFlipped += OnFlip;
                gm.CardsSwapped += OnSwap;
                gm.MoveBlocked += OnBlocked;
                gm.CascadeResolved += OnCascade;
                gm.ModalRequested += OnModal;
                gm.ShopChanged += OnPurchase;
            }

            StartMusic();
        }

        void OnDestroy() { Unbind(); }

        void Unbind()
        {
            if (gm == null) return;
            gm.CardFlipped -= OnFlip;
            gm.CardsSwapped -= OnSwap;
            gm.MoveBlocked -= OnBlocked;
            gm.CascadeResolved -= OnCascade;
            gm.ModalRequested -= OnModal;
            gm.ShopChanged -= OnPurchase;
            gm = null;
        }

        void StartMusic()
        {
            if (library == null || library.music == null) return;
            music = gameObject.AddComponent<AudioSource>();
            music.clip = library.music;
            music.loop = true;
            music.volume = library.musicVolume * library.masterVolume;
            music.playOnAwake = false;
            music.Play();
        }

        void OnFlip() { Play(GameSfx.Flip); }
        void OnSwap() { Play(GameSfx.Swap); }
        void OnBlocked() { Play(GameSfx.InvalidMove); }
        void OnPurchase() { Play(GameSfx.Purchase); }

        void OnCascade(CascadeStep step)
        {
            Play(step.hasTriad ? GameSfx.Triad : GameSfx.ClearStep);
        }

        void OnModal(ModalRequest req)
        {
            Play(req.win ? GameSfx.LevelWon : GameSfx.LevelLost);
        }

        void Play(GameSfx which)
        {
            if (library == null || sfx == null) return;
            AudioClip clip = library.Resolve(which, out float volume);
            if (clip != null) sfx.PlayOneShot(clip, volume);
        }
    }
}
