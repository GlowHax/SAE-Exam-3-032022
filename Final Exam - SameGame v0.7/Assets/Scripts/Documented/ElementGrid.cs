using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Implementation of a regular grid which can hold instances of the Element class in its cells. 
/// <BR>Provides all needed functionality to represent a playing field of the SameGame game. 
/// </summary>
public class ElementGrid
{
    //---Member---
    /// <summary>
    /// Returns the total number of cells in the grid.
    /// </summary>
    /// <value> Number of cells in the grid.</value>
    public int CellCount
    {
        get { return elements.Length; }
    }

    /// <summary>
    /// The origin of the Grid (left bottom corner of the grid).
    /// </summary>
    Vector3 origin;

    /// <summary>
    /// The size of each cell.
    /// </summary>
    Vector2 cellSize;

    /// <summary>
    /// Returns the number of cells in a row (cellCount_X). 
    /// </summary>
    int cellsInARow;
    /// <summary>
    /// Returns the number of cells in a column (cellCount_Y).
    /// </summary>
    int cellsInAColumn
    {
        get { return elements.Length / cellsInARow; }
    }

    /// <summary>
    /// The elements array. Holds all Elements of the grid in a 1D Array.
    /// </summary>
    Element[] elements;


    //---Constructor---
    /// <summary>
    /// Initializes a new instance of the <see cref="ElementGrid"/> class.
    /// </summary>
    /// <param name="origin">The origin of the Grid (left bottom corner of the grid).</param>
    /// <param name="cellSize">The size of each cell.</param>
    /// <param name="cellCount_X">Number of cells in a row (x-direction).</param>
    /// <param name="cellCount_Y">Number of cells in a column (y-direction).</param>
    public ElementGrid(Vector3 origin, Vector2 cellSize, int cellCount_X, int cellCount_Y)
    {
        this.cellsInARow = cellCount_X;
        this.elements = new Element[cellCount_Y * cellCount_X];
        this.origin = origin;
        this.cellSize = cellSize;

        PrintGrid();
    }


    //---Methods---
    /// <summary>
    /// Returns the left bottom point of the cell with the corresponding index.
    /// </summary>
    /// <returns>The left bottom point of the cell with the index "cellIndex". The z-coordinate equals the z-coordinate of the origin of the grid.</returns>
    /// <param name="cellIndex">Index of the corresponding cell.</param>
    Vector3 GetCellMinPoint(int cellIndex)
    {
        int x = cellIndex % cellsInARow;
        int y = cellIndex / cellsInARow;

        return new Vector3((x * cellSize.x) + origin.x,
                            (y * cellSize.y) + origin.y,
                            origin.z);
    }

    /// <summary>
    /// Returns the center (point in worldSpace) of the cell with the corresponding cellIndex.
    /// </summary>
    /// <returns>The center of the cell. The z-coordinate equals the z-coordinate of the origin of the grid.</returns>
    /// <param name="cellIndex">Index of the cell.</param>
    public Vector3 GetCellCenter(int cellIndex)
    {
        return GetCellMinPoint(cellIndex) + new Vector3(cellSize.x * 0.5f, cellSize.y * 0.5f, 0f);
    }

    /// <summary>
    /// Transforms a worldspace position into a cellIndex. <B>Attention:</B> If the point is outside of the grid bounds, the returned index is not valid!
    /// </summary>
    /// <returns><c>The index of the corresponding cell:</c> if the point is inside the grid bounds.
    /// <BR> <c>-1:</c> otherwise</returns>
    /// <param name="positionInWorldSpace">A position in world space.</param>
    public int PointToIndex(Vector3 positionInWorldSpace)
    {
        positionInWorldSpace -= origin;

        return CalculateIndex(Mathf.FloorToInt(positionInWorldSpace.x / cellSize.x), Mathf.FloorToInt(positionInWorldSpace.y / cellSize.y));
    }

