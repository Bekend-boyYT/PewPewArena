#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using SniperGame.Weapons;

namespace SniperGame.EditorTools
{
    [CustomEditor(typeof(SniperWeapon))]
    public class SniperWeaponEditor : UnityEditor.Editor
    {
        private int _previewSlot = 0;

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        }

        private void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                SniperWeapon weapon = target as SniperWeapon;
                if (weapon != null && weapon.IsEditorPreviewActive)
                {
                    weapon.ClearEditorPreview();
                }
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            SniperWeapon weapon = (SniperWeapon)target;

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("3rd-Person Weapon Socket Live Tuner", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Preview controls
            EditorGUILayout.BeginHorizontal();
            if (!weapon.IsEditorPreviewActive)
            {
                GUI.backgroundColor = new Color(0.35f, 0.85f, 0.45f);
                if (GUILayout.Button("▶ Preview Weapon in Scene View", GUILayout.Height(32)))
                {
                    weapon.ShowEditorPreview(_previewSlot);
                }
                GUI.backgroundColor = Color.white;
            }
            else
            {
                GUI.backgroundColor = new Color(0.9f, 0.35f, 0.35f);
                if (GUILayout.Button("■ Clear Scene Preview", GUILayout.Height(32)))
                {
                    weapon.ClearEditorPreview();
                }
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            if (weapon.IsEditorPreviewActive)
            {
                EditorGUILayout.Space(6);
                int newSlot = EditorGUILayout.Popup("Preview Weapon", _previewSlot, new string[] { "Sniper 1 (Classic)", "Sniper 2 (Heavy)" });
                if (newSlot != _previewSlot)
                {
                    _previewSlot = newSlot;
                    weapon.ShowEditorPreview(_previewSlot);
                }

                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("1-Click Alignment Presets", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Forward Combat Ready"))
                {
                    Undo.RecordObject(weapon, "Set Combat Ready Preset");
                    weapon.SocketLocalPosition = new Vector3(0.06f, 0.0f, 0.01f);
                    weapon.SocketLocalRotation = new Vector3(-146f, 74f, 33f);
                    weapon.ApplySocketTransform();
                    EditorUtility.SetDirty(weapon);
                }
                if (GUILayout.Button("Patrol / Low Ready"))
                {
                    Undo.RecordObject(weapon, "Set Low Ready Preset");
                    weapon.SocketLocalPosition = new Vector3(0.06f, -0.05f, 0.02f);
                    weapon.SocketLocalRotation = new Vector3(-110f, 60f, 15f);
                    weapon.ApplySocketTransform();
                    EditorUtility.SetDirty(weapon);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Flip Scope 180°"))
                {
                    Undo.RecordObject(weapon, "Flip Scope 180");
                    Vector3 rot = weapon.SocketLocalRotation;
                    rot.z = Mathf.Repeat(rot.z + 180f + 180f, 360f) - 180f;
                    weapon.SocketLocalRotation = rot;
                    weapon.ApplySocketTransform();
                    EditorUtility.SetDirty(weapon);
                }
                if (GUILayout.Button("Flip Barrel 180°"))
                {
                    Undo.RecordObject(weapon, "Flip Barrel 180");
                    Vector3 rot = weapon.SocketLocalRotation;
                    rot.y = Mathf.Repeat(rot.y + 180f + 180f, 360f) - 180f;
                    weapon.SocketLocalRotation = rot;
                    weapon.ApplySocketTransform();
                    EditorUtility.SetDirty(weapon);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(6);
                EditorGUILayout.HelpBox("INTERACTIVE GIZMO ACTIVE: In the Scene View, standard Move (W) and Rotate (E) handles are active on the weapon socket. You can drag them with your mouse to visually position and rotate the rifle in real-time!", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void OnSceneGUI()
        {
            SniperWeapon weapon = (SniperWeapon)target;
            if (!weapon.IsEditorPreviewActive || weapon.WeaponSocket == null) return;

            Transform socket = weapon.WeaponSocket;

            EditorGUI.BeginChangeCheck();
            Vector3 newPos = Handles.PositionHandle(socket.position, socket.rotation);
            Quaternion newRot = Handles.RotationHandle(socket.rotation, socket.position);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(weapon, "Transform Weapon Socket");
                socket.position = newPos;
                socket.rotation = newRot;

                weapon.SocketLocalPosition = socket.localPosition;
                weapon.SocketLocalRotation = socket.localEulerAngles;
                EditorUtility.SetDirty(weapon);
            }
        }
    }
}
#endif
