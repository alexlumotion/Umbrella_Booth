using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;

[RequireComponent(typeof(Button))]
public class TransitionClient : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TransitionManager manager;
    [SerializeField] private RectTransform targetScreen;

    [Header("Transition")]
    [SerializeField] private SlideDirection direction = SlideDirection.Left;

    [Header("Optional Overrides")]
    [SerializeField] private bool overrideDuration = false;
    [SerializeField] private float customDuration = 0.45f;

    [SerializeField] private bool overrideEase = false;
    [SerializeField] private Ease customEaseIn = Ease.OutCubic;
    [SerializeField] private Ease customEaseOut = Ease.OutCubic;

    [Header("Callbacks")]
    [Tooltip("Викликається, коли анімація переходу завершена (саме для цього клієнта)")]
    public UnityEvent onTransitionComplete;

    private Button _button;

    void Awake()
    {
        if (!manager) manager = FindObjectOfType<TransitionManager>();
        _button = GetComponent<Button>();
        if (_button) _button.onClick.AddListener(Trigger);
    }

    void OnDestroy()
    {
        if (_button) _button.onClick.RemoveListener(Trigger);
    }

    public void Trigger()
    {
        if (!manager || !targetScreen) return;

        UnityAction done = () =>
        {
            // локальний інспекторний колбек клієнта
            onTransitionComplete?.Invoke();
        };

        if (overrideDuration && overrideEase)
        {
            manager.Show(targetScreen, direction, customEaseIn, customEaseOut, customDuration, done);
        }
        else if (overrideDuration)
        {
            manager.Show(targetScreen, direction, null, null, customDuration, done);
        }
        else if (overrideEase)
        {
            manager.Show(targetScreen, direction, customEaseIn, customEaseOut, null, done);
        }
        else
        {
            manager.Show(targetScreen, direction, null, null, null, done);
        }
    }

    // Альтернативний виклик із коду
    public void TriggerWith(RectTransform target, SlideDirection dir, UnityAction onDone = null)
    {
        targetScreen = target;
        direction = dir;
        if (onDone != null) onTransitionComplete.AddListener(onDone);
        Trigger();
        if (onDone != null) onTransitionComplete.RemoveListener(onDone);
    }
}