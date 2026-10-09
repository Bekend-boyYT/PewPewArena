using System;
using UnityEngine;
using UnityEditor;
using SniperGame.Weapons;

namespace SniperGame.Editor
{
    public static class VFXPrefabGenerator
    {
        private const string VfxMatDir = "Assets/_Project/Materials/VFX";
        private const string VfxPrefabDir = "Assets/_Project/Prefabs/VFX";

        [MenuItem("PewPewArena/Generate Sniper VFX Prefabs")]
        public static void GenerateAllPrefabs()
        {
            try
            {
                // Verify materials exist
                Material bulletTracer1 = AssetDatabase.LoadAssetAtPath<Material>($"{VfxMatDir}/BulletTracer1_Mat.mat");
                Material bulletTracer2 = AssetDatabase.LoadAssetAtPath<Material>($"{VfxMatDir}/BulletTracer2_Mat.mat");
                Material muzzleFlash1 = AssetDatabase.LoadAssetAtPath<Material>($"{VfxMatDir}/MuzzleFlash1_Mat.mat");
                Material muzzleFlash2 = AssetDatabase.LoadAssetAtPath<Material>($"{VfxMatDir}/MuzzleFlash2_Mat.mat");
                Material needleSpike1 = AssetDatabase.LoadAssetAtPath<Material>($"{VfxMatDir}/NeedleSpike1_Mat.mat");
                Material needleSpike2 = AssetDatabase.LoadAssetAtPath<Material>($"{VfxMatDir}/NeedleSpike2_Mat.mat");
                Material shockwave2 = AssetDatabase.LoadAssetAtPath<Material>($"{VfxMatDir}/Shockwave2_Mat.mat");
                Material sparks1 = AssetDatabase.LoadAssetAtPath<Material>($"{VfxMatDir}/Sparks1_Mat.mat");
                Material sparks2 = AssetDatabase.LoadAssetAtPath<Material>($"{VfxMatDir}/Sparks2_Mat.mat");
                Material smokeMat = AssetDatabase.LoadAssetAtPath<Material>($"{VfxMatDir}/Smoke_Mat.mat");
                Material punctureDustMat = AssetDatabase.LoadAssetAtPath<Material>($"{VfxMatDir}/PunctureDust_Mat.mat");

                if (bulletTracer1 == null || bulletTracer2 == null)
                {
                    Debug.LogWarning("[VFXPrefabGenerator] Tracer materials not found yet, skipping generation until loaded.");
                    return;
                }

                BuildBullet1(bulletTracer1, sparks1, muzzleFlash1);
                BuildBullet2(bulletTracer2, sparks2, muzzleFlash2);
                BuildMuzzleFlash1(muzzleFlash1, needleSpike1, sparks1, smokeMat);
                BuildMuzzleFlash2(muzzleFlash2, needleSpike2, shockwave2, sparks2, smokeMat);
                BuildImpact1(muzzleFlash1, sparks1, punctureDustMat);
                BuildImpact2(muzzleFlash2, shockwave2, sparks2, punctureDustMat);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[VFXPrefabGenerator] Successfully generated and configured all 6 Sniper VFX prefabs!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VFXPrefabGenerator] Error generating VFX prefabs: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void BuildBullet1(Material tracerMat, Material sparksMat, Material headMat)
        {
            string path = $"{VfxPrefabDir}/Bullet_Sniper1.prefab";
            GameObject root = new GameObject("Bullet_Sniper1");

            root.AddComponent<CosmeticBullet>();

            // TrailRenderer
            TrailRenderer trail = root.AddComponent<TrailRenderer>();
            trail.material = tracerMat;
            trail.time = 0.08f;
            trail.minVertexDistance = 0.08f;
            trail.widthMultiplier = 0.045f;
            trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.8f, 0.95f, 1f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.85f, 0.6f), new GradientAlphaKey(0f, 1f) }
            );
            trail.colorGradient = gradient;
            trail.alignment = LineAlignment.View;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.emitting = true;

