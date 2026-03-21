using UnityEngine;
using TMPro;

// 수감자 머리 위 말풍선. LateUpdate에서 카메라를 향해 회전.
public class SpeechBubble : MonoBehaviour
{
    [SerializeField] private TextMeshPro bubbleText;
    [SerializeField] private GameObject bubbleRoot;

    private Camera mainCam;

    void Awake()
    {
        mainCam = Camera.main;
        if (bubbleRoot != null) bubbleRoot.SetActive(false);
    }

    void LateUpdate()
    {
        if (bubbleRoot == null || !bubbleRoot.activeSelf) return;
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam != null)
            transform.forward = mainCam.transform.forward;
    }

    public void Show(string message)
    {
        if (bubbleRoot != null) bubbleRoot.SetActive(true);
        if (bubbleText != null) bubbleText.text = message;
    }

    public void Hide()
    {
        if (bubbleRoot != null) bubbleRoot.SetActive(false);
    }
}
