using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CarouselButtonClient : MonoBehaviour
{
    public WorldSpaceCarouselManager manager;
    public bool next = true;

    private Button _button;

    void Awake()
    {
        _button = GetComponent<Button>();
        if (manager == null) manager = FindObjectOfType<WorldSpaceCarouselManager>();
        _button.onClick.AddListener(OnClick);
    }

    void OnDestroy()
    {
        if (_button != null) _button.onClick.RemoveListener(OnClick);
    }

    public void OnClick()
    {
        if (manager == null) return;
        if (next) manager.ShowNext(); else manager.ShowPrev();
    }
}