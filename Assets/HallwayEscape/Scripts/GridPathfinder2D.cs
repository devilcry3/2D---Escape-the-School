using System.Collections.Generic;
using UnityEngine;

/*
 * GRID PATHFINDER — PREBUILT INSTRUCTOR SCRIPT (ADVANCED)
 *
 * WHAT IT DOES:
 *   1. Divides the play area into a grid of small square cells.
 *   2. Checks which cells have WALLS inside them.
 *   3. Finds a connected route around blocked cells.
 *
 * The route uses BREADTH-FIRST SEARCH (BFS). BFS explores nearby cells
 * before farther cells. With four-direction movement and equal-cost cells,
 * it returns a shortest route measured in grid steps.
 *
 * Students do NOT need to understand this algorithm to edit the level.
 * It works with BoxCollider2D, PolygonCollider2D, TilemapCollider2D, etc.,
 * as long as the colliders are on a layer selected by WallLayers.
 *
 * STATIC MAP RULE:
 * Grid is created when the scene starts. Walls moved BEFORE pressing
 * Play are detected automatically. Walls moved DURING Play require
 * BuildGrid() to be called again (or use this component's context menu).
 */
public class GridPathfinder2D : MonoBehaviour
{
    [Header("Navigation Area")]
    // The BoxCollider2D defines which part of the map the monitor can use.
    // It can be set to Is Trigger because it is NOT a physical wall.
    public BoxCollider2D PlayArea;

    // Only colliders on these Unity layers count as blocked cells.
    public LayerMask WallLayers;

    [Min(0.2f)]// Smaller cells = more detail and more work. 
    public float CellSize = 0.5f; // Smaller cells = more detail and more work.

    [Range(0.5f, 1.5f)]// A larger factor = more padding around walls for the enemy.
    public float ClearanceFactor = 1.4f; // Padding around walls for the enemy.

    private bool[,] cellIsOpen;
    private int gridWidth;
    private int gridHeight;
    private Vector2 minimumGridPosition;

