using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ux.UI
{
    partial class TestItem
    {

        protected override void OnShow()
        {
            if (TryGetParam(out int index))
            {
                txtNum.text = index.ToString();
            }
            if (Index == 0)
            {
                EventMgr.Ins.On(11111111, this, test);
            }
        }

        void test()
        {
            Log.Debug("xxxxxxxxxx111 == " + Index);
            Log.Debug("xxxxxxxxxx222 == " + txtNum.text);
        }
    }

    partial class Test22Item
    {

        protected override void OnShow()
        {
            if (TryGetParam(out int index))
            {
                txtNum.text = index.ToString();
            }
        }
    }
}