            // Trail energy particles (World simulation space)
            GameObject energyGo = new GameObject("TrailEnergy");
            energyGo.transform.SetParent(root.transform, false);
            ParticleSystem ps = energyGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 0.12f;
            main.startSpeed = 0f;
            main.startSize = 0.035f;
            main.startColor = new Color(0.85f, 0.98f, 1f, 0.9f);
            main.loop = true;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.rateOverDistance = 25f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.02f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient colGrad = new Gradient();
            colGrad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.7f, 0.9f, 1f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = colGrad;

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            var psr = energyGo.GetComponent<ParticleSystemRenderer>();
            psr.material = sparksMat;
            psr.renderMode = ParticleSystemRenderMode.Billboard;

            // Head glow
            GameObject headGo = new GameObject("BulletHead");
            headGo.transform.SetParent(root.transform, false);
            ParticleSystem headPs = headGo.AddComponent<ParticleSystem>();
            var hMain = headPs.main;
            hMain.simulationSpace = ParticleSystemSimulationSpace.Local;
            hMain.startLifetime = 0.05f;
            hMain.startSpeed = 0f;
            hMain.startSize = 0.07f;
            hMain.startColor = Color.white;
            hMain.loop = true;
            hMain.playOnAwake = true;

            var hEm = headPs.emission;
            hEm.rateOverTime = 30f;
            hEm.rateOverDistance = 0;

            var hShape = headPs.shape;
            hShape.shapeType = ParticleSystemShapeType.Sphere;
            hShape.radius = 0.01f;

            var hPsr = headGo.GetComponent<ParticleSystemRenderer>();
            hPsr.material = headMat;
            hPsr.renderMode = ParticleSystemRenderMode.Billboard;

