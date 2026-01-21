using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    class Node
    {
        public Node Parent { get; private set; }
        public int Key { get; private set; }
        public Vector2Int Pos { get; private set; }
        public Vector2Int Start { get; private set; }
        public Vector2Int End { get; private set; }
        public float G { get; private set; }
        public float F => G + _h;
        float _h;
        public void Init(int key, Vector2Int pos, Vector2Int start, Vector2Int end, float g = -1)
        {
            Key = key;
            Pos = pos;
            Start = start;
            End = end;
            var x = pos.x - end.x;
            var y = pos.y - end.y;
            _h = x * x + y * y;
            G = g;
        }
        public void Release()
        {
            Pool.Push(this);
        }

        public void Set(float g, Node parent)
        {
            G = g;
            Parent = parent;
        }
        public bool IsGoal => Pos.x == End.x && Pos.y == End.y;
    }
    public class AStart
    {
        static int[,] _directions = { { -1, 0 }, { 1, 0 }, { 0, -1 }, { 0, 1 } };
        static int _OpenCompare(Node a, Node b)
        {
            if (a.F < b.F) return 1;
            if (a.F > b.F) return -1;
            return 0;
        }

        int[,] _maps;
        BinaryHeap<Node> _opens = new BinaryHeap<Node>(_OpenCompare);
        HashSet<int> _closes = new();
        Dictionary<int, Node> _nodes = new();
        List<Node> _neighbors = new();
        public void Init(int[,] maps)
        {
            _maps = maps;
        }
        public void Release()
        {
            _Reset();
            Pool.Push(this);
        }
        public List<Vector2Int> Find(Vector2Int start, Vector2Int end)
        {
            if (_maps == null)
            {
                Log.Error("未初始化寻路数据");
                return null;
            }
            var node = Pool.Get<Node>();
            node.Init(_GetKey(start.x, start.y), start, start, end, 0);
            _nodes.Add(node.Key, node);
            _opens.Push(node);
            _closes.Add(node.Key);
            Node goal = null;
            while (_opens.Count > 0)
            {
                var cur = _opens.Pop();
                if (cur.IsGoal)
                {
                    // 找到目标                    
                    goal = cur;
                    break;
                }
                _FindNeighbors(cur, start, end);
                foreach (var neighbor in _neighbors)
                {
                    _closes.Add(neighbor.Key);
                    var tG = cur.G + 1;
                    if (neighbor.G == -1 || tG < neighbor.G)
                    {
                        neighbor.Set(tG, cur);
                        _opens.Push(neighbor);
                    }
                }
            }
            return _BuildPath(goal);
        }
        void _FindNeighbors(Node node, Vector2Int start, Vector2Int end)
        {
            _neighbors.Clear();
            for (int i = 0; i < _directions.GetLength(0); i++)
            {
                var x = node.Pos.x + _directions[i, 0];
                var y = node.Pos.y + _directions[i, 1];
                if (x < 0 || x >= _maps.GetLength(0)) continue;
                if (y < 0 || y >= _maps.GetLength(1)) continue;
                if (_maps[x, y] == 0)
                {
                    continue;
                }
                var key = _GetKey(x, y);
                if (_closes.Contains(key))
                {
                    continue;
                }
                if (!_nodes.TryGetValue(key, out var neighbor))
                {
                    neighbor = Pool.Get<Node>();
                    neighbor.Init(key, new Vector2Int(x, y), start, end);
                    _nodes.Add(key, neighbor);
                }
                _neighbors.Add(neighbor);
            }
        }
        List<Vector2Int> _BuildPath(Node node)
        {
            List<Vector2Int> path = null;
            while (node != null && node.Parent != null)
            {
                path ??= new();
                path.Insert(0, node.Pos);
                node = node.Parent;
            }
            Release();
            return path;
        }
        int _GetKey(int x, int y)
        {
            return x * 10000000 + y;
        }

        void _Reset()
        {
            _closes.Clear();
            _opens.Clear();
            _maps = null;
            foreach (var (k, node) in _nodes)
            {
                node.Release();
            }
            _nodes.Clear();
        }


    }
}

