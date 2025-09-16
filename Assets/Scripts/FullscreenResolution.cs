using UnityEngine;

public class FullscreenResolution : MonoBehaviour
{
    [Header("Бажана роздільна здатність")]
    public int width = 1080;
    public int height = 1920;

    [Header("Фулскрін режим")]
    public FullScreenMode mode = FullScreenMode.FullScreenWindow;
    // Альтернативи:
    // FullScreenMode.ExclusiveFullScreen – класичний ексклюзивний фулскрін
    // FullScreenMode.FullScreenWindow – безрамковий фулскрін (рекомендується)
    // FullScreenMode.Windowed – звичайне вікно

    void Start()
    {
#if !UNITY_EDITOR
        // тільки у білді
        Screen.SetResolution(width, height, mode);
#endif
    }
}