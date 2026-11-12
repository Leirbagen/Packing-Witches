using Fusion;
using UnityEngine;

public class BoxGrid : NetworkBehaviour
{
    public int width = 4;
    public int height = 4;
    public float cellSize = 0.1f;
    public Transform gridOrigin;
    [Networked, Capacity(64)] private NetworkArray<int> Cells => default;
    [Networked] private int NextPlacementId { get; set; } = 1;
    
    private int Index(int x, int y)
    {
        return y * width + x;
    }

    public bool IsInside(int x, int y)
    {
        if (x < 0 || y < 0)
        {
            return false;
        } 
        if (x >= width || y >= height)
        {
            return false;
        } 
        return true;
    }
    
    public bool IsFree(int x, int y)
    {
        if (!IsInside(x, y))
        {
            return false;
        } 
        return Cells[Index(x, y)] == 0;
    }

    public Vector2Int GetSize(Vector2Int footprint, int rotation)
    {
        if (rotation % 2 == 0)
        {
            return footprint;
        }
        return new Vector2Int(footprint.y, footprint.x);
    }

    public bool CanPlace(Vector2Int size, int x, int y)
    {
        for (int dy = 0; dy < size.y; dy++)
        {
            for (int dx = 0; dx < size.x; dx++)
            {
                if (!IsFree(x + dx, y + dy))
                {
                    return false;
                }
            }
        }
        return true;
    }

    public int Place(Vector2Int size, int x, int y)
    {
        if (!HasStateAuthority)
        {
            return -1;
        } 
        if (!CanPlace(size, x, y))
        {
            return -1;
        }

        int placementId = NextPlacementId;
        NextPlacementId++;

        for (int dy = 0; dy < size.y; dy++)
        {
            for (int dx = 0; dx < size.x; dx++)
            {
                Cells.Set(Index(x + dx, y + dy), placementId);
            }
        }
        return placementId;
    }
    public bool WorldToCell(Vector3 worldPoint, out int x, out int y)
    {
        Vector3 local = gridOrigin.InverseTransformPoint(worldPoint);
        x = Mathf.FloorToInt(local.x / cellSize);
        y = Mathf.FloorToInt(local.z / cellSize);
        return IsInside(x, y);
    }

    public Vector3 FootprintCenterWorld(Vector2Int size, int x, int y)
    {
        Vector3 local = new Vector3((x + size.x * 0.5f) * cellSize, 0f, (y + size.y * 0.5f) * cellSize);
        return gridOrigin.TransformPoint(local);
    }

    private void OnDrawGizmos()
    {
        if (gridOrigin == null) return;

        Gizmos.matrix = gridOrigin.localToWorldMatrix;
        Gizmos.color = Color.cyan;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3 center = new Vector3((x + 0.5f) * cellSize, 0f, (y + 0.5f) * cellSize);
                Gizmos.DrawWireCube(center, new Vector3(cellSize, 0.01f, cellSize));
            }
        }
    }
}
