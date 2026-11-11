using Cysharp.Threading.Tasks;
using HybridCLR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using YooAsset;

namespace Ux
{
    public class HotFixMgr : Singleton<HotFixMgr>
    {
        //热更DLL，注意顺序
        public readonly string[] HotfixAssembly = new string[2] {
            "Unity.HotfixBase",
            "Assembly-CSharp"
        };

        private List<Type> _hotfixTypes;

        public void Init()
        {
            Load();
            if (Assemblys == null && Assemblys.Count == 0)
            {
                Log.Error("没有加载热更DLL");
                return;
            }

            YooMgr.Ins.GetPackage(YooType.Main).Package.LoadSceneAsync("Assets/Scenes/Hotfix.unity");
        }

        public List<Assembly> Assemblys { get; private set; } = new List<Assembly>();

        void Load()
        {
            //是否使用热更代码
            if (GameMain.Ins.HotfixCode)
            {
                LoadMetadataForAOTAssembly();
                var rawPackage = YooMgr.Ins.GetPackage(YooType.RawFile);
                foreach (var hotfixName in HotfixAssembly)
                {
                    // dll 放在 RawFile 包里是原样字节（不经 AB）；地址由 AddressByFileName 决定，
                    // 源文件名是 xxx.dll.bytes，去掉一个后缀后正好是 xxx.dll。
                    byte[] assBytes;
                    using (var handle = rawPackage.Package.LoadAssetSync<RawFileObject>($"{hotfixName}.dll"))
                    {
                        assBytes = (handle.AssetObject as RawFileObject)?.GetBytes();
                    }

                    if (assBytes == null || assBytes.Length == 0)
                    {
                        Log.Error($"HotFixMgr.Load 加载失败:{hotfixName}");
                        continue;
                    }
                    Assemblys.Add(Assembly.Load(assBytes));
                }
            }
            else
            {
                foreach (var hotfixName in HotfixAssembly)
                {
                    Assemblys.Add(AppDomain.CurrentDomain.GetAssemblies()
                    .First(assembly => assembly.GetName().Name == hotfixName));
                }
            }
        }

        /// <summary>
        /// 为aot assembly加载原始metadata， 这个代码放aot或者热更新都行。
        /// 一旦加载后，如果AOT泛型函数对应native实现不存在，则自动替换为解释模式执行
        /// </summary>
        void LoadMetadataForAOTAssembly()
        {
            // 可以加载任意aot assembly的对应的dll。但要求dll必须与unity build过程中生成的裁剪后的dll一致，而不能直接使用原始dll。
            // 我们在BuildProcessor_xxx里添加了处理代码，这些裁剪后的dll在打包时自动被复制到 {项目目录}/HybridCLRData/AssembliesPostIl2CppStrip/{Target} 目录。

            // 注意，补充元数据是给AOT dll补充元数据，而不是给热更新dll补充元数据。
            // 热更新dll不缺元数据，不需要补充，如果调用LoadMetadataForAOTAssembly会返回错误

            const HomologousImageMode mode = HomologousImageMode.Consistent;
            foreach (var aotDllName in AOTGenericReferences.PatchedAOTAssemblyList)
            {
                var dllName = $"Code/{aotDllName}";
                var ta = Resources.Load<TextAsset>(dllName);
                byte[] assBytes = ta.bytes;
                if (assBytes == null)
                {
                    Log.Error($"LoadMetadataForAOTAssembly 加载失败:{dllName}");
                    continue;
                }
                // 加载assembly对应的dll，会自动为它hook。一旦aot泛型函数的native函数不存在，用解释器版本代码
                var err = RuntimeApi.LoadMetadataForAOTAssembly(assBytes, mode);
                Log.Debug($"LoadMetadataForAOTAssembly:{dllName}. ret:{err}");
            }
        }
    }
}