using UnityEngine;
using DG.Tweening;
using UnityEngine.Video;


public class VideoManager : MonoBehaviour
{

    [Header("Target")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Fade Settings")]
    public float fadeDuration = 0.5f;
    public Ease fadeEase = Ease.OutCubic;
    public VideoPlayer videoPlayer;

    public TransitionClient transitionClient;

    // Start is called before the first frame update
    void Start()
    {
        videoPlayer.loopPointReached += OnVideoEnded;
        videoPlayer.frame = 0;
    }

    public bool updateFrame = false;

    // Update is called once per frame
    void Update()
    {
        if (updateFrame)
        {
            updateFrame = false;
            videoPlayer.frame = 0;
        }
    }

    public void Complete()
    {
        Debug.Log("Complete");
        //FadeIn();
        videoPlayer.Play();
    }

    void OnVideoEnded(VideoPlayer vp)
    {
        Debug.Log("OnVideoEnded");
        //vp.frame = 0;
        transitionClient.Trigger();
    }

    public void FadeIn()
    {
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        if (!canvasGroup) return;

        canvasGroup.alpha = 0f;
        canvasGroup.DOKill(); // скасовує попередні tween’и
        canvasGroup.DOFade(1f, fadeDuration).SetEase(fadeEase).OnComplete(() =>
        {
            videoPlayer.Play();
        });
    }

    /// <summary>
    /// Плавно змінює альфу CanvasGroup із 1 на 0
    /// </summary>
    public void FadeOut()
    {
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        if (!canvasGroup) return;

        canvasGroup.alpha = 1f;
        canvasGroup.DOKill();
        canvasGroup.DOFade(0f, fadeDuration).SetEase(fadeEase);
    }

}
