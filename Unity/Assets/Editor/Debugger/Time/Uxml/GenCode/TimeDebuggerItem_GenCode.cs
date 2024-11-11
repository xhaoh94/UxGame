//自动生成的代码，请勿修改!!!
//CodeGen By [Assets/UIElements/CodeGenByUxml]
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
namespace Ux.Editor.Debugger.Time
{
	public partial class TimeDebuggerItem
	{
		protected VisualElement root;
		public Label txtID;
		public ListView list;
		protected void CreateChildren()
		{
			var _visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Debugger/Time/Uxml/TimeDebuggerItem.uxml");
			if (_visualAsset == null) return;
			root = _visualAsset.CloneTree();
			root.style.flexGrow = 1f;
			txtID = root.Q<Label>("txtID");
			list = root.Q<ListView>("list");
			list.makeItem = ()=> { var e = new VisualElement(); _OnMakeListItem(e); return e; };
			list.bindItem = (e,i)=> _OnBindListItem(e,i);
			#if UNITY_2022_1_OR_NEWER
			list.selectionChanged += e => _OnListItemClick(e);
			#else
			list.onSelectionChange += e => _OnListItemClick(e);
			#endif
		}
		partial void _OnMakeListItem(VisualElement e);
		partial void _OnBindListItem(VisualElement e,int index);
		partial void _OnListItemClick(IEnumerable<object> objs);
	}
}
