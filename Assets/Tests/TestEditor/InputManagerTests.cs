using System.Collections;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

public class InputManagerPlayModeTests
{
    private GameObject inputManagerGO;
    private InputManager inputManager;
    private EventSystem eventSystem;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        // Создаем EventSystem, если его нет
        if (EventSystem.current == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            eventSystem = esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
        }
        else
        {
            eventSystem = EventSystem.current;
        }

        inputManagerGO = new GameObject("InputManager");
        inputManager = inputManagerGO.AddComponent<InputManager>();

        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Object.DestroyImmediate(inputManagerGO);
        // Если мы создавали свой EventSystem, удаляем его
        if (eventSystem != null && eventSystem.gameObject.name == "EventSystem")
        {
            Object.DestroyImmediate(eventSystem.gameObject);
        }
        yield return null;
    }

    [UnityTest]
    public IEnumerator OnPointerSwipe_ShouldInvokeOnInputReceived_WithCorrectDirection()
    {
        Vector2 receivedDirection = Vector2.zero;
        bool eventFired = false;
        inputManager.OnInputReceived += (Vector2 dir) =>
        {
            receivedDirection = dir;
            eventFired = true;
        };

        // Симулируем UI-событие: свайп вправо.
        PointerEventData pointerDownData = new PointerEventData(eventSystem)
        {
            position = new Vector2(100, 100)
        };
        inputManager.OnPointerDown(pointerDownData);

        PointerEventData pointerUpData = new PointerEventData(eventSystem)
        {
            position = new Vector2(200, 100)  // свайп delta = (100,0), что больше minSwipeDistance (50)
        };
        inputManager.OnPointerUp(pointerUpData);

        yield return null;

        eventFired.Should().BeTrue("Событие OnInputReceived должно сработать при UI свайпе");
        receivedDirection.Should().Be(Vector2.right);

        yield break;
    }

    [UnityTest]
    public IEnumerator ProcessSwipe_ShouldNotInvokeEvent_IfSwipeTooShort()
    {
        Vector2 receivedDirection = Vector2.zero;
        bool eventFired = false;
        inputManager.OnInputReceived += (Vector2 dir) =>
        {
            receivedDirection = dir;
            eventFired = true;
        };

        // Вызываем приватный метод ProcessSwipe через рефлексию с коротким свайпом.
        MethodInfo processSwipeMethod = typeof(InputManager)
            .GetMethod("ProcessSwipe", BindingFlags.NonPublic | BindingFlags.Instance);
        Vector2 startPos = new Vector2(100, 100);
        Vector2 endPos = new Vector2(120, 100); // delta = (20,0) < 50
        processSwipeMethod.Invoke(inputManager, new object[] { startPos, endPos, "Test Swipe" });
        yield return null;

        eventFired.Should().BeFalse("Свайп слишком короткий – событие не должно срабатывать");

        yield break;
    }

    [UnityTest]
    public IEnumerator ProcessSwipe_ShouldInvokeEvent_IfSwipeLongEnough()
    {
        Vector2 receivedDirection = Vector2.zero;
        bool eventFired = false;
        inputManager.OnInputReceived += (Vector2 dir) =>
        {
            receivedDirection = dir;
            eventFired = true;
        };

        // Вызываем ProcessSwipe с достаточной длиной свайпа.
        MethodInfo processSwipeMethod = typeof(InputManager)
            .GetMethod("ProcessSwipe", BindingFlags.NonPublic | BindingFlags.Instance);
        Vector2 startPos = new Vector2(100, 100);
        Vector2 endPos = new Vector2(200, 100); // delta = (100,0) > 50
        processSwipeMethod.Invoke(inputManager, new object[] { startPos, endPos, "Test Swipe" });
        yield return null;

        eventFired.Should().BeTrue("Свайп достаточной длины – событие должно срабатывать");
        receivedDirection.Should().Be(Vector2.right);

        yield break;
    }

    [UnityTest]
    public IEnumerator GetSwipeDirection_ShouldReturnCorrectDirection()
    {
        // Получаем приватный метод GetSwipeDirection через рефлексию
        MethodInfo getSwipeDirectionMethod = typeof(InputManager)
            .GetMethod("GetSwipeDirection", BindingFlags.NonPublic | BindingFlags.Instance);

        // Тест горизонтального свайпа вправо:
        Vector2 delta = new Vector2(100, 30);
        Vector2 result = (Vector2)getSwipeDirectionMethod.Invoke(inputManager, new object[] { delta });
        result.Should().Be(Vector2.right);

        // Тест горизонтального свайпа влево:
        delta = new Vector2(-80, 20);
        result = (Vector2)getSwipeDirectionMethod.Invoke(inputManager, new object[] { delta });
        result.Should().Be(Vector2.left);

        // Тест вертикального свайпа вверх:
        delta = new Vector2(20, 100);
        result = (Vector2)getSwipeDirectionMethod.Invoke(inputManager, new object[] { delta });
        result.Should().Be(Vector2.up);

        // Тест вертикального свайпа вниз:
        delta = new Vector2(30, -90);
        result = (Vector2)getSwipeDirectionMethod.Invoke(inputManager, new object[] { delta });
        result.Should().Be(Vector2.down);

        yield return null;
    }
}
