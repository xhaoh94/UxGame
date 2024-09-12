using System;
using System.Collections.Generic;

namespace Ux
{
    public interface IStateMachine
    {

    }
    public class StateMachine: IStateMachine
    {
        private readonly Dictionary<string, IStateNode> _nodes = new Dictionary<string, IStateNode>(100);
        bool _isFromPool;
        /// <summary>
        /// 状态机持有者
        /// </summary>
        public object Owner { private set; get; }

        public static T Create<T>(object owner = null) where T : StateMachine, new()
        {
            var stateMachine = new T();
            stateMachine.Init(false, owner);
            return stateMachine;
        }
        public static T CreateByPool<T>(object owner = null) where T : StateMachine, new()
        {
            var stateMachine = Pool.Get<T>();
            stateMachine.Init(true, owner);
            return stateMachine;
        }
        protected void Init(bool isFromPool, object owner = null)
        {
            _isFromPool = isFromPool;
            Owner = owner;
            OnInit();
        }
        protected virtual void OnInit() { }
        public void Release()
        {
            OnRelease();
            Owner = null;
            _nodes.ForEachValue(x => x.Release());
            _nodes.Clear();

            if (_isFromPool)
                Pool.Push(this);
            _isFromPool = false;
        }
        protected virtual void OnRelease() { }

        protected virtual bool IsCanEnter(IStateNode node) { return true; }
        protected virtual bool IsCanExit(IStateNode node) { return true; }
        /// <summary>
        /// 启动状态机
        /// </summary>
        public void Enter<TNode>() where TNode : IStateNode
        {
            Enter(typeof(TNode).Name);
        }


        public void Enter(string nodeName)
        {
            if (string.IsNullOrEmpty(nodeName))
            {
                Log.Error("节点名字为空");
                return;
            }

            IStateNode node = GetNode(nodeName);
            if (node == null)
            {
                Log.Error($"找不到节点 : {nodeName}");
                return;
            }
            Enter(node);
        }
        public void Enter(IStateNode node)
        {
            if (!IsCanEnter(node))
            {
                return;
            }

            OnEnter(node);
            node.Enter();
        }
        protected virtual void OnEnter(IStateNode node)
        {

        }

        public void Exit<TNode>() where TNode : IStateNode
        {
            Exit(typeof(TNode).Name);
        }
        public void Exit(string nodeName)
        {
            if (string.IsNullOrEmpty(nodeName))
            {
                Log.Error("节点名字为空");
                return;
            }

            IStateNode node = GetNode(nodeName);
            if (node == null)
            {
                Log.Error($"找不到节点 : {nodeName}");
                return;
            }
            Exit(node);
        }
        public void Exit(IStateNode node)
        {
            if (!IsCanExit(node))
            {
                return;
            }

            OnExit(node);
            node.Exit();
        }
        protected virtual void OnExit(IStateNode node)
        {

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
        public IStateNode GetNode(string nodeName)
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
