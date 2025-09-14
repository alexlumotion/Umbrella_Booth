using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using DG.Tweening;

[RequireComponent(typeof(VideoPlayer))]
public class VideoPreRollPlayer : MonoBehaviour
{
    [Header("References")]
    public RawImage rawImage;                // UI RawImage для показу відео
    public CanvasGroup rawImageCanvasGroup;  // щоб плавно показувати/ховати
    public VideoClip clip;

    [Header("Settings")]
    public float fadeDuration = 0.3f;

    private VideoPlayer vp;

    public bool play, stop = false;

    void Awake()
    {
        vp = GetComponent<VideoPlayer>();
        vp.source = VideoSource.VideoClip;
        vp.clip = clip;
        vp.playOnAwake = false;
        vp.waitForFirstFrame = true;   // чекаємо перший кадр
        vp.audioOutputMode = VideoAudioOutputMode.None; // якщо аудіо не треба

        if (!rawImageCanvasGroup) rawImageCanvasGroup = rawImage.GetComponent<CanvasGroup>();
        if (!rawImageCanvasGroup) rawImageCanvasGroup = rawImage.gameObject.AddComponent<CanvasGroup>();

        // Спочатку прихований
        rawImageCanvasGroup.alpha = 0f;
    }

    void Update()
    {
        if (play) { play = false; PlaySmoothly(); }
        if (stop) { stop = false; StopSmoothly(); }
    }

    public void PlaySmoothly()
    {
        rawImageCanvasGroup.DOKill();
        rawImageCanvasGroup.alpha = 0f;

        if (vp.isPrepared)
        {
            // плеєр уже готовий – одразу викликаємо OnPrepared
            OnPrepared(vp);
        }
        else
        {
            vp.Stop(); // скинути стан, якщо треба підготувати заново
            vp.Prepare();
            vp.prepareCompleted += OnPrepared;
        }
    }

    private void OnPrepared(VideoPlayer source)
    {
        vp.prepareCompleted -= OnPrepared;

        // 3) перемотуємо на початок і малюємо перший кадр
        vp.frame = 0;
        vp.Play();
        vp.Pause();

        // 4) плавно показуємо RawImage
        rawImageCanvasGroup.DOFade(1f, fadeDuration).OnComplete(() =>
        {
            // 5) після fade запускаємо реальне відтворення
            vp.Play();
        });
    }

    public void StopSmoothly()
    {
        // 6) плавно ховаємо й зупиняємо
        rawImageCanvasGroup.DOFade(0f, fadeDuration).OnComplete(() =>
        {
            vp.Stop();
        });
    }
}