using UnityEngine;
using UnityEngine.EventSystems;

// 터치한 위치에 조이스틱이 나타나는 다이나믹 조이스틱.
// Canvas 하위 전체화면 투명 패널에 부착.
// joystickBackground: 원형 배경 RectTransform (자식)
// handle: 핸들 RectTransform (joystickBackground의 자식)
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform joystickBackground;
    [SerializeField] private RectTransform handle;
    [SerializeField] private float maxRadius = 80f;

    public Vector2 InputDirection { get; private set; }

    private Canvas canvas;
    private Camera uiCamera;

    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        uiCamera = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? canvas.worldCamera : null;

        if (joystickBackground != null)
            joystickBackground.gameObject.SetActive(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (joystickBackground == null) return;

        // 터치 위치에 조이스틱 배경 배치
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)canvas.transform,
            eventData.position,
            uiCamera,
            out localPoint);

        joystickBackground.anchoredPosition = localPoint;
        joystickBackground.gameObject.SetActive(true);

        if (handle != null) handle.anchoredPosition = Vector2.zero;
        InputDirection = Vector2.zero;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (joystickBackground == null || handle == null) return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBackground,
            eventData.position,
            uiCamera,
            out localPoint);

        Vector2 clamped = Vector2.ClampMagnitude(localPoint, maxRadius);
        handle.anchoredPosition = clamped;
        InputDirection = clamped / maxRadius;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (handle != null) handle.anchoredPosition = Vector2.zero;
        InputDirection = Vector2.zero;
        if (joystickBackground != null)
            joystickBackground.gameObject.SetActive(false);
    }
}
