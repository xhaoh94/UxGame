using FairyGUI;
using FairyGUI.Utils;

namespace Ux
{
    public class UIProgressBar : UIObject
    {
        public GProgressBar ProgressBar => ObjAs<GProgressBar>();
        #region FairyGUI 属性-方法
        public ProgressTitleType titleType
        {
            get => ProgressBar.titleType;
            set => ProgressBar.titleType = value;
        }

        /// <summary>
        /// 
        /// </summary>
        public double min
        {
            get => ProgressBar.min;
            set => ProgressBar.min = value;
        }

        /// <summary>
        /// 
        /// </summary>
        public double max
        {
            get => ProgressBar.max;
            set => ProgressBar.max = value;
        }

        /// <summary>
        /// 
        /// </summary>
        public double value
        {
            get => ProgressBar.value;
            set => ProgressBar.value = value;
        }

        public bool reverse
        {
            get => ProgressBar.reverse;
            set => ProgressBar.reverse = value;
        }

        /// <summary>
        /// 动态改变进度值。
        /// </summary>
        /// <param name="val"></param>
        /// <param name="duration"></param>
        public GTweener TweenValue(double val, float duration)
        {
            return ProgressBar.TweenValue(val, duration);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newValue"></param>
        public void Update(double newValue)
        {
            ProgressBar.Update(newValue);
        }



        public void Setup_AfterAdd(ByteBuffer buffer, int beginPos)
        {
            ProgressBar.Setup_AfterAdd(buffer, beginPos);
        }
        #endregion
    }
}
