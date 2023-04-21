using System.Collections.Generic;

namespace Ux
{
    public abstract class TagBase
    {
        public int TagId { get; private set; }
        private bool _inited;
        private bool _isRed;
        private int _redNum = -1;

        protected object _data;
        public List<TagBase> _parents;

        /// <summary>
        /// 消耗 如果有消耗会优先判断是否满足消耗，不满足的话是不会走OnCheck的
        /// </summary>
        /// <returns></returns>
        //protected virtual IList<CostSO> CostSOs() { return null; }
        /// <summary>
        /// 开启条件 如果有开启条件会判断是否满足开启条件，不满足不走OnCheck.且条件满足后会自动更新状态
        /// </summary>
        /// <returns></returns>
        //protected abstract IList<string> Conditions();

        /// <summary>
        /// 事件列表，用于驱动红点是否需要重新检测
        /// </summary>
        /// <returns></returns>
        protected abstract IList<int> EvtTypes();

        protected abstract bool OnCheck();

        public bool IsRed()
        {
            return _isRed;
        }

        public int RedNum()
        {
            return _redNum;
        }

        public void Init(int tagId, object args)
        {
            if (this._inited) return;
            TagId = tagId;
            _data = args;
            _inited = true;
            var mcs = this.EvtTypes();
            if (mcs is { Count: > 0 })
            {
                foreach (var mc in mcs)
                {
                    EventMgr.Instance.On(mc, _DoMessage);
                }
            }

            OnInit();
            _DoMessage();
        }

        protected virtual void OnInit()
        {
        }

        private long timeKey;

        protected void _DoMessage()
        {
            if (timeKey != 0)
            {
                return;
            }

            timeKey = TimeMgr.Instance.DoOnce(0.2f, _Check);
        }

        private bool isUnLock;
        private bool isAddListener;

        private void _Check()
        {
            timeKey = 0;

            // bool b = true;
            //TODO 判断是否功能已解锁
            // if (!b)
            // {
            //     CheckRed(false);
            //     return;
            // }
            
            //TODO 判断是否消耗不足
            // if (!b)
            // {
            //     CheckRed(false);
            //     return;
            // }
            CheckRed(OnCheck());
        }

        private void CheckRed(bool b)
        {
            bool isChanged = false;
            if (b != _isRed)
            {
                isChanged = true;
                _isRed = b;
                TagMgr.Instance.UpdateStatusByTag(this);
            }

            int num = CheckRedNum();
            if (num != _redNum)
            {
                isChanged = true;
                _redNum = num;
                TagMgr.Instance.UpdateNumByTag(this);
            }

            if (!isChanged || _parents is not { Count: > 0 }) return;
            foreach (var par in _parents)
            {
                par._DoMessage();
            }
        }

        protected T GetData<T>()
        {
            if (this._data != null) return (T)this._data;
            else return default(T);
        }

        protected virtual int CheckRedNum()
        {
            return _isRed ? 1 : 0;
        }

        public virtual void Clear()
        {
            if (timeKey != 0)
            {
                TimeMgr.Instance.RemoveKey(timeKey);
            }

            EventMgr.Instance.OffAll(this);
            TagMgr.Instance.Off(this);
            _parents?.Clear();
        }
    }
}