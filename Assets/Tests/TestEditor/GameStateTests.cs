using NUnit.Framework;
using FluentAssertions;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;

[TestFixture]
public class GameStateTests
{
    [Test]
    public void Serialization_ShouldPreserveGameStateData()
    {
        // Arrange
        GameState originalState = new GameState();
        originalState.currentScore = 100;
        originalState.bestScore = 200;
        originalState.cells = new List<CellData>
        {
            new CellData { x = 0, y = 0, value = 2 },
            new CellData { x = 1, y = 0, value = 4 }
        };

        // Act: сериализуем и десериализуем в памяти
        BinaryFormatter bf = new BinaryFormatter();
        GameState loadedState;
        using (MemoryStream ms = new MemoryStream())
        {
            bf.Serialize(ms, originalState);
            ms.Position = 0;
            loadedState = (GameState)bf.Deserialize(ms);
        }

        // Assert
        loadedState.currentScore.Should().Be(100);
        loadedState.bestScore.Should().Be(200);
        loadedState.cells.Should().HaveCount(2);
        loadedState.cells[0].x.Should().Be(0);
        loadedState.cells[0].y.Should().Be(0);
        loadedState.cells[0].value.Should().Be(2);
        loadedState.cells[1].x.Should().Be(1);
        loadedState.cells[1].y.Should().Be(0);
        loadedState.cells[1].value.Should().Be(4);
    }
}