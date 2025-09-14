using UnityEngine;
using UnityEngine.UI;
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
    [Tooltip("Якщо вимкнено — будуть використані дефолтні налаштування менеджера.")]
    [SerializeField] private bool overrideDuration = false;
    [SerializeField] private float customDuration = 0.45f;

    [SerializeField] private bool overrideEase = false;
    [SerializeField] private Ease customEaseIn = Ease.OutCubic;
    [SerializeField] private Ease customEaseOut = Ease.OutCubic;

    private Button _button;

    void Awake()
    {
        if (manager == null) manager = FindObjectOfType<TransitionManager>();
        _button = GetComponent<Button>();
        if (_button != null)
        {
            // передплачуємось на OnClick у момент Awake
            _button.onClick.AddListener(Trigger);
        }
    }

    void OnDestroy()
    {
        // не забуваємо відписатися
        if (_button != null)
        {
            _button.onClick.RemoveListener(Trigger);
        }
    }

    /// <summary>
    /// Викликається при натисканні кнопки (підписка в Awake)
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

    // Якщо треба викликати з іншого коду
    public void TriggerWith(RectTransform target, SlideDirection dir)
    {
        targetScreen = target;
        direction = dir;
        Trigger();
    }
}