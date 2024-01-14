using System.Collections.Generic;
public class AOTGenericReferences : UnityEngine.MonoBehaviour
{

	// {{ AOT assemblies
	public static readonly IReadOnlyList<string> PatchedAOTAssemblyList = new List<string>
	{
		"Luban.dll",
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
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid.<>c<object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid<object>
	// Cysharp.Threading.Tasks.CompilerServices.IStateMachineRunnerPromise<object>
	// Cysharp.Threading.Tasks.ITaskPoolNode<object>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,object>>
	// Cysharp.Threading.Tasks.IUniTaskSource<object>
	// Cysharp.Threading.Tasks.TaskPool<object>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,object>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<object>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,object>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<object>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,object>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<object>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,object>>
	// Cysharp.Threading.Tasks.UniTask<object>
	// Cysharp.Threading.Tasks.UniTaskCompletionSourceCore<Cysharp.Threading.Tasks.AsyncUnit>
	// Singleton<object>
	// System.Action<System.DateTime>
	// System.Action<UnityEngine.InputSystem.InputAction.CallbackContext>
	// System.Action<UnityEngine.Vector3>
	// System.Action<int>
	// System.Action<long>
	// System.Action<object,object,object>
	// System.Action<object,object>
	// System.Action<object>
	// System.Collections.Generic.ArraySortHelper<System.DateTime>
	// System.Collections.Generic.ArraySortHelper<UnityEngine.Vector3>
	// System.Collections.Generic.ArraySortHelper<int>
	// System.Collections.Generic.ArraySortHelper<long>
	// System.Collections.Generic.ArraySortHelper<object>
	// System.Collections.Generic.Comparer<System.DateTime>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,object>>
	// System.Collections.Generic.Comparer<UnityEngine.Vector3>
	// System.Collections.Generic.Comparer<byte>
	// System.Collections.Generic.Comparer<int>
	// System.Collections.Generic.Comparer<long>
	// System.Collections.Generic.Comparer<object>
	// System.Collections.Generic.Dictionary.Enumerator<System.ValueTuple<int,int>,object>
	// System.Collections.Generic.Dictionary.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.Enumerator<long,object>
	// System.Collections.Generic.Dictionary.Enumerator<object,UnityEngine.Playables.PlayableBinding>
	// System.Collections.Generic.Dictionary.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.Enumerator<uint,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<System.ValueTuple<int,int>,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<long,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,UnityEngine.Playables.PlayableBinding>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<uint,object>
	// System.Collections.Generic.Dictionary.KeyCollection<System.ValueTuple<int,int>,object>
	// System.Collections.Generic.Dictionary.KeyCollection<int,object>
	// System.Collections.Generic.Dictionary.KeyCollection<long,object>
	// System.Collections.Generic.Dictionary.KeyCollection<object,UnityEngine.Playables.PlayableBinding>
	// System.Collections.Generic.Dictionary.KeyCollection<object,int>
	// System.Collections.Generic.Dictionary.KeyCollection<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection<uint,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<System.ValueTuple<int,int>,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<long,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,UnityEngine.Playables.PlayableBinding>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<uint,object>
	// System.Collections.Generic.Dictionary.ValueCollection<System.ValueTuple<int,int>,object>
	// System.Collections.Generic.Dictionary.ValueCollection<int,object>
	// System.Collections.Generic.Dictionary.ValueCollection<long,object>
	// System.Collections.Generic.Dictionary.ValueCollection<object,UnityEngine.Playables.PlayableBinding>
	// System.Collections.Generic.Dictionary.ValueCollection<object,int>
	// System.Collections.Generic.Dictionary.ValueCollection<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection<uint,object>
	// System.Collections.Generic.Dictionary<System.ValueTuple<int,int>,object>
	// System.Collections.Generic.Dictionary<int,object>
	// System.Collections.Generic.Dictionary<long,object>
	// System.Collections.Generic.Dictionary<object,UnityEngine.Playables.PlayableBinding>
	// System.Collections.Generic.Dictionary<object,int>
	// System.Collections.Generic.Dictionary<object,object>
	// System.Collections.Generic.Dictionary<uint,object>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,object>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<int,int>>
	// System.Collections.Generic.EqualityComparer<UnityEngine.Playables.PlayableBinding>
	// System.Collections.Generic.EqualityComparer<byte>
	// System.Collections.Generic.EqualityComparer<int>
	// System.Collections.Generic.EqualityComparer<long>
	// System.Collections.Generic.EqualityComparer<object>
	// System.Collections.Generic.EqualityComparer<uint>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.ValueTuple<int,int>,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,UnityEngine.Playables.PlayableBinding>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<uint,object>>
	// System.Collections.Generic.ICollection<System.DateTime>
	// System.Collections.Generic.ICollection<UnityEngine.Vector3>
	// System.Collections.Generic.ICollection<int>
	// System.Collections.Generic.ICollection<long>
	// System.Collections.Generic.ICollection<object>
	// System.Collections.Generic.IComparer<System.DateTime>
	// System.Collections.Generic.IComparer<UnityEngine.Vector3>
	// System.Collections.Generic.IComparer<int>
	// System.Collections.Generic.IComparer<long>
	// System.Collections.Generic.IComparer<object>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.ValueTuple<int,int>,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,UnityEngine.Playables.PlayableBinding>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<uint,object>>
	// System.Collections.Generic.IEnumerable<System.DateTime>
	// System.Collections.Generic.IEnumerable<UnityEngine.Vector3>
	// System.Collections.Generic.IEnumerable<int>
	// System.Collections.Generic.IEnumerable<long>
	// System.Collections.Generic.IEnumerable<object>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<System.ValueTuple<int,int>,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,UnityEngine.Playables.PlayableBinding>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<uint,object>>
	// System.Collections.Generic.IEnumerator<System.DateTime>
	// System.Collections.Generic.IEnumerator<UnityEngine.Vector3>
	// System.Collections.Generic.IEnumerator<int>
	// System.Collections.Generic.IEnumerator<long>
	// System.Collections.Generic.IEnumerator<object>
	// System.Collections.Generic.IEqualityComparer<System.ValueTuple<int,int>>
	// System.Collections.Generic.IEqualityComparer<int>
	// System.Collections.Generic.IEqualityComparer<long>
	// System.Collections.Generic.IEqualityComparer<object>
	// System.Collections.Generic.IEqualityComparer<uint>
	// System.Collections.Generic.IList<System.DateTime>
	// System.Collections.Generic.IList<UnityEngine.Vector3>
	// System.Collections.Generic.IList<int>
	// System.Collections.Generic.IList<long>
	// System.Collections.Generic.IList<object>
	// System.Collections.Generic.KeyValuePair<System.ValueTuple<int,int>,object>
	// System.Collections.Generic.KeyValuePair<int,object>
	// System.Collections.Generic.KeyValuePair<long,object>
	// System.Collections.Generic.KeyValuePair<object,UnityEngine.Playables.PlayableBinding>
	// System.Collections.Generic.KeyValuePair<object,int>
	// System.Collections.Generic.KeyValuePair<object,object>
	// System.Collections.Generic.KeyValuePair<uint,object>
	// System.Collections.Generic.List.Enumerator<System.DateTime>
	// System.Collections.Generic.List.Enumerator<UnityEngine.Vector3>
	// System.Collections.Generic.List.Enumerator<int>
	// System.Collections.Generic.List.Enumerator<long>
	// System.Collections.Generic.List.Enumerator<object>
	// System.Collections.Generic.List<System.DateTime>
	// System.Collections.Generic.List<UnityEngine.Vector3>
	// System.Collections.Generic.List<int>
	// System.Collections.Generic.List<long>
	// System.Collections.Generic.List<object>
	// System.Collections.Generic.ObjectComparer<System.DateTime>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,object>>
	// System.Collections.Generic.ObjectComparer<UnityEngine.Vector3>
	// System.Collections.Generic.ObjectComparer<byte>
	// System.Collections.Generic.ObjectComparer<int>
	// System.Collections.Generic.ObjectComparer<long>
	// System.Collections.Generic.ObjectComparer<object>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,object>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<int,int>>
	// System.Collections.Generic.ObjectEqualityComparer<UnityEngine.Playables.PlayableBinding>
	// System.Collections.Generic.ObjectEqualityComparer<byte>
	// System.Collections.Generic.ObjectEqualityComparer<int>
	// System.Collections.Generic.ObjectEqualityComparer<long>
	// System.Collections.Generic.ObjectEqualityComparer<object>
	// System.Collections.Generic.ObjectEqualityComparer<uint>
	// System.Collections.Generic.Queue.Enumerator<object>
	// System.Collections.Generic.Queue<object>
	// System.Collections.ObjectModel.ReadOnlyCollection<System.DateTime>
	// System.Collections.ObjectModel.ReadOnlyCollection<UnityEngine.Vector3>
	// System.Collections.ObjectModel.ReadOnlyCollection<int>
	// System.Collections.ObjectModel.ReadOnlyCollection<long>
	// System.Collections.ObjectModel.ReadOnlyCollection<object>
	// System.Comparison<System.DateTime>
	// System.Comparison<UnityEngine.Vector3>
	// System.Comparison<int>
	// System.Comparison<long>
	// System.Comparison<object>
	// System.Func<byte>
	// System.Func<int>
	// System.Func<object,byte>
	// System.Func<object,object>
	// System.Nullable<long>
	// System.Predicate<System.DateTime>
	// System.Predicate<UnityEngine.InputSystem.InputControlScheme>
	// System.Predicate<UnityEngine.Vector3>
	// System.Predicate<int>
	// System.Predicate<long>
	// System.Predicate<object>
	// System.Runtime.CompilerServices.ConditionalWeakTable.CreateValueCallback<object,object>
	// System.Runtime.CompilerServices.ConditionalWeakTable.Enumerator<object,object>
	// System.Runtime.CompilerServices.ConditionalWeakTable<object,object>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,object>>
	// System.ValueTuple<byte,object>
	// System.ValueTuple<int,int>
	// UnityEngine.InputSystem.InputBindingComposite<UnityEngine.Vector2>
	// UnityEngine.InputSystem.InputControl<UnityEngine.Vector2>
	// UnityEngine.InputSystem.InputProcessor<UnityEngine.Vector2>
	// UnityEngine.InputSystem.Utilities.InlinedArray<object>
	// UnityEngine.InputSystem.Utilities.ReadOnlyArray.Enumerator<UnityEngine.InputSystem.InputControlScheme>
	// UnityEngine.InputSystem.Utilities.ReadOnlyArray<UnityEngine.InputSystem.InputControlScheme>
	// UnityEngine.Playables.ScriptPlayable<object>
	// Ux.EventMgr.<>c__DisplayClass23_0<object>
	// Ux.EventMgr.EventData<object,object,object>
	// Ux.EventMgr.EventData<object,object>
	// Ux.EventMgr.EventData<object>
	// Ux.EventMgr.EventExe<object,object,object>
	// Ux.EventMgr.EventExe<object,object>
	// Ux.EventMgr.EventExe<object>
	// Ux.IAwakeSystem<UnityEngine.Playables.PlayableGraph,object,object,int>
	// Ux.IAwakeSystem<UnityEngine.Playables.PlayableGraph>
	// Ux.IAwakeSystem<object,object>
	// Ux.IAwakeSystem<object>
	// Ux.ModuleBase<object>
	// Ux.StateMachine.<>c__DisplayClass26_0<object>
	// Ux.TimeMgr.HandleExe<object,object,object>
	// Ux.TimeMgr.HandleExe<object,object>
	// Ux.TimeMgr.HandleExe<object>
	// Ux.UIMgr.UITask<object>
	// }}

	public void RefMethods()
	{
		// string Bright.Common.StringUtil.CollectionToString<object>(System.Collections.Generic.IEnumerable<object>)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,object>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,object&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.Start<object>(object&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.Start<Ux.NetMgr.<Call>d__8<object>>(Ux.NetMgr.<Call>d__8<object>&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.Start<Ux.ResMgr.<LoadAssetAsync>d__27<object>>(Ux.ResMgr.<LoadAssetAsync>d__27<object>&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.Start<Ux.UIMgr.<ShowAsync>d__38<object>>(Ux.UIMgr.<ShowAsync>d__38<object>&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,object>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,object&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.YieldAwaitable.Awaiter,object>(Cysharp.Threading.Tasks.YieldAwaitable.Awaiter&,object&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.Start<object>(object&)
		// object Pool.Get<object>()
		// object Pool.Get<object>(System.Type)
		// object System.Activator.CreateInstance<object>()
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,object>(Cysharp.Threading.Tasks.UniTask.Awaiter&,object&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,object>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,object&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start<object>(object&)
		// System.Void* Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AddressOf<UnityEngine.Vector2>(UnityEngine.Vector2&)
		// int Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf<UnityEngine.Vector2>()
		// object UnityEngine.GameObject.AddComponent<object>()
		// object UnityEngine.GameObject.GetComponent<object>()
		// object UnityEngine.GameObject.GetComponentInChildren<object>()
		// object UnityEngine.GameObject.GetComponentInChildren<object>(bool)
		// UnityEngine.Vector2 UnityEngine.InputSystem.InputAction.CallbackContext.ReadValue<UnityEngine.Vector2>()
		// UnityEngine.Vector2 UnityEngine.InputSystem.InputActionState.ApplyProcessors<UnityEngine.Vector2>(int,UnityEngine.Vector2,UnityEngine.InputSystem.InputControl<UnityEngine.Vector2>)
		// UnityEngine.Vector2 UnityEngine.InputSystem.InputActionState.ReadValue<UnityEngine.Vector2>(int,int,bool)
		// UnityEngine.Playables.PlayableGraph UnityEngine.Playables.PlayableExtensions.GetGraph<UnityEngine.Playables.Playable>(UnityEngine.Playables.Playable)
		// int UnityEngine.Playables.PlayableExtensions.GetInputCount<UnityEngine.Animations.AnimationLayerMixerPlayable>(UnityEngine.Animations.AnimationLayerMixerPlayable)
		// System.Void UnityEngine.Playables.PlayableExtensions.SetInputCount<UnityEngine.Animations.AnimationLayerMixerPlayable>(UnityEngine.Animations.AnimationLayerMixerPlayable,int)
		// int UnityEngine.Playables.PlayableOutputExtensions.GetSourceOutputPort<UnityEngine.Animations.AnimationPlayableOutput>(UnityEngine.Animations.AnimationPlayableOutput)
		// System.Void UnityEngine.Playables.PlayableOutputExtensions.SetSourcePlayable<UnityEngine.Animations.AnimationPlayableOutput,UnityEngine.Animations.AnimationLayerMixerPlayable>(UnityEngine.Animations.AnimationPlayableOutput,UnityEngine.Animations.AnimationLayerMixerPlayable)
		// System.Void Ux.DictionaryEx.ForEachValue<object,object>(System.Collections.Generic.Dictionary<object,object>,System.Action<object>)
		// Ux.Entity Ux.Entity.AddChild<UnityEngine.Playables.PlayableGraph,object,object,int>(long,System.Type,UnityEngine.Playables.PlayableGraph,object,object,int,bool)
		// Ux.Entity Ux.Entity.AddChild<UnityEngine.Playables.PlayableGraph>(long,System.Type,UnityEngine.Playables.PlayableGraph,bool)
		// Ux.Entity Ux.Entity.AddChild<object>(System.Type,object,bool)
		// Ux.Entity Ux.Entity.AddChild<object>(long,System.Type,object,bool)
		// object Ux.Entity.AddChild<object,UnityEngine.Playables.PlayableGraph,object,object,int>(long,UnityEngine.Playables.PlayableGraph,object,object,int,bool)
		// object Ux.Entity.AddChild<object,UnityEngine.Playables.PlayableGraph>(long,UnityEngine.Playables.PlayableGraph,bool)
		// object Ux.Entity.AddChild<object,object>(long,object,bool)
		// object Ux.Entity.AddChild<object,object>(object,bool)
		// Ux.Entity Ux.Entity.AddComponent<object,object>(System.Type,object,object,bool)
		// Ux.Entity Ux.Entity.AddComponent<object>(System.Type,object,bool)
		// object Ux.Entity.AddComponent<object,object,object>(object,object,bool)
		// object Ux.Entity.AddComponent<object,object>(object,bool)
		// object Ux.Entity.AddComponent<object>(bool)
		// object Ux.Entity.Create<object>(bool)
		// object Ux.Entity.GetChild<object>(long)
		// object Ux.Entity.GetComponent<object>()
		// object Ux.Entity.ParentAs<object>()
		// System.Void Ux.Entity._InitSystem<UnityEngine.Playables.PlayableGraph,object,object,int>(UnityEngine.Playables.PlayableGraph,object,object,int)
		// System.Void Ux.Entity._InitSystem<UnityEngine.Playables.PlayableGraph>(UnityEngine.Playables.PlayableGraph)
		// System.Void Ux.Entity._InitSystem<object,object>(object,object)
		// System.Void Ux.Entity._InitSystem<object>(object)
		// object Ux.EntityMono.GetEntity<object>()
		// System.Void Ux.EventMgr.Off<object,object,object>(int,object,System.Action<object,object,object>)
		// System.Void Ux.EventMgr.Off<object,object>(int,object,System.Action<object,object>)
		// System.Void Ux.EventMgr.Off<object>(int,object,System.Action<object>)
		// long Ux.EventMgr.On<object,object,object>(int,object,System.Action<object,object,object>)
		// long Ux.EventMgr.On<object,object>(int,object,System.Action<object,object>)
		// long Ux.EventMgr.On<object>(int,object,System.Action<object>)
		// System.Void Ux.EventMgr.Run<object,object,object>(int,object,object,object)
		// System.Void Ux.EventMgr.Run<object,object>(int,object,object)
		// System.Void Ux.EventMgr.Run<object>(int,object)
		// object Ux.EventMgr._Add<object>(long&,int,object,System.Delegate)
		// object Ux.EventMgr._Add<object>(long)
		// System.Void Ux.EventMgr.___SetEvtAttribute<object>()
		// object Ux.GameObjectEx.GetOrAddComponent<object>(UnityEngine.GameObject)
		// System.Void Ux.MainEventMgrEx.Off<object,object,object>(Ux.EventMgr,Ux.MainEventType,object,System.Action<object,object,object>)
		// System.Void Ux.MainEventMgrEx.Off<object,object>(Ux.EventMgr,Ux.MainEventType,object,System.Action<object,object>)
		// System.Void Ux.MainEventMgrEx.Off<object>(Ux.EventMgr,Ux.MainEventType,object,System.Action<object>)
		// System.Void Ux.MainEventMgrEx.On<object,object,object>(Ux.EventMgr,Ux.MainEventType,object,System.Action<object,object,object>)
		// System.Void Ux.MainEventMgrEx.On<object,object>(Ux.EventMgr,Ux.MainEventType,object,System.Action<object,object>)
		// System.Void Ux.MainEventMgrEx.On<object>(Ux.EventMgr,Ux.MainEventType,object,System.Action<object>)
		// Cysharp.Threading.Tasks.UniTask<object> Ux.NetMgr.Call<object>(uint,object)
		// object Ux.ResMgr.LoadAsset<object>(string,Ux.ResType)
		// Cysharp.Threading.Tasks.UniTask<object> Ux.ResMgr.LoadAssetAsync<object>(string,Ux.ResType)
		// object Ux.ResMgr._LoadAsset<object>(string,YooAsset.AssetHandle)
		// object Ux.SkillAsset.GetClip<object>(string,string)
		// object Ux.SkillAsset.GetTrack<object>(string)
		// System.Void Ux.StateMachine.AddNode<object>(object,bool)
		// System.Void Ux.StateMachine.Enter<object>(object)
		// System.Void Ux.StateMachine.ForEach<object>(System.Action<object>)
		// long Ux.TimeMgr.Create<object,object,object>(bool,Ux.TimeMgr.HandleMap,float,float,int,object,System.Action<object,object,object>,object,object,object,System.Action)
		// long Ux.TimeMgr.Create<object,object,object>(bool,Ux.TimeMgr.HandleMap,float,float,int,object,System.Action<object,object,object>,object,object,object,System.Action<object>,object)
		// long Ux.TimeMgr.Create<object,object>(bool,Ux.TimeMgr.HandleMap,float,float,int,object,System.Action<object,object>,object,object,System.Action)
		// long Ux.TimeMgr.Create<object,object>(bool,Ux.TimeMgr.HandleMap,float,float,int,object,System.Action<object,object>,object,object,System.Action<object>,object)
		// long Ux.TimeMgr.Create<object>(bool,Ux.TimeMgr.HandleMap,float,float,int,object,System.Action<object>,object,System.Action)
		// long Ux.TimeMgr.Create<object>(bool,Ux.TimeMgr.HandleMap,float,float,int,object,System.Action<object>,object,System.Action<object>,object)
		// object Ux.TimeMgr.CreateHandle<object>(long&,Ux.TimeMgr.HandleMap,System.Delegate,object)
		// long Ux.TimeMgr.DoCron<object,object,object>(string,object,System.Action<object,object,object>,object,object,object,bool)
		// long Ux.TimeMgr.DoCron<object,object>(string,object,System.Action<object,object>,object,object,bool)
		// long Ux.TimeMgr.DoCron<object>(string,object,System.Action<object>,object,bool)
		// long Ux.TimeMgr.DoFrame<object,object,object>(int,int,int,object,System.Action<object,object,object>,object,object,object,System.Action)
		// long Ux.TimeMgr.DoFrame<object,object,object>(int,int,int,object,System.Action<object,object,object>,object,object,object,System.Action<object>,object)
		// long Ux.TimeMgr.DoFrame<object,object,object>(int,int,object,System.Action<object,object,object>,object,object,object,System.Action)
		// long Ux.TimeMgr.DoFrame<object,object,object>(int,int,object,System.Action<object,object,object>,object,object,object,System.Action<object>,object)
		// long Ux.TimeMgr.DoFrame<object,object>(int,int,int,object,System.Action<object,object>,object,object,System.Action)
		// long Ux.TimeMgr.DoFrame<object,object>(int,int,int,object,System.Action<object,object>,object,object,System.Action<object>,object)
		// long Ux.TimeMgr.DoFrame<object,object>(int,int,object,System.Action<object,object>,object,object,System.Action)
		// long Ux.TimeMgr.DoFrame<object,object>(int,int,object,System.Action<object,object>,object,object,System.Action<object>,object)
		// long Ux.TimeMgr.DoFrame<object>(int,int,int,object,System.Action<object>,object,System.Action)
		// long Ux.TimeMgr.DoFrame<object>(int,int,int,object,System.Action<object>,object,System.Action<object>,object)
		// long Ux.TimeMgr.DoFrame<object>(int,int,object,System.Action<object>,object,System.Action)
		// long Ux.TimeMgr.DoFrame<object>(int,int,object,System.Action<object>,object,System.Action<object>,object)
		// long Ux.TimeMgr.DoTimeStamp<object,object,object>(long,object,System.Action<object,object,object>,object,object,object,bool)
		// long Ux.TimeMgr.DoTimeStamp<object,object>(long,object,System.Action<object,object>,object,object,bool)
		// long Ux.TimeMgr.DoTimeStamp<object>(long,object,System.Action<object>,object,bool)
		// long Ux.TimeMgr.DoTimer<object,object,object>(float,float,int,object,System.Action<object,object,object>,object,object,object,System.Action)
		// long Ux.TimeMgr.DoTimer<object,object,object>(float,float,int,object,System.Action<object,object,object>,object,object,object,System.Action<object>,object)
		// long Ux.TimeMgr.DoTimer<object,object,object>(float,int,object,System.Action<object,object,object>,object,object,object,System.Action)
		// long Ux.TimeMgr.DoTimer<object,object,object>(float,int,object,System.Action<object,object,object>,object,object,object,System.Action<object>,object)
		// long Ux.TimeMgr.DoTimer<object,object>(float,float,int,object,System.Action<object,object>,object,object,System.Action)
		// long Ux.TimeMgr.DoTimer<object,object>(float,float,int,object,System.Action<object,object>,object,object,System.Action<object>,object)
		// long Ux.TimeMgr.DoTimer<object,object>(float,int,object,System.Action<object,object>,object,object,System.Action)
		// long Ux.TimeMgr.DoTimer<object,object>(float,int,object,System.Action<object,object>,object,object,System.Action<object>,object)
		// long Ux.TimeMgr.DoTimer<object>(float,float,int,object,System.Action<object>,object,System.Action)
		// long Ux.TimeMgr.DoTimer<object>(float,float,int,object,System.Action<object>,object,System.Action<object>,object)
		// long Ux.TimeMgr.DoTimer<object>(float,int,object,System.Action<object>,object,System.Action)
		// long Ux.TimeMgr.DoTimer<object>(float,int,object,System.Action<object>,object,System.Action<object>,object)
		// System.Void Ux.TimeMgr.RemoveCron<object,object,object>(object,System.Action<object,object,object>)
		// System.Void Ux.TimeMgr.RemoveCron<object,object>(object,System.Action<object,object>)
		// System.Void Ux.TimeMgr.RemoveCron<object>(object,System.Action<object>)
		// System.Void Ux.TimeMgr.RemoveFrame<object,object,object>(object,System.Action<object,object,object>)
		// System.Void Ux.TimeMgr.RemoveFrame<object,object>(object,System.Action<object,object>)
		// System.Void Ux.TimeMgr.RemoveFrame<object>(object,System.Action<object>)
		// System.Void Ux.TimeMgr.RemoveTimeStamp<object,object,object>(object,System.Action<object,object,object>)
		// System.Void Ux.TimeMgr.RemoveTimeStamp<object,object>(object,System.Action<object,object>)
		// System.Void Ux.TimeMgr.RemoveTimeStamp<object>(object,System.Action<object>)
		// System.Void Ux.TimeMgr.RemoveTimer<object,object,object>(object,System.Action<object,object,object>)
		// System.Void Ux.TimeMgr.RemoveTimer<object,object>(object,System.Action<object,object>)
		// System.Void Ux.TimeMgr.RemoveTimer<object>(object,System.Action<object>)
		// System.Void Ux.UIDialogFactory.SetDefalutType<object>()
		// System.Void Ux.UIMgr.Hide<object>(bool)
		// Ux.UIMgr.UITask<object> Ux.UIMgr.Show<object>(int,object,bool)
		// Ux.UIMgr.UITask<object> Ux.UIMgr.Show<object>(object,bool)
		// Cysharp.Threading.Tasks.UniTask<object> Ux.UIMgr.ShowAsync<object>(bool,int,object,bool)
		// object Ux.UIObject.ObjAs<object>()
		// System.Void Ux.UITabFrame.SetTabRenderer<object>()
		// object YooAsset.AssetHandle.GetAssetObject<object>()
		// YooAsset.AssetHandle YooAsset.ResourcePackage.LoadAssetSync<object>(string)
		// string string.Join<object>(string,System.Collections.Generic.IEnumerable<object>)
		// string string.JoinCore<object>(System.Char*,int,System.Collections.Generic.IEnumerable<object>)
	}
}