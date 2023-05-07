public class AOTGenericReferences : UnityEngine.MonoBehaviour
{

	// {{ AOT assemblies
	// System.Core.dll
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
	// System.Action<object>
	// System.Action<UnityEngine.InputSystem.InputAction.CallbackContext>
	// System.Collections.Generic.Dictionary<int,object>
	// System.Collections.Generic.Dictionary<object,object>
	// System.Collections.Generic.Dictionary.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,object>
	// System.Collections.Generic.IEnumerable<object>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerator<object>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.KeyValuePair<object,object>
	// System.Collections.Generic.List<object>
	// System.Collections.Generic.List<UnityEngine.Vector3>
	// System.Collections.Generic.List<byte>
	// System.Collections.Generic.List.Enumerator<object>
	// System.Collections.Generic.Stack<object>
	// System.Func<byte>
	// System.Func<System.Collections.Generic.KeyValuePair<object,object>,byte>
	// System.Func<object,int,int,object>
	// System.IEquatable<object>
	// System.Nullable<short>
	// System.Nullable<byte>
	// System.Nullable<double>
	// System.Nullable<float>
	// System.Nullable<int>
	// System.Nullable<long>
	// UnityEngine.InputSystem.InputControl<UnityEngine.Vector2>
	// UnityEngine.InputSystem.Utilities.ReadOnlyArray<UnityEngine.InputSystem.InputControlScheme>
	// Ux.IAwakeSystem<object>
	// Ux.ModuleBase<object>
	// }}

	public void RefMethods()
	{
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,Ux.UnitStateNode.<AddAnimation>d__14>(Cysharp.Threading.Tasks.UniTask.Awaiter&,Ux.UnitStateNode.<AddAnimation>d__14&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,Ux.AStarComponent.<_load>d__2>(Cysharp.Threading.Tasks.UniTask.Awaiter&,Ux.AStarComponent.<_load>d__2&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,Ux.Player.<LoadPlayer>d__24>(Cysharp.Threading.Tasks.UniTask.Awaiter&,Ux.Player.<LoadPlayer>d__24&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.Start<Ux.AStarComponent.<_load>d__2>(Ux.AStarComponent.<_load>d__2&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.Start<Ux.UnitStateNode.<AddAnimation>d__14>(Ux.UnitStateNode.<AddAnimation>d__14&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.Start<Ux.Player.<LoadPlayer>d__24>(Ux.Player.<LoadPlayer>d__24&)
		// byte[] System.Array.Empty<byte>()
		// System.Collections.Generic.KeyValuePair<object,object> System.Linq.Enumerable.ElementAt<System.Collections.Generic.KeyValuePair<object,object>>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,object>>,int)
		// System.Collections.Generic.KeyValuePair<object,object> System.Linq.Enumerable.First<System.Collections.Generic.KeyValuePair<object,object>>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,object>>)
		// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,object>> System.Linq.Enumerable.Where<System.Collections.Generic.KeyValuePair<object,object>>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,object>>,System.Func<System.Collections.Generic.KeyValuePair<object,object>,bool>)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,Ux.MapModule.<EnterMap>d__2>(Cysharp.Threading.Tasks.UniTask.Awaiter&,Ux.MapModule.<EnterMap>d__2&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,Ux.LoginModule.<LoginAccountRPC>d__3>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,Ux.LoginModule.<LoginAccountRPC>d__3&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start<Ux.MapModule.<EnterMap>d__2>(Ux.MapModule.<EnterMap>d__2&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start<Ux.LoginModule.<LoginAccountRPC>d__3>(Ux.LoginModule.<LoginAccountRPC>d__3&)
		// object UnityEngine.GameObject.GetComponent<object>()
		// object UnityEngine.GameObject.GetComponentInChildren<object>()
		// UnityEngine.Vector2 UnityEngine.InputSystem.InputAction.CallbackContext.ReadValue<UnityEngine.Vector2>()
		// object Ux.Entity.AddChild<object,object>(object,bool)
		// object Ux.Entity.AddComponent<object,object>(object,bool)
		// object Ux.Entity.AddComponent<object>(bool)
		// object Ux.Entity.Create<object>(bool)
		// System.Void Ux.EventMgr.___SetEvtAttribute<object>()
		// object Ux.GameObjectEx.GetOrAddComponent<object>(UnityEngine.GameObject)
		// Cysharp.Threading.Tasks.UniTask<object> Ux.ModuleBase<object>.Call<object>(uint,object)
		// YooAsset.AssetOperationHandle Ux.ResMgr.LoadAssetAsync<object>(string)
		// System.Void Ux.StateMachine.AddNode<object>(object,bool)
		// System.Void Ux.StateMachine.Enter<object>(object)
		// System.Void Ux.StateMachine.ForEach<object>(System.Action<object>)
		// System.Void Ux.UIMgr.Hide<object>(bool)
		// Ux.UIMgr.UITask<object> Ux.UIMgr.Show<object>(object,bool)
		// object Ux.UIObject.ObjAs<object>()
		// object YooAsset.AssetOperationHandle.GetAssetObject<object>()
	}
}