using System.Collections.Generic;
public class AOTGenericReferences : UnityEngine.MonoBehaviour
{

	// {{ AOT assemblies
	public static readonly IReadOnlyList<string> PatchedAOTAssemblyList = new List<string>
	{
		"Luban.dll",
		"Protobuf-net.dll",
		"System.Core.dll",
		"UniTask.dll",
		"Unity.InputSystem.dll",
		"Unity.Main.dll",
		"UnityEngine.CoreModule.dll",
		"YooAsset.dll",
		"mscorlib.dll",
	};
	// }}

	// {{ constraint implement type
	// }} 

	// {{ AOT generic types
	// Cysharp.Threading.Tasks.AutoResetUniTaskCompletionSource.<>c<object>
	// Cysharp.Threading.Tasks.AutoResetUniTaskCompletionSource<object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<Ux.ByteArray.<PopToWebSocketAsync>d__42>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<Ux.ByteArray.<PushByWebSocketAsync>d__25,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<Ux.NetMgr.<Call>d__10<object>,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<Ux.ResMgr.<GetRawFileDataAsync>d__17,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<Ux.ResMgr.<GetRawFileDataAsync>d__20,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<Ux.ResMgr.<GetRawFilePathAsync>d__19,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<Ux.ResMgr.<GetRawFilePathAsync>d__22,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<Ux.ResMgr.<GetRawFileTextAsync>d__18,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<Ux.ResMgr.<GetRawFileTextAsync>d__21,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<Ux.ResMgr.<LoadAssetAsync>d__6,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<Ux.ResMgr.<LoadAssetAsync>d__7,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<Ux.ResMgr.<LoadAssetAsync>d__8<object>,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<Ux.ResMgr.<LoadAssetAsync>d__9<object>,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<Ux.ResMgr.<_LoadAssetAsync>d__10<object>,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<Ux.SceneModule.<EnterScene>d__5>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<Ux.StateMgr.<GetTimeLineAssetAsync>d__1,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<Ux.UIMgr.<CreateUI>d__41,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<Ux.UIMgr.<LoaUIdPackage>d__72,byte>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<Ux.UIMgr.<ShowAsync>d__39<object>,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<Ux.UIMgr.<ShowAsync>d__40,byte>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<Ux.UIMgr.<_ToLoadUIPackage>d__73,byte>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<Ux.WSocket.<ConnectAsync>d__9>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<Ux.ByteArray.<PopToWebSocketAsync>d__42>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<Ux.ByteArray.<PushByWebSocketAsync>d__25,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<Ux.NetMgr.<Call>d__10<object>,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<Ux.ResMgr.<GetRawFileDataAsync>d__17,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<Ux.ResMgr.<GetRawFileDataAsync>d__20,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<Ux.ResMgr.<GetRawFilePathAsync>d__19,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<Ux.ResMgr.<GetRawFilePathAsync>d__22,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<Ux.ResMgr.<GetRawFileTextAsync>d__18,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<Ux.ResMgr.<GetRawFileTextAsync>d__21,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<Ux.ResMgr.<LoadAssetAsync>d__6,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<Ux.ResMgr.<LoadAssetAsync>d__7,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<Ux.ResMgr.<LoadAssetAsync>d__8<object>,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<Ux.ResMgr.<LoadAssetAsync>d__9<object>,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<Ux.ResMgr.<_LoadAssetAsync>d__10<object>,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<Ux.SceneModule.<EnterScene>d__5>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<Ux.StateMgr.<GetTimeLineAssetAsync>d__1,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<Ux.UIMgr.<CreateUI>d__41,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<Ux.UIMgr.<LoaUIdPackage>d__72,byte>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<Ux.UIMgr.<ShowAsync>d__39<object>,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<Ux.UIMgr.<ShowAsync>d__40,byte>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<Ux.UIMgr.<_ToLoadUIPackage>d__73,byte>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<Ux.WSocket.<ConnectAsync>d__9>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<byte>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid.<>c<Ux.AStarComponent.<_Load>d__12>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid.<>c<Ux.FogOfWarComponent.<_UpdateMapDataAsync>d__5>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid.<>c<Ux.LoginModule.<LoginAccount>d__3>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid.<>c<Ux.LoginModule.<_EnterMap>d__4>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid.<>c<Ux.UIObject.<_CheckHide>d__40>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid.<>c<Ux.UIObject.<_CheckShow>d__35>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid.<>c<Ux.Unit.<LoadPlayer>d__35>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid.<>c<Ux.UnitStateAnim.<LoadAsset>d__9>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid.<>c<Ux.UnitStateTimeLine.<LoadAsset>d__12>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid.<>c<Ux.WSocket.<StartRecv>d__12>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid.<>c<Ux.WSocket.<_SendSync>d__11>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid<Ux.AStarComponent.<_Load>d__12>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid<Ux.FogOfWarComponent.<_UpdateMapDataAsync>d__5>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid<Ux.LoginModule.<LoginAccount>d__3>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid<Ux.LoginModule.<_EnterMap>d__4>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid<Ux.UIObject.<_CheckHide>d__40>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid<Ux.UIObject.<_CheckShow>d__35>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid<Ux.Unit.<LoadPlayer>d__35>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid<Ux.UnitStateAnim.<LoadAsset>d__9>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid<Ux.UnitStateTimeLine.<LoadAsset>d__12>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid<Ux.WSocket.<StartRecv>d__12>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid<Ux.WSocket.<_SendSync>d__11>
	// Cysharp.Threading.Tasks.CompilerServices.IStateMachineRunnerPromise<byte>
	// Cysharp.Threading.Tasks.CompilerServices.IStateMachineRunnerPromise<object>
	// Cysharp.Threading.Tasks.ITaskPoolNode<object>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,byte>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,byte>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,object>>
	// Cysharp.Threading.Tasks.IUniTaskSource<byte>
	// Cysharp.Threading.Tasks.IUniTaskSource<object>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,byte>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,byte>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,object>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<byte>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<object>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,byte>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,byte>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,object>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<byte>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<object>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,byte>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,byte>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,object>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<byte>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<object>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,byte>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,byte>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,object>>
	// Cysharp.Threading.Tasks.UniTask<byte>
	// Cysharp.Threading.Tasks.UniTask<object>
	// Cysharp.Threading.Tasks.UniTaskCompletionSourceCore<Cysharp.Threading.Tasks.AsyncUnit>
	// Cysharp.Threading.Tasks.UniTaskCompletionSourceCore<byte>
	// Cysharp.Threading.Tasks.UniTaskCompletionSourceCore<object>
	// System.Action<Cysharp.Threading.Tasks.UniTask>
	// System.Action<System.DateTime>
	// System.Action<UnityEngine.InputSystem.InputAction.CallbackContext>
	// System.Action<UnityEngine.Vector2Int>
	// System.Action<UnityEngine.Vector3>
	// System.Action<Ux.ConditionMgr.ConditionParse>
	// System.Action<Ux.ModuleMgr.ModuleParse>
	// System.Action<Ux.TagMgr.TagParse>
	// System.Action<Ux.UIMgr.BlurStack>
	// System.Action<Ux.UIMgr.UIParse>
	// System.Action<Ux.UIMgr.UIStack>
	// System.Action<byte,object>
	// System.Action<byte>
	// System.Action<int,byte>
	// System.Action<int,int>
	// System.Action<int,object,byte>
	// System.Action<int>
	// System.Action<long>
	// System.Action<object,int>
	// System.Action<object,object,byte>
	// System.Action<object,object,object>
	// System.Action<object,object>
	// System.Action<object>
	// System.Action<uint>
	// System.ArraySegment.Enumerator<byte>
	// System.ArraySegment<byte>
	// System.ByReference<byte>
	// System.Collections.Generic.ArraySortHelper<Cysharp.Threading.Tasks.UniTask>
	// System.Collections.Generic.ArraySortHelper<System.DateTime>
	// System.Collections.Generic.ArraySortHelper<UnityEngine.Vector2Int>
	// System.Collections.Generic.ArraySortHelper<UnityEngine.Vector3>
	// System.Collections.Generic.ArraySortHelper<Ux.ConditionMgr.ConditionParse>
	// System.Collections.Generic.ArraySortHelper<Ux.ModuleMgr.ModuleParse>
	// System.Collections.Generic.ArraySortHelper<Ux.TagMgr.TagParse>
	// System.Collections.Generic.ArraySortHelper<Ux.UIMgr.BlurStack>
	// System.Collections.Generic.ArraySortHelper<Ux.UIMgr.UIParse>
	// System.Collections.Generic.ArraySortHelper<Ux.UIMgr.UIStack>
	// System.Collections.Generic.ArraySortHelper<int>
	// System.Collections.Generic.ArraySortHelper<long>
	// System.Collections.Generic.ArraySortHelper<object>
	// System.Collections.Generic.ArraySortHelper<uint>
	// System.Collections.Generic.Comparer<Cysharp.Threading.Tasks.UniTask>
	// System.Collections.Generic.Comparer<System.DateTime>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,byte>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,byte>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,object>>
	// System.Collections.Generic.Comparer<UnityEngine.Vector2Int>
	// System.Collections.Generic.Comparer<UnityEngine.Vector3>
	// System.Collections.Generic.Comparer<Ux.ConditionMgr.ConditionParse>
	// System.Collections.Generic.Comparer<Ux.ModuleMgr.ModuleParse>
	// System.Collections.Generic.Comparer<Ux.TagMgr.TagParse>
	// System.Collections.Generic.Comparer<Ux.UIMgr.BlurStack>
	// System.Collections.Generic.Comparer<Ux.UIMgr.UIParse>
	// System.Collections.Generic.Comparer<Ux.UIMgr.UIStack>
	// System.Collections.Generic.Comparer<byte>
	// System.Collections.Generic.Comparer<int>
	// System.Collections.Generic.Comparer<long>
	// System.Collections.Generic.Comparer<object>
	// System.Collections.Generic.Comparer<uint>
	// System.Collections.Generic.Dictionary.Enumerator<System.ValueTuple<int,int>,object>
	// System.Collections.Generic.Dictionary.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.Enumerator<long,object>
	// System.Collections.Generic.Dictionary.Enumerator<object,UnityEngine.Playables.PlayableBinding>
	// System.Collections.Generic.Dictionary.Enumerator<object,Ux.CronData>
	// System.Collections.Generic.Dictionary.Enumerator<object,Ux.EvalMgr.ArgBool>
	// System.Collections.Generic.Dictionary.Enumerator<object,Ux.EvalMgr.MatchData>
	// System.Collections.Generic.Dictionary.Enumerator<object,Ux.EvalMgr.ValueBool>
	// System.Collections.Generic.Dictionary.Enumerator<object,double>
	// System.Collections.Generic.Dictionary.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.Enumerator<uint,float>
	// System.Collections.Generic.Dictionary.Enumerator<uint,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<System.ValueTuple<int,int>,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<long,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,UnityEngine.Playables.PlayableBinding>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,Ux.CronData>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,Ux.EvalMgr.ArgBool>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,Ux.EvalMgr.MatchData>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,Ux.EvalMgr.ValueBool>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,double>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<uint,float>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<uint,object>
	// System.Collections.Generic.Dictionary.KeyCollection<System.ValueTuple<int,int>,object>
	// System.Collections.Generic.Dictionary.KeyCollection<int,object>
	// System.Collections.Generic.Dictionary.KeyCollection<long,object>
	// System.Collections.Generic.Dictionary.KeyCollection<object,UnityEngine.Playables.PlayableBinding>
	// System.Collections.Generic.Dictionary.KeyCollection<object,Ux.CronData>
	// System.Collections.Generic.Dictionary.KeyCollection<object,Ux.EvalMgr.ArgBool>
	// System.Collections.Generic.Dictionary.KeyCollection<object,Ux.EvalMgr.MatchData>
	// System.Collections.Generic.Dictionary.KeyCollection<object,Ux.EvalMgr.ValueBool>
	// System.Collections.Generic.Dictionary.KeyCollection<object,double>
	// System.Collections.Generic.Dictionary.KeyCollection<object,int>
	// System.Collections.Generic.Dictionary.KeyCollection<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection<uint,float>
	// System.Collections.Generic.Dictionary.KeyCollection<uint,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<System.ValueTuple<int,int>,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<long,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,UnityEngine.Playables.PlayableBinding>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,Ux.CronData>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,Ux.EvalMgr.ArgBool>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,Ux.EvalMgr.MatchData>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,Ux.EvalMgr.ValueBool>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,double>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<uint,float>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<uint,object>
	// System.Collections.Generic.Dictionary.ValueCollection<System.ValueTuple<int,int>,object>
	// System.Collections.Generic.Dictionary.ValueCollection<int,object>
	// System.Collections.Generic.Dictionary.ValueCollection<long,object>
	// System.Collections.Generic.Dictionary.ValueCollection<object,UnityEngine.Playables.PlayableBinding>
	// System.Collections.Generic.Dictionary.ValueCollection<object,Ux.CronData>
	// System.Collections.Generic.Dictionary.ValueCollection<object,Ux.EvalMgr.ArgBool>
	// System.Collections.Generic.Dictionary.ValueCollection<object,Ux.EvalMgr.MatchData>
	// System.Collections.Generic.Dictionary.ValueCollection<object,Ux.EvalMgr.ValueBool>
	// System.Collections.Generic.Dictionary.ValueCollection<object,double>
	// System.Collections.Generic.Dictionary.ValueCollection<object,int>
	// System.Collections.Generic.Dictionary.ValueCollection<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection<uint,float>
	// System.Collections.Generic.Dictionary.ValueCollection<uint,object>
	// System.Collections.Generic.Dictionary<System.ValueTuple<int,int>,object>
	// System.Collections.Generic.Dictionary<int,object>
	// System.Collections.Generic.Dictionary<long,object>
	// System.Collections.Generic.Dictionary<object,UnityEngine.Playables.PlayableBinding>
	// System.Collections.Generic.Dictionary<object,Ux.CronData>
	// System.Collections.Generic.Dictionary<object,Ux.EvalMgr.ArgBool>
	// System.Collections.Generic.Dictionary<object,Ux.EvalMgr.MatchData>
	// System.Collections.Generic.Dictionary<object,Ux.EvalMgr.ValueBool>
	// System.Collections.Generic.Dictionary<object,double>
	// System.Collections.Generic.Dictionary<object,int>
	// System.Collections.Generic.Dictionary<object,object>
	// System.Collections.Generic.Dictionary<uint,float>
	// System.Collections.Generic.Dictionary<uint,object>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,byte>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,byte>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,object>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<int,int>>
	// System.Collections.Generic.EqualityComparer<UnityEngine.Playables.PlayableBinding>
	// System.Collections.Generic.EqualityComparer<Ux.CronData>
	// System.Collections.Generic.EqualityComparer<Ux.EvalMgr.ArgBool>
	// System.Collections.Generic.EqualityComparer<Ux.EvalMgr.MatchData>
	// System.Collections.Generic.EqualityComparer<Ux.EvalMgr.ValueBool>
	// System.Collections.Generic.EqualityComparer<byte>
	// System.Collections.Generic.EqualityComparer<double>
	// System.Collections.Generic.EqualityComparer<float>
	// System.Collections.Generic.EqualityComparer<int>
	// System.Collections.Generic.EqualityComparer<long>
	// System.Collections.Generic.EqualityComparer<object>
	// System.Collections.Generic.EqualityComparer<uint>
	// System.Collections.Generic.HashSet.Enumerator<int>
	// System.Collections.Generic.HashSet.Enumerator<object>
	// System.Collections.Generic.HashSet<int>
	// System.Collections.Generic.HashSet<object>
	// System.Collections.Generic.HashSetEqualityComparer<int>
	// System.Collections.Generic.HashSetEqualityComparer<object>
	// System.Collections.Generic.ICollection<Cysharp.Threading.Tasks.UniTask>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.ValueTuple<int,int>,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,UnityEngine.Playables.PlayableBinding>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,Ux.CronData>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,Ux.EvalMgr.ArgBool>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,Ux.EvalMgr.MatchData>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,Ux.EvalMgr.ValueBool>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,double>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<uint,float>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<uint,object>>
	// System.Collections.Generic.ICollection<System.DateTime>
	// System.Collections.Generic.ICollection<UnityEngine.Vector2Int>
	// System.Collections.Generic.ICollection<UnityEngine.Vector3>
	// System.Collections.Generic.ICollection<Ux.ConditionMgr.ConditionParse>
	// System.Collections.Generic.ICollection<Ux.ModuleMgr.ModuleParse>
	// System.Collections.Generic.ICollection<Ux.TagMgr.TagParse>
	// System.Collections.Generic.ICollection<Ux.UIMgr.BlurStack>
	// System.Collections.Generic.ICollection<Ux.UIMgr.UIParse>
	// System.Collections.Generic.ICollection<Ux.UIMgr.UIStack>
	// System.Collections.Generic.ICollection<int>
	// System.Collections.Generic.ICollection<long>
	// System.Collections.Generic.ICollection<object>
	// System.Collections.Generic.ICollection<uint>
	// System.Collections.Generic.IComparer<Cysharp.Threading.Tasks.UniTask>
	// System.Collections.Generic.IComparer<System.DateTime>
	// System.Collections.Generic.IComparer<UnityEngine.Vector2Int>
	// System.Collections.Generic.IComparer<UnityEngine.Vector3>
	// System.Collections.Generic.IComparer<Ux.ConditionMgr.ConditionParse>
	// System.Collections.Generic.IComparer<Ux.ModuleMgr.ModuleParse>
	// System.Collections.Generic.IComparer<Ux.TagMgr.TagParse>
	// System.Collections.Generic.IComparer<Ux.UIMgr.BlurStack>
	// System.Collections.Generic.IComparer<Ux.UIMgr.UIParse>
	// System.Collections.Generic.IComparer<Ux.UIMgr.UIStack>
	// System.Collections.Generic.IComparer<int>
	// System.Collections.Generic.IComparer<long>
	// System.Collections.Generic.IComparer<object>
	// System.Collections.Generic.IComparer<uint>
	// System.Collections.Generic.IDictionary<object,Ux.EvalMgr.ArgBool>
	// System.Collections.Generic.IDictionary<object,Ux.EvalMgr.MatchData>
	// System.Collections.Generic.IDictionary<object,Ux.EvalMgr.ValueBool>
	// System.Collections.Generic.IDictionary<object,double>
	// System.Collections.Generic.IDictionary<object,object>
	// System.Collections.Generic.IEnumerable<Cysharp.Threading.Tasks.UniTask>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.ValueTuple<int,int>,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,UnityEngine.Playables.PlayableBinding>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,Ux.CronData>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,Ux.EvalMgr.ArgBool>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,Ux.EvalMgr.MatchData>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,Ux.EvalMgr.ValueBool>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,double>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<uint,float>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<uint,object>>
	// System.Collections.Generic.IEnumerable<System.DateTime>
	// System.Collections.Generic.IEnumerable<UnityEngine.Playables.PlayableBinding>
	// System.Collections.Generic.IEnumerable<UnityEngine.Vector2Int>
	// System.Collections.Generic.IEnumerable<UnityEngine.Vector3>
	// System.Collections.Generic.IEnumerable<Ux.ConditionMgr.ConditionParse>
	// System.Collections.Generic.IEnumerable<Ux.ModuleMgr.ModuleParse>
	// System.Collections.Generic.IEnumerable<Ux.TagMgr.TagParse>
	// System.Collections.Generic.IEnumerable<Ux.UIMgr.BlurStack>
	// System.Collections.Generic.IEnumerable<Ux.UIMgr.UIParse>
	// System.Collections.Generic.IEnumerable<Ux.UIMgr.UIStack>
	// System.Collections.Generic.IEnumerable<byte>
	// System.Collections.Generic.IEnumerable<int>
	// System.Collections.Generic.IEnumerable<long>
	// System.Collections.Generic.IEnumerable<object>
	// System.Collections.Generic.IEnumerable<uint>
	// System.Collections.Generic.IEnumerator<Cysharp.Threading.Tasks.UniTask>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<System.ValueTuple<int,int>,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,UnityEngine.Playables.PlayableBinding>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,Ux.CronData>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,Ux.EvalMgr.ArgBool>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,Ux.EvalMgr.MatchData>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,Ux.EvalMgr.ValueBool>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,double>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<uint,float>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<uint,object>>
	// System.Collections.Generic.IEnumerator<System.DateTime>
	// System.Collections.Generic.IEnumerator<UnityEngine.Playables.PlayableBinding>
	// System.Collections.Generic.IEnumerator<UnityEngine.Vector2Int>
	// System.Collections.Generic.IEnumerator<UnityEngine.Vector3>
	// System.Collections.Generic.IEnumerator<Ux.ConditionMgr.ConditionParse>
	// System.Collections.Generic.IEnumerator<Ux.ModuleMgr.ModuleParse>
	// System.Collections.Generic.IEnumerator<Ux.TagMgr.TagParse>
	// System.Collections.Generic.IEnumerator<Ux.UIMgr.BlurStack>
	// System.Collections.Generic.IEnumerator<Ux.UIMgr.UIParse>
	// System.Collections.Generic.IEnumerator<Ux.UIMgr.UIStack>
	// System.Collections.Generic.IEnumerator<byte>
	// System.Collections.Generic.IEnumerator<int>
	// System.Collections.Generic.IEnumerator<long>
	// System.Collections.Generic.IEnumerator<object>
	// System.Collections.Generic.IEnumerator<uint>
	// System.Collections.Generic.IEqualityComparer<System.ValueTuple<int,int>>
	// System.Collections.Generic.IEqualityComparer<int>
	// System.Collections.Generic.IEqualityComparer<long>
	// System.Collections.Generic.IEqualityComparer<object>
	// System.Collections.Generic.IEqualityComparer<uint>
	// System.Collections.Generic.IList<Cysharp.Threading.Tasks.UniTask>
	// System.Collections.Generic.IList<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.IList<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IList<System.DateTime>
	// System.Collections.Generic.IList<UnityEngine.Vector2Int>
	// System.Collections.Generic.IList<UnityEngine.Vector3>
	// System.Collections.Generic.IList<Ux.ConditionMgr.ConditionParse>
	// System.Collections.Generic.IList<Ux.ModuleMgr.ModuleParse>
	// System.Collections.Generic.IList<Ux.TagMgr.TagParse>
	// System.Collections.Generic.IList<Ux.UIMgr.BlurStack>
	// System.Collections.Generic.IList<Ux.UIMgr.UIParse>
	// System.Collections.Generic.IList<Ux.UIMgr.UIStack>
	// System.Collections.Generic.IList<int>
	// System.Collections.Generic.IList<long>
	// System.Collections.Generic.IList<object>
	// System.Collections.Generic.IList<uint>
	// System.Collections.Generic.KeyValuePair<System.ValueTuple<int,int>,object>
	// System.Collections.Generic.KeyValuePair<int,object>
	// System.Collections.Generic.KeyValuePair<long,object>
	// System.Collections.Generic.KeyValuePair<object,UnityEngine.Playables.PlayableBinding>
	// System.Collections.Generic.KeyValuePair<object,Ux.CronData>
	// System.Collections.Generic.KeyValuePair<object,Ux.EvalMgr.ArgBool>
	// System.Collections.Generic.KeyValuePair<object,Ux.EvalMgr.MatchData>
	// System.Collections.Generic.KeyValuePair<object,Ux.EvalMgr.ValueBool>
	// System.Collections.Generic.KeyValuePair<object,double>
	// System.Collections.Generic.KeyValuePair<object,int>
	// System.Collections.Generic.KeyValuePair<object,object>
	// System.Collections.Generic.KeyValuePair<uint,float>
	// System.Collections.Generic.KeyValuePair<uint,object>
	// System.Collections.Generic.List.Enumerator<Cysharp.Threading.Tasks.UniTask>
	// System.Collections.Generic.List.Enumerator<System.DateTime>
	// System.Collections.Generic.List.Enumerator<UnityEngine.Vector2Int>
	// System.Collections.Generic.List.Enumerator<UnityEngine.Vector3>
	// System.Collections.Generic.List.Enumerator<Ux.ConditionMgr.ConditionParse>
	// System.Collections.Generic.List.Enumerator<Ux.ModuleMgr.ModuleParse>
	// System.Collections.Generic.List.Enumerator<Ux.TagMgr.TagParse>
	// System.Collections.Generic.List.Enumerator<Ux.UIMgr.BlurStack>
	// System.Collections.Generic.List.Enumerator<Ux.UIMgr.UIParse>
	// System.Collections.Generic.List.Enumerator<Ux.UIMgr.UIStack>
	// System.Collections.Generic.List.Enumerator<int>
	// System.Collections.Generic.List.Enumerator<long>
	// System.Collections.Generic.List.Enumerator<object>
	// System.Collections.Generic.List.Enumerator<uint>
	// System.Collections.Generic.List<Cysharp.Threading.Tasks.UniTask>
	// System.Collections.Generic.List<System.DateTime>
	// System.Collections.Generic.List<UnityEngine.Vector2Int>
	// System.Collections.Generic.List<UnityEngine.Vector3>
	// System.Collections.Generic.List<Ux.ConditionMgr.ConditionParse>
	// System.Collections.Generic.List<Ux.ModuleMgr.ModuleParse>
	// System.Collections.Generic.List<Ux.TagMgr.TagParse>
	// System.Collections.Generic.List<Ux.UIMgr.BlurStack>
	// System.Collections.Generic.List<Ux.UIMgr.UIParse>
	// System.Collections.Generic.List<Ux.UIMgr.UIStack>
	// System.Collections.Generic.List<int>
	// System.Collections.Generic.List<long>
	// System.Collections.Generic.List<object>
	// System.Collections.Generic.List<uint>
	// System.Collections.Generic.ObjectComparer<Cysharp.Threading.Tasks.UniTask>
	// System.Collections.Generic.ObjectComparer<System.DateTime>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,byte>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,byte>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,object>>
	// System.Collections.Generic.ObjectComparer<UnityEngine.Vector2Int>
	// System.Collections.Generic.ObjectComparer<UnityEngine.Vector3>
	// System.Collections.Generic.ObjectComparer<Ux.ConditionMgr.ConditionParse>
	// System.Collections.Generic.ObjectComparer<Ux.ModuleMgr.ModuleParse>
	// System.Collections.Generic.ObjectComparer<Ux.TagMgr.TagParse>
	// System.Collections.Generic.ObjectComparer<Ux.UIMgr.BlurStack>
	// System.Collections.Generic.ObjectComparer<Ux.UIMgr.UIParse>
	// System.Collections.Generic.ObjectComparer<Ux.UIMgr.UIStack>
	// System.Collections.Generic.ObjectComparer<byte>
	// System.Collections.Generic.ObjectComparer<int>
	// System.Collections.Generic.ObjectComparer<long>
	// System.Collections.Generic.ObjectComparer<object>
	// System.Collections.Generic.ObjectComparer<uint>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,byte>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,byte>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,object>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<int,int>>
	// System.Collections.Generic.ObjectEqualityComparer<UnityEngine.Playables.PlayableBinding>
	// System.Collections.Generic.ObjectEqualityComparer<Ux.CronData>
	// System.Collections.Generic.ObjectEqualityComparer<Ux.EvalMgr.ArgBool>
	// System.Collections.Generic.ObjectEqualityComparer<Ux.EvalMgr.MatchData>
	// System.Collections.Generic.ObjectEqualityComparer<Ux.EvalMgr.ValueBool>
	// System.Collections.Generic.ObjectEqualityComparer<byte>
	// System.Collections.Generic.ObjectEqualityComparer<double>
	// System.Collections.Generic.ObjectEqualityComparer<float>
	// System.Collections.Generic.ObjectEqualityComparer<int>
	// System.Collections.Generic.ObjectEqualityComparer<long>
	// System.Collections.Generic.ObjectEqualityComparer<object>
	// System.Collections.Generic.ObjectEqualityComparer<uint>
	// System.Collections.Generic.Queue.Enumerator<Ux.Entity.DelayFn>
	// System.Collections.Generic.Queue.Enumerator<int>
	// System.Collections.Generic.Queue.Enumerator<object>
	// System.Collections.Generic.Queue<Ux.Entity.DelayFn>
	// System.Collections.Generic.Queue<int>
	// System.Collections.Generic.Queue<object>
	// System.Collections.Generic.Stack.Enumerator<Ux.UIMgr.UIStack>
	// System.Collections.Generic.Stack.Enumerator<int>
	// System.Collections.Generic.Stack<Ux.UIMgr.UIStack>
	// System.Collections.Generic.Stack<int>
	// System.Collections.ObjectModel.ReadOnlyCollection<Cysharp.Threading.Tasks.UniTask>
	// System.Collections.ObjectModel.ReadOnlyCollection<System.DateTime>
	// System.Collections.ObjectModel.ReadOnlyCollection<UnityEngine.Vector2Int>
	// System.Collections.ObjectModel.ReadOnlyCollection<UnityEngine.Vector3>
	// System.Collections.ObjectModel.ReadOnlyCollection<Ux.ConditionMgr.ConditionParse>
	// System.Collections.ObjectModel.ReadOnlyCollection<Ux.ModuleMgr.ModuleParse>
	// System.Collections.ObjectModel.ReadOnlyCollection<Ux.TagMgr.TagParse>
	// System.Collections.ObjectModel.ReadOnlyCollection<Ux.UIMgr.BlurStack>
	// System.Collections.ObjectModel.ReadOnlyCollection<Ux.UIMgr.UIParse>
	// System.Collections.ObjectModel.ReadOnlyCollection<Ux.UIMgr.UIStack>
	// System.Collections.ObjectModel.ReadOnlyCollection<int>
	// System.Collections.ObjectModel.ReadOnlyCollection<long>
	// System.Collections.ObjectModel.ReadOnlyCollection<object>
	// System.Collections.ObjectModel.ReadOnlyCollection<uint>
	// System.Comparison<Cysharp.Threading.Tasks.UniTask>
	// System.Comparison<System.DateTime>
	// System.Comparison<UnityEngine.Vector2Int>
	// System.Comparison<UnityEngine.Vector3>
	// System.Comparison<Ux.ConditionMgr.ConditionParse>
	// System.Comparison<Ux.ModuleMgr.ModuleParse>
	// System.Comparison<Ux.TagMgr.TagParse>
	// System.Comparison<Ux.UIMgr.BlurStack>
	// System.Comparison<Ux.UIMgr.UIParse>
	// System.Comparison<Ux.UIMgr.UIStack>
	// System.Comparison<int>
	// System.Comparison<long>
	// System.Comparison<object>
	// System.Comparison<uint>
	// System.Dynamic.Utils.CacheDict.Entry<object,object>
	// System.Dynamic.Utils.CacheDict<object,object>
	// System.EventHandler<object>
	// System.Func<byte,byte>
	// System.Func<byte>
	// System.Func<double,double,double>
	// System.Func<double,double>
	// System.Func<double>
	// System.Func<int,byte>
	// System.Func<int>
	// System.Func<object,byte,byte>
	// System.Func<object,byte>
	// System.Func<object,double>
	// System.Func<object,int>
	// System.Func<object,object,byte,object,object>
	// System.Func<object,object,object>
	// System.Func<object,object>
	// System.Func<object>
	// System.Linq.Buffer<object>
	// System.Linq.Enumerable.Iterator<byte>
	// System.Linq.Enumerable.Iterator<int>
	// System.Linq.Enumerable.Iterator<object>
	// System.Linq.Enumerable.WhereArrayIterator<int>
	// System.Linq.Enumerable.WhereArrayIterator<object>
	// System.Linq.Enumerable.WhereEnumerableIterator<byte>
	// System.Linq.Enumerable.WhereEnumerableIterator<int>
	// System.Linq.Enumerable.WhereEnumerableIterator<object>
	// System.Linq.Enumerable.WhereListIterator<int>
	// System.Linq.Enumerable.WhereListIterator<object>
	// System.Linq.Enumerable.WhereSelectArrayIterator<object,byte>
	// System.Linq.Enumerable.WhereSelectArrayIterator<object,int>
	// System.Linq.Enumerable.WhereSelectEnumerableIterator<object,byte>
	// System.Linq.Enumerable.WhereSelectEnumerableIterator<object,int>
	// System.Linq.Enumerable.WhereSelectListIterator<object,byte>
	// System.Linq.Enumerable.WhereSelectListIterator<object,int>
	// System.Linq.Expressions.Expression<object>
	// System.Nullable<Ux.TriggerData>
	// System.Nullable<Ux.UIMgr.BlurStack>
	// System.Nullable<Ux.UIMgr.CallBackData>
	// System.Nullable<long>
	// System.Predicate<Cysharp.Threading.Tasks.UniTask>
	// System.Predicate<System.DateTime>
	// System.Predicate<UnityEngine.InputSystem.InputControlScheme>
	// System.Predicate<UnityEngine.Vector2Int>
	// System.Predicate<UnityEngine.Vector3>
	// System.Predicate<Ux.ConditionMgr.ConditionParse>
	// System.Predicate<Ux.ModuleMgr.ModuleParse>
	// System.Predicate<Ux.TagMgr.TagParse>
	// System.Predicate<Ux.UIMgr.BlurStack>
	// System.Predicate<Ux.UIMgr.UIParse>
	// System.Predicate<Ux.UIMgr.UIStack>
	// System.Predicate<int>
	// System.Predicate<long>
	// System.Predicate<object>
	// System.Predicate<uint>
	// System.ReadOnlySpan.Enumerator<byte>
	// System.ReadOnlySpan<byte>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter<object>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable<object>
	// System.Runtime.CompilerServices.ReadOnlyCollectionBuilder.Enumerator<object>
	// System.Runtime.CompilerServices.ReadOnlyCollectionBuilder<object>
	// System.Runtime.CompilerServices.TaskAwaiter<object>
	// System.Runtime.CompilerServices.TrueReadOnlyCollection<object>
	// System.Span.Enumerator<byte>
	// System.Span<byte>
	// System.Threading.Tasks.ContinuationTaskFromResultTask<object>
	// System.Threading.Tasks.Task<object>
	// System.Threading.Tasks.TaskFactory.<>c__DisplayClass35_0<object>
	// System.Threading.Tasks.TaskFactory<object>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,byte>>
	// System.ValueTuple<byte,System.ValueTuple<byte,object>>
	// System.ValueTuple<byte,byte>
	// System.ValueTuple<byte,object>
	// System.ValueTuple<int,int>
	// UnityEngine.InputSystem.InputBindingComposite<UnityEngine.Vector2>
	// UnityEngine.InputSystem.InputControl<UnityEngine.Vector2>
	// UnityEngine.InputSystem.InputProcessor<UnityEngine.Vector2>
	// UnityEngine.InputSystem.Utilities.InlinedArray<object>
	// UnityEngine.InputSystem.Utilities.ReadOnlyArray.Enumerator<UnityEngine.InputSystem.InputControlScheme>
	// UnityEngine.InputSystem.Utilities.ReadOnlyArray<UnityEngine.InputSystem.InputControlScheme>
	// Ux.StateMachine.<>c__DisplayClass29_0<object>
	// }}

