using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FairyGUI;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEditor;

namespace Ux
{
    public enum UIState
    {
        Show,
        ShowAnim,
        Hide,
        HideAnim
    }
    public interface IUIAsync
    {
        void Change(bool b);
    }
    public class UIObject
    {
        private UIEvent _event;
        private List<UIObject> _components;
        public List<UIObject> Components => _components ??= new List<UIObject>();
        private UIState _state = UIState.Hide;
        public virtual UIState State
        {
            get
            {
                foreach (var component in Components)
                {
                    var state = component.State;
                    if (state is UIState.ShowAnim or UIState.HideAnim)
                    {
                        return state;
                    }
                }
                return _state;
            }
        }

        public virtual UIObject Parent { get; private set; }
        public GObject GObject { get; private set; }

        IUIParam _paramVo;
        IUIParam ParamVo
        {
            get => _paramVo;
            set
            {
                //只有UIBase的界面，才回收
                if (this is UIBase)
                {
                    _paramVo?.Release();
                }
                _paramVo = value;
            }
        }
        protected bool TryGetParam<V>(out V value, UIParamType type = UIParamType.A)
        {
            if (_paramVo == null)
            {
                value = default;
                return false;
            }
            return ParamVo.TryGet(out value, type);
        }


        /// <summary>
        /// 是否可以使用特性快速注册事件函数        
        /// </summary>
        protected virtual bool IsRegisterFastMethod => true;
        /// <summary>
        /// 显示动效
        /// </summary>
        protected virtual IUIAnim ShowAnim { get; set; } = null;

        /// <summary>
        /// 关闭动效
        /// </summary>
        protected virtual IUIAnim HideAnim { get; set; } = null;

        protected Action<int, IUIParam, bool> OnShowCallBack;
        protected Action OnHideCallBack;

        protected T ParentAs<T>() where T : UIObject
        {
            return Parent as T;
        }

        protected T ObjAs<T>() where T : GObject
        {
            return GObject as T;
        }

        protected void Init(GObject gObj, UIObject parent = null)
        {
            if (gObj == null || GObject != null) return;
            _event ??= Pool.Get<UIEvent>();           
            GObject = gObj;
            Parent = parent;
            CreateChildren();
            OnInit();
        }


        protected virtual void OnInit()
        {
        }
        void _SetInfo(MemberInfo info, GObject child)
        {
            if (info == null) return;
            object[] attributes = info.GetCustomAttributes(typeof(UIComponentAttribute), true);
            if (attributes.Length == 0)
            {
                if (info is FieldInfo field)
                {
                    field.SetValue(this, child);
                }
                else if (info is PropertyInfo property)
                {
                    property.SetValue(this, child);
                }
            }
            else
            {
                var attribute = (UIComponentAttribute)attributes[0];
                UIObject com = null;
                if (attribute.Component != null)
                {
                    com = (UIObject)Activator.CreateInstance(attribute.Component);
                }
                else
                {
                    if (info is FieldInfo field)
                    {
                        com = (UIObject)Activator.CreateInstance(field.FieldType);
                    }
                    else if (info is PropertyInfo property)
                    {
                        com = (UIObject)Activator.CreateInstance(property.PropertyType);
                    }
                }
                if (com != null)
                {
                    com.Init(child, this);
                    if (info is FieldInfo field)
                    {
                        field.SetValue(this, com);
                    }
                    else if (info is PropertyInfo property)
                    {
                        property.SetValue(this, com);
                    }
                    Components.Add(com);
                }
            }
        }

        void _SetInfo(MemberInfo info, object child)
        {
            if (info == null) return;

            if (info is FieldInfo field)
            {
                field.SetValue(this, child);
            }
            else if (info is PropertyInfo property)
            {
                property.SetValue(this, child);
            }
        }

