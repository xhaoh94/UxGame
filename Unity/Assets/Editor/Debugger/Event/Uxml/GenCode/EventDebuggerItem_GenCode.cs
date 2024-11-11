//自动生成的代码，请勿修改!!!
//CodeGen By [Assets/UIElements/CodeGenByUxml]
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
namespace Ux.Editor.Debugger.Event
{
	public partial class EventDebuggerItem
	{
		protected VisualElement root;
		public Label txtID;
		public ListView listEvt;
		protected void CreateChildren()
		{
			var _visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Debugger/Event/Uxml/EventDebuggerItem.uxml");
			if (_visualAsset == null) return;
			root = _visualAsset.CloneTree();
			root.style.flexGrow = 1f;
			txtID = root.Q<Label>("txtID");
			listEvt = root.Q<ListView>("listEvt");
			listEvt.makeItem = ()=> { var e = new VisualElement(); _OnMakeListEvtItem(e); return e; };
			listEvt.bindItem = (e,i)=> _OnBindListEvtItem(e,i);
			#if UNITY_2022_1_OR_NEWER
			listEvt.selectionChanged += e => _OnListEvtItemClick(e);
			#else
			listEvt.onSelectionChange += e => _OnListEvtItemClick(e);
			#endif
		}
		partial void _OnMakeListEvtItem(VisualElement e);
		partial void _OnBindListEvtItem(VisualElement e,int index);
		partial void _OnListEvtItemClick(IEnumerable<object> objs);
	}
}