	public void RefMethods()
	{
		// string Bright.Common.StringUtil.CollectionToString<object>(System.Collections.Generic.IEnumerable<object>)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,Ux.ByteArray.<PopToWebSocketAsync>d__42>(Cysharp.Threading.Tasks.UniTask.Awaiter&,Ux.ByteArray.<PopToWebSocketAsync>d__42&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,Ux.SceneModule.<EnterScene>d__5>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,Ux.SceneModule.<EnterScene>d__5&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,Ux.WSocket.<ConnectAsync>d__9>(System.Runtime.CompilerServices.TaskAwaiter&,Ux.WSocket.<ConnectAsync>d__9&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<byte>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,Ux.UIMgr.<_ToLoadUIPackage>d__73>(Cysharp.Threading.Tasks.UniTask.Awaiter&,Ux.UIMgr.<_ToLoadUIPackage>d__73&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<byte>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<byte>,Ux.UIMgr.<LoaUIdPackage>d__72>(Cysharp.Threading.Tasks.UniTask.Awaiter<byte>&,Ux.UIMgr.<LoaUIdPackage>d__72&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<byte>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<byte>,Ux.UIMgr.<ShowAsync>d__40>(Cysharp.Threading.Tasks.UniTask.Awaiter<byte>&,Ux.UIMgr.<ShowAsync>d__40&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<byte>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,Ux.UIMgr.<ShowAsync>d__40>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,Ux.UIMgr.<ShowAsync>d__40&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<byte>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.YieldAwaitable.Awaiter,Ux.UIMgr.<LoaUIdPackage>d__72>(Cysharp.Threading.Tasks.YieldAwaitable.Awaiter&,Ux.UIMgr.<LoaUIdPackage>d__72&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<byte>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.YieldAwaitable.Awaiter,Ux.UIMgr.<ShowAsync>d__40>(Cysharp.Threading.Tasks.YieldAwaitable.Awaiter&,Ux.UIMgr.<ShowAsync>d__40&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,Ux.ResMgr.<GetRawFileDataAsync>d__17>(Cysharp.Threading.Tasks.UniTask.Awaiter&,Ux.ResMgr.<GetRawFileDataAsync>d__17&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,Ux.ResMgr.<GetRawFileDataAsync>d__20>(Cysharp.Threading.Tasks.UniTask.Awaiter&,Ux.ResMgr.<GetRawFileDataAsync>d__20&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,Ux.ResMgr.<GetRawFilePathAsync>d__19>(Cysharp.Threading.Tasks.UniTask.Awaiter&,Ux.ResMgr.<GetRawFilePathAsync>d__19&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,Ux.ResMgr.<GetRawFilePathAsync>d__22>(Cysharp.Threading.Tasks.UniTask.Awaiter&,Ux.ResMgr.<GetRawFilePathAsync>d__22&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,Ux.ResMgr.<GetRawFileTextAsync>d__18>(Cysharp.Threading.Tasks.UniTask.Awaiter&,Ux.ResMgr.<GetRawFileTextAsync>d__18&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,Ux.ResMgr.<GetRawFileTextAsync>d__21>(Cysharp.Threading.Tasks.UniTask.Awaiter&,Ux.ResMgr.<GetRawFileTextAsync>d__21&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,Ux.ResMgr.<_LoadAssetAsync>d__10<object>>(Cysharp.Threading.Tasks.UniTask.Awaiter&,Ux.ResMgr.<_LoadAssetAsync>d__10<object>&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<byte>,Ux.UIMgr.<CreateUI>d__41>(Cysharp.Threading.Tasks.UniTask.Awaiter<byte>&,Ux.UIMgr.<CreateUI>d__41&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<byte>,Ux.UIMgr.<ShowAsync>d__39<object>>(Cysharp.Threading.Tasks.UniTask.Awaiter<byte>&,Ux.UIMgr.<ShowAsync>d__39<object>&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,Ux.NetMgr.<Call>d__10<object>>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,Ux.NetMgr.<Call>d__10<object>&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,Ux.ResMgr.<LoadAssetAsync>d__6>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,Ux.ResMgr.<LoadAssetAsync>d__6&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,Ux.ResMgr.<LoadAssetAsync>d__7>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,Ux.ResMgr.<LoadAssetAsync>d__7&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,Ux.ResMgr.<LoadAssetAsync>d__8<object>>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,Ux.ResMgr.<LoadAssetAsync>d__8<object>&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,Ux.ResMgr.<LoadAssetAsync>d__9<object>>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,Ux.ResMgr.<LoadAssetAsync>d__9<object>&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,Ux.StateMgr.<GetTimeLineAssetAsync>d__1>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,Ux.StateMgr.<GetTimeLineAssetAsync>d__1&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<object>,Ux.ByteArray.<PushByWebSocketAsync>d__25>(System.Runtime.CompilerServices.TaskAwaiter<object>&,Ux.ByteArray.<PushByWebSocketAsync>d__25&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.Start<Ux.ByteArray.<PopToWebSocketAsync>d__42>(Ux.ByteArray.<PopToWebSocketAsync>d__42&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.Start<Ux.SceneModule.<EnterScene>d__5>(Ux.SceneModule.<EnterScene>d__5&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.Start<Ux.WSocket.<ConnectAsync>d__9>(Ux.WSocket.<ConnectAsync>d__9&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<byte>.Start<Ux.UIMgr.<LoaUIdPackage>d__72>(Ux.UIMgr.<LoaUIdPackage>d__72&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<byte>.Start<Ux.UIMgr.<ShowAsync>d__40>(Ux.UIMgr.<ShowAsync>d__40&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<byte>.Start<Ux.UIMgr.<_ToLoadUIPackage>d__73>(Ux.UIMgr.<_ToLoadUIPackage>d__73&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.Start<Ux.ByteArray.<PushByWebSocketAsync>d__25>(Ux.ByteArray.<PushByWebSocketAsync>d__25&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.Start<Ux.NetMgr.<Call>d__10<object>>(Ux.NetMgr.<Call>d__10<object>&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.Start<Ux.ResMgr.<GetRawFileDataAsync>d__17>(Ux.ResMgr.<GetRawFileDataAsync>d__17&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.Start<Ux.ResMgr.<GetRawFileDataAsync>d__20>(Ux.ResMgr.<GetRawFileDataAsync>d__20&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.Start<Ux.ResMgr.<GetRawFilePathAsync>d__19>(Ux.ResMgr.<GetRawFilePathAsync>d__19&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.Start<Ux.ResMgr.<GetRawFilePathAsync>d__22>(Ux.ResMgr.<GetRawFilePathAsync>d__22&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.Start<Ux.ResMgr.<GetRawFileTextAsync>d__18>(Ux.ResMgr.<GetRawFileTextAsync>d__18&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.Start<Ux.ResMgr.<GetRawFileTextAsync>d__21>(Ux.ResMgr.<GetRawFileTextAsync>d__21&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.Start<Ux.ResMgr.<LoadAssetAsync>d__6>(Ux.ResMgr.<LoadAssetAsync>d__6&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.Start<Ux.ResMgr.<LoadAssetAsync>d__7>(Ux.ResMgr.<LoadAssetAsync>d__7&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.Start<Ux.ResMgr.<LoadAssetAsync>d__8<object>>(Ux.ResMgr.<LoadAssetAsync>d__8<object>&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.Start<Ux.ResMgr.<LoadAssetAsync>d__9<object>>(Ux.ResMgr.<LoadAssetAsync>d__9<object>&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.Start<Ux.ResMgr.<_LoadAssetAsync>d__10<object>>(Ux.ResMgr.<_LoadAssetAsync>d__10<object>&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.Start<Ux.StateMgr.<GetTimeLineAssetAsync>d__1>(Ux.StateMgr.<GetTimeLineAssetAsync>d__1&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.Start<Ux.UIMgr.<CreateUI>d__41>(Ux.UIMgr.<CreateUI>d__41&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.Start<Ux.UIMgr.<ShowAsync>d__39<object>>(Ux.UIMgr.<ShowAsync>d__39<object>&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,Ux.WSocket.<_SendSync>d__11>(Cysharp.Threading.Tasks.UniTask.Awaiter&,Ux.WSocket.<_SendSync>d__11&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<byte>,Ux.UIObject.<_CheckHide>d__40>(Cysharp.Threading.Tasks.UniTask.Awaiter<byte>&,Ux.UIObject.<_CheckHide>d__40&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<byte>,Ux.UIObject.<_CheckShow>d__35>(Cysharp.Threading.Tasks.UniTask.Awaiter<byte>&,Ux.UIObject.<_CheckShow>d__35&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,Ux.AStarComponent.<_Load>d__12>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,Ux.AStarComponent.<_Load>d__12&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,Ux.LoginModule.<LoginAccount>d__3>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,Ux.LoginModule.<LoginAccount>d__3&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,Ux.LoginModule.<_EnterMap>d__4>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,Ux.LoginModule.<_EnterMap>d__4&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,Ux.Unit.<LoadPlayer>d__35>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,Ux.Unit.<LoadPlayer>d__35&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,Ux.UnitStateAnim.<LoadAsset>d__9>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,Ux.UnitStateAnim.<LoadAsset>d__9&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,Ux.UnitStateTimeLine.<LoadAsset>d__12>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,Ux.UnitStateTimeLine.<LoadAsset>d__12&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,Ux.WSocket.<StartRecv>d__12>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,Ux.WSocket.<StartRecv>d__12&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.YieldAwaitable.Awaiter,Ux.FogOfWarComponent.<_UpdateMapDataAsync>d__5>(Cysharp.Threading.Tasks.YieldAwaitable.Awaiter&,Ux.FogOfWarComponent.<_UpdateMapDataAsync>d__5&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.YieldAwaitable.Awaiter,Ux.UIObject.<_CheckHide>d__40>(Cysharp.Threading.Tasks.YieldAwaitable.Awaiter&,Ux.UIObject.<_CheckHide>d__40&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.YieldAwaitable.Awaiter,Ux.UIObject.<_CheckShow>d__35>(Cysharp.Threading.Tasks.YieldAwaitable.Awaiter&,Ux.UIObject.<_CheckShow>d__35&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,Ux.WSocket.<StartRecv>d__12>(System.Runtime.CompilerServices.TaskAwaiter&,Ux.WSocket.<StartRecv>d__12&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.Start<Ux.AStarComponent.<_Load>d__12>(Ux.AStarComponent.<_Load>d__12&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.Start<Ux.FogOfWarComponent.<_UpdateMapDataAsync>d__5>(Ux.FogOfWarComponent.<_UpdateMapDataAsync>d__5&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.Start<Ux.LoginModule.<LoginAccount>d__3>(Ux.LoginModule.<LoginAccount>d__3&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.Start<Ux.LoginModule.<_EnterMap>d__4>(Ux.LoginModule.<_EnterMap>d__4&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.Start<Ux.UIObject.<_CheckHide>d__40>(Ux.UIObject.<_CheckHide>d__40&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.Start<Ux.UIObject.<_CheckShow>d__35>(Ux.UIObject.<_CheckShow>d__35&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.Start<Ux.Unit.<LoadPlayer>d__35>(Ux.Unit.<LoadPlayer>d__35&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.Start<Ux.UnitStateAnim.<LoadAsset>d__9>(Ux.UnitStateAnim.<LoadAsset>d__9&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.Start<Ux.UnitStateTimeLine.<LoadAsset>d__12>(Ux.UnitStateTimeLine.<LoadAsset>d__12&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.Start<Ux.WSocket.<StartRecv>d__12>(Ux.WSocket.<StartRecv>d__12&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.Start<Ux.WSocket.<_SendSync>d__11>(Ux.WSocket.<_SendSync>d__11&)
		// object Pool.Get<object>()
		// object Pool.Get<object>(System.Type)
		// System.Void ProtoBuf.Serializer.Serialize<object>(System.IO.Stream,object)
		// object System.Activator.CreateInstance<object>()
		// object[] System.Array.Empty<object>()
		// System.Collections.ObjectModel.ReadOnlyCollection<object> System.Dynamic.Utils.CollectionExtensions.ToReadOnly<object>(System.Collections.Generic.IEnumerable<object>)
		// bool System.Linq.Enumerable.Any<byte>(System.Collections.Generic.IEnumerable<byte>,System.Func<byte,bool>)
		// bool System.Linq.Enumerable.Any<object>(System.Collections.Generic.IEnumerable<object>)
		// System.Collections.Generic.KeyValuePair<long,object> System.Linq.Enumerable.ElementAt<System.Collections.Generic.KeyValuePair<long,object>>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<long,object>>,int)
		// System.Collections.Generic.KeyValuePair<object,object> System.Linq.Enumerable.ElementAt<System.Collections.Generic.KeyValuePair<object,object>>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,object>>,int)
		// object System.Linq.Enumerable.ElementAt<object>(System.Collections.Generic.IEnumerable<object>,int)
		// System.Collections.Generic.IEnumerable<byte> System.Linq.Enumerable.Select<object,byte>(System.Collections.Generic.IEnumerable<object>,System.Func<object,byte>)
		// System.Collections.Generic.IEnumerable<int> System.Linq.Enumerable.Select<object,int>(System.Collections.Generic.IEnumerable<object>,System.Func<object,int>)
		// object[] System.Linq.Enumerable.ToArray<object>(System.Collections.Generic.IEnumerable<object>)
		// System.Collections.Generic.List<int> System.Linq.Enumerable.ToList<int>(System.Collections.Generic.IEnumerable<int>)
		// System.Collections.Generic.IEnumerable<int> System.Linq.Enumerable.Where<int>(System.Collections.Generic.IEnumerable<int>,System.Func<int,bool>)
		// System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.Where<object>(System.Collections.Generic.IEnumerable<object>,System.Func<object,bool>)
		// System.Collections.Generic.IEnumerable<byte> System.Linq.Enumerable.Iterator<object>.Select<byte>(System.Func<object,byte>)
		// System.Collections.Generic.IEnumerable<int> System.Linq.Enumerable.Iterator<object>.Select<int>(System.Func<object,int>)
		// System.Linq.Expressions.Expression<object> System.Linq.Expressions.Expression.Lambda<object>(System.Linq.Expressions.Expression,System.Linq.Expressions.ParameterExpression[])
		// System.Linq.Expressions.Expression<object> System.Linq.Expressions.Expression.Lambda<object>(System.Linq.Expressions.Expression,bool,System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression>)
		// System.Linq.Expressions.Expression<object> System.Linq.Expressions.Expression.Lambda<object>(System.Linq.Expressions.Expression,string,bool,System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression>)
		// System.Span<byte> System.MemoryExtensions.AsSpan<byte>(byte[],int,int)
		// object System.Reflection.CustomAttributeExtensions.GetCustomAttribute<object>(System.Reflection.MemberInfo)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,Ux.StateGameIn.<OnEnter>d__1>(Cysharp.Threading.Tasks.UniTask.Awaiter&,Ux.StateGameIn.<OnEnter>d__1&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,Ux.UIMgr.<_LoadTextureFn>d__74>(Cysharp.Threading.Tasks.UniTask.Awaiter&,Ux.UIMgr.<_LoadTextureFn>d__74&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,Ux.StateGameIn.<OnEnter>d__1>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,Ux.StateGameIn.<OnEnter>d__1&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,Ux.StateLogin.<OnEnter>d__1>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,Ux.StateLogin.<OnEnter>d__1&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start<Ux.StateGameIn.<OnEnter>d__1>(Ux.StateGameIn.<OnEnter>d__1&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start<Ux.StateLogin.<OnEnter>d__1>(Ux.StateLogin.<OnEnter>d__1&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start<Ux.UIMgr.<_LoadTextureFn>d__74>(Ux.UIMgr.<_LoadTextureFn>d__74&)
		// System.Void* Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AddressOf<UnityEngine.Vector2>(UnityEngine.Vector2&)
		// int Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf<UnityEngine.Vector2>()
		// object UnityEngine.Component.GetComponent<object>()
		// object UnityEngine.GameObject.AddComponent<object>()
		// object UnityEngine.GameObject.GetComponent<object>()
		// object UnityEngine.GameObject.GetComponentInChildren<object>()
		// object UnityEngine.GameObject.GetComponentInChildren<object>(bool)
		// UnityEngine.Vector2 UnityEngine.InputSystem.InputAction.CallbackContext.ReadValue<UnityEngine.Vector2>()
		// UnityEngine.Vector2 UnityEngine.InputSystem.InputActionState.ApplyProcessors<UnityEngine.Vector2>(int,UnityEngine.Vector2,UnityEngine.InputSystem.InputControl<UnityEngine.Vector2>)
		// UnityEngine.Vector2 UnityEngine.InputSystem.InputActionState.ReadValue<UnityEngine.Vector2>(int,int,bool)
		// System.Void UnityEngine.Playables.PlayableExtensions.ConnectInput<UnityEngine.Animations.AnimationMixerPlayable,UnityEngine.Animations.AnimationClipPlayable>(UnityEngine.Animations.AnimationMixerPlayable,int,UnityEngine.Animations.AnimationClipPlayable,int)
		// System.Void UnityEngine.Playables.PlayableExtensions.ConnectInput<UnityEngine.Animations.AnimationMixerPlayable,UnityEngine.Animations.AnimationClipPlayable>(UnityEngine.Animations.AnimationMixerPlayable,int,UnityEngine.Animations.AnimationClipPlayable,int,float)
		// System.Void UnityEngine.Playables.PlayableExtensions.ConnectInput<UnityEngine.Playables.Playable,UnityEngine.Animations.AnimationMixerPlayable>(UnityEngine.Playables.Playable,int,UnityEngine.Animations.AnimationMixerPlayable,int)
		// System.Void UnityEngine.Playables.PlayableExtensions.ConnectInput<UnityEngine.Playables.Playable,UnityEngine.Animations.AnimationMixerPlayable>(UnityEngine.Playables.Playable,int,UnityEngine.Animations.AnimationMixerPlayable,int,float)
		// UnityEngine.Playables.PlayableGraph UnityEngine.Playables.PlayableExtensions.GetGraph<UnityEngine.Animations.AnimationMixerPlayable>(UnityEngine.Animations.AnimationMixerPlayable)
		// UnityEngine.Playables.PlayableGraph UnityEngine.Playables.PlayableExtensions.GetGraph<UnityEngine.Playables.Playable>(UnityEngine.Playables.Playable)
		// int UnityEngine.Playables.PlayableExtensions.GetInputCount<UnityEngine.Animations.AnimationLayerMixerPlayable>(UnityEngine.Animations.AnimationLayerMixerPlayable)
		// int UnityEngine.Playables.PlayableExtensions.GetInputCount<UnityEngine.Animations.AnimationMixerPlayable>(UnityEngine.Animations.AnimationMixerPlayable)
		// float UnityEngine.Playables.PlayableExtensions.GetInputWeight<UnityEngine.Animations.AnimationMixerPlayable>(UnityEngine.Animations.AnimationMixerPlayable,int)
		// float UnityEngine.Playables.PlayableExtensions.GetInputWeight<UnityEngine.Playables.Playable>(UnityEngine.Playables.Playable,int)
		// UnityEngine.Playables.PlayState UnityEngine.Playables.PlayableExtensions.GetPlayState<UnityEngine.Playables.Playable>(UnityEngine.Playables.Playable)
		// double UnityEngine.Playables.PlayableExtensions.GetSpeed<UnityEngine.Playables.Playable>(UnityEngine.Playables.Playable)
		// double UnityEngine.Playables.PlayableExtensions.GetTime<UnityEngine.Playables.Playable>(UnityEngine.Playables.Playable)
		// bool UnityEngine.Playables.PlayableExtensions.IsDone<UnityEngine.Playables.Playable>(UnityEngine.Playables.Playable)
		// bool UnityEngine.Playables.PlayableExtensions.IsValid<UnityEngine.Playables.Playable>(UnityEngine.Playables.Playable)
		// System.Void UnityEngine.Playables.PlayableExtensions.Pause<UnityEngine.Animations.AnimationClipPlayable>(UnityEngine.Animations.AnimationClipPlayable)
		// System.Void UnityEngine.Playables.PlayableExtensions.Pause<UnityEngine.Playables.Playable>(UnityEngine.Playables.Playable)
		// System.Void UnityEngine.Playables.PlayableExtensions.Play<UnityEngine.Playables.Playable>(UnityEngine.Playables.Playable)
		// System.Void UnityEngine.Playables.PlayableExtensions.SetDone<UnityEngine.Animations.AnimationClipPlayable>(UnityEngine.Animations.AnimationClipPlayable,bool)
		// System.Void UnityEngine.Playables.PlayableExtensions.SetDone<UnityEngine.Playables.Playable>(UnityEngine.Playables.Playable,bool)
		// System.Void UnityEngine.Playables.PlayableExtensions.SetDuration<UnityEngine.Animations.AnimationClipPlayable>(UnityEngine.Animations.AnimationClipPlayable,double)
		// System.Void UnityEngine.Playables.PlayableExtensions.SetInputCount<UnityEngine.Animations.AnimationLayerMixerPlayable>(UnityEngine.Animations.AnimationLayerMixerPlayable,int)
		// System.Void UnityEngine.Playables.PlayableExtensions.SetInputCount<UnityEngine.Animations.AnimationMixerPlayable>(UnityEngine.Animations.AnimationMixerPlayable,int)
		// System.Void UnityEngine.Playables.PlayableExtensions.SetInputWeight<UnityEngine.Animations.AnimationMixerPlayable>(UnityEngine.Animations.AnimationMixerPlayable,int,float)
		// System.Void UnityEngine.Playables.PlayableExtensions.SetInputWeight<UnityEngine.Playables.Playable>(UnityEngine.Playables.Playable,int,float)
		// System.Void UnityEngine.Playables.PlayableExtensions.SetSpeed<UnityEngine.Animations.AnimationClipPlayable>(UnityEngine.Animations.AnimationClipPlayable,double)
		// System.Void UnityEngine.Playables.PlayableExtensions.SetSpeed<UnityEngine.Playables.Playable>(UnityEngine.Playables.Playable,double)
		// System.Void UnityEngine.Playables.PlayableExtensions.SetTime<UnityEngine.Playables.Playable>(UnityEngine.Playables.Playable,double)
		// bool UnityEngine.Playables.PlayableGraph.Connect<UnityEngine.Animations.AnimationClipPlayable,UnityEngine.Animations.AnimationMixerPlayable>(UnityEngine.Animations.AnimationClipPlayable,int,UnityEngine.Animations.AnimationMixerPlayable,int)
		// bool UnityEngine.Playables.PlayableGraph.Connect<UnityEngine.Animations.AnimationMixerPlayable,UnityEngine.Playables.Playable>(UnityEngine.Animations.AnimationMixerPlayable,int,UnityEngine.Playables.Playable,int)
		// bool UnityEngine.Playables.PlayableGraph.Connect<UnityEngine.Playables.Playable,UnityEngine.Playables.Playable>(UnityEngine.Playables.Playable,int,UnityEngine.Playables.Playable,int)
		// System.Void UnityEngine.Playables.PlayableGraph.DestroySubgraph<UnityEngine.Playables.Playable>(UnityEngine.Playables.Playable)
		// System.Void UnityEngine.Playables.PlayableGraph.Disconnect<UnityEngine.Playables.Playable>(UnityEngine.Playables.Playable,int)
		// int UnityEngine.Playables.PlayableOutputExtensions.GetSourceOutputPort<UnityEngine.Animations.AnimationPlayableOutput>(UnityEngine.Animations.AnimationPlayableOutput)
		// System.Void UnityEngine.Playables.PlayableOutputExtensions.SetSourcePlayable<UnityEngine.Animations.AnimationPlayableOutput,UnityEngine.Animations.AnimationLayerMixerPlayable>(UnityEngine.Animations.AnimationPlayableOutput,UnityEngine.Animations.AnimationLayerMixerPlayable)
		// System.Void Ux.DictionaryEx.ForEachValue<object,object>(System.Collections.Generic.Dictionary<object,object>,System.Action<object>)
		// object Ux.GameObjectEx.GetOrAddComponent<object>(UnityEngine.GameObject)
		// System.Void Ux.StateMachine.AddNode<object>(object,bool)
		// object Ux.StateMachine.CreateByPool<object>(bool,object)
		// bool Ux.StateMachine.Enter<object>()
		// System.Void Ux.StateMachine.ForEach<object>(System.Action<object>)
		// object YooAsset.AssetHandle.GetAssetObject<object>()
		// YooAsset.AssetHandle YooAsset.ResourcePackage.LoadAssetAsync<object>(string,uint)
		// YooAsset.AssetHandle YooAsset.ResourcePackage.LoadAssetSync<object>(string)
		// string string.Join<object>(string,System.Collections.Generic.IEnumerable<object>)
		// string string.JoinCore<object>(System.Char*,int,System.Collections.Generic.IEnumerable<object>)
	}
}