        void _ToCreateChildren(Type viewType, GComponent component)
        {
            for (int i = 0; i < component.numChildren; i++)
            {
                var child = component.GetChildAt(i);
                const BindingFlags flag = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
                MemberInfo info;
                info = viewType.GetField(child.name, flag);
                if (info == null)
                {
                    info = viewType.GetProperty(child.name, flag);
                }
                _SetInfo(info, child);
            }

            for (int i = 0; i < component.Controllers.Count; i++)
            {
                var cont = component.GetControllerAt(i);
                const BindingFlags flag = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
                MemberInfo info;
                info = viewType.GetField(cont.name, flag);
                if (info == null)
                {
                    info = viewType.GetProperty(cont.name, flag);
                }
                _SetInfo(info, cont);
            }

            for (int i = 0; i < component.Transitions.Count; i++)
            {
                var trans = component.GetTransitionAt(i);
                const BindingFlags flag = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
                MemberInfo info;
                info = viewType.GetField(trans.name, flag);
                if (info == null)
                {
                    info = viewType.GetProperty(trans.name, flag);
                }
                _SetInfo(info, trans);
            }
        }
        /// <summary>
        /// 创建子项，默认采用反射，使用代码生成会重写方法，采用API获取
        /// </summary>
        protected virtual void CreateChildren()
        {
            var component = GObject is Window ? ObjAs<Window>().contentPane : ObjAs<GComponent>();
            if (component == null) return;            
            try
            {
                _ToCreateChildren(GetType(), component);
            }
            catch
            {
                Log.Error("ToCreateChildren失败,检查是否存在不规范的命名");
            }
        }

        protected virtual void ToShow(bool isAnim, int id, IUIParam param, bool checkStack, CancellationTokenSource token)
        {
            HideAnim?.Stop();
            if (isAnim && ShowAnim != null)
            {
                _state = UIState.ShowAnim;
                ShowAnim?.SetToStart();
                ShowAnim?.Play(() => { _state = UIState.Show; });
            }
            else
            {
                _state = UIState.Show;
                ShowAnim?.SetToEnd();
            }
            RemoveTag();
            if (IsRegisterFastMethod)
            {
                EventMgr.Ins.RegisterFastMethod(this);
            }
            OnAddEvent();
            foreach (var component in Components)
            {
                component.ToShow(isAnim, id, param, checkStack, token);
            }
            ParamVo = param;
            OnShow();
            _CheckShow(id, param, checkStack, token).Forget();
        }
        void _ChangeAsync(bool b)
        {
            if (this is IUIAsync async)
            {
                async.Change(b);
            }
        }
        async UniTaskVoid _CheckShow(int id, IUIParam param, bool checkStack, CancellationTokenSource token)
        {
            _ChangeAsync(true);
            bool isCanceled = false;
            while (State != UIState.Show || Parent is { State: UIState.ShowAnim })
            {
                if (token != null && !isCanceled)
                {
                    isCanceled = await UniTask.Yield(token.Token).SuppressCancellationThrow();
                    if (isCanceled)
                    {
                        ShowAnim?.SetToEnd();
                        _state = UIState.Show;                           
                    }
                }
                else
                {
                    await UniTask.Yield();
                }
            }
            _ChangeAsync(false);
            if (isCanceled)
            {
                return;
            }
            OnShowAnimComplete();
            OnShowCallBack?.Invoke(id, param, checkStack);
        }

        protected virtual void ToOverwrite(IUIParam param)
        {
            ParamVo = param;
            foreach (var component in Components)
            {
                component.ToOverwrite(ParamVo);
            }
            OnOverwrite();
        }
        protected virtual void OnOverwrite()
        {
        }
        protected virtual void OnShow()
        {
        }

        protected virtual void OnShowAnimComplete()
        {
        }

        protected virtual void OnAddEvent()
        {

        }

