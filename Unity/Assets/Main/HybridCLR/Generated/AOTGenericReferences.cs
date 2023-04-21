public class AOTGenericReferences : UnityEngine.MonoBehaviour
{

	// {{ AOT assemblies
	// UniTask.dll
	// Unity.Main.dll
	// mscorlib.dll
	// }}

	// {{ constraint implement type
	// }} 

	// {{ AOT generic types
	// Cysharp.Threading.Tasks.UniTask<object>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<object>
	// Main.ModuleBase<object>
	// Singleton<object>
	// System.Collections.Generic.Dictionary<object,object>
	// System.Collections.Generic.List<object>
	// System.Func<byte>
	// }}

	public void RefMethods()
	{
		// System.Void Main.EventMgr.___SetEvtAttribute<object>()
		// Cysharp.Threading.Tasks.UniTask<object> Main.ModuleBase<object>.Call<object>(uint,object)
		// System.Void Main.UIMgr.Hide<object>(bool)
		// Main.UIMgr.UITask Main.UIMgr.Show<object>(object,bool)
		// object Main.UIObject.ObjAs<object>()
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,Hotfix.LoginModule.<LoginAccountRPC>d__2>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,Hotfix.LoginModule.<LoginAccountRPC>d__2&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start<Hotfix.LoginModule.<LoginAccountRPC>d__2>(Hotfix.LoginModule.<LoginAccountRPC>d__2&)
	}
}