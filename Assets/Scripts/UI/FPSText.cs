using TMPro;
using UnityEngine;

//显示FPS的文本组件，挂载在UI节点上
public class FPSText : MonoBehaviour
{
    TextMeshProUGUI textMesh;
    float deltaTime = 0.0f;

    void Start()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
        int fps = (int)(1.0f / deltaTime);
        textMesh.text = $"FPS: {fps}";
    }
}
