using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch;

public class JoystickController : MonoBehaviour
{
    [SerializeField] private GameObject joystickPrefab;
    public Vector2 JoystickSize = new Vector2(300, 300);
    public bool isActive;
    public Finger movementFinger;
    public Vector2 movementAmount;
    [HideInInspector] public Joystick floatingJoystick;

    private RectTransform activationPanel;

    [System.Serializable]
    public class PanelMargins
    {
        [Range(0f, 1f)] public float left = 0f;
        [Range(0f, 1f)] public float top = 0f;
        [Range(0f, 1f)] public float right = 0f;
        [Range(0f, 1f)] public float bottom = 0f;
    }

    [Header("Panel Margins (percent of screen)")]
    [SerializeField] private PanelMargins panelMargins = new PanelMargins();

    public virtual void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        ETouch.Touch.onFingerDown += HandleFingerDown;
        ETouch.Touch.onFingerUp += HandleLoseFinger;
        ETouch.Touch.onFingerMove += HandleFingerMove;
    }

    public virtual void OnDisable()
    {
        movementFinger = null;
        if (floatingJoystick != null)
        {
            floatingJoystick.Knob.anchoredPosition = Vector2.zero;
            floatingJoystick.gameObject.SetActive(false);
        }
        movementAmount = Vector2.zero;
        isActive = false;
        ETouch.Touch.onFingerDown -= HandleFingerDown;
        ETouch.Touch.onFingerUp -= HandleLoseFinger;
        ETouch.Touch.onFingerMove -= HandleFingerMove;
        EnhancedTouchSupport.Disable();
    }

    public virtual void Start()
    {
        activationPanel = GetComponent<RectTransform>();
        StretchPanelToScreenWithPercentageMargins();

        GameObject spawnedPrefab = Instantiate(joystickPrefab);
        RectTransform rectTransform = spawnedPrefab.GetComponent<RectTransform>();
        rectTransform.SetParent(transform, false);
        floatingJoystick = spawnedPrefab.GetComponent<Joystick>();
        floatingJoystick.gameObject.SetActive(false);
    }

    private void StretchPanelToScreenWithPercentageMargins()
    {
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        activationPanel.anchorMin = new Vector2(0, 0);
        activationPanel.anchorMax = new Vector2(1, 1);
        activationPanel.pivot = new Vector2(0.5f, 0.5f);

        float leftMargin = panelMargins.left * screenWidth;
        float rightMargin = panelMargins.right * screenWidth;
        float topMargin = panelMargins.top * screenHeight;
        float bottomMargin = panelMargins.bottom * screenHeight;

        activationPanel.offsetMin = new Vector2(leftMargin, bottomMargin); // left, bottom
        activationPanel.offsetMax = new Vector2(-rightMargin, -topMargin); // -right, -top
    }

    public virtual void HandleFingerMove(Finger MovedFinger)
    {
        if (MovedFinger == movementFinger)
        {
            Vector2 knobPosition;
            float maxMovement = JoystickSize.x / 2f;
            ETouch.Touch currentTouch = MovedFinger.currentTouch;

            Vector2 localPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                activationPanel,
                currentTouch.screenPosition,
                null,
                out localPosition
            );

            Vector2 joystickCenter = floatingJoystick.RectTransform.anchoredPosition;

            Vector2 delta = localPosition - joystickCenter;

            if (delta.magnitude > maxMovement)
            {
                knobPosition = delta.normalized * maxMovement;
            }
            else
            {
                knobPosition = delta;
            }

            floatingJoystick.Knob.anchoredPosition = knobPosition;
            movementAmount = knobPosition / maxMovement;
        }
    }

    public virtual void HandleLoseFinger(Finger LostFinger)
    {
        if (LostFinger == movementFinger)
        {
            movementFinger = null;
            floatingJoystick.Knob.anchoredPosition = Vector2.zero;
            floatingJoystick.gameObject.SetActive(false);
            movementAmount = Vector2.zero;
            isActive = false;
        }
    }

    public virtual void HandleFingerDown(Finger TouchedFinger)
    {
        if (movementFinger == null && IsTouchWithinPanel(TouchedFinger.screenPosition))
        {
            movementFinger = TouchedFinger;
            movementAmount = Vector2.zero;
            isActive = true;

            floatingJoystick.gameObject.SetActive(true);
            floatingJoystick.RectTransform.sizeDelta = JoystickSize;

            Vector2 localPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                activationPanel,
                TouchedFinger.screenPosition,
                null,
                out localPosition
            );

            localPosition = ClampLocalPositionWithinPanel(localPosition);

            floatingJoystick.RectTransform.anchoredPosition = localPosition;
        }
    }

    private Vector2 ClampLocalPositionWithinPanel(Vector2 localPosition)
    {
        Vector2 clampedPosition = localPosition;

        float halfWidth = JoystickSize.x / 2f;
        float halfHeight = JoystickSize.y / 2f;

        Rect rect = activationPanel.rect;

        clampedPosition.x = Mathf.Clamp(clampedPosition.x, rect.xMin + halfWidth, rect.xMax - halfWidth);
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, rect.yMin + halfHeight, rect.yMax - halfHeight);

        return clampedPosition;
    }

    private bool IsTouchWithinPanel(Vector2 screenPosition)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(activationPanel, screenPosition, null, out localPoint);
        return activationPanel.rect.Contains(localPoint);
    }
}
