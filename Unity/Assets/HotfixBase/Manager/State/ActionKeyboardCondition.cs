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
        public override ConditionType Condition => ConditionType.Action_Keyboard;
        public Key Key { get; }
        public TriggerType Trigger { get; }
        public ActionKeyboardCondition(Key key, TriggerType trigger)
        {
            Key = key;
            Trigger = trigger;
        }

        public override bool IsValid
        {
            get
            {
                var btn = Keyboard.current[Key];
                if (btn != null)
                {
                    switch (Trigger)
                    {
                        case TriggerType.Down:                            
                            return btn.wasPressedThisFrame;
                        case TriggerType.Up:                            
                            return btn.wasReleasedThisFrame;
                        case TriggerType.Pressed:
                            return btn.isPressed;
                    }
                }
                return false;
            }
        }
    }
}
