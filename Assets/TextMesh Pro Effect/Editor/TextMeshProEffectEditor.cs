// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace
#pragma warning disable 0649
using UnityEditor;
using UnityEngine;

namespace TMPro.Editor
{
	[CustomEditor(typeof(TextMeshProEffect))]
	public class TextMeshProEffectEditor : UnityEditor.Editor
	{
		public override bool RequiresConstantRepaint()
		{
			return true;
		}

		public override void OnInspectorGUI()
		{
			if (!Application.isPlaying)
			{
				EditorApplication.QueuePlayerLoopUpdate();
				SceneView.RepaintAll();
			}
			base.OnInspectorGUI();
		}
	}
}
