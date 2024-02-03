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
        bool _isUpdate;
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

        public static T Create<T>(bool isUpdate = false, object owner = null) where T : StateMachine, new()
        {
            var stateMachine = new T();
            stateMachine.Init(false, isUpdate, owner);
            return stateMachine;
        }
        public static StateMachine Create(bool isUpdate = false, object owner = null)
        {
            return Create<StateMachine>(isUpdate, owner);
        }
        public static T CreateByPool<T>(bool isUpdate = false, object owner = null) where T : StateMachine, new()
        {
            var stateMachine = Pool.Get<T>();
            stateMachine.Init(true, isUpdate, owner);
            return stateMachine;
        }
        public static StateMachine CreateByPool(bool isUpdate = false, object owner = null)
        {
            return CreateByPool<StateMachine>(isUpdate, owner);
        }

        protected void Init(bool isFromPool, bool isUpdate, object owner = null)
        {
            _isFromPool = isFromPool;
            _isUpdate = isUpdate;
            Owner = owner;
        }
        public void Release()
        {
            if (_isUpdate)
            {
                GameMain.Ins.RemoveUpdate(Update);
            }
            Owner = null;
            _curNode?.Exit();
            _curNode = null;
            _preNode = null;
            _nodes.ForEachValue(x => x.Release());
            _nodes.Clear();

            if (_isFromPool)
                Pool.Push(this);

            _isFromPool = false;
            _isUpdate = false;
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
        public bool Enter<TNode>() where TNode : IStateNode
        {
            return Enter(typeof(TNode));
        }
        public bool Enter(Type entryNode)
        {
            return Enter(entryNode.Name);
        }
        public virtual bool Enter(string entryNode)
        {
            if (string.IsNullOrEmpty(entryNode))
            {
                Log.Error("节点名字为空");
                return false;
            }

            IStateNode node = GetNode(entryNode);
            if (node == null)
            {
                Log.Error($"找不到节点 : {entryNode}");
                return false;
            }
            if (!node.CheckValid())
            {
                return false;
            }
            if (_curNode != null && _curNode == node)
            {
                _curNode.Enter();
                return true;
            }

            if (_curNode != null)
            {
                //Log.Debug($"{OwnerName} 更改节点：{_curNode.Name} --> {node.Name}");
                _preNode = _curNode;
                _curNode.Exit();
            }
            else
            {
                if (_isUpdate)
                {
                    GameMain.Ins.AddUpdate(Update);
                }
                //Log.Debug($"{OwnerName} 进入节点：{node.Name}");
                _preNode = _curNode;
            }
            _curNode = node;
            _curNode.Enter();
            return true;
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
