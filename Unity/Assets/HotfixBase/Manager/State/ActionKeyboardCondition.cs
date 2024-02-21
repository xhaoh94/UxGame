using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.InputSystem;

namespace Ux
{
    public class ActionKeyboardCondition : StateConditionBase
    {
        public override Type ConditionType => Type.Action_Keyboard;
        public Key Key { get; }
        public Trigger Tri { get; }
        public ActionKeyboardCondition(Key key, Trigger trigger)
        {
            this.Key = key;
            this.Tri = trigger;
        }

        public override bool IsValid
        {
            get
            {
                var btn = Keyboard.current[Key];
                if (btn != null)
                {
                    switch (Tri)
                    {
                        case Trigger.Down:                            
                            return btn.wasPressedThisFrame;
                        case Trigger.Up:                            
                            return btn.wasReleasedThisFrame;
                        case Trigger.Pressed:
                            return btn.isPressed;
                    }
                }
                return false;
            }
        }
    }
}
