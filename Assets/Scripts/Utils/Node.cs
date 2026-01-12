using UnityEngine;

public class Node : IHeapItem<Node>
{
    public bool walkable;       // 能否行走
    public Vector3 worldPosition; // 世界坐标
    public int gridX;           // 网格X坐标
    public int gridY;           // 网格Y坐标

    public int gCost;           // 距离起点的代价
    public int hCost;           // 距离终点的预估代价
    public Node parent;         // 父节点（用于回溯路径）

    private int heapIndex;      // 在堆中的索引

    public Node(bool _walkable, Vector3 _worldPos, int _gridX, int _gridY)
    {
        walkable = _walkable;
        worldPosition = _worldPos;
        gridX = _gridX;
        gridY = _gridY;
    }

    public int fCost { get { return gCost + hCost; } }

    public int HeapIndex { get { return heapIndex; } set { heapIndex = value; } }

    // 实现比较接口，让Heap知道谁的代价更小
    public int CompareTo(Node nodeToCompare)
    {
        int compare = fCost.CompareTo(nodeToCompare.fCost);
        if (compare == 0)
        {
            compare = hCost.CompareTo(nodeToCompare.hCost);
        }
        return -compare;
    }
}
