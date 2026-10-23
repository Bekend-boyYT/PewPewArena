#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SniperGame.EditorTools
{
    [InitializeOnLoad]
    public static class SoldierUpperBodySetup
    {
        private const string ControllerPath = "Assets/_Project/Animations/Character/SoldierAnimatorController.controller";
        private const string ResourceControllerPath = "Assets/Resources/SoldierAnimatorController.controller";
        private const string MaskPath = "Assets/_Project/Animations/Character/Soldier_UpperBody.mask";
        private const string FbxPath = "Assets/_Project/Scenes/Models/Soldier model/Low poly soldier.fbx";
        private const string ClipsPath = "Assets/_Project/Animations/Character/Clips";

        static SoldierUpperBodySetup()
        {
            EditorApplication.delayCall += () =>
            {
                // Auto-run if mask does not exist or UpperBody Layer missing
                if (!File.Exists(MaskPath) || NeedsUpperBodyLayer())
                {
                    ExecuteSetup();
                }
            };
        }

        private static bool NeedsUpperBodyLayer()
        {
            AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (ctrl == null) return false;
            foreach (var l in ctrl.layers)
            {
                if (l.name == "UpperBody Layer") return false;
            }
            return true;
        }

        [MenuItem("Tools/PewPewArena/Setup Soldier UpperBody Layer")]
        public static void ExecuteSetup()
        {
            Debug.Log("[SoldierUpperBodySetup] Starting Setup...");

            AvatarMask mask = SetupAvatarMask();
            SetupAnimatorController(ControllerPath, mask);

            // Copy to Resources
            if (File.Exists(ControllerPath))
            {
                string dir = Path.GetDirectoryName(ResourceControllerPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.Copy(ControllerPath, ResourceControllerPath, true);
                Debug.Log($"[SoldierUpperBodySetup] Synced controller to {ResourceControllerPath}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SoldierUpperBodySetup] Setup Completed Successfully!");
        }

        public static AvatarMask SetupAvatarMask()
        {
            AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(MaskPath);
            if (mask == null)
            {
                mask = new AvatarMask();
                mask.name = "Soldier_UpperBody";
                AssetDatabase.CreateAsset(mask, MaskPath);
            }

            // Gather all transform paths from the FBX rig
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            List<string> transformPaths = new List<string>();

            if (fbx != null)
            {
                Transform rigRoot = fbx.transform.Find("rig");
                if (rigRoot != null)
                {
                    CollectPathsRecursive(rigRoot, "rig", transformPaths);
                }
                else
                {
                    CollectPathsRecursive(fbx.transform, "", transformPaths);
                }
            }

            if (transformPaths.Count == 0)
            {
                Debug.LogWarning("[SoldierUpperBodySetup] Could not load FBX transforms, using fallback bone paths.");
                transformPaths = GetFallbackBonePaths();
            }

            mask.transformCount = transformPaths.Count;
            int upperCount = 0;
            int lowerCount = 0;

            for (int i = 0; i < transformPaths.Count; i++)
            {
                string path = transformPaths[i];
                mask.SetTransformPath(i, path);

                bool isUpper = IsUpperBodyBone(path);
                mask.SetTransformActive(i, isUpper);

                if (isUpper) upperCount++;
                else lowerCount++;
            }

            EditorUtility.SetDirty(mask);
            Debug.Log($"[SoldierUpperBodySetup] AvatarMask configured with {transformPaths.Count} transforms ({upperCount} UpperBody active, {lowerCount} LowerBody disabled).");
            return mask;
        }

        private static void CollectPathsRecursive(Transform current, string currentPath, List<string> list)
        {
            if (!string.IsNullOrEmpty(currentPath))
            {
                list.Add(currentPath);
            }

            for (int i = 0; i < current.childCount; i++)
            {
                Transform child = current.GetChild(i);
                string childPath = string.IsNullOrEmpty(currentPath) ? child.name : $"{currentPath}/{child.name}";
                CollectPathsRecursive(child, childPath, list);
            }
        }

        public static bool IsUpperBodyBone(string path)
        {
            string lower = path.ToLowerInvariant();

            // Lower body and root trajectory bones must always be inactive on UpperBody layer
            if (lower.Contains("thigh") || lower.Contains("leg") || lower.Contains("foot") ||
                lower.Contains("toe") || lower.EndsWith("c_pos") || lower.EndsWith("c_traj") ||
                lower.EndsWith("c_root.x") || lower.EndsWith("root.x") || lower == "rig")
            {
                return false;
            }

            // Upper body bones: Spine, chest, neck, head, shoulders, arms, hands, fingers, hand IK
            if (lower.Contains("spine") || lower.Contains("neck") || lower.Contains("head") ||
                lower.Contains("shoulder") || lower.Contains("arm") || lower.Contains("forearm") ||
                lower.Contains("hand") || lower.Contains("finger") || lower.Contains("thumb") ||
                lower.Contains("index") || lower.Contains("middle") || lower.Contains("ring") ||
                lower.Contains("pinky"))
            {
                return true;
            }

            return false;
        }

        public static void SetupAnimatorController(string path, AvatarMask mask)
        {
            AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (ctrl == null)
            {
                Debug.LogError($"[SoldierUpperBodySetup] AnimatorController not found at {path}");
                return;
            }

            // Ensure parameters exist
            AddParameterIfMissing(ctrl, "Shoot", AnimatorControllerParameterType.Trigger);
            AddParameterIfMissing(ctrl, "Reload", AnimatorControllerParameterType.Trigger);
            AddParameterIfMissing(ctrl, "IsScoped", AnimatorControllerParameterType.Bool);

            // Find or create UpperBody Layer
            int upperIndex = -1;
            var layers = ctrl.layers;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].name == "UpperBody Layer")
                {
                    upperIndex = i;
                    break;
                }
            }

            if (upperIndex == -1)
            {
                ctrl.AddLayer("UpperBody Layer");
                layers = ctrl.layers;
                upperIndex = layers.Length - 1;
            }

            // Configure layer settings
            layers[upperIndex].avatarMask = mask;
            layers[upperIndex].defaultWeight = 1.0f;
            layers[upperIndex].blendingMode = AnimatorLayerBlendingMode.Override;
            layers[upperIndex].iKPass = false;
            ctrl.layers = layers;

            AnimatorStateMachine sm = layers[upperIndex].stateMachine;

            // Load animation clips
            AnimationClip clipIdle = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{ClipsPath}/Soldier_Idle.anim");
            AnimationClip clipShoot = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{ClipsPath}/Soldier_Shoot.anim");
            AnimationClip clipReload = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{ClipsPath}/Soldier_Reload.anim");
            AnimationClip clipScope = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{ClipsPath}/Soldier_Scope.anim");
            AnimationClip clipEmpty = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{ClipsPath}/Soldier_empty.anim");

            // States
            AnimatorState stateReady = GetOrCreateState(sm, "Combat_Ready", clipIdle, new Vector3(300, 100, 0));
            AnimatorState stateShoot = GetOrCreateState(sm, "Combat_Shoot", clipShoot, new Vector3(600, 0, 0));
            AnimatorState stateReload = GetOrCreateState(sm, "Combat_Reload", clipReload, new Vector3(600, 100, 0));
            AnimatorState stateScope = GetOrCreateState(sm, "Combat_Scope", clipScope, new Vector3(600, 200, 0));
            AnimatorState stateRagdoll = GetOrCreateState(sm, "Combat_Ragdoll", clipEmpty, new Vector3(300, -100, 0));

            sm.defaultState = stateReady;

            // Clean existing transitions to prevent duplicate buildup
            ClearTransitions(sm, stateReady, stateShoot, stateReload, stateScope, stateRagdoll);

            // AnyState -> Combat_Shoot
            var tShoot = sm.AddAnyStateTransition(stateShoot);
            tShoot.AddCondition(AnimatorConditionMode.If, 0, "Shoot");
            tShoot.duration = 0.05f;
            tShoot.hasExitTime = false;
            tShoot.canTransitionToSelf = false;

            // Combat_Shoot -> Combat_Ready
            var tShootBack = stateShoot.AddTransition(stateReady);
            tShootBack.hasExitTime = true;
            tShootBack.exitTime = 0.85f;
            tShootBack.duration = 0.15f;

            // AnyState -> Combat_Reload
            var tReload = sm.AddAnyStateTransition(stateReload);
            tReload.AddCondition(AnimatorConditionMode.If, 0, "Reload");
            tReload.duration = 0.08f;
            tReload.hasExitTime = false;
            tReload.canTransitionToSelf = false;

            // Combat_Reload -> Combat_Ready
            var tReloadBack = stateReload.AddTransition(stateReady);
            tReloadBack.hasExitTime = true;
            tReloadBack.exitTime = 0.90f;
            tReloadBack.duration = 0.15f;

            // Combat_Ready <-> Combat_Scope
            var tScope = stateReady.AddTransition(stateScope);
            tScope.AddCondition(AnimatorConditionMode.If, 0, "IsScoped");
            tScope.duration = 0.15f;
            tScope.hasExitTime = false;

            var tScopeBack = stateScope.AddTransition(stateReady);
            tScopeBack.AddCondition(AnimatorConditionMode.IfNot, 0, "IsScoped");
            tScopeBack.duration = 0.15f;
            tScopeBack.hasExitTime = false;

            // AnyState -> Combat_Ragdoll (Die)
            var tDie = sm.AddAnyStateTransition(stateRagdoll);
            tDie.AddCondition(AnimatorConditionMode.If, 0, "Die");
            tDie.duration = 0.05f;
            tDie.hasExitTime = false;
            tDie.canTransitionToSelf = false;

            // Combat_Ragdoll -> Combat_Ready (Revive)
            var tRevive = stateRagdoll.AddTransition(stateReady);
            tRevive.AddCondition(AnimatorConditionMode.If, 0, "Revive");
            tRevive.duration = 0.1f;
            tRevive.hasExitTime = false;

            EditorUtility.SetDirty(ctrl);
            Debug.Log($"[SoldierUpperBodySetup] Configured UpperBody Layer on {path}");
        }

        private static void ClearTransitions(AnimatorStateMachine sm, params AnimatorState[] states)
        {
            sm.anyStateTransitions = new AnimatorStateTransition[0];
            foreach (var s in states)
            {
                if (s != null) s.transitions = new AnimatorStateTransition[0];
            }
        }

        private static AnimatorState GetOrCreateState(AnimatorStateMachine sm, string name, Motion motion, Vector3 position)
        {
            foreach (var childState in sm.states)
            {
                if (childState.state.name == name)
                {
                    childState.state.motion = motion;
                    return childState.state;
                }
            }

            AnimatorState state = sm.AddState(name, position);
            state.motion = motion;
            return state;
        }

        private static void AddParameterIfMissing(AnimatorController ctrl, string name, AnimatorControllerParameterType type)
        {
            foreach (var param in ctrl.parameters)
            {
                if (param.name == name) return;
            }
            ctrl.AddParameter(name, type);
        }

        private static List<string> GetFallbackBonePaths()
        {
            return new List<string>
            {
                "rig",
                "rig/c_arms_pole.l",
                "rig/c_arms_pole.r",
                "rig/c_foot_ik.l",
                "rig/c_foot_ik.r",
                "rig/c_hand_ik.l",
                "rig/c_hand_ik.r",
                "rig/c_leg_pole.l",
                "rig/c_leg_pole.r",
                "rig/c_pos",
                "rig/c_pos/c_stretch_arm_pin.l",
                "rig/c_pos/c_stretch_arm_pin.r",
                "rig/c_pos/c_stretch_leg_pin.l",
                "rig/c_pos/c_stretch_leg_pin.r",
                "rig/c_pos/c_traj",
                "rig/c_pos/c_traj/arm_stretch.l",
                "rig/c_pos/c_traj/arm_stretch.r",
                "rig/c_pos/c_traj/forearm_stretch.l",
                "rig/c_pos/c_traj/forearm_stretch.r",
                "rig/c_pos/c_traj/head_scale_fix.x",
                "rig/c_pos/c_traj/leg_stretch.l",
                "rig/c_pos/c_traj/leg_stretch.r",
                "rig/c_pos/c_traj/thigh_stretch.l",
                "rig/c_pos/c_traj/thigh_stretch.r",
                "rig/c_pos/c_traj/c_root_master.x",
                "rig/c_pos/c_traj/c_root_master.x/c_root.x",
                "rig/c_pos/c_traj/c_root_master.x/c_root.x/root.x",
                "rig/c_pos/c_traj/c_root_master.x/c_spine_01.x"
            };
        }
    }
}
#endif
