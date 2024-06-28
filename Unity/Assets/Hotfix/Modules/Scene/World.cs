﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ux
{
    public class World : Entity
    {
        public Scene NowScene { get; private set; }

        public void EnterScene(Scene newScene)
        {
            if (NowScene != null)
            {
                Remove(NowScene);
            }
            NowScene = newScene;
        }
        public void LeaveScene()
        {
            if (NowScene != null)
            {
                Remove(NowScene);
            }
        }

        protected override void OnDestroy()
        {
            NowScene = null;
        }
    }

}
