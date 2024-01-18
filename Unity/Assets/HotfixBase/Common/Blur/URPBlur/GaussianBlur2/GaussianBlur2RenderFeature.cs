using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GaussianBlur2RenderFeature : ScriptableRendererFeature
{

    public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;      // 设置Pass渲染的位置-初始值

    GaussianBlurPass2 _blurPass;
    public override void Create()         //固定写法-override void Create是 父类ScriptableRendererFeature的内容
    {
        this.name = "GaussianBlurPass";     // 设置RenderFeature名字-(在RenderFeature面板可以看到，也是面板标题)
        _blurPass = new GaussianBlurPass2(renderPassEvent);      // 创建pass-(在RenderFeature面板可以看到pass的输入参数)
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)      //固定写法-override void AddRenderPasses是 父类ScriptableRendererFeature的内容 
    {
        renderer.EnqueuePass(_blurPass);     // Pass入队
    }
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        base.SetupRenderPasses(renderer, renderingData);
        _blurPass.SetUp(renderingData.cameraData.renderer.cameraColorTargetHandle);       //设置当前渲染目标(RT)-先拿到相机的当前RT
    }


    //【Pass】
    public class GaussianBlurPass2 : ScriptableRenderPass
    {
        //[变量:设置Tag或获取ID]
        static readonly string k_RenderTag = "Render GaussianBlur Effects";   //RenderTitle---给commandBuffer的，在”FrameDebugger工具“里面可以显示commandBuffer
        static readonly int mainTexID = Shader.PropertyToID("_MainTex");    //Shader的_MainTex变量(后处理Shader必须有_MainTex),作为主贴图,给个ID
        static readonly int tempTexID_01 = Shader.PropertyToID("_TempGaussianBlurTex_01");     //临时贴图，给个ID
        static readonly int tempTexID_02 = Shader.PropertyToID("_TempGaussianBlurTex_02");     //临时贴图，给个ID

        Material _mat;               // 新建材质         

        RTHandle _handle;
        GaussianBlur2 _volume;
        public void SetUp(RTHandle handle) // 当前渲染目标(RT)---我们即将绘制的画面
        {
            _handle = handle;
            var stack = VolumeManager.instance.stack;       // 从栈中传入volume
            _volume = stack.GetComponent<GaussianBlur2>();        // 传入volume中的脚本
        }

        public GaussianBlurPass2(RenderPassEvent evt)        // 构造函数输入 RenderPassEvent-渲染事件位置, Pass所使用的Shader;这2个参数会暴露在面板中
        {
            this.renderPassEvent = evt;              //固定写法----设置渲染事件位置 - renderPassEvent 
            Shader gaussianBlurshader = Shader.Find("Ux/URP_PostProcessing/GrainyBlur2");
            if (gaussianBlurshader)
            {
                this._mat = CoreUtils.CreateEngineMaterial(gaussianBlurshader);       //新建材质
            }
        }

        #region 执行Pass-----Execute
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)      //固定写法-override void Execute 是父类ScriptableRenderPass的内容   renderingData里面有渲染的结果,所以用ref
        {
            //[判断执行条件:材质是否初始化成功]
            if (_mat == null)        // 若材质初始化失败
            {
                Debug.LogError("材质初始化失败!");
                return;
            }
            //[判断执行条件:相机是否开启后处理]
            if (!renderingData.cameraData.postProcessEnabled)       // 若摄像机没开启后处理,  摄像机的参数从RenderingData renderingData里面拿
            {
                return;
            }

            //[判断执行条件:volume组件是否有要的脚本]
            if (_volume == null)       // 若volume中无所要脚本
            {
                Debug.LogError("获取volume组件失败");
                return;
            }


            //[开始执行Pass]
            var commandBuffer = CommandBufferPool.Get(k_RenderTag);             //从CommandBufferPool中分配得到commandBuffer(命令缓存区),并设置commandBuffer的Title
            Render(commandBuffer, ref renderingData);        // 设置渲染函数
            context.ExecuteCommandBuffer(commandBuffer);     // 执行commandBuffer里的渲染绘制命令
            CommandBufferPool.Release(commandBuffer);        // 释放commandBuffer
        }

        #endregion

        // 渲染函数
        void Render(CommandBuffer cmd, ref RenderingData renderingData)     // 关于ref,作用是函数内部参数改变，对应外部参数也跟着改变 https://blog.csdn.net/qq_42481369/article/details/115186003
        {
            // 传入参数
            ref var cameraData = ref renderingData.cameraData;       // 传入相机参数-大多后处理要用到相机的参数，这里ColorTint没有用到            
            int destination01 = tempTexID_01;         // 传入临时贴图ID,作为目标
            int destination02 = tempTexID_02;         // 传入临时贴图ID,作为目标

            // 获取参数
            int gaussianBlurTimes = _volume.times.value;        // 模糊次数
            int downsample = _volume.downSample.value;              // 图片放缩程度
            // 计算
            int rtW = cameraData.camera.scaledPixelWidth / downsample;
            int rtH = cameraData.camera.scaledPixelHeight / downsample;
            // 传递参数->材质
            this._mat.SetFloat("_GaussianBlurRadius", _volume.radius.value);        // 传入模糊半径给材质
            this._mat.SetFloat("_BlurDepth", _volume.depth.value);        // 传入模糊景深
            this._mat.SetFloat("_FullBlurValue", _volume.value.value);     // 传入总体模糊程度

            // commandBuffer里的渲染指令
            cmd.SetGlobalTexture(mainTexID, _handle);        // 给材质的MaintexID赋值---将source(RT)的渲染结果作为贴图传给材质-（mainTex只是一张图,RT则还有其他数据）
            cmd.GetTemporaryRT(nameID: destination01,          // 新建临时RT，其存储的临时贴图ID
                                width: rtW,
                                height: rtH,
                                depthBuffer: 0,              // 深度缓冲区位数为0   --depthBuffer可以为0，16，24
                                filter: FilterMode.Trilinear,        // 滤波模式选择三项滤波
                                format: RenderTextureFormat.Default);        //渲染格式：默认
            cmd.GetTemporaryRT(nameID: destination02,          // 新建临时RT，其存储的临时贴图ID
                                width: rtW,
                                height: rtH,
                                depthBuffer: 0,              // 深度缓冲区位数为0   --depthBuffer可以为0，16，24
                                filter: FilterMode.Trilinear,        // 滤波模式选择三项滤波
                                format: RenderTextureFormat.Default);        //渲染格式：默认
            //【开始绘制】
            if (gaussianBlurTimes > 0)  // 如果模糊次数>0
            {
                cmd.Blit(_handle, destination01);                // source传给 destination01
                for (int i = 0; i < gaussianBlurTimes; i++)        // 执行多次模糊
                {
                    cmd.Blit(destination01, destination02, _mat, 0);      // destination01执行Mat的Pass0-横模糊,输出给destination02
                    cmd.Blit(destination02, destination01, _mat, 1);      // destination02执行Mat的Pass0-横模糊,输出给destination01              
                }
                cmd.Blit(destination01, _handle);    // destination01传给 source
            }
        }

    }
}







