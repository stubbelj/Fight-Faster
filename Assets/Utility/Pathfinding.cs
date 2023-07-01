using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System;
using System.Threading;

public class Pathfinding {
    Vector2Int worldSize = new Vector2Int(50, 50);
    int cellSize = 16;
    public static List<List<bool>> groundMask = new List<List<bool>>();
    public static List<List<bool>> traversableMask = new List<List<bool>>();

    List<List<List<Cell>>> cellGrid;
    int maxJumpVal;
    int[] startCoords;
    int[] endCoords;
    Cell startCell;
    Cell endCell;
    List<Cell> openList;
    List<Cell> closedList;

    public void GenerateCollisionMasks(Tilemap groundTilemap, Tilemap traversableTilemap) {
        //generate a new mask for collision
        for (int i = 0; i < worldSize.x * 2; i ++) {
            groundMask.Add(new List<bool>());
            traversableMask.Add(new List<bool>());
            for (int j = 0; j < worldSize.y * 2; j ++) {
                groundMask[i].Add(false);
                traversableMask[i].Add(false);
            }
        }
        for (int i = -worldSize.x; i < worldSize.x; i++) {
            for (int j = -worldSize.y; j < worldSize.y; j++) {
                groundMask[i + worldSize.x][j + worldSize.y] = groundTilemap.GetTile(new Vector3Int(i, j, 0)) != null;
                traversableMask[i + worldSize.x][j + worldSize.y] = traversableTilemap.GetTile(new Vector3Int(i, j, 0)) != null;
            }
        }
    }

    class Cell {
        public int x;
        public int y;
        public bool ground;
        public bool traversable;
        public float g; //distance from start node
        public float h; //distance from end node
        public float f; //g + h
        public Cell prev;

        public int jumpVal;
    };

    public void InitPathFind() {
        cellGrid = new List<List<List<Cell>>>();

        for (int i = 0; i < worldSize.x * 2; i ++) {
            cellGrid.Add(new List<List<Cell>>());
            for (int j = 0; j < worldSize.y * 2; j ++) {
            cellGrid[i].Add(new List<Cell>());
                for (int k = 0; k <= worldSize.y; k ++) {
                    cellGrid[i][j].Add(new Cell());
                }
            }
        }

        for (int i = 0; i < worldSize.x * 2; i ++) {
            for (int j = 0; j < worldSize.y * 2; j ++) {
                for (int k = 0; k <= worldSize.y; k ++) {
                    cellGrid[i][j][k].x = i;
                    cellGrid[i][j][k].y = j;
                    cellGrid[i][j][k].ground = groundMask[i][j];
                    cellGrid[i][j][k].traversable = !traversableMask[i][j];
                    cellGrid[i][j][k].g = CellDistance(cellGrid[i][j][0], cellGrid[startCell.x][startCell.y][0]);
                    cellGrid[i][j][k].h = CellDistance(cellGrid[i][j][0], endCell);
                    cellGrid[i][j][k].f = cellGrid[i][j][0].g + cellGrid[i][j][0].h;
                    cellGrid[i][j][k].jumpVal = k;
                }
            }
        }

        openList.Add(cellGrid[startCell.x][startCell.y][0]); //adding start cell

    }

