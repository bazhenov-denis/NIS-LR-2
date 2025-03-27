using System.Collections;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class CellViewPlayModeTests
{
    private GameObject cellPrefabGO;
    private GameObject gameFieldGO;
    private GameField gameField;
    private GameObject gridContainerGO;
    private GameObject placeholderGO;

    // Для тестирования CellView достаточно задать fieldSize = 1.
    private int fieldSize = 1;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        // --- Создаем префаб для CellView ---
        cellPrefabGO = new GameObject("CellPrefab");
        // Добавляем компонент Image (для фонового цвета)
        cellPrefabGO.AddComponent<Image>();
        // Добавляем сам компонент CellView
        var cellViewComp = cellPrefabGO.AddComponent<CellView>();
        // Создаем дочерний объект для текста
        GameObject textChild = new GameObject("CellText");
        textChild.transform.SetParent(cellPrefabGO.transform, false);
        Text textComp = textChild.AddComponent<Text>();
        // Назначаем textComp в приватное поле cellText через рефлексию
        var cellTextField = typeof(CellView)
            .GetField("cellText", BindingFlags.NonPublic | BindingFlags.Instance);
        cellTextField.SetValue(cellViewComp, textComp);

        // --- Создаем минимальный объект GameField ---
        gameFieldGO = new GameObject("GameField");
        gameField = gameFieldGO.AddComponent<GameField>();

        // Устанавливаем fieldSize в 1 для упрощения (тогда нужен один placeholder)
        FieldInfo fieldSizeField = typeof(GameField)
            .GetField("fieldSize", BindingFlags.NonPublic | BindingFlags.Instance);
        fieldSizeField.SetValue(gameField, fieldSize);

        // Создаем gridContainer с 1 placeholder
        gridContainerGO = new GameObject("GridContainer");
        placeholderGO = new GameObject("Placeholder");
        placeholderGO.transform.SetParent(gridContainerGO.transform, false);

        // Назначаем gridContainer в GameField через рефлексию
        typeof(GameField).GetField("gridContainer", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(gameField, gridContainerGO.transform);

        // Назначаем префаб для клеток в GameField, чтобы CreateCell() не выдавал ошибку
        typeof(GameField).GetField("cellPrefab", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(gameField, cellPrefabGO.GetComponent<CellView>());

        // Для корректной работы GameField задаем dummy UI-элементы (не критично для тестов CellView)
        GameObject dummyScoreText = new GameObject("ScoreText");
        dummyScoreText.AddComponent<Text>();
        typeof(GameField).GetField("scoreText", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(gameField, dummyScoreText.GetComponent<Text>());
        GameObject dummyHighScoreText = new GameObject("HighScoreText");
        dummyHighScoreText.AddComponent<Text>();
        typeof(GameField).GetField("highScoreText", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(gameField, dummyHighScoreText.GetComponent<Text>());
        GameObject dummyGameOverPanel = new GameObject("GameOverPanel");
        dummyGameOverPanel.SetActive(false);
        typeof(GameField).GetField("gameOverPanel", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(gameField, dummyGameOverPanel);

        // Вызываем Start() на GameField для инициализации массива плейсхолдеров
        gameField.Start();

        // Ждем один кадр, чтобы все методы Start() и события отработали
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Object.DestroyImmediate(cellPrefabGO);
        Object.DestroyImmediate(gameFieldGO);
        Object.DestroyImmediate(gridContainerGO);
        yield return null;
    }

    [UnityTest]
    public IEnumerator Init_ShouldSetTextAndBackgroundColor()
    {
        // Создаем экземпляр CellView, инстанциируя наш префаб на placeholder
        CellView cellView = Object.Instantiate(cellPrefabGO, placeholderGO.transform)
            .GetComponent<CellView>();
        // Создаем модель клетки с начальным значением 2 и позицией (0,0)
        Cell cell = new Cell(new Vector2Int(0, 0), 2);

        // Вызываем Init с использованием настоящего GameField
        cellView.Init(cell, gameField);
        yield return null;

        // Проверяем, что текст равен "2"
        Text displayedText = cellView.GetComponentInChildren<Text>();
        displayedText.text.Should().Be("2");

        // Проверяем, что цвет Image равен тому, что возвращает GameField для 2
        Color expectedColor = gameField.GetColorForValue(2);
        Image imageComp = cellView.GetComponent<Image>();
        imageComp.color.Should().Be(expectedColor);

        yield break;
    }

    [UnityTest]
    public IEnumerator UpdateValue_ShouldChangeTextAndColor()
    {
        // Создаем экземпляр CellView и инициализируем его
        CellView cellView = Object.Instantiate(cellPrefabGO, placeholderGO.transform)
            .GetComponent<CellView>();
        Cell cell = new Cell(new Vector2Int(0, 0), 2);
        cellView.Init(cell, gameField);
        yield return null;

        // Изменяем значение модели клетки
        cell.Value = 4;
        yield return null; // Ждем, чтобы событие OnValueChanged отработало

        // Текст должен обновиться на "4"
        Text displayedText = cellView.GetComponentInChildren<Text>();
        displayedText.text.Should().Be("4");

        // Цвет Image должен соответствовать GameField для 4
        Color expectedColor = gameField.GetColorForValue(4);
        Image imageComp = cellView.GetComponent<Image>();
        imageComp.color.Should().Be(expectedColor);

        yield break;
    }

    [UnityTest]
    public IEnumerator UpdatePosition_ShouldReparentToCorrectPlaceholder()
    {
        // Создаем экземпляр CellView и инициализируем его
        CellView cellView = Object.Instantiate(cellPrefabGO, placeholderGO.transform)
            .GetComponent<CellView>();
        Cell cell = new Cell(new Vector2Int(0, 0), 2);
        cellView.Init(cell, gameField);
        yield return null;

        // Изменяем позицию модели клетки. Поскольку fieldSize == 1, корректная позиция только (0,0).
        cell.Position = new Vector2Int(0, 0);
        yield return null;

        // После обновления позиции CellView должен сменить родителя на плейсхолдер, возвращаемый GameField
        Transform expectedParent = gameField.GetPlaceholderAt(new Vector2Int(0, 0));
        cellView.transform.parent.Should().Be(expectedParent);

        yield break;
    }

    [UnityTest]
    public IEnumerator SetBackgroundColor_ShouldSetImageColor()
    {
        // Создаем экземпляр CellView и инициализируем его
        CellView cellView = Object.Instantiate(cellPrefabGO, placeholderGO.transform)
            .GetComponent<CellView>();
        Cell cell = new Cell(new Vector2Int(0, 0), 2);
        cellView.Init(cell, gameField);
        yield return null;

        // Вызываем SetBackgroundColor с новым цветом (например, magenta)
        Color newColor = Color.magenta;
        cellView.SetBackgroundColor(newColor);
        yield return null;

        // Проверяем, что компонент Image получил новый цвет
        Image imageComp = cellView.GetComponent<Image>();
        imageComp.color.Should().Be(newColor);

        yield break;
    }
}
