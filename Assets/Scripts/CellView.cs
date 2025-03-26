using UnityEngine;
using UnityEngine.UI;

public class CellView : MonoBehaviour
{
    [SerializeField] private Text cellText;
    private Image cellImage;
    private Cell cell;
    private GameField gameField;

    public Cell LinkedCell => cell;

    public void Init(Cell cell, GameField gameField)
    {
        this.cell = cell;
        this.gameField = gameField;

        cellImage = GetComponent<Image>();
        if (cellImage == null)
            Debug.LogWarning("Image компонент не найден на " + gameObject.name);

        cell.OnValueChanged += UpdateValue;
        cell.OnPositionChanged += UpdatePosition;

        UpdateValue(cell.Value);
        UpdatePosition(cell.Position);
    }

    private void OnDestroy()
    {
        if (cell != null)
        {
            cell.OnValueChanged -= UpdateValue;
            cell.OnPositionChanged -= UpdatePosition;
        }
    }

    public void SetBackgroundColor(Color color)
    {
        if (cellImage == null)
            cellImage = GetComponent<Image>();
        if (cellImage != null)
            cellImage.color = color;
    }

    private void UpdateValue(int newValue)
    {
        cellText.text = newValue.ToString();
        Color colorForValue = gameField.GetColorForValue(newValue);

        // Назначаем цвет фона
        if (cellImage != null)
        {
            cellImage.color = colorForValue;
        }
    }

    private void UpdatePosition(Vector2Int newPosition)
    {
        Transform placeholder = gameField.GetPlaceholderAt(newPosition);
        if (placeholder == null)
        {
            Debug.LogWarning("Placeholder is null for position: " + newPosition);
            return;
        }
        transform.SetParent(placeholder, false);
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localPosition = Vector3.zero;
        }
    }
}