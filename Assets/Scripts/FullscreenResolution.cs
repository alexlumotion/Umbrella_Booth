using UnityEngine;

public class FullscreenResolution : MonoBehaviour
{
    [Header("Бажана роздільна здатність")]
    public int width = 1080;
    public int height = 1920;

    [Header("Фулскрін режим")]
    public FullScreenMode mode = FullScreenMode.FullScreenWindow;

    [Header("Який дисплей (0 = перший, 1 = другий...)")]
    public int targetDisplayIndex = 1; // другий екран

    void Start()
    {
#if !UNITY_EDITOR
        // якщо кілька дисплеїв – активуємо другий
        if (Display.displays.Length > targetDisplayIndex)
        {
            Display.displays[targetDisplayIndex].Activate();
        }

        // новий API Unity 2021+ — переносимо головне вікно на обраний дисплей
        if (Screen.mainWindowDisplayInfo.displayIndex != targetDisplayIndex)
        {
            var info = Display.displays[targetDisplayIndex].displayInfo;
            Screen.MoveMainWindowTo(info, Vector2Int.zero); // переміщаємо у 0,0 на другому дисплеї
        }

        // встановлюємо роздільну здатність + фулскрін
        Screen.SetResolution(width, height, mode);
#endif
    }
}