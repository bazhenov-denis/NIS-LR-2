using UnityEngine;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private float minSwipeDistance = 50f;
    private Vector2 startTouchPosition;
    private bool isSwiping = false;

    public delegate void InputEvent(Vector2 direction);
    public event InputEvent OnInputReceived;

    private void Update()
    {
        // Обработка ввода с клавиатуры (WASD/стрелки)
        Vector2 keyboardDirection = Vector2.zero;
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            keyboardDirection = Vector2.up;
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            keyboardDirection = Vector2.down;
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            keyboardDirection = Vector2.left;
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            keyboardDirection = Vector2.right;

        if (keyboardDirection != Vector2.zero)
        {
            Debug.Log("Keyboard Input: " + keyboardDirection);
            OnInputReceived?.Invoke(keyboardDirection);
        }

        // Обработка тач ввода (мобильные устройства)
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                startTouchPosition = touch.position;
                isSwiping = true;
            }
            else if (touch.phase == TouchPhase.Ended && isSwiping)
            {
                ProcessSwipe(startTouchPosition, touch.position, "Touch Swipe");
                isSwiping = false;
            }
        }

        // Обработка свайпа мышью (на ПК)
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            startTouchPosition = Input.mousePosition;
            isSwiping = true;
        }
        if (Input.GetMouseButtonUp(0) && isSwiping)
        {
            ProcessSwipe(startTouchPosition, Input.mousePosition, "Mouse Swipe");
            isSwiping = false;
        }
    }

    private void ProcessSwipe(Vector2 startPos, Vector2 endPos, string source)
    {
        Vector2 swipeDelta = endPos - startPos;
        if (swipeDelta.magnitude < minSwipeDistance)
            return;

        Vector2 direction = GetSwipeDirection(swipeDelta);
        Debug.Log(source + " Input: " + direction);
        OnInputReceived?.Invoke(direction);
    }

    private Vector2 GetSwipeDirection(Vector2 swipeDelta)
    {
        swipeDelta.Normalize();
        return (Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y))
            ? (swipeDelta.x > 0 ? Vector2.right : Vector2.left)
            : (swipeDelta.y > 0 ? Vector2.up : Vector2.down);
    }

    // Методы для работы с UI (если нужно)
    public void OnPointerDown(PointerEventData eventData)
    {
        startTouchPosition = eventData.position;
        isSwiping = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ProcessSwipe(startTouchPosition, eventData.position, "UI Swipe");
        isSwiping = false;
    }
}
