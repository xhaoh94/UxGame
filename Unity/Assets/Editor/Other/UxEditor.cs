using Cysharp.Threading.Tasks;
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Ux.Editor.Build.Config;
using Ux.Editor.Build.Proto;
using Ux.Editor.Build.UI;
namespace Ux.Editor
{
    public class UxEditor
    {
        public static async UniTask<bool> Export(bool _ui = true, bool _config = true, bool _proto = true, bool _isRefresh = true)
        {
            if (_ui)
            {
                //生成YooAsset UI收集器配置
                UIClassifyWindow.CreateYooAssetUIGroup();
                //生成UI代码文件
                if (!UICodeGenWindow.Export())
                {
                    return false;
                }
            }

            if (_config)
            {
                //生成配置代码文件
                await ConfigWindow.Export();
            }

            if (_proto)
            {
                //生成协议文件
                await ProtoWindow.Export();
            }

            if (_isRefresh) AssetDatabase.Refresh();

            return true;
        }

        [MenuItem("UxGame/初始化", false, 10)]
        public static void Init()
        {
            //Export().Forget();
            //var test = new BinaryHeap<int>((a, b) => { return b - a; });
            //while (test.Count < 10)
            //{
            //    var num = UnityEngine.Random.Range(0, 100);
            //    Log.Debug(num);
            //    test.Push(num);
            //}
            //Log.Info(test._list);
            //test.Sort();
            //Log.Info(test._list);

            WayfindingMgr.Ins.Test();
        }
        [MenuItem("UxGame/切换到/Boot", false, 1000)]
        public static void ChangeBoot()
        {
            EditorSceneManager.OpenScene(EditorBuildSettings.scenes[0].path, OpenSceneMode.Single);
        }
        [MenuItem("UxGame/切换到/Boot并启动", false, 1001)]
        public static void ChangeBootRun()
        {
            ChangeBoot();
            UnityEditor.EditorApplication.isPlaying = true;
        }

        [MenuItem("UxGame/Test/Eval")]
        public static void TestEval()
        {
            string eval = "max(1+1,test(a+b+c,c-b)*-((-b-2)*(a+c)))";
            EvalMgr.Ins.AddVariable("a", 2);
            EvalMgr.Ins.AddVariable("b", -1);
            EvalMgr.Ins.AddVariable("c", 2);
            EvalMgr.Ins.AddFunction("test", (nums) => {
                return nums[0] + nums[1];
            });
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            EvalMgr.Ins.Release();
            var value = EvalMgr.Ins.Parse(eval);
            sw.Stop();            
            Log.Debug(eval+":{0},ms:{1}",value, sw.ElapsedMilliseconds);
        }
    }

}
