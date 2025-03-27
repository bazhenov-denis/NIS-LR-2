using NUnit.Framework;
using FluentAssertions;
using UnityEngine;

[TestFixture]
public class CellTests
{
    [Test]
    public void SettingValue_ShouldTriggerOnValueChanged()
    {
        // Arrange
        Cell cell = new Cell(new Vector2Int(0, 0), 2);
        int eventValue = 0;
        bool eventFired = false;
        cell.OnValueChanged += (newVal) => { eventValue = newVal; eventFired = true; };

        // Act
        cell.Value = 4;

        // Assert
        eventFired.Should().BeTrue("при изменении значения должно сработать событие OnValueChanged");
        eventValue.Should().Be(4);

        // Проверяем, что повторное присвоение того же значения не вызывает событие
        eventFired = false;
        cell.Value = 4;
        eventFired.Should().BeFalse("при присвоении того же значения событие не должно срабатывать");
    }

    [Test]
    public void SettingPosition_ShouldTriggerOnPositionChanged()
    {
        // Arrange
        Cell cell = new Cell(new Vector2Int(0, 0), 2);
        Vector2Int eventPosition = new Vector2Int(-1, -1);
        bool eventFired = false;
        cell.OnPositionChanged += (newPos) => { eventPosition = newPos; eventFired = true; };

        // Act
        Vector2Int newPos = new Vector2Int(3, 5);
        cell.Position = newPos;

        // Assert
        eventFired.Should().BeTrue("при изменении позиции должно сработать событие OnPositionChanged");
        eventPosition.Should().Be(newPos);

        // Проверяем, что повторное присвоение той же позиции не вызывает событие
        eventFired = false;
        cell.Position = newPos;
        eventFired.Should().BeFalse("при присвоении той же позиции событие не должно срабатывать");
    }
}