        protected virtual void ToHide(bool isAnim, bool checkStack, CancellationTokenSource token)
        {
            ParamVo = null;
            ShowAnim?.Stop();
            if (isAnim && HideAnim != null)
            {
                _state = UIState.HideAnim;
                HideAnim?.SetToStart();
                HideAnim?.Play(() => { _state = UIState.Hide; });
            }
            else
            {
                _state = UIState.Hide;
                HideAnim?.SetToEnd();
            }

            foreach (var component in Components)
            {
                component.ToHide(isAnim, checkStack, token);
            }

            OnHide();
            RemoveTag();
            _CheckHide(token).Forget();
        }

        async UniTaskVoid _CheckHide(CancellationTokenSource token)
        {
            _ChangeAsync(true);
            bool isCanceled = false;
            while (State != UIState.Hide || Parent is { State: UIState.HideAnim })
            {
                if (token != null && !isCanceled)
                {
                    isCanceled = await UniTask.Yield(token.Token).SuppressCancellationThrow();
                    if (isCanceled)
                    {
                        HideAnim?.SetToEnd();
                        _state = UIState.Hide;                        
                    }
                }
                else
                {
                    await UniTask.Yield();
                }
            }
            _ChangeAsync(false);
            if (isCanceled)
            {
                return;
            }
            OnHideAnimComplete();
            OnHideCallBack?.Invoke();
        }

        /// <summary>
        /// 注意！！！
        /// 不要在这里注册定时器或是监听事件。
        /// 在这里注册的定时器或是事件，都会流程给清空的
        /// </summary>
        protected virtual void OnHide()
        {
        }
        /// <summary>
        /// 注意！！！
        /// 不要在这里注册定时器或是监听事件。
        /// 在这里注册的定时器或是事件，都会流程给清空的
        /// </summary>
        protected virtual void OnHideAnimComplete()
        {
        }

        protected void ToDispose(bool isDisposeGObject = true)
        {
            _event.Release();
            _event = null;
            OnShowCallBack = null;
            OnHideCallBack = null;
            if (_components != null)
            {
                foreach (var component in _components)
                {
                    component.ToDispose(isDisposeGObject);
                }

                _components.Clear();
                _components = null;
            }
            OnDispose();
            if (isDisposeGObject) GObject?.Dispose();
            GObject = null;
            Parent = null;
        }

        protected virtual void OnDispose()
        {
        }


        #region 事件
        protected void AddEvent(UIObject gObj, string type, EventCallback1 fn)
        {
            if (gObj == null) return;
            AddEvent(gObj.GObject, type, fn);
        }

        protected void AddEvent(UIObject gObj, string type, EventCallback0 fn)
        {
            if (gObj == null) return;
            AddEvent(gObj.GObject, type, fn);
        }
        protected void AddEvent(GObject gObj, string type, EventCallback1 fn)
        {
            if (gObj == null) return;
            AddEvent(new EventListener(gObj, type), fn);
        }

        protected void AddEvent(GObject gObj, string type, EventCallback0 fn)
        {
            if (gObj == null) return;
            AddEvent(new EventListener(gObj, type), fn);
        }

        protected void AddEvent(EventListener listener, EventCallback1 fn)
        {
            _event?.AddEvent(listener, fn);
        }

        protected void AddEvent(EventListener listener, EventCallback0 fn)
        {
            _event?.AddEvent(listener, fn);
        }
        protected void AddMultipleClick(UIObject gObj, EventCallback0 fn0, int clickCnt = 2, float gapTime = 0.3f)
        {
            if (gObj == null) return;
            AddMultipleClick(gObj.GObject, fn0, clickCnt, gapTime);
        }
        /// <summary>
        /// 多次点击事件，注册了多次点击事件，即使是单击也会受到gapTime延时触发
        /// </summary>        
        protected void AddMultipleClick(GObject gObj, EventCallback0 fn0, int clickCnt = 2, float gapTime = 0.3f)
        {
            if (gObj == null) return;
            _event?.AddMultipleClick(gObj, fn0, clickCnt, gapTime);
        }
        protected void AddMultipleClick(UIObject gObj, EventCallback1 fn1, int clickCnt = 2, float gapTime = 0.3f)
        {
            if (gObj == null) return;
            AddMultipleClick(gObj.GObject, fn1, clickCnt, gapTime);
        }
        /// <summary>
        /// 多次点击事件，注册了多次点击事件，即使是单击也会受到gapTime延时触发
        /// </summary>    
        protected void AddMultipleClick(GObject gObj, EventCallback1 fn1, int clickCnt = 2, float gapTime = 0.3f)
        {
            if (gObj == null) return;
            _event?.AddMultipleClick(gObj, fn1, clickCnt, gapTime);
        }

