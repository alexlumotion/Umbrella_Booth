using UnityEngine;

public class EmulateMouse : MonoBehaviour
{
    void Awake()
    {
        Input.simulateMouseWithTouches = true;
    }
}
