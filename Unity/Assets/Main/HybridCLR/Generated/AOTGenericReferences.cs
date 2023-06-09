public class AOTGenericReferences : UnityEngine.MonoBehaviour
{

	// {{ AOT assemblies
	// Luban.dll
	// UniTask.dll
	// Unity.InputSystem.dll
	// Unity.Main.dll
	// UnityEngine.CoreModule.dll
	// YooAsset.dll
	// mscorlib.dll
	// }}

	// {{ constraint implement type
	// }} 

	// {{ AOT generic types
	// Cysharp.Threading.Tasks.UniTask<object>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<object>
	// Singleton<object>
	// System.Action<UnityEngine.InputSystem.InputAction.CallbackContext>
	// System.Action<object>
	// System.Collections.Generic.Dictionary<System.ValueTuple<int,int>,object>
	// System.Collections.Generic.Dictionary<object,object>
	// System.Collections.Generic.Dictionary<int,object>
	// System.Collections.Generic.IEnumerable<object>
	// System.Collections.Generic.IEnumerator<object>
	// System.Collections.Generic.List<UnityEngine.Vector3>
	// System.Collections.Generic.List<int>
	// System.Collections.Generic.List<object>
	// System.Collections.Generic.List.Enumerator<object>
	// System.Func<byte>
	// System.Func<object,object>
	// System.Nullable<long>
	// System.ValueTuple<int,int>
	// UnityEngine.InputSystem.InputControl<UnityEngine.Vector2>
	// UnityEngine.InputSystem.Utilities.ReadOnlyArray<UnityEngine.InputSystem.InputControlScheme>
	// Ux.IAwakeSystem<object>
	// Ux.ModuleBase<object>
	// Ux.UIMgr.UITask<object>
	// }}

	public void RefMethods()
	{
		// string Bright.Common.StringUtil.CollectionToString<object>(System.Collections.Generic.IEnumerable<object>)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,Ux.UnitStateNode.<AddAnimation>d__14>(Cysharp.Threading.Tasks.UniTask.Awaiter&,Ux.UnitStateNode.<AddAnimation>d__14&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,Ux.AStarComponent.<_load>d__2>(Cysharp.Threading.Tasks.UniTask.Awaiter&,Ux.AStarComponent.<_load>d__2&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,Ux.Player.<LoadPlayer>d__24>(Cysharp.Threading.Tasks.UniTask.Awaiter&,Ux.Player.<LoadPlayer>d__24&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.Start<Ux.AStarComponent.<_load>d__2>(Ux.AStarComponent.<_load>d__2&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.Start<Ux.UnitStateNode.<AddAnimation>d__14>(Ux.UnitStateNode.<AddAnimation>d__14&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.Start<Ux.Player.<LoadPlayer>d__24>(Ux.Player.<LoadPlayer>d__24&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,Ux.MapModule.<EnterMap>d__2>(Cysharp.Threading.Tasks.UniTask.Awaiter&,Ux.MapModule.<EnterMap>d__2&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,Ux.LoginModule.<LoginAccountRPC>d__3>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,Ux.LoginModule.<LoginAccountRPC>d__3&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,Ux.StateGameIn.<OnEnter>d__1>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,Ux.StateGameIn.<OnEnter>d__1&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start<Ux.StateGameIn.<OnEnter>d__1>(Ux.StateGameIn.<OnEnter>d__1&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start<Ux.MapModule.<EnterMap>d__2>(Ux.MapModule.<EnterMap>d__2&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start<Ux.LoginModule.<LoginAccountRPC>d__3>(Ux.LoginModule.<LoginAccountRPC>d__3&)
		// object UnityEngine.GameObject.GetComponent<object>()
		// object UnityEngine.GameObject.GetComponentInChildren<object>()
		// UnityEngine.Vector2 UnityEngine.InputSystem.InputAction.CallbackContext.ReadValue<UnityEngine.Vector2>()
		// object Ux.Entity.AddChild<object,object>(object,bool)
		// object Ux.Entity.AddComponent<object>(bool)
		// object Ux.Entity.AddComponent<object,object>(object,bool)
		// object Ux.Entity.Create<object>(bool)
		// System.Void Ux.EventMgr.___SetEvtAttribute<object>()
		// object Ux.GameObjectEx.GetOrAddComponent<object>(UnityEngine.GameObject)
		// Cysharp.Threading.Tasks.UniTask<object> Ux.ModuleBase<object>.Call<object>(uint,object)
		// YooAsset.AssetOperationHandle Ux.ResMgr.LoadAssetAsync<object>(string)
		// YooAsset.AssetOperationHandle Ux.ResMgr.LoadAssetSync<object>(string)
		// System.Void Ux.StateMachine.AddNode<object>(object,bool)
		// System.Void Ux.StateMachine.Enter<object>(object)
		// System.Void Ux.StateMachine.ForEach<object>(System.Action<object>)
		// System.Void Ux.UIDialogFactory.SetDefalutType<object>()
		// System.Void Ux.UIMgr.Hide<object>(bool)
		// Ux.UIMgr.UITask<object> Ux.UIMgr.Show<object>(object,bool)
		// object Ux.UIObject.ObjAs<object>()
		// object YooAsset.AssetOperationHandle.GetAssetObject<object>()
	}
}