        protected void AddLongPress(UIObject gObj, Func<bool> fn, float delay = 0.2f, int loopCnt = 0,
       int holdRangeRadius = 50)
        {
            if (gObj == null) return;
            AddLongPress(gObj.GObject, fn, delay, loopCnt, holdRangeRadius);
        }
        /// <summary>
        /// 长按事件
        /// </summary>
        /// <param name="gObj"></param>
        /// <param name="fn"></param>        
        /// <param name="delay">后续触发间隔</param>
        /// <param name="loopCnt">触发次数</param>
        /// <param name="holdRangeRadius">手指触发范围</param>
        protected void AddLongPress(GObject gObj, Func<bool> fn, float delay = 0.2f, int loopCnt = 0,
            int holdRangeRadius = 50)
        {
            if (gObj == null) return;
            _event?.AddLongPress(gObj, fn, -1, delay, loopCnt, holdRangeRadius);
        }
        protected void AddLongPress(UIObject gObj, float first, Func<bool> fn, float delay = 0.2f, int loopCnt = 0,
            int holdRangeRadius = 50)
        {
            if (gObj == null) return;
            AddLongPress(gObj.GObject, first, fn, delay, loopCnt, holdRangeRadius);
        }
        /// <summary>
        /// 长按事件
        /// </summary>
        /// <param name="gObj"></param>
        /// <param name="fn"></param>
        /// <param name="first">第一次触发时间</param>
        /// <param name="delay">后续触发间隔</param>
        /// <param name="loopCnt">触发次数</param>
        /// <param name="holdRangeRadius">手指触发范围</param>
        protected void AddLongPress(GObject gObj, float first, Func<bool> fn, float delay = 0.2f, int loopCnt = 0,
            int holdRangeRadius = 50)
        {
            if (gObj == null) return;
            _event?.AddLongPress(gObj, fn, first, delay, loopCnt, holdRangeRadius);
        }
        protected void AddClick(UIObject gObj, EventCallback1 fn)
        {
            if (gObj == null) return;
            AddClick(gObj.GObject, fn);
        }
        protected void AddClick(GObject gObj, EventCallback1 fn)
        {
            if (gObj == null) return;
            AddEvent(gObj.onClick, fn);
        }
        protected void AddClick(UIObject gObj, EventCallback0 fn)
        {
            if (gObj == null) return;
            AddClick(gObj.GObject, fn);
        }
        protected void AddClick(GObject gObj, EventCallback0 fn)
        {
            if (gObj == null) return;
            AddEvent(gObj.onClick, fn);
        }

        protected void AddItemClick(UIList list, Action<IItemRenderer> fn)
        {
            if (list == null) return;
            list.AddItemClick(fn);
        }

        protected void AddItemClick(GList list, EventCallback1 fn)
        {
            if (list == null) return;
            AddEvent(list.onClickItem, fn);
        }

        protected void AddItemClick(GList list, EventCallback0 fn)
        {
            if (list == null) return;
            AddEvent(list.onClickItem, fn);
        }

        #endregion

        protected void RemoveTag()
        {
            _event?.RemoveEvents();
            EventMgr.Ins.OffTag(this);
            TimeMgr.Ins.RemoveTag(this);
        }
    }
}