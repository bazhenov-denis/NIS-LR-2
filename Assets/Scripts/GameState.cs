using System;
using System.Collections.Generic;

[Serializable]
public class GameState
{
    public int currentScore;
    public int bestScore;
    public List<CellData> cells;
}

[Serializable]
public class CellData
{
    public int x;
    public int y;
    public int value;
}