    // Vector2Int stores whole-number X/Y directions in the grid.
    // Four directions mean there are no diagonal shortcuts through corners.
    private static readonly Vector2Int[] NeighborDirections =
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left
    };

    private void Awake()
    {
        BuildGrid();
    }

    // In Play Mode, use the component's three-dot/context menu and choose
    // "Rebuild Navigation Grid" after moving/adding/removing live walls.
    [ContextMenu("Rebuild Navigation Grid")]
    public void BuildGrid()
    {
        if (PlayArea == null)
        {
            cellIsOpen = null;
            Debug.LogError("GridPathfinder2D: assign a Play Area BoxCollider2D.", this);
            return;
        }

        if (WallLayers.value == 0) //
        {
            Debug.LogWarning("GridPathfinder2D: Wall Layers is empty; walls will be ignored.", this);
            // We still build the grid, but all cells will be open.
        }

        // Ensure Physics2D sees collider changes made immediately beforehand.
        Physics2D.SyncTransforms();

        Bounds playAreaBounds = PlayArea.bounds;
        minimumGridPosition = new Vector2(playAreaBounds.min.x, playAreaBounds.min.y);
        // The grid's bottom-left corner is the minimum X/Y of the BoxCollider2D.

        gridWidth = Mathf.Max(1, Mathf.FloorToInt(playAreaBounds.size.x / CellSize));
        // The grid's width is the BoxCollider2D's width divided by the cell size.
        gridHeight = Mathf.Max(1, Mathf.FloorToInt(playAreaBounds.size.y / CellSize));
        // The grid's height is the BoxCollider2D's height divided by the cell size.
        cellIsOpen = new bool[gridWidth, gridHeight];
        // cellIsOpen[x, y] = true means the cell is free to move through.

        // Check a slightly enlarged box around each cell center. This helps
        // stop a circular enemy squeezing through a path narrower than itself.
        float checkBoxWidth = CellSize * ClearanceFactor;
        Vector2 checkBoxSize = new Vector2(checkBoxWidth, checkBoxWidth);

        for (int x = 0; x < gridWidth; x = x + 1)
        {
            for (int y = 0; y < gridHeight; y = y + 1)
            {
                Vector2 cellCenter = GetCellCenter(x, y);
                Collider2D wallFound = Physics2D.OverlapBox(
                    cellCenter, checkBoxSize, 0f, WallLayers
                );

                if (wallFound == null)
                {
                    cellIsOpen[x, y] = true;
                }
                else
                {
                    cellIsOpen[x, y] = false;
                }
            }
        }

        /*
         * ALTERNATIVE SYNTAX FOR THE SAME BLOCKING CHECK:
         * cellIsOpen[x, y] = wallFound == null;
         *
         * The expression on the right already produces true or false.
         * Our longer if/else version is easier to inspect while learning.
         */
    }

    // Called by the hall monitor when it needs a new route.
    // "outputPath" is filled with positions that the monitor should visit.
    // Return value: true = route found; false = cannot reach destination.
    public bool FindPath(Vector2 startingPosition, Vector2 goalPosition,
        List<Vector2> outputPath)
    {
        outputPath.Clear(); // Never leave old route points in the list.

        if (cellIsOpen == null)
        {
            BuildGrid();
        }

        if (cellIsOpen == null)
        {
            return false;
        }

        Vector2Int startingCell = WorldPositionToCell(startingPosition);
        Vector2Int goalCell = WorldPositionToCell(goalPosition);

        if (IsInsideGrid(startingCell) == false)
        {
            return false;
        }

        if (IsInsideGrid(goalCell) == false)
        {
            return false;
        }

        if (cellIsOpen[goalCell.x, goalCell.y] == false)
        {
            return false; // A destination INSIDE a wall is not reachable.
        }

        // Internally BFS uses a one-dimensional number for every grid cell.
        int totalCellCount = gridWidth * gridHeight;
        int[] cameFrom = new int[totalCellCount];
        bool[] hasBeenVisited = new bool[totalCellCount];

        // -1 means "we have not discovered a previous cell yet".
        for (int cellNumber = 0; cellNumber < totalCellCount; cellNumber = cellNumber + 1)
        {
            cameFrom[cellNumber] = -1;
        }

        int startingCellNumber = CellToNumber(startingCell);
        int goalCellNumber = CellToNumber(goalCell);

        // A Queue follows "first in, first out" order, like a waiting line.
        Queue<int> cellsToVisit = new Queue<int>();
        cellsToVisit.Enqueue(startingCellNumber);
        hasBeenVisited[startingCellNumber] = true;

        // Keep exploring until there are no more cells to check.
        while (cellsToVisit.Count > 0)
        {
            int currentCellNumber = cellsToVisit.Dequeue();

            if (currentCellNumber == goalCellNumber)
            {
                break; // We reached the goal.
            }

            Vector2Int currentCell = NumberToCell(currentCellNumber);

            foreach (Vector2Int direction in NeighborDirections)
            {
                Vector2Int nextCell = currentCell + direction;

                if (IsInsideGrid(nextCell) == false)
                {
                    continue; // Skip cells outside the navigation rectangle.
                }

                if (cellIsOpen[nextCell.x, nextCell.y] == false)
                {
                    continue; // Skip walls/blocked cells.
                }

                int nextCellNumber = CellToNumber(nextCell);

                if (hasBeenVisited[nextCellNumber] == true)
                {
                    continue; // Do not explore the same cell twice.
                }

                hasBeenVisited[nextCellNumber] = true;
                cameFrom[nextCellNumber] = currentCellNumber;
                cellsToVisit.Enqueue(nextCellNumber);
            }
        }

        if (hasBeenVisited[goalCellNumber] == false)
        {
            return false; // No chain of open cells leads to the goal.
        }

        // BACKTRACK: follow cameFrom links from goal back to starting cell.
        int pathCellNumber = goalCellNumber;

        while (pathCellNumber != startingCellNumber)
        {
            Vector2Int pathCell = NumberToCell(pathCellNumber);
            Vector2 worldPosition = GetCellCenter(pathCell.x, pathCell.y);
            outputPath.Add(worldPosition);
            pathCellNumber = cameFrom[pathCellNumber];
        }

        outputPath.Reverse(); // Now the route runs from start toward goal.
        outputPath.Add(goalPosition); // Include exact goal inside its open cell.

        return true;

        /*
         * ADVANCED ALTERNATIVES:
         * - A* (A-star) searches toward the goal using a distance estimate.
         * - Unity navigation packages can also create routes, but this
         *   starter has no third-party 2D navigation dependencies.
         *
         * The shortest route through GRID CELLS is not always the shortest
         * visually smooth route across the map. This is intentional.
         */
    }

    private Vector2Int WorldPositionToCell(Vector2 worldPosition)
    {
        float offsetX = worldPosition.x - minimumGridPosition.x;
        float offsetY = worldPosition.y - minimumGridPosition.y;
        int x = Mathf.FloorToInt(offsetX / CellSize);
        int y = Mathf.FloorToInt(offsetY / CellSize);
        return new Vector2Int(x, y);
    }

    private Vector2 GetCellCenter(int x, int y)
    {
        float worldX = minimumGridPosition.x + (x + 0.5f) * CellSize;
        float worldY = minimumGridPosition.y + (y + 0.5f) * CellSize;
        return new Vector2(worldX, worldY);
    }

    private bool IsInsideGrid(Vector2Int cell)
    {
        bool xIsInside = cell.x >= 0 && cell.x < gridWidth;
        bool yIsInside = cell.y >= 0 && cell.y < gridHeight;
        return xIsInside && yIsInside;
    }

    // Convert (x, y) grid coordinates to one whole-number cell index.
    private int CellToNumber(Vector2Int cell)
    {
        return cell.y * gridWidth + cell.x;
    }

    // Convert a cell index back to (x, y) grid coordinates.
    private Vector2Int NumberToCell(int number)
    {
        int x = number % gridWidth; // % means remainder after division.
        int y = number / gridWidth; // Integer division drops decimals.
        return new Vector2Int(x, y);
    }
}