            SaveAndDestroy(root, path);
        }

        private static void BuildBullet2(Material tracerMat, Material sparksMat, Material headMat)
        {
            string path = $"{VfxPrefabDir}/Bullet_Sniper2.prefab";
            GameObject root = new GameObject("Bullet_Sniper2");

            root.AddComponent<CosmeticBullet>();

            // TrailRenderer
            TrailRenderer trail = root.AddComponent<TrailRenderer>();
            trail.material = tracerMat;
            trail.time = 0.15f;
            trail.minVertexDistance = 0.08f;
            trail.widthMultiplier = 0.12f;
            trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(1f, 0.75f, 0.2f), 0.5f),
                    new GradientColorKey(new Color(1f, 0.45f, 0.05f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.85f, 0.6f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            trail.colorGradient = gradient;
            trail.alignment = LineAlignment.View;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.emitting = true;

            // Turbulent embers and peeling sparks (World simulation space)
            GameObject emberGo = new GameObject("EmbersAndSparks");
            emberGo.transform.SetParent(root.transform, false);
            ParticleSystem ps = emberGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.38f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
            main.startColor = new Color(1f, 0.72f, 0.18f, 1f);
            main.loop = true;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.rateOverDistance = 35f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient colGrad = new Gradient();
            colGrad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.4f, 0.05f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = colGrad;

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            var psr = emberGo.GetComponent<ParticleSystemRenderer>();
            psr.material = sparksMat;
            psr.renderMode = ParticleSystemRenderMode.Billboard;

            // Heavy head core
            GameObject headGo = new GameObject("BulletHead");
            headGo.transform.SetParent(root.transform, false);
            ParticleSystem headPs = headGo.AddComponent<ParticleSystem>();
            var hMain = headPs.main;
            hMain.simulationSpace = ParticleSystemSimulationSpace.Local;
            hMain.startLifetime = 0.06f;
            hMain.startSpeed = 0f;
            hMain.startSize = 0.16f;
            hMain.startColor = new Color(1f, 0.95f, 0.85f, 1f);
            hMain.loop = true;
            hMain.playOnAwake = true;

            var hEm = headPs.emission;
            hEm.rateOverTime = 30f;
            hEm.rateOverDistance = 0;

            var hShape = headPs.shape;
            hShape.shapeType = ParticleSystemShapeType.Sphere;
            hShape.radius = 0.02f;

            var hPsr = headGo.GetComponent<ParticleSystemRenderer>();
            hPsr.material = headMat;
            hPsr.renderMode = ParticleSystemRenderMode.Billboard;

            SaveAndDestroy(root, path);
        }

        private static void BuildMuzzleFlash1(Material flashMat, Material needleMat, Material sparksMat, Material smokeMat)
        {
            string path = $"{VfxPrefabDir}/MuzzleFlash_Sniper1.prefab";
            GameObject root = new GameObject("MuzzleFlash_Sniper1");

            var cleanup = root.AddComponent<VFXAutoCleanup>();
            SetSerializedField(cleanup, "lifetime", 0.45f);
            SetSerializedField(cleanup, "lightDuration", 0.06f);

            // Crown flash
            GameObject crownGo = new GameObject("CrownFlash");
            crownGo.transform.SetParent(root.transform, false);
            ParticleSystem crownPs = crownGo.AddComponent<ParticleSystem>();
            var cMain = crownPs.main;
            cMain.duration = 0.05f;
            cMain.loop = false;
            cMain.startLifetime = 0.05f;
            cMain.startSpeed = 0f;
            cMain.startSize = 0.24f;
            cMain.startColor = new Color(0.95f, 0.98f, 1f, 1f);
            var cEm = crownPs.emission;
            cEm.rateOverTime = 0;
            cEm.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 1) });
            var cPsr = crownGo.GetComponent<ParticleSystemRenderer>();
            cPsr.material = flashMat;
            cPsr.renderMode = ParticleSystemRenderMode.Billboard;

            // Forward needle
            GameObject needleGo = new GameObject("ForwardNeedle");
            needleGo.transform.SetParent(root.transform, false);
            ParticleSystem needlePs = needleGo.AddComponent<ParticleSystem>();
            var nMain = needlePs.main;
            nMain.duration = 0.05f;
            nMain.loop = false;
            nMain.startLifetime = 0.06f;
            nMain.startSpeed = 12f;
            nMain.startSize3D = true;
            nMain.startSizeX = 0.08f;
            nMain.startSizeY = 0.42f;
            nMain.startSizeZ = 0.08f;
            nMain.startColor = new Color(1f, 0.96f, 0.85f, 1f);
            var nEm = needlePs.emission;
            nEm.rateOverTime = 0;
            nEm.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 1) });
            var nShape = needlePs.shape;
            nShape.shapeType = ParticleSystemShapeType.Cone;
            nShape.angle = 0f;
            nShape.radius = 0.01f;
            var nPsr = needleGo.GetComponent<ParticleSystemRenderer>();
            nPsr.material = needleMat;
            nPsr.renderMode = ParticleSystemRenderMode.Stretch;
            nPsr.lengthScale = 1.8f;
            nPsr.velocityScale = 0.15f;

            // Sparks
            GameObject sparksGo = new GameObject("Sparks");
            sparksGo.transform.SetParent(root.transform, false);
            ParticleSystem sparksPs = sparksGo.AddComponent<ParticleSystem>();
            var sMain = sparksPs.main;
            sMain.duration = 0.05f;
            sMain.loop = false;
            sMain.startLifetime = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
            sMain.startSpeed = new ParticleSystem.MinMaxCurve(16f, 26f);
            sMain.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.04f);
            sMain.startColor = new Color(1f, 0.85f, 0.4f, 1f);
            var sEm = sparksPs.emission;
            sEm.rateOverTime = 0;
            sEm.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 8) });
            var sShape = sparksPs.shape;
            sShape.shapeType = ParticleSystemShapeType.Cone;
            sShape.angle = 8f;
            sShape.radius = 0.01f;
            var sPsr = sparksGo.GetComponent<ParticleSystemRenderer>();
            sPsr.material = sparksMat;
            sPsr.renderMode = ParticleSystemRenderMode.Stretch;
            sPsr.lengthScale = 1.5f;
            sPsr.velocityScale = 0.1f;

            // Thin rapid smoke
            GameObject smokeGo = new GameObject("ThinSmoke");
            smokeGo.transform.SetParent(root.transform, false);
            ParticleSystem smokePs = smokeGo.AddComponent<ParticleSystem>();
            var smMain = smokePs.main;
            smMain.duration = 0.08f;
            smMain.loop = false;
            smMain.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.28f);
            smMain.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.5f);
            smMain.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.28f);
            smMain.startColor = new Color(0.85f, 0.85f, 0.85f, 0.18f);
            var smEm = smokePs.emission;
            smEm.rateOverTime = 0;
            smEm.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 2) });
            var smShape = smokePs.shape;
            smShape.shapeType = ParticleSystemShapeType.Cone;
            smShape.angle = 12f;
            smShape.radius = 0.02f;
            var smCol = smokePs.colorOverLifetime;
            smCol.enabled = true;
            Gradient smGrad = new Gradient();
            smGrad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(0.85f, 0.85f, 0.85f), 0f), new GradientColorKey(new Color(0.85f, 0.85f, 0.85f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.2f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            smCol.color = smGrad;
            var smSol = smokePs.sizeOverLifetime;
            smSol.enabled = true;
            smSol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.8f, 1f, 1.8f));
            var smPsr = smokeGo.GetComponent<ParticleSystemRenderer>();
            smPsr.material = smokeMat;
            smPsr.renderMode = ParticleSystemRenderMode.Billboard;

            // Flash light
            GameObject lightGo = new GameObject("FlashLight");
            lightGo.transform.SetParent(root.transform, false);
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.92f, 0.65f);
            light.intensity = 4.5f;
            light.range = 5.5f;

            SaveAndDestroy(root, path);
        }

        private static void BuildMuzzleFlash2(Material flashMat, Material needleMat, Material shockwaveMat, Material sparksMat, Material smokeMat)
        {
            string path = $"{VfxPrefabDir}/MuzzleFlash_Sniper2.prefab";
            GameObject root = new GameObject("MuzzleFlash_Sniper2");

            var cleanup = root.AddComponent<VFXAutoCleanup>();
            SetSerializedField(cleanup, "lifetime", 0.75f);
            SetSerializedField(cleanup, "lightDuration", 0.08f);

            // PlasmaCore (intense white-hot center)
            GameObject coreGo = new GameObject("PlasmaCore");
            coreGo.transform.SetParent(root.transform, false);
            ParticleSystem corePs = coreGo.AddComponent<ParticleSystem>();
            var cMain = corePs.main;
            cMain.duration = 0.08f;
            cMain.loop = false;
            cMain.startLifetime = 0.07f;
            cMain.startSpeed = 0f;
            cMain.startSize = 0.65f;
            cMain.startColor = new Color(1f, 0.95f, 0.85f, 1f);
            var cEm = corePs.emission;
            cEm.rateOverTime = 0;
            cEm.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 1) });
            var cPsr = coreGo.GetComponent<ParticleSystemRenderer>();
            cPsr.material = flashMat;
            cPsr.renderMode = ParticleSystemRenderMode.Billboard;

            // PlasmaLance (forward flame bursts)
            GameObject lanceGo = new GameObject("PlasmaLance");
            lanceGo.transform.SetParent(root.transform, false);
            ParticleSystem lancePs = lanceGo.AddComponent<ParticleSystem>();
            var lMain = lancePs.main;
            lMain.duration = 0.08f;
            lMain.loop = false;
            lMain.startLifetime = 0.09f;
            lMain.startSpeed = 16f;
            lMain.startSize3D = true;
            lMain.startSizeX = 0.2f;
            lMain.startSizeY = 0.9f;
            lMain.startSizeZ = 0.2f;
            lMain.startColor = new Color(1f, 0.7f, 0.2f, 1f);
            var lEm = lancePs.emission;
            lEm.rateOverTime = 0;
            lEm.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 3) });
            var lShape = lancePs.shape;
            lShape.shapeType = ParticleSystemShapeType.Cone;
            lShape.angle = 6f;
            lShape.radius = 0.02f;
            var lPsr = lanceGo.GetComponent<ParticleSystemRenderer>();
            lPsr.material = needleMat;
            lPsr.renderMode = ParticleSystemRenderMode.Stretch;
            lPsr.lengthScale = 2.2f;
            lPsr.velocityScale = 0.18f;

            // MachRing (expanding shockwave ring)
            GameObject ringGo = new GameObject("MachRing");
            ringGo.transform.SetParent(root.transform, false);
            ParticleSystem ringPs = ringGo.AddComponent<ParticleSystem>();
            var rMain = ringPs.main;
            rMain.duration = 0.1f;
            rMain.loop = false;
            rMain.startLifetime = 0.12f;
            rMain.startSpeed = 0f;
            rMain.startSize = 0.25f;
            rMain.startColor = new Color(1f, 0.7f, 0.2f, 0.95f);
            var rEm = ringPs.emission;
            rEm.rateOverTime = 0;
            rEm.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 1) });
            var rCol = ringPs.colorOverLifetime;
            rCol.enabled = true;
            Gradient rGrad = new Gradient();
            rGrad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.6f, 0.15f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            rCol.color = rGrad;
            var rSol = ringPs.sizeOverLifetime;
            rSol.enabled = true;
            rSol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 3.5f));
            var rPsr = ringGo.GetComponent<ParticleSystemRenderer>();
            rPsr.material = shockwaveMat;
            rPsr.renderMode = ParticleSystemRenderMode.Billboard;

            // Lateral muzzle brake vents
            GameObject brakeGo = new GameObject("BrakeVents");
            brakeGo.transform.SetParent(root.transform, false);
            ParticleSystem brakePs = brakeGo.AddComponent<ParticleSystem>();
            var bMain = brakePs.main;
            bMain.duration = 0.08f;
            bMain.loop = false;
            bMain.startLifetime = 0.08f;
            bMain.startSpeed = new ParticleSystem.MinMaxCurve(12f, 20f);
            bMain.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
            bMain.startColor = new Color(1f, 0.65f, 0.15f, 1f);
            var bEm = brakePs.emission;
            bEm.rateOverTime = 0;
            bEm.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 14) });
            var bShape = brakePs.shape;
            bShape.shapeType = ParticleSystemShapeType.Sphere;
            bShape.radius = 0.04f;
            var bPsr = brakeGo.GetComponent<ParticleSystemRenderer>();
            bPsr.material = sparksMat;
            bPsr.renderMode = ParticleSystemRenderMode.Stretch;
            bPsr.lengthScale = 1.6f;

            // Heavy sparks outward
            GameObject sparksGo = new GameObject("HeavySparks");
            sparksGo.transform.SetParent(root.transform, false);
            ParticleSystem sparksPs = sparksGo.AddComponent<ParticleSystem>();
            var sMain = sparksPs.main;
            sMain.duration = 0.08f;
            sMain.loop = false;
            sMain.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.25f);
            sMain.startSpeed = new ParticleSystem.MinMaxCurve(20f, 38f);
            sMain.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
            sMain.startColor = new Color(1f, 0.8f, 0.25f, 1f);
            var sEm = sparksPs.emission;
            sEm.rateOverTime = 0;
            sEm.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 32) });
            var sShape = sparksPs.shape;
            sShape.shapeType = ParticleSystemShapeType.Cone;
            sShape.angle = 25f;
            sShape.radius = 0.02f;
            var sPsr = sparksGo.GetComponent<ParticleSystemRenderer>();
            sPsr.material = sparksMat;
            sPsr.renderMode = ParticleSystemRenderMode.Stretch;
            sPsr.lengthScale = 1.8f;
            sPsr.velocityScale = 0.12f;

            // Dense smoke puff
            GameObject smokeGo = new GameObject("DenseSmoke");
            smokeGo.transform.SetParent(root.transform, false);
            ParticleSystem smokePs = smokeGo.AddComponent<ParticleSystem>();
            var smMain = smokePs.main;
            smMain.duration = 0.12f;
            smMain.loop = false;
            smMain.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.65f);
            smMain.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
            smMain.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
            smMain.startColor = new Color(0.72f, 0.68f, 0.65f, 0.35f);
            var smEm = smokePs.emission;
            smEm.rateOverTime = 0;
            smEm.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 6) });
            var smShape = smokePs.shape;
            smShape.shapeType = ParticleSystemShapeType.Cone;
            smShape.angle = 20f;
            smShape.radius = 0.04f;
            var smCol = smokePs.colorOverLifetime;
            smCol.enabled = true;
            Gradient smGrad = new Gradient();
            smGrad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(0.7f, 0.68f, 0.65f), 0f), new GradientColorKey(new Color(0.6f, 0.58f, 0.55f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.35f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            smCol.color = smGrad;
            var smSol = smokePs.sizeOverLifetime;
            smSol.enabled = true;
            smSol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.8f, 1f, 2.4f));
            var smPsr = smokeGo.GetComponent<ParticleSystemRenderer>();
            smPsr.material = smokeMat;
            smPsr.renderMode = ParticleSystemRenderMode.Billboard;

            // Intense flash light
            GameObject lightGo = new GameObject("FlashLight");
            lightGo.transform.SetParent(root.transform, false);
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.68f, 0.18f);
            light.intensity = 8.5f;
            light.range = 9.5f;

            SaveAndDestroy(root, path);
        }

        private static void BuildImpact1(Material flashMat, Material sparksMat, Material dustMat)
        {
            string path = $"{VfxPrefabDir}/Impact_Sniper1.prefab";
            GameObject root = new GameObject("Impact_Sniper1");

            var cleanup = root.AddComponent<VFXAutoCleanup>();
            SetSerializedField(cleanup, "lifetime", 0.6f);
            SetSerializedField(cleanup, "lightDuration", 0.05f);

            // ImpactFlash (brief white-blue flash)
            GameObject flashGo = new GameObject("ImpactFlash");
            flashGo.transform.SetParent(root.transform, false);
            ParticleSystem flashPs = flashGo.AddComponent<ParticleSystem>();
            var fMain = flashPs.main;
            fMain.duration = 0.05f;
            fMain.loop = false;
            fMain.startLifetime = 0.05f;
            fMain.startSpeed = 0f;
            fMain.startSize = 0.35f;
            fMain.startColor = new Color(0.92f, 0.96f, 1f, 1f);
            var fEm = flashPs.emission;
            fEm.rateOverTime = 0;
            fEm.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 1) });
            var fPsr = flashGo.GetComponent<ParticleSystemRenderer>();
            fPsr.material = flashMat;
            fPsr.renderMode = ParticleSystemRenderMode.Billboard;

            // KineticSpall (sharp directional sparks)
            GameObject spallGo = new GameObject("KineticSpall");
            spallGo.transform.SetParent(root.transform, false);
            ParticleSystem spallPs = spallGo.AddComponent<ParticleSystem>();
            var sMain = spallPs.main;
            sMain.duration = 0.05f;
            sMain.loop = false;
            sMain.startLifetime = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            sMain.startSpeed = new ParticleSystem.MinMaxCurve(10f, 22f);
            sMain.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
            sMain.startColor = new Color(1f, 0.9f, 0.5f, 1f);
            var sEm = spallPs.emission;
            sEm.rateOverTime = 0;
            sEm.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 14) });
            var sShape = spallPs.shape;
            sShape.shapeType = ParticleSystemShapeType.Cone;
            sShape.angle = 40f;
            sShape.radius = 0.02f;
            var sPsr = spallGo.GetComponent<ParticleSystemRenderer>();
            sPsr.material = sparksMat;
            sPsr.renderMode = ParticleSystemRenderMode.Stretch;
            sPsr.lengthScale = 1.6f;
            sPsr.velocityScale = 0.12f;

            // PunctureDust (subtle dust puff on solid surfaces)
            GameObject dustGo = new GameObject("PunctureDust");
            dustGo.transform.SetParent(root.transform, false);
            ParticleSystem dustPs = dustGo.AddComponent<ParticleSystem>();
            var dMain = dustPs.main;
            dMain.duration = 0.08f;
            dMain.loop = false;
            dMain.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.55f);
            dMain.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2.5f);
            dMain.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
            dMain.startColor = new Color(0.75f, 0.72f, 0.68f, 0.22f);
            var dEm = dustPs.emission;
            dEm.rateOverTime = 0;
            dEm.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 4) });
            var dShape = dustPs.shape;
            dShape.shapeType = ParticleSystemShapeType.Cone;
            dShape.angle = 35f;
            dShape.radius = 0.03f;
            var dCol = dustPs.colorOverLifetime;
            dCol.enabled = true;
            Gradient dGrad = new Gradient();
            dGrad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(0.75f, 0.72f, 0.68f), 0f), new GradientColorKey(new Color(0.7f, 0.68f, 0.65f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.25f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            dCol.color = dGrad;
            var dSol = dustPs.sizeOverLifetime;
            dSol.enabled = true;
            dSol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.8f, 1f, 1.8f));
            var dPsr = dustGo.GetComponent<ParticleSystemRenderer>();
            dPsr.material = dustMat;
            dPsr.renderMode = ParticleSystemRenderMode.Billboard;

            // Impact light
            GameObject lightGo = new GameObject("ImpactLight");
            lightGo.transform.SetParent(root.transform, false);
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.9f, 0.6f);
            light.intensity = 4f;
            light.range = 4f;

            SaveAndDestroy(root, path);
        }

        private static void BuildImpact2(Material flashMat, Material shockwaveMat, Material sparksMat, Material dustMat)
        {
            string path = $"{VfxPrefabDir}/Impact_Sniper2.prefab";
            GameObject root = new GameObject("Impact_Sniper2");

            var cleanup = root.AddComponent<VFXAutoCleanup>();
            SetSerializedField(cleanup, "lifetime", 1.1f);
            SetSerializedField(cleanup, "lightDuration", 0.08f);

            // PlasmaCore (intense white-hot impact flash)
            GameObject coreGo = new GameObject("PlasmaCore");
            coreGo.transform.SetParent(root.transform, false);
            ParticleSystem corePs = coreGo.AddComponent<ParticleSystem>();
            var cMain = corePs.main;
            cMain.duration = 0.08f;
            cMain.loop = false;
            cMain.startLifetime = 0.08f;
            cMain.startSpeed = 0f;
            cMain.startSize = 0.8f;
            cMain.startColor = new Color(1f, 0.95f, 0.85f, 1f);
            var cEm = corePs.emission;
            cEm.rateOverTime = 0;
            cEm.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 2) });
            var cPsr = coreGo.GetComponent<ParticleSystemRenderer>();
            cPsr.material = flashMat;
            cPsr.renderMode = ParticleSystemRenderMode.Billboard;

            // SurfaceShockwave (expanding planar ring)
            GameObject ringGo = new GameObject("SurfaceShockwave");
            ringGo.transform.SetParent(root.transform, false);
            ParticleSystem ringPs = ringGo.AddComponent<ParticleSystem>();
            var rMain = ringPs.main;
            rMain.duration = 0.1f;
            rMain.loop = false;
            rMain.startLifetime = 0.16f;
            rMain.startSpeed = 0f;
            rMain.startSize = 0.35f;
            rMain.startColor = new Color(1f, 0.68f, 0.18f, 1f);
            var rEm = ringPs.emission;
            rEm.rateOverTime = 0;
            rEm.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 1) });
            var rCol = ringPs.colorOverLifetime;
            rCol.enabled = true;
            Gradient rGrad = new Gradient();
            rGrad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.6f, 0.15f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            rCol.color = rGrad;
            var rSol = ringPs.sizeOverLifetime;
            rSol.enabled = true;
            rSol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 3.8f));
            var rPsr = ringGo.GetComponent<ParticleSystemRenderer>();
            rPsr.material = shockwaveMat;
            rPsr.renderMode = ParticleSystemRenderMode.Billboard;

            // IonSpall (violent heavy spark shower)
            GameObject spallGo = new GameObject("IonSpall");
            spallGo.transform.SetParent(root.transform, false);
            ParticleSystem spallPs = spallGo.AddComponent<ParticleSystem>();
            var sMain = spallPs.main;
            sMain.duration = 0.08f;
            sMain.loop = false;
            sMain.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.35f);
            sMain.startSpeed = new ParticleSystem.MinMaxCurve(18f, 38f);
            sMain.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
            sMain.startColor = new Color(1f, 0.75f, 0.2f, 1f);
            var sEm = spallPs.emission;
            sEm.rateOverTime = 0;
            sEm.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 35) });
            var sShape = spallPs.shape;
            sShape.shapeType = ParticleSystemShapeType.Cone;
            sShape.angle = 55f;
            sShape.radius = 0.04f;
            var sPsr = spallGo.GetComponent<ParticleSystemRenderer>();
            sPsr.material = sparksMat;
            sPsr.renderMode = ParticleSystemRenderMode.Stretch;
            sPsr.lengthScale = 1.8f;
            sPsr.velocityScale = 0.12f;

            // Lingering embers spreading outward
            GameObject emberGo = new GameObject("LingeringEmbers");
            emberGo.transform.SetParent(root.transform, false);
            ParticleSystem emberPs = emberGo.AddComponent<ParticleSystem>();
            var eMain = emberPs.main;
            eMain.duration = 0.1f;
            eMain.loop = false;
            eMain.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
            eMain.startSpeed = new ParticleSystem.MinMaxCurve(4f, 10f);
            eMain.gravityModifier = 0.5f;
            eMain.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
            eMain.startColor = new Color(1f, 0.5f, 0.1f, 1f);
            var eEm = emberPs.emission;
            eEm.rateOverTime = 0;
            eEm.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 16) });
            var eShape = emberPs.shape;
            eShape.shapeType = ParticleSystemShapeType.Hemisphere;
            eShape.radius = 0.05f;
            var eCol = emberPs.colorOverLifetime;
            eCol.enabled = true;
            Gradient eGrad = new Gradient();
            eGrad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.7f, 0.2f), 0f), new GradientColorKey(new Color(1f, 0.3f, 0.05f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) }
            );
            eCol.color = eGrad;
            var ePsr = emberGo.GetComponent<ParticleSystemRenderer>();
            ePsr.material = sparksMat;
            ePsr.renderMode = ParticleSystemRenderMode.Billboard;

            // PunctureDebris (heavy dust and debris, suppressed on character hit)
            GameObject dustGo = new GameObject("PunctureDebris");
            dustGo.transform.SetParent(root.transform, false);
            ParticleSystem dustPs = dustGo.AddComponent<ParticleSystem>();
            var dMain = dustPs.main;
            dMain.duration = 0.12f;
            dMain.loop = false;
            dMain.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 0.85f);
            dMain.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            dMain.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            dMain.startColor = new Color(0.72f, 0.68f, 0.62f, 0.35f);
            var dEm = dustPs.emission;
            dEm.rateOverTime = 0;
            dEm.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 8) });
            var dShape = dustPs.shape;
            dShape.shapeType = ParticleSystemShapeType.Cone;
            dShape.angle = 45f;
            dShape.radius = 0.05f;
            var dCol = dustPs.colorOverLifetime;
            dCol.enabled = true;
            Gradient dGrad = new Gradient();
            dGrad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(0.72f, 0.68f, 0.62f), 0f), new GradientColorKey(new Color(0.6f, 0.55f, 0.5f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.35f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            dCol.color = dGrad;
            var dSol = dustPs.sizeOverLifetime;
            dSol.enabled = true;
            dSol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.8f, 1f, 2.5f));
            var dPsr = dustGo.GetComponent<ParticleSystemRenderer>();
            dPsr.material = dustMat;
            dPsr.renderMode = ParticleSystemRenderMode.Billboard;

            // Dynamic impact light
            GameObject lightGo = new GameObject("ImpactLight");
            lightGo.transform.SetParent(root.transform, false);
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.65f, 0.15f);
            light.intensity = 7f;
            light.range = 6.5f;

            SaveAndDestroy(root, path);
        }

        private static void SaveAndDestroy(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void SetSerializedField(Component target, string fieldName, object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                if (value is float f) prop.floatValue = f;
                else if (value is int i) prop.intValue = i;
                else if (value is bool b) prop.boolValue = b;
                else if (value is string s) prop.stringValue = s;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
