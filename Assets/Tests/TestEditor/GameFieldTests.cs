using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class GameFieldPlayModeTests
{
    private GameObject gameFieldGO;
    private GameField gameField;

    private GameObject gridContainerGO;
    private GameObject scoreTextGO;
    private GameObject highScoreTextGO;
    private GameObject gameOverPanelGO;
    private GameObject cellPrefabGO;
    private GameObject inputManagerGO;
    private InputManager dummyInputManager;

    private int fieldSize = 4;
    private string savePath => Path.Combine(Application.persistentDataPath, "save.dat");

    /// <summary>
    /// Создаёт все необходимые объекты и формирует «префаб» GameField,
    /// чтобы до вызова Awake все ссылки (например, gridContainer) были установлены.
    /// </summary>
    private IEnumerator CreateGameField()
    {
        // 1. Создаём GridContainer и заполняем его placeholder-ами (количество = fieldSize×fieldSize)
        gridContainerGO = new GameObject("GridContainer");
        for (int i = 0; i < fieldSize * fieldSize; i++)
        {
            new GameObject("Placeholder").transform.SetParent(gridContainerGO.transform);
        }

        // 2. Создаём UI-объекты для счета и рекорда
        scoreTextGO = new GameObject("ScoreText");
        scoreTextGO.AddComponent<Text>();

        highScoreTextGO = new GameObject("HighScoreText");
        highScoreTextGO.AddComponent<Text>();

        // 3. Создаём панель GameOver
        gameOverPanelGO = new GameObject("GameOverPanel");
        gameOverPanelGO.SetActive(false);

        // 4. Создаём InputManager
        inputManagerGO = new GameObject("InputManager");
        dummyInputManager = inputManagerGO.AddComponent<InputManager>();

        // 5. Создаём «префаб» для ячейки (CellView)
        cellPrefabGO = new GameObject("CellPrefab");
        cellPrefabGO.AddComponent<Image>(); // Добавляем Image (для фонового цвета)
        var cellView = cellPrefabGO.AddComponent<CellView>();

        // Создаем дочерний объект для текста (CellText)
        var textChildGO = new GameObject("CellText");
        textChildGO.transform.SetParent(cellPrefabGO.transform, false);
        var textComponent = textChildGO.AddComponent<Text>();

        // Назначаем компонент Text в приватное поле cellText через рефлексию
        var cellTextField = typeof(CellView).GetField("cellText", BindingFlags.NonPublic | BindingFlags.Instance);
        cellTextField.SetValue(cellView, textComponent);

        // 6. Создаем "префаб" GameField, устанавливая все сериализованные поля
        var gameFieldPrefab = new GameObject("GameFieldPrefab");
        var gf = gameFieldPrefab.AddComponent<GameField>();

        // Назначаем ссылки, которые обычно задаются через инспектор
        typeof(GameField).GetField("scoreText", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(gf, scoreTextGO.GetComponent<Text>());
        typeof(GameField).GetField("highScoreText", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(gf, highScoreTextGO.GetComponent<Text>());
        typeof(GameField).GetField("gameOverPanel", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(gf, gameOverPanelGO);
        typeof(GameField).GetField("cellPrefab", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(gf, cellPrefabGO.GetComponent<CellView>());
        typeof(GameField).GetField("gridContainer", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(gf, gridContainerGO.transform);
        FieldInfo fieldSizeField = typeof(GameField).GetField("fieldSize", BindingFlags.NonPublic | BindingFlags.Instance);
        fieldSizeField.SetValue(gf, fieldSize);

        // 7. Инстанцируем GameField из префаба – тогда сериализованные поля уже установлены до вызова Awake.
        gameFieldGO = GameObject.Instantiate(gameFieldPrefab);
        gameField = gameFieldGO.GetComponent<GameField>();

        // Удаляем временный префаб
        GameObject.Destroy(gameFieldPrefab);

        // Ждем один кадр, чтобы Unity вызвала Awake и Start на инстанциированном объекте.
        yield return null;
    }

    [TearDown]
    public void TearDown()
    {
        if (gameFieldGO) Object.Destroy(gameFieldGO);
        if (gridContainerGO) Object.Destroy(gridContainerGO);
        if (scoreTextGO) Object.Destroy(scoreTextGO);
        if (highScoreTextGO) Object.Destroy(highScoreTextGO);
        if (gameOverPanelGO) Object.Destroy(gameOverPanelGO);
        if (cellPrefabGO) Object.Destroy(cellPrefabGO);
        if (inputManagerGO) Object.Destroy(inputManagerGO);

        if (File.Exists(savePath))
            File.Delete(savePath);
    }

    [UnityTest]
    public IEnumerator StartNewGame_ShouldResetScoreAndCreateTwoCells()
    {
        yield return CreateGameField();

        gameField.StartNewGame();

        // Проверяем, что счет сброшен до 0
        gameField.score.Should().Be(0);

        // Получаем список клеток (cells)
        var cells = (List<Cell>)typeof(GameField)
            .GetField("cells", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(gameField);
        cells.Count.Should().Be(2, "StartNewGame должен создавать ровно 2 клетки");

        scoreTextGO.GetComponent<Text>().text.Should().Be("0");
        highScoreTextGO.GetComponent<Text>().text.Should().Be("0");

        yield break;
    }

    [UnityTest]
    public IEnumerator CreateCell_ShouldAddNewCellToBoard()
    {
        yield return CreateGameField();

        // Очищаем игровое поле через приватный метод ClearBoard
        MethodInfo clearBoardMethod = typeof(GameField)
            .GetMethod("ClearBoard", BindingFlags.NonPublic | BindingFlags.Instance);
        clearBoardMethod.Invoke(gameField, null);

        var cells = (List<Cell>)typeof(GameField)
            .GetField("cells", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(gameField);
        int initialCount = cells.Count;

        gameField.CreateCell();

        cells = (List<Cell>)typeof(GameField)
            .GetField("cells", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(gameField);
        cells.Count.Should().Be(initialCount + 1);

        yield break;
    }

    [UnityTest]
    public IEnumerator GetEmptyPosition_ShouldReturnValidPosition_WhenEmptyExists()
    {
        yield return CreateGameField();

        Vector2Int pos = gameField.GetEmptyPosition();
        pos.x.Should().BeGreaterThanOrEqualTo(0);
        pos.y.Should().BeGreaterThanOrEqualTo(0);

        yield break;
    }

    [UnityTest]
    public IEnumerator GetEmptyPosition_ShouldReturnNegative_WhenBoardIsFull()
    {
        yield return CreateGameField();

        // Заполняем поле клетками
        Cell[,] cellGrid = new Cell[fieldSize, fieldSize];
        for (int x = 0; x < fieldSize; x++)
        {
            for (int y = 0; y < fieldSize; y++)
            {
                cellGrid[x, y] = new Cell(new Vector2Int(x, y), 2);
            }
        }
        typeof(GameField).GetField("cellGrid", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(gameField, cellGrid);

        Vector2Int pos = gameField.GetEmptyPosition();
        pos.Should().Be(new Vector2Int(-1, -1));

        yield break;
    }

    [UnityTest]
    public IEnumerator AddScore_ShouldUpdateScoreAndUI()
    {
        yield return CreateGameField();

        typeof(GameField).GetField("score", BindingFlags.Public | BindingFlags.Instance)
            .SetValue(gameField, 0);
        gameField.AddScore(4);

        gameField.score.Should().Be(4);
        scoreTextGO.GetComponent<Text>().text.Should().Be("4");
        highScoreTextGO.GetComponent<Text>().text.Should().Be("4");

        yield break;
    }

    [UnityTest]
    public IEnumerator MoveCells_MergeCells_Left_ShouldMergeAndAddScore()
    {
        yield return CreateGameField();

        // Очищаем игровое поле
        MethodInfo clearBoardMethod = typeof(GameField)
            .GetMethod("ClearBoard", BindingFlags.NonPublic | BindingFlags.Instance);
        clearBoardMethod.Invoke(gameField, null);

        // Создаем две клетки со значением 2 в позициях (1,0) и (2,0)
        Cell cell1 = new Cell(new Vector2Int(1, 0), 2);
        Cell cell2 = new Cell(new Vector2Int(2, 0), 2);

        Cell[,] cellGrid = new Cell[fieldSize, fieldSize];
        cellGrid[1, 0] = cell1;
        cellGrid[2, 0] = cell2;
        typeof(GameField).GetField("cellGrid", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(gameField, cellGrid);

        List<Cell> cellsList = new List<Cell> { cell1, cell2 };
        typeof(GameField).GetField("cells", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(gameField, cellsList);

        typeof(GameField).GetField("score", BindingFlags.Public | BindingFlags.Instance)
            .SetValue(gameField, 0);

        gameField.MoveCells(Vector2Int.left);

        cellGrid = (Cell[,])typeof(GameField).GetField("cellGrid", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(gameField);
        cellGrid[0, 0].Should().NotBeNull();
        cellGrid[0, 0].Value.Should().Be(4);
        gameField.score.Should().Be(4);

        yield break;
    }

    [UnityTest]
    public IEnumerator CheckGameOver_ShouldTriggerGameOver_WhenNoMovesLeft()
    {
        yield return CreateGameField();

        // Заполняем поле клетками без пустых ячеек и возможных слияний
        Cell[,] cellGrid = new Cell[fieldSize, fieldSize];
        for (int x = 0; x < fieldSize; x++)
        {
            for (int y = 0; y < fieldSize; y++)
            {
                int value = ((x + y) % 2 == 0) ? 2 : 4;
                cellGrid[x, y] = new Cell(new Vector2Int(x, y), value);
            }
        }
        typeof(GameField).GetField("cellGrid", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(gameField, cellGrid);

        List<Cell> cellsList = new List<Cell>();
        for (int x = 0; x < fieldSize; x++)
        {
            for (int y = 0; y < fieldSize; y++)
            {
                cellsList.Add(cellGrid[x, y]);
            }
        }
        typeof(GameField).GetField("cells", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(gameField, cellsList);

        // Вызываем сдвиг влево – сдвиг не произойдёт, и CheckGameOver() не вызовется внутри MoveCells.
        gameField.MoveCells(Vector2Int.left);

        // Вручную вызываем CheckGameOver(), чтобы проверить, что игра окончена.
        MethodInfo checkGameOverMethod = typeof(GameField).GetMethod("CheckGameOver", BindingFlags.NonPublic | BindingFlags.Instance);
        checkGameOverMethod.Invoke(gameField, null);

        bool isGameOver = (bool)typeof(GameField)
            .GetField("isGameOver", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(gameField);
        isGameOver.Should().BeTrue();
        gameOverPanelGO.activeSelf.Should().BeTrue();
        dummyInputManager.enabled.Should().BeFalse();

        yield break;
    }

    [UnityTest]
    public IEnumerator OnRestartButton_ShouldResetGameOverAndRestartGame()
    {
        yield return CreateGameField();

        typeof(GameField).GetField("isGameOver", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(gameField, true);
        gameOverPanelGO.SetActive(true);
        dummyInputManager.enabled = false;

        gameField.OnRestartButton();

        bool isGameOver = (bool)typeof(GameField)
            .GetField("isGameOver", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(gameField);
        isGameOver.Should().BeFalse();
        gameOverPanelGO.activeSelf.Should().BeFalse();
        dummyInputManager.enabled.Should().BeTrue();

        var cells = (List<Cell>)typeof(GameField)
            .GetField("cells", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(gameField);
        cells.Count.Should().Be(2, "OnRestartButton должен запускать игру с 2 клетками");

        yield break;
    }

    [UnityTest]
    public IEnumerator SaveAndLoadGame_ShouldPersistGameState()
    {
        yield return CreateGameField();

        gameField.StartNewGame();
        gameField.AddScore(8);
        gameField.CreateCell(); // добавляем еще одну клетку

        // Сохраняем игру
        gameField.SaveGame();

        int savedScore = gameField.score;
        var savedCells = (List<Cell>)typeof(GameField)
            .GetField("cells", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(gameField);

        // Очищаем поле, имитируя перезапуск
        MethodInfo clearBoardMethod = typeof(GameField)
            .GetMethod("ClearBoard", BindingFlags.NonPublic | BindingFlags.Instance);
        clearBoardMethod.Invoke(gameField, null);
        typeof(GameField).GetField("score", BindingFlags.Public | BindingFlags.Instance)
            .SetValue(gameField, 0);

        // Загружаем игру
        gameField.LoadGame();

        int loadedScore = gameField.score;
        loadedScore.Should().Be(savedScore);

        var loadedCells = (List<Cell>)typeof(GameField)
            .GetField("cells", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(gameField);
        loadedCells.Count.Should().Be(savedCells.Count);

        yield break;
    }

}
