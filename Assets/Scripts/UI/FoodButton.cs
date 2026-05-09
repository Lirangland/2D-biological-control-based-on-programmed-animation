using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class FoodButton : MonoBehaviour
{
    Button button;
    TextMeshProUGUI buttonText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(RegisterGenerateFood);
        buttonText = GetComponentInChildren<TextMeshProUGUI>();
        buttonText.text = "Generate Food";
    }

    // Update is called once per frame
    void Update()
    {
        
    }

     public void RegisterGenerateFood()
    {
        InputManager.Instance.leftclickAction += GenerateFood;
        button.onClick.RemoveListener(RegisterGenerateFood);
        button.onClick.AddListener(UnregisterGenerateFood);
        buttonText.text = "Stop Generating";
    }

    public void UnregisterGenerateFood()
    {
        InputManager.Instance.leftclickAction -= GenerateFood;
        button.onClick.RemoveListener(UnregisterGenerateFood);
        button.onClick.AddListener(RegisterGenerateFood);
        buttonText.text = "Generate Food";
    }

    void GenerateFood()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mousePos.z = 0;
        Food food = Instantiate(Resources.Load<Food>("Prefabs/Food"), mousePos, Quaternion.identity);
    }
}