    /// <summary>
    /// Sets the Element instance for this cell and updates the position of the elements visuals (see <see cref="Element"/>) to the center of the given cell.
    /// </summary>
    /// <param name="cellIndex">Index of the cell.</param>
    /// <param name="element">The Element instance to set for this cell.</param>
    public void SetElement(int cellIndex, Element element)
    {
        if (!IsIndexValid(cellIndex))
        {
            Debug.LogError("ElementArray::SetCell -> IndexOutOfRange!");
            return;
        }

        element.UpdateElement(GetCellCenter(cellIndex));
        elements[cellIndex] = element;
    }

    /// <summary>
    /// Returns the Element instance from the cell with the index "cellIndex".
    /// </summary>
    /// <returns>The Element instance of the cell or null (if the cell is empty).</returns>
    /// <param name="cellIndex">Index of the cell.</param>
    public Element GetElement(int cellIndex)
    {
        if (!IsIndexValid(cellIndex))
        {
            Debug.LogError("ElementArray::GetCell -> IndexOutOfRange!");
            return null;
        }

        return elements[cellIndex];
    }

    /// <summary>
    /// Removes the elements of the cells given by the cellIndices. The Content of these cells is null afterwards.
    /// <BR>The ElementGrid also moves the remaining Elements down (per column) if there are empty cells (Element instance is null) in between and moves the remaining (not empty) columns to the left if there are empty columns in between.
    /// <BR>If Elements are moved, the position of their visuals (GameObjects in the scene) are updated automatically (corresponding to their new cell).
    /// <BR><B>Attention:</B> The indices of Elements could change through this process!
    /// </summary>
    /// <param name="cellIndices">Indices of the cells to delete.</param>
    public void RemoveElements(int[] cellIndices)
    {
        // Delete gameobjects & memorize columns to update
        List<int> columns = new List<int>();
        foreach (int index in cellIndices)
        {
            Element e = elements[index];
            if (e != null)
            {
                e.DestroyElement();
            }

            elements[index] = null;

            int cellIndex_X = index % cellsInARow;
            if (!columns.Contains(cellIndex_X))
            {
                columns.Add(cellIndex_X);
            }
        }

        // Update Columns
        int move = 0;
        foreach (int x in columns)
        {
            move = 0;
            for (int y = 0; y < cellsInAColumn; y++)
            {
                int fromIndex = CalculateIndex(x, y);
                if (elements[fromIndex] == null)
                {
                    move++;
                }
                else
                {
                    if (move != 0)
                    {
                        int toIndex = CalculateIndex(x, y - move);
                        MoveElement(fromIndex, toIndex);
                    }
                }
            }
        }

        // Delete Columns
        move = 0;
        for (int x = 0; x < cellsInARow; x++)
        {
            if (elements[x] == null)
            {
                move++;
            }
            else
            {
                if (move != 0)
                {
                    MoveColumn(x, x - move);
                }
            }
        }
    }

    /// <summary>
    /// Returns the (not empty) neighbours ("4er Nachbarschaft") as indices of the neighbouring cells.
    /// </summary>
    /// <returns>The indices of the (not empty) neighbours ("4er Nachbarschaft") as array. <BR><B>Attention: </B>The number of neighbours can vary between 0 and 4.</returns>
    /// <param name="cellIndex">Index of the cell.</param>
    public int[] GetNeighbours(int cellIndex)
    {
        int x = cellIndex % cellsInARow;
        int y = cellIndex / cellsInARow;

        List<int> neighbours = new List<int>();

        int index;
        if (x > 0)
        {
            if (elements[index = CalculateIndex(x - 1, y)] != null)
            {
                neighbours.Add(index);
            }
        }
        if (x < (cellsInARow - 1))
        {
            if (elements[index = CalculateIndex(x + 1, y)] != null)
            {
                neighbours.Add(index);
            }
        }
        if (y > 0)
        {
            if (elements[index = CalculateIndex(x, y - 1)] != null)
            {
                neighbours.Add(index);
            }
        }
        if (y < (cellsInAColumn - 1))
        {
            if (elements[index = CalculateIndex(x, y + 1)] != null)
            {
                neighbours.Add(index);
            }
        }

        return neighbours.ToArray();
    }