    public List<Vector3> PathFind(Vector3 startPos, Vector3 endPos, float initialJumpVal) {

        maxJumpVal = 0;
        while (initialJumpVal >= cellSize) {
            initialJumpVal -= cellSize;
            maxJumpVal++;
        }

        maxJumpVal = 6;

        startCoords = WorldPointToCellCoords(startPos);
        endCoords = WorldPointToCellCoords(endPos);
        startCell = cellGrid[startCoords[0] + worldSize.x][startCoords[1] + worldSize.y][0];
        endCell = cellGrid[endCoords[0] + worldSize.x][endCoords[1] + worldSize.y][0];
        openList = new List<Cell>();
        closedList = new List<Cell>();

        startCell.x = startCoords[0] + worldSize.x;
        startCell.y = startCoords[1] + worldSize.y;
        endCell.x = endCoords[0] + worldSize.x;
        endCell.y = endCoords[1] + worldSize.y;

        int iterations = 0;

        while (iterations < 50000) {
            Cell current = openList[0];

            foreach (Cell openCell in openList) {
                if (openCell.f < current.f) {
                    current = openCell;
                }
            }

            openList.Remove(current);
            closedList.Add(current);

            if (current == endCell) {
                List<Vector3> path = new List<Vector3>();
                while (current != startCell) {
                    path.Add(CellCoordsToWorldPoint(current));
                    current = current.prev;
                }
                path.Reverse();
                Debug.Log("found path in " + iterations + " iterations");
                return path;
            }

            foreach (Cell neighbor in Neighbors(current)) {
                if (!Traversable(neighbor) || closedList.Contains(neighbor)) {
                    continue;
                }

                float newPath = current.g + CellDistance(neighbor, current);
                if (newPath <= neighbor.g || !openList.Contains(neighbor)) {
                    neighbor.g = newPath;
                    neighbor.prev = current;
                    if (!openList.Contains(neighbor)) {
                        openList.Add(neighbor);
                    }
                }
            }
            iterations++;
        }

        if (iterations == 50000) {
            Debug.Log("failed to find path in less than 40000 iterations");
        }
        return new List<Vector3>();

    }

    float Vector3Distance(Vector3 startPos, Vector3 endPos) {
            return Mathf.Abs((endPos - startPos).magnitude);
        }

        float CellDistance(Cell startCell, Cell endCell) {
            if (startCell == endCell) {
                return 0;
            }
            Vector3 startPos = CellCoordsToWorldPoint(startCell);
            Vector3 endPos = CellCoordsToWorldPoint(endCell);
            return Mathf.Abs((endPos - startPos).magnitude);
        }

        bool Traversable(Cell cell) {
            return cell.traversable;
        }

        List<Cell> Neighbors(Cell cell) {
            List<Cell> neighbors = new List<Cell>();
            if (cell.jumpVal < worldSize.y) {
                if (cell.x > 0 && cell.jumpVal % 2 == 0) {
                    if (cellGrid[cell.x - 1][cell.y - 1][0].ground) {
                        neighbors.Add(cellGrid[cell.x - 1][cell.y][0]);
                    } else {
                        neighbors.Add(cellGrid[cell.x - 1][cell.y][cell.jumpVal + 1]);
                    }
                }

                if (cell.x < worldSize.x * 2 && cell.jumpVal % 2 == 0) {
                    if (cellGrid[cell.x + 1][cell.y - 1][0].ground) {
                        neighbors.Add(cellGrid[cell.x + 1][cell.y][0]);
                    } else {
                        neighbors.Add(cellGrid[cell.x + 1][cell.y][cell.jumpVal + 1]);
                    }
                }

                if (cell.jumpVal < maxJumpVal) {
                    if (cell.y < worldSize.y * 2) {
                        if (cellGrid[cell.x][cell.y][0].ground) {
                            neighbors.Add(cellGrid[cell.x][cell.y + 1][0]);
                        } else {
                            if ((cell.jumpVal % 2 == 0 && cell.jumpVal + 2 <= maxJumpVal)) {
                                neighbors.Add(cellGrid[cell.x][cell.y + 1][cell.jumpVal + (cell.jumpVal % 2 == 0 ? 2 : 1)]);
                            }
                        }
                    }
                }

                if (cell.y > 0) {
                    neighbors.Add(cellGrid[cell.x][cell.y - 1][cell.jumpVal + (cell.jumpVal % 2 == 0 ? 2 : 1)]);
                }
            }

            return neighbors;
        }

        int[] WorldPointToCellCoords(Vector3 pos) {
            Vector3Int temp = new Vector3Int(Mathf.FloorToInt(pos.x / cellSize), Mathf.FloorToInt(pos.y / cellSize), 0);
            return new int[]{temp.x, temp.y};
        }

        Vector3 CellCoordsToWorldPoint(Cell cell) {
            return new Vector3((cell.x - worldSize.x) * cellSize + cellSize / 2, (cell.y - worldSize.y) * cellSize + cellSize / 2, 0);
        }
}
