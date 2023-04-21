using System;
using System.Collections.Generic;

namespace Ux
{
    public class StateMachine
    {
        private readonly Dictionary<string, IStateNode> _nodes = new Dictionary<string, IStateNode>(100);
        private IStateNode _curNode;
        private IStateNode _preNode;
        bool _isFromPool;
        /// <summary>
        /// 状态机持有者
        /// </summary>
        public object Owner { private set; get; }

        string OwnerName => Owner?.GetType().Name;

        /// <summary>
        /// 当前运行的节点
        /// </summary>
        public IStateNode CurrentNode => _curNode;

        /// <summary>
        /// 上一个运行的节点
        /// </summary>
        public IStateNode PreviousNode => _preNode;

        public static StateMachine Create(object owner = null)
        {
            var stateMachine = new StateMachine();
            stateMachine.Init(false, owner);
            return stateMachine;
        }
        public static StateMachine CreateByPool(object owner = null)
        {
            var stateMachine = Pool.Get<StateMachine>();
            stateMachine.Init(true, owner);
            return stateMachine;
        }

        void Init(bool isFromPool, object owner = null)
        {
            _isFromPool = isFromPool;
            Owner = owner;
        }
        public void Release()
        {
            TimeMgr.Instance.RemoveUpdate(Update);
            Owner = null;
            _curNode?.Exit();
            _curNode = null;
            _preNode = null;
            _nodes.ForEachValue(x => x.Release());
            _nodes.Clear();

            if (_isFromPool)
                Pool.Push(this);
        }

        /// <summary>
        /// 更新状态机
        /// </summary>
        void Update()
        {
            if (_curNode != null)
                _curNode.Update();
        }
        /// <summary>
        /// 启动状态机
        /// </summary>
        public void Enter<TNode>(object args = null) where TNode : IStateNode
        {
            Enter(typeof(TNode), args);
        }
        public void Enter(Type entryNode, object args = null)
        {
            Enter(entryNode.FullName, args);
        }
        void Enter(string entryNode, object args = null)
        {
            if (string.IsNullOrEmpty(entryNode))
            {
                Log.Error("节点名字为空");
                return;
            }

            IStateNode node = GetNode(entryNode);
            if (node == null)
            {
                Log.Error($"找不到节点 : {entryNode}");
                return;
            }
            if (_curNode != null && _curNode == node)
            {
                Log.Warning($"{OwnerName} 重复进入节点：{node.Name}");
                _curNode.Enter(args);
                return;
            }

            if (_curNode != null)
            {
                Log.Debug($"{OwnerName} 更改节点：{_curNode.Name} --> {node.Name}");
                _preNode = _curNode;
                _curNode.Exit();
            }
            else
            {
                TimeMgr.Instance.DoUpdate(Update);
                Log.Debug($"{OwnerName} 进入节点：{node.Name}");
                _preNode = _curNode;
            }
            _curNode = node;
            _curNode.Enter(args);
        }

        /// <summary>
        /// 加入一个节点
        /// </summary>
        public void AddNode<TNode>(object args = null, bool isFromPool = true) where TNode : IStateNode
        {
            var stateNode = isFromPool ? Pool.Get<TNode>() : Activator.CreateInstance<TNode>();
            _AddNode(stateNode, args, isFromPool);
        }
        public void AddNode(IStateNode stateNode, object args = null)
        {
            _AddNode(stateNode, args, false);
        }
        void _AddNode(IStateNode stateNode, object args = null, bool isFromPool = true)
        {
            if (stateNode == null)
                throw new ArgumentNullException();

            var nodeName = stateNode.Name;

            if (!_nodes.ContainsKey(nodeName))
            {
                stateNode.Create(this, args, isFromPool);
                _nodes.Add(nodeName, stateNode);
            }
            else
            {
                Log.Error($"节点重复添加 : {nodeName}");
            }
        }
        private IStateNode GetNode(string nodeName)
        {
            _nodes.TryGetValue(nodeName, out IStateNode result);
            return result;
        }

        public void ForEach<T>(Action<T> action) where T : IStateNode
        {
            _nodes.ForEachValue(a => action?.Invoke((T)a));
        }
    }
}
