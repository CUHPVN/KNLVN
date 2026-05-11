using CUHP;
using UnityEngine;

public class GridUnit
{
    private Grid<GridUnit> g;
    private int x;
    private int y;

    public GridUnit(Grid<GridUnit> g, int x, int y)
    {
        this.g = g;
        this.x = x;
        this.y = y;
    }
}
