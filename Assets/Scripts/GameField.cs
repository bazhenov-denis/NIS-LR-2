using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class GameField : MonoBehaviour
{
    [Header("Счёт")]
    public int score;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text highScoreText; // Рекорд (High)
    
    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel;
    
    private bool isGameOver = false;


    [Header("Настройки поля")]
    [SerializeField] private int fieldSize = 4;           // Размер поля (4×4)
    [SerializeField] private CellView cellPrefab;         // Префаб клетки (CellView)

    [Header("Ссылки на UI")]
    [SerializeField] private Transform gridContainer;     // Родитель плейсхолдеров

    private Transform[,] placeholders;
    private Cell[,] cellGrid;
    private List<Cell> cells = new List<Cell>();

    // Рекорд (лучший счет), загружается из файла
    private int bestScore = 0;

    private InputManager inputManager;

    private string saveFilePath => Path.Combine(Application.persistentDataPath, "save.dat");

    private void Awake()
    {
        // Инициализация массивов
        placeholders = new Transform[fieldSize, fieldSize];
        cellGrid = new Cell[fieldSize, fieldSize];

        int childIndex = 0;
        for (int y = 0; y < fieldSize; y++)
        {
            for (int x = 0; x < fieldSize; x++)
            {
                placeholders[x, y] = gridContainer.GetChild(childIndex);
                childIndex++;
            }
        }
    }

    private void Start()
    {
        // Попытка загрузки сохраненного состояния (если файл существует)
        LoadGame();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        // Если сохранения нет, запускаем новую игру с 2 клетками
        if (cells.Count == 0)
        {
            score = 0;
            UpdateScoreUI();
            CreateCell();
            CreateCell();
        }

        inputManager = FindObjectOfType<InputManager>();
        if (inputManager != null)
        {
            inputManager.OnInputReceived += OnInputReceivedHandler;
        }
        else
        {
            Debug.LogWarning("InputManager не найден на сцене!");
        }
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    /// <summary>
    /// Обработчик ввода из InputManager. Преобразует Vector2 в Vector2Int и передает в MoveCells.
    /// </summary>
    private void OnInputReceivedHandler(Vector2 direction)
    {
        Vector2Int moveDir = new Vector2Int((int)direction.x, (int)direction.y);
        Debug.Log("GameField получил ввод: " + moveDir);
        MoveCells(moveDir);
    }

    public void MoveCells(Vector2Int direction)
    {
        bool movedOrMerged = false;
        
        if (isGameOver)
            return; 

        if (direction == Vector2Int.left)
        {
            for (int y = 0; y < fieldSize; y++)
            {
                List<Cell> rowCells = new List<Cell>();
                for (int x = 0; x < fieldSize; x++)
                {
                    if (cellGrid[x, y] != null)
                        rowCells.Add(cellGrid[x, y]);
                }

                List<Cell> mergedRow = MergeRowLeft(rowCells);

                for (int x = 0; x < fieldSize; x++)
                    cellGrid[x, y] = null;

                for (int x = 0; x < mergedRow.Count; x++)
                {
                    cellGrid[x, y] = mergedRow[x];
                    Vector2Int newPos = new Vector2Int(x, y);
                    if (mergedRow[x].Position != newPos)
                    {
                        mergedRow[x].Position = newPos;
                        movedOrMerged = true;
                    }
                }
            }
        }
        else if (direction == Vector2Int.right)
        {
            for (int y = 0; y < fieldSize; y++)
            {
                List<Cell> rowCells = new List<Cell>();
                for (int x = fieldSize - 1; x >= 0; x--)
                {
                    if (cellGrid[x, y] != null)
                        rowCells.Add(cellGrid[x, y]);
                }

                List<Cell> mergedRow = MergeRowLeft(rowCells);
                mergedRow.Reverse();

                for (int x = 0; x < fieldSize; x++)
                    cellGrid[x, y] = null;

                int count = mergedRow.Count;
                for (int i = 0; i < count; i++)
                {
                    int x = fieldSize - count + i;
                    cellGrid[x, y] = mergedRow[i];
                    Vector2Int newPos = new Vector2Int(x, y);
                    if (mergedRow[i].Position != newPos)
                    {
                        mergedRow[i].Position = newPos;
                        movedOrMerged = true;
                    }
                }
            }
        }
        else if (direction == Vector2Int.up)
        {
            for (int x = 0; x < fieldSize; x++)
            {
                List<Cell> colCells = new List<Cell>();
                for (int y = 0; y < fieldSize; y++)
                {
                    if (cellGrid[x, y] != null)
                        colCells.Add(cellGrid[x, y]);
                }

                List<Cell> mergedCol = MergeRowLeft(colCells);

                for (int y = 0; y < fieldSize; y++)
                    cellGrid[x, y] = null;

                for (int y = 0; y < mergedCol.Count; y++)
                {
                    cellGrid[x, y] = mergedCol[y];
                    Vector2Int newPos = new Vector2Int(x, y);
                    if (mergedCol[y].Position != newPos)
                    {
                        mergedCol[y].Position = newPos;
                        movedOrMerged = true;
                    }
                }
            }
        }
        else if (direction == Vector2Int.down)
        {
            for (int x = 0; x < fieldSize; x++)
            {
                List<Cell> colCells = new List<Cell>();
                for (int y = fieldSize - 1; y >= 0; y--)
                {
                    if (cellGrid[x, y] != null)
                        colCells.Add(cellGrid[x, y]);
                }

                List<Cell> mergedCol = MergeRowLeft(colCells);
                mergedCol.Reverse();

                for (int y = 0; y < fieldSize; y++)
                    cellGrid[x, y] = null;

                int count = mergedCol.Count;
                for (int i = 0; i < count; i++)
                {
                    int y = fieldSize - count + i;
                    cellGrid[x, y] = mergedCol[i];
                    Vector2Int newPos = new Vector2Int(x, y);
                    if (mergedCol[i].Position != newPos)
                    {
                        mergedCol[i].Position = newPos;
                        movedOrMerged = true;
                    }
                }
            }
        }

        if (movedOrMerged)
        {
            CreateCell();
            CheckGameOver();
        }
    }

    /// <summary>
    /// Сливает клетки в ряду (слева направо). Если две соседние клетки имеют одинаковое значение – объединяет их.
    /// </summary>
    private List<Cell> MergeRowLeft(List<Cell> rowCells)
    {
        List<Cell> result = new List<Cell>();

        for (int i = 0; i < rowCells.Count; i++)
        {
            Cell current = rowCells[i];
            if (i < rowCells.Count - 1)
            {
                Cell next = rowCells[i + 1];
                if (current.Value == next.Value)
                {
                    int newValue = current.Value + next.Value;
                    current.Value = newValue;
                    AddScore(newValue);
                    i++; // Пропускаем следующую клетку
                    RemoveCell(next);
                }
            }
            result.Add(current);
        }

        return result;
    }

    /// <summary>
    /// Удаляет клетку из модели и уничтожает её визуальное представление.
    /// </summary>
    private void RemoveCell(Cell cellToRemove)
    {
        Vector2Int pos = cellToRemove.Position;
        if (cellGrid[pos.x, pos.y] == cellToRemove)
            cellGrid[pos.x, pos.y] = null;

        if (cells.Contains(cellToRemove))
            cells.Remove(cellToRemove);

        CellView[] allViews = FindObjectsOfType<CellView>();
        foreach (var view in allViews)
        {
            if (view.LinkedCell == cellToRemove)
            {
                Destroy(view.gameObject);
                break;
            }
        }
    }

    /// <summary>
    /// Проверяет, остались ли ходы. Если нет – игра окончена.
    /// При окончании игры сравнивается текущий счет с лучшим, сохраняется и вызывается сброс сессии.
    /// </summary>
    private void CheckGameOver()
    {
        // Проверяем наличие пустых ячеек
        for (int x = 0; x < fieldSize; x++)
        {
            for (int y = 0; y < fieldSize; y++)
            {
                if (cellGrid[x, y] == null)
                    return;
            }
        }

        // Проверяем возможность слияния соседних клеток
        for (int x = 0; x < fieldSize; x++)
        {
            for (int y = 0; y < fieldSize; y++)
            {
                Cell current = cellGrid[x, y];
                if (x + 1 < fieldSize && cellGrid[x + 1, y].Value == current.Value)
                    return;
                if (y + 1 < fieldSize && cellGrid[x, y + 1].Value == current.Value)
                    return;
            }
        }

        Debug.Log("Game Over!");
        // Обновляем рекорд, если необходимо
        if (score > bestScore)
        {
            bestScore = score;
            Debug.Log("Новый рекорд: " + bestScore);
        }
        SaveGame();
        // Останавливаем игру
        isGameOver = true;

        // Выключаем обработку ввода (если у вас есть inputManager)
        if (inputManager != null)
        {
            inputManager.enabled = false;
        }

        // Показываем панель Game Over
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Сбрасывает игровую сессию: очищает поле, обнуляет счет и создает начальные клетки.
    /// </summary>
    public void StartNewGame()
    {
        ClearBoard();
        score = 0;
        UpdateScoreUI();
        CreateCell();
        CreateCell();
    }

    /// <summary>
    /// Удаляет все клетки с поля и очищает модель.
    /// </summary>
    private void ClearBoard()
    {
        CellView[] allViews = FindObjectsOfType<CellView>();
        foreach (var view in allViews)
            Destroy(view.gameObject);

        cells.Clear();
        cellGrid = new Cell[fieldSize, fieldSize];
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = score.ToString();
        
        if (highScoreText != null)
            highScoreText.text = bestScore.ToString();
    }

    public void AddScore(int valueToAdd)
    {
        score += valueToAdd;
        UpdateScoreUI();
        if (score > bestScore)
        {
            bestScore = score;
            UpdateScoreUI();
        }
    }

    /// <summary>
    /// Возвращает случайную пустую позицию на поле.
    /// </summary>
    public Vector2Int GetEmptyPosition()
    {
        List<Vector2Int> emptyPositions = new List<Vector2Int>();
        for (int x = 0; x < fieldSize; x++)
        {
            for (int y = 0; y < fieldSize; y++)
            {
                if (cellGrid[x, y] == null)
                    emptyPositions.Add(new Vector2Int(x, y));
            }
        }
        if (emptyPositions.Count == 0)
            return new Vector2Int(-1, -1);
        int randomIndex = Random.Range(0, emptyPositions.Count);
        return emptyPositions[randomIndex];
    }
    
    public Color GetColorForValue(int value)
    {
        switch (value)
        {
            case 2:   return new Color(0.93f, 0.89f, 0.85f); // Светлый оттенок
            case 4:   return new Color(0.93f, 0.88f, 0.78f);
            case 8:   return new Color(0.96f, 0.76f, 0.46f);
            case 16:  return new Color(0.96f, 0.68f, 0.38f);
            case 32:  return new Color(0.96f, 0.58f, 0.38f);
            case 64:  return new Color(0.96f, 0.48f, 0.38f);
            case 128:   return new Color(0.96f, 0.38f, 0.32f);
            case 256:  return new Color(0.96f, 0.32f, 0.32f);
            case 512:  return new Color(0.86f, 0.26f, 0.32f);
            case 1024:  return new Color(0.80f, 0.16f, 0.32f);
            case 2048:  return new Color(0.76f, 0.06f, 0.32f);

            default:  return new Color(0.55f, 0.1f, 0.2f); // Для неизвестных значений
        }
    }

    /// <summary>
    /// Создает новую клетку на случайной пустой позиции.
    /// </summary>
    public void CreateCell()
    {
        Vector2Int emptyPos = GetEmptyPosition();
        if (emptyPos.x < 0)
            return;

        int newValue = (Random.value < 0.8f) ? 2 : 4;
        Cell newCell = new Cell(emptyPos, newValue);
        cellGrid[emptyPos.x, emptyPos.y] = newCell;
        cells.Add(newCell);

        Transform placeholder = placeholders[emptyPos.x, emptyPos.y];
        CellView newCellView = Instantiate(cellPrefab, placeholder);
        newCellView.Init(newCell, this);

        Color cellColor = GetColorForValue(newValue);

        newCellView.SetBackgroundColor(cellColor);

        RectTransform childRect = newCellView.GetComponent<RectTransform>();
        if (childRect != null)
        {
            childRect.anchorMin = Vector2.zero;
            childRect.anchorMax = Vector2.one;
            childRect.offsetMin = Vector2.zero;
            childRect.offsetMax = Vector2.zero;
            childRect.localPosition = Vector3.zero;
        }
    }

    /// <summary>
    /// Возвращает плейсхолдер (Transform) для заданной позиции на поле.
    /// Используется в CellView для обновления позиции клетки.
    /// </summary>
    public Transform GetPlaceholderAt(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= fieldSize || pos.y < 0 || pos.y >= fieldSize)
            return null;
        return placeholders[pos.x, pos.y];
    }

    /// <summary>
    /// Сохраняет состояние игры в двоичный файл по пути persistentDataPath.
    /// </summary>
    public void SaveGame()
    {
        GameState state = new GameState();
        state.currentScore = score;
        state.bestScore = bestScore;
        state.cells = new List<CellData>();

        for (int x = 0; x < fieldSize; x++)
        {
            for (int y = 0; y < fieldSize; y++)
            {
                if (cellGrid[x, y] != null)
                {
                    CellData cd = new CellData();
                    cd.x = x;
                    cd.y = y;
                    cd.value = cellGrid[x, y].Value;
                    state.cells.Add(cd);
                }
            }
        }

        BinaryFormatter bf = new BinaryFormatter();
        using (FileStream fs = new FileStream(saveFilePath, FileMode.Create))
        {
            bf.Serialize(fs, state);
        }
        Debug.Log("Игра сохранена: " + saveFilePath);
    }

    /// <summary>
    /// Загружает состояние игры из файла, если он существует.
    /// </summary>
    public void LoadGame()
    {
        if (File.Exists(saveFilePath))
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (FileStream fs = new FileStream(saveFilePath, FileMode.Open))
            {
                GameState state = (GameState)bf.Deserialize(fs);
                score = state.currentScore;
                bestScore = state.bestScore;
                UpdateScoreUI();

                ClearBoard();
                foreach (CellData cd in state.cells)
                {
                    Cell cell = new Cell(new Vector2Int(cd.x, cd.y), cd.value);
                    cellGrid[cd.x, cd.y] = cell;
                    cells.Add(cell);
                    Transform placeholder = placeholders[cd.x, cd.y];
                    CellView newCellView = Instantiate(cellPrefab, placeholder);
                    newCellView.Init(cell, this);

                    // Выбор случайного цвета, аналогичный CreateCell
                    Color cellColor = GetColorForValue(cd.value);

                    newCellView.SetBackgroundColor(cellColor);
                }
                Debug.Log("Игра загружена: " + saveFilePath);
            }
        }
    }
    
    public void OnRestartButton()
    {
        StartNewGame(); // ваш метод, который обнуляет счёт, очищает поле и создаёт 2 новые клетки

        // Скрываем панель
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // Снова разрешаем ввод
        if (inputManager != null)
        {
            inputManager.enabled = true;
        }

        // Сбрасываем флаг
        isGameOver = false;
    }

}