    /// <summary>
    /// Determines whether the given cellIndex is a valid index for this grid.
    /// </summary>
    /// <returns><c>true:</c> if the cellIndex is valid
    /// <BR><c>false:</c> otherwise</returns>
    /// <param name="cellIndex">An arbitrary index.</param>
    public bool IsIndexValid(int cellIndex)
    {
        if (elements == null)
        {
            Debug.LogError("ElementsArray is null");
            return false;
        }

        if (cellIndex < 0 || cellIndex >= elements.Length)
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// Destroys the grid (Removes all corresponding GameObjects from the Scene).
    /// </summary>
    public void DestroyGrid()
    {
        if (elements != null)
        {
            foreach (Element e in elements)
            {
                if (e != null)
                    e.DestroyElement();
            }
            elements = null;
        }
    }


    //--- private Helper ---
    int CalculateIndex(int x, int y)
    {
        if (x < 0 || y < 0 || x >= cellsInARow || y >= cellsInAColumn)
            return -1;
        return x + y * cellsInARow;
    }

    void MoveElement(int from, int to)
    {
        if (!IsIndexValid(from))
        {
            Debug.LogError("ElementArray::MoveElement -> fromIndexOutOfRange!");
            return;
        }
        if (!IsIndexValid(to))
        {
            Debug.LogError("ElementArray::MoveElement -> toIndexOutOfRange!");
            return;
        }

        if (elements[from] != null)
        {
            elements[from].UpdateElement(GetCellCenter(to));
            elements[to] = elements[from];
            elements[from] = null;
        }
    }

    void MoveColumn(int from, int to)
    {
        if (from < 0 || from >= cellsInARow)
        {
            Debug.LogError("ElementArray::MoveColumn -> fromIndexOutOfRange!");
            return;
        }
        if (to < 0 || to >= cellsInARow)
        {
            Debug.LogError("ElementArray::MoveColumn -> toIndexOutOfRange!");
            return;
        }

        int fromIndex;
        int toIndex;

        for (int y = 0; y < cellsInAColumn; y++)
        {
            fromIndex = CalculateIndex(from, y);
            toIndex = CalculateIndex(to, y);
            MoveElement(fromIndex, toIndex);
        }
    }

    void PrintGrid()
    {
        Color color = Color.green;
        float duration = 1000f;

        for (int x = 0; x < cellsInARow; x++)
        {
            Debug.DrawLine(GetCellMinPoint(CalculateIndex(x, 0)), GetCellMinPoint(CalculateIndex(x, cellsInAColumn - 1)) + new Vector3(0, cellSize.y, 0), color, duration);
        }
        Debug.DrawLine(GetCellMinPoint(CalculateIndex(cellsInARow - 1, 0)) + new Vector3(cellSize.x, 0, 0), GetCellMinPoint(CalculateIndex(cellsInARow - 1, cellsInAColumn - 1)) + new Vector3(cellSize.x, cellSize.y, 0), color, duration);

        for (int y = 0; y < cellsInAColumn; y++)
        {
            Debug.DrawLine(GetCellMinPoint(CalculateIndex(0, y)), GetCellMinPoint(CalculateIndex(cellsInARow - 1, y)) + new Vector3(cellSize.x, 0, 0), color, duration);
        }
        Debug.DrawLine(GetCellMinPoint(CalculateIndex(0, cellsInARow - 1)) + new Vector3(0, cellSize.y, 0), GetCellMinPoint(CalculateIndex(cellsInARow - 1, cellsInAColumn - 1)) + new Vector3(cellSize.x, cellSize.y, 0), color, duration);
    }
}
