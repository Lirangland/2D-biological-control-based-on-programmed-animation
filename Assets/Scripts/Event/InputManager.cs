using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    public UnityAction leftclickAction = null;
    bool clickUI = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    void Update()
    {
        //判断是否点击了UI
        clickUI = UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        if (clickUI)
        {
            return;
        }
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            leftclickAction?.Invoke();
        }
    }
}
