using UnityEngine;
using DG.Tweening;

public class TransitionClient : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TransitionManager manager;
    [SerializeField] private RectTransform targetScreen;

    [Header("Transition")]
    [SerializeField] private SlideDirection direction = SlideDirection.Left;

    [Header("Optional Overrides")]
    [Tooltip("Якщо вимкнено — будуть використані дефолтні налаштування менеджера.")]
    [SerializeField] private bool overrideDuration = false;
    [SerializeField] private float customDuration = 0.45f;

    [SerializeField] private bool overrideEase = false;
    [SerializeField] private Ease customEaseIn = Ease.OutCubic;
    [SerializeField] private Ease customEaseOut = Ease.OutCubic;

    void Reset()
    {
        // Автопошук менеджера у сцені
        if (manager == null) manager = FindObjectOfType<TransitionManager>();
    }

    void Awake()
    {
        if (manager == null) manager = FindObjectOfType<TransitionManager>();
    }

    /// <summary>
    /// Викликати цей метод із UI Button (OnClick) або з іншого скрипта.
    /// </summary>
    public void Trigger()
    {
        if (manager == null || targetScreen == null) return;

        if (overrideDuration && overrideEase)
        {
            manager.Show(targetScreen, direction, customEaseIn, customEaseOut, customDuration);
        }
        else if (overrideDuration)
        {
            manager.Show(targetScreen, direction, null, null, customDuration);
        }
        else if (overrideEase)
        {
            manager.Show(targetScreen, direction, customEaseIn, customEaseOut, null);
        }
        else
        {
            manager.Show(targetScreen, direction);
        }
    }

    // На випадок якщо хочеш викликати в коді напряму з параметрами:
    public void TriggerWith(RectTransform target, SlideDirection dir)
    {
        targetScreen = target;
        direction = dir;
        Trigger();
    }
}