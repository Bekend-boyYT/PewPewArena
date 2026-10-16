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

            var bullet = root.AddComponent<CosmeticBullet>();
            SetSerializedField(bullet, "trailTime", 0.35f);
            SetSerializedField(bullet, "trailWidth", 0.075f);
            SetSerializedField(bullet, "isHeavyBullet", false);
            SetSerializedField(bullet, "bulletLightColor", new Color(0.5f, 0.9f, 1f));
            SetSerializedField(bullet, "bulletLightIntensity", 3.5f);
            SetSerializedField(bullet, "bulletLightRange", 5.5f);
            SetSerializedField(bullet, "trailMaterial", tracerMat);
            SetSerializedField(bullet, "sparksMaterial", sparksMat);

            // Dynamic Point Light on root
            Light light = root.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.5f, 0.9f, 1f);
            light.intensity = 3.5f;
            light.range = 5.5f;
            light.shadows = LightShadows.None;

            // TrailRenderer
            TrailRenderer trail = root.AddComponent<TrailRenderer>();
            trail.material = tracerMat;
            trail.time = 0.35f;
            trail.minVertexDistance = 0.05f;
            trail.widthMultiplier = 0.075f;

            AnimationCurve widthCurve = new AnimationCurve();
            widthCurve.AddKey(new Keyframe(0f, 1f));
            widthCurve.AddKey(new Keyframe(0.35f, 0.95f));
            widthCurve.AddKey(new Keyframe(0.7f, 0.55f));
            widthCurve.AddKey(new Keyframe(1f, 0f));
            trail.widthCurve = widthCurve;

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(0.4f, 0.88f, 1f), 0.35f),
                    new GradientColorKey(new Color(0.12f, 0.6f, 1f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.85f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                }
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
            main.startLifetime = 0.22f;
            main.startSpeed = 0f;
            main.startSize = 0.045f;
            main.startColor = new Color(0.6f, 0.92f, 1f, 0.85f);
            main.loop = true;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.rateOverDistance = 30f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.02f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient colGrad = new Gradient();
            colGrad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.4f, 0.85f, 1f), 1f) },
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
            hMain.startSize = 0.14f;
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

            var bullet = root.AddComponent<CosmeticBullet>();
            SetSerializedField(bullet, "trailTime", 0.48f);
            SetSerializedField(bullet, "trailWidth", 0.16f);
            SetSerializedField(bullet, "isHeavyBullet", true);
            SetSerializedField(bullet, "bulletLightColor", new Color(1f, 0.7f, 0.2f));
            SetSerializedField(bullet, "bulletLightIntensity", 6.5f);
            SetSerializedField(bullet, "bulletLightRange", 8.0f);
            SetSerializedField(bullet, "trailMaterial", tracerMat);
            SetSerializedField(bullet, "sparksMaterial", sparksMat);

            // Dynamic Point Light on root
            Light light = root.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.7f, 0.2f);
            light.intensity = 6.5f;
            light.range = 8.0f;
            light.shadows = LightShadows.None;

            // TrailRenderer
            TrailRenderer trail = root.AddComponent<TrailRenderer>();
            trail.material = tracerMat;
            trail.time = 0.48f;
            trail.minVertexDistance = 0.05f;
            trail.widthMultiplier = 0.16f;

            AnimationCurve widthCurve = new AnimationCurve();
            widthCurve.AddKey(new Keyframe(0f, 1f));
            widthCurve.AddKey(new Keyframe(0.3f, 0.95f));
            widthCurve.AddKey(new Keyframe(0.7f, 0.55f));
            widthCurve.AddKey(new Keyframe(1f, 0f));
            trail.widthCurve = widthCurve;

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(1f, 0.78f, 0.22f), 0.3f),
                    new GradientColorKey(new Color(1f, 0.42f, 0.05f), 0.75f),
                    new GradientColorKey(new Color(0.85f, 0.15f, 0.02f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.95f, 0.65f),
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
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
            main.gravityModifier = 0.25f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
            main.startColor = new Color(1f, 0.7f, 0.18f, 1f);
            main.loop = true;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.rateOverDistance = 45f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.04f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient colGrad = new Gradient();
            colGrad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.4f, 0.05f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.85f, 0.5f), new GradientAlphaKey(0f, 1f) }
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
            hMain.startSize = 0.26f;
            hMain.startColor = new Color(1f, 0.96f, 0.88f, 1f);
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

            // Crown flash (crisp cyan-white starburst)
            GameObject crownGo = new GameObject("CrownFlash");
            crownGo.transform.SetParent(root.transform, false);
            ParticleSystem crownPs = crownGo.AddComponent<ParticleSystem>();
            var cMain = crownPs.main;
            cMain.duration = 0.05f;
            cMain.loop = false;
            cMain.startLifetime = 0.05f;
            cMain.startSpeed = 0f;
            cMain.startSize = 0.32f;
            cMain.startColor = new Color(0.92f, 0.98f, 1f, 1f);
            var cEm = crownPs.emission;
            cEm.rateOverTime = 0;
            cEm.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 1) });
            var cPsr = crownGo.GetComponent<ParticleSystemRenderer>();
            cPsr.material = flashMat;
            cPsr.renderMode = ParticleSystemRenderMode.Billboard;

            // Forward needle (supersonic rail piercing spike)
            GameObject needleGo = new GameObject("ForwardNeedle");
            needleGo.transform.SetParent(root.transform, false);
            ParticleSystem needlePs = needleGo.AddComponent<ParticleSystem>();
            var nMain = needlePs.main;
            nMain.duration = 0.05f;
            nMain.loop = false;
            nMain.startLifetime = 0.06f;
            nMain.startSpeed = 14f;
            nMain.startSize3D = true;
            nMain.startSizeX = 0.09f;
            nMain.startSizeY = 0.48f;
            nMain.startSizeZ = 0.09f;
            nMain.startColor = new Color(0.95f, 0.98f, 1f, 1f);
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
            nPsr.lengthScale = 2.2f;
            nPsr.velocityScale = 0.18f;

            // Lateral brake vents (horizontal gas jet needles)
            GameObject lateralGo = new GameObject("LateralVents");
            lateralGo.transform.SetParent(root.transform, false);
            ParticleSystem lateralPs = lateralGo.AddComponent<ParticleSystem>();
            var latMain = lateralPs.main;
            latMain.duration = 0.05f;
            latMain.loop = false;
            latMain.startLifetime = 0.05f;
            latMain.startSpeed = new ParticleSystem.MinMaxCurve(10f, 18f);
            latMain.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
            latMain.startColor = new Color(0.8f, 0.95f, 1f, 1f);
            var latEm = lateralPs.emission;
            latEm.rateOverTime = 0;
            latEm.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 8) });
            var latShape = lateralPs.shape;
            latShape.shapeType = ParticleSystemShapeType.Sphere;
            latShape.radius = 0.02f;
            var latPsr = lateralGo.GetComponent<ParticleSystemRenderer>();
            latPsr.material = sparksMat;
            latPsr.renderMode = ParticleSystemRenderMode.Stretch;
            latPsr.lengthScale = 1.4f;

            // Hypersonic Sparks
            GameObject sparksGo = new GameObject("Sparks");
            sparksGo.transform.SetParent(root.transform, false);
            ParticleSystem sparksPs = sparksGo.AddComponent<ParticleSystem>();
            var sMain = sparksPs.main;
            sMain.duration = 0.05f;
            sMain.loop = false;
            sMain.startLifetime = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
            sMain.startSpeed = new ParticleSystem.MinMaxCurve(18f, 28f);
            sMain.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.045f);
            sMain.startColor = new Color(0.85f, 0.95f, 1f, 1f);
            var sEm = sparksPs.emission;
            sEm.rateOverTime = 0;
            sEm.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 12) });
            var sShape = sparksPs.shape;
            sShape.shapeType = ParticleSystemShapeType.Cone;
            sShape.angle = 10f;
            sShape.radius = 0.01f;
            var sPsr = sparksGo.GetComponent<ParticleSystemRenderer>();
            sPsr.material = sparksMat;
            sPsr.renderMode = ParticleSystemRenderMode.Stretch;
            sPsr.lengthScale = 1.6f;
            sPsr.velocityScale = 0.12f;

            // Thin rapid smoke vapor
            GameObject smokeGo = new GameObject("ThinSmoke");
            smokeGo.transform.SetParent(root.transform, false);
            ParticleSystem smokePs = smokeGo.AddComponent<ParticleSystem>();
            var smMain = smokePs.main;
            smMain.duration = 0.08f;
            smMain.loop = false;
            smMain.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.28f);
            smMain.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.5f);
            smMain.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.28f);
            smMain.startColor = new Color(0.85f, 0.88f, 0.92f, 0.18f);
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
                new GradientColorKey[] { new GradientColorKey(new Color(0.85f, 0.88f, 0.92f), 0f), new GradientColorKey(new Color(0.85f, 0.88f, 0.92f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.2f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            smCol.color = smGrad;
            var smSol = smokePs.sizeOverLifetime;
            smSol.enabled = true;
            smSol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.8f, 1f, 1.8f));
            var smPsr = smokeGo.GetComponent<ParticleSystemRenderer>();
            smPsr.material = smokeMat;
            smPsr.renderMode = ParticleSystemRenderMode.Billboard;

            // Dynamic Flash light (electric white-cyan pulse)
            GameObject lightGo = new GameObject("FlashLight");
            lightGo.transform.SetParent(root.transform, false);
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.85f, 0.95f, 1f);
            light.intensity = 5.5f;
            light.range = 7.0f;

            SaveAndDestroy(root, path);
        }

        private static void BuildMuzzleFlash2(Material flashMat, Material needleMat, Material shockwaveMat, Material sparksMat, Material smokeMat)
        {
            string path = $"{VfxPrefabDir}/MuzzleFlash_Sniper2.prefab";
            GameObject root = new GameObject("MuzzleFlash_Sniper2");

            var cleanup = root.AddComponent<VFXAutoCleanup>();
            SetSerializedField(cleanup, "lifetime", 0.75f);
            SetSerializedField(cleanup, "lightDuration", 0.09f);

            // PlasmaCore (intense white-hot explosion center)
            GameObject coreGo = new GameObject("PlasmaCore");
            coreGo.transform.SetParent(root.transform, false);
            ParticleSystem corePs = coreGo.AddComponent<ParticleSystem>();
            var cMain = corePs.main;
            cMain.duration = 0.08f;
            cMain.loop = false;
            cMain.startLifetime = 0.07f;
            cMain.startSpeed = 0f;
            cMain.startSize = 0.75f;
            cMain.startColor = new Color(1f, 0.96f, 0.88f, 1f);
            var cEm = corePs.emission;
            cEm.rateOverTime = 0;
            cEm.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 1) });
            var cPsr = coreGo.GetComponent<ParticleSystemRenderer>();
            cPsr.material = flashMat;
            cPsr.renderMode = ParticleSystemRenderMode.Billboard;

            // PlasmaLance (forward searing flame bursts)
            GameObject lanceGo = new GameObject("PlasmaLance");
            lanceGo.transform.SetParent(root.transform, false);
            ParticleSystem lancePs = lanceGo.AddComponent<ParticleSystem>();
            var lMain = lancePs.main;
            lMain.duration = 0.08f;
            lMain.loop = false;
            lMain.startLifetime = 0.09f;
            lMain.startSpeed = 18f;
            lMain.startSize3D = true;
            lMain.startSizeX = 0.22f;
            lMain.startSizeY = 1.05f;
            lMain.startSizeZ = 0.22f;
            lMain.startColor = new Color(1f, 0.72f, 0.18f, 1f);
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
            lPsr.lengthScale = 2.5f;
            lPsr.velocityScale = 0.2f;

            // MachRing (expanding shockwave ring)
            GameObject ringGo = new GameObject("MachRing");
            ringGo.transform.SetParent(root.transform, false);
            ParticleSystem ringPs = ringGo.AddComponent<ParticleSystem>();
            var rMain = ringPs.main;
            rMain.duration = 0.1f;
            rMain.loop = false;
            rMain.startLifetime = 0.14f;
            rMain.startSpeed = 0f;
            rMain.startSize = 0.35f;
            rMain.startColor = new Color(1f, 0.72f, 0.2f, 0.95f);
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

            // Lateral muzzle brake vents
            GameObject brakeGo = new GameObject("BrakeVents");
            brakeGo.transform.SetParent(root.transform, false);
            ParticleSystem brakePs = brakeGo.AddComponent<ParticleSystem>();
            var bMain = brakePs.main;
            bMain.duration = 0.08f;
            bMain.loop = false;
            bMain.startLifetime = 0.08f;
            bMain.startSpeed = new ParticleSystem.MinMaxCurve(14f, 22f);
            bMain.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
            bMain.startColor = new Color(1f, 0.65f, 0.15f, 1f);
            var bEm = brakePs.emission;
            bEm.rateOverTime = 0;
            bEm.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 16) });
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
            sMain.startSpeed = new ParticleSystem.MinMaxCurve(22f, 42f);
            sMain.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
            sMain.startColor = new Color(1f, 0.8f, 0.25f, 1f);
            var sEm = sparksPs.emission;
            sEm.rateOverTime = 0;
            sEm.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 36) });
            var sShape = sparksPs.shape;
            sShape.shapeType = ParticleSystemShapeType.Cone;
            sShape.angle = 25f;
            sShape.radius = 0.02f;
            var sPsr = sparksGo.GetComponent<ParticleSystemRenderer>();
            sPsr.material = sparksMat;
            sPsr.renderMode = ParticleSystemRenderMode.Stretch;
            sPsr.lengthScale = 1.8f;
            sPsr.velocityScale = 0.12f;

            // Dense combustion smoke puff
            GameObject smokeGo = new GameObject("DenseSmoke");
            smokeGo.transform.SetParent(root.transform, false);
            ParticleSystem smokePs = smokeGo.AddComponent<ParticleSystem>();
            var smMain = smokePs.main;
            smMain.duration = 0.12f;
            smMain.loop = false;
            smMain.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.7f);
            smMain.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
            smMain.startSize = new ParticleSystem.MinMaxCurve(0.45f, 0.85f);
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

            // Massive flash light (searing molten amber)
            GameObject lightGo = new GameObject("FlashLight");
            lightGo.transform.SetParent(root.transform, false);
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.65f, 0.15f);
            light.intensity = 10f;
            light.range = 12f;

            SaveAndDestroy(root, path);
        }

        private static void BuildImpact1(Material flashMat, Material sparksMat, Material dustMat)
        {
            string path = $"{VfxPrefabDir}/Impact_Sniper1.prefab";
            GameObject root = new GameObject("Impact_Sniper1");

            var cleanup = root.AddComponent<VFXAutoCleanup>();
            SetSerializedField(cleanup, "lifetime", 0.65f);
            SetSerializedField(cleanup, "lightDuration", 0.06f);

            // ImpactFlash (sharp electric-cyan micro-flash)
            GameObject flashGo = new GameObject("ImpactFlash");
            flashGo.transform.SetParent(root.transform, false);
            ParticleSystem flashPs = flashGo.AddComponent<ParticleSystem>();
            var fMain = flashPs.main;
            fMain.duration = 0.05f;
            fMain.loop = false;
            fMain.startLifetime = 0.05f;
            fMain.startSpeed = 0f;
            fMain.startSize = 0.42f;
            fMain.startColor = new Color(0.92f, 0.98f, 1f, 1f);
            var fEm = flashPs.emission;
            fEm.rateOverTime = 0;
            fEm.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 1) });
            var fPsr = flashGo.GetComponent<ParticleSystemRenderer>();
            fPsr.material = flashMat;
            fPsr.renderMode = ParticleSystemRenderMode.Billboard;

            // KineticSpall (sharp razor sparks ricocheting off surface)
            GameObject spallGo = new GameObject("KineticSpall");
            spallGo.transform.SetParent(root.transform, false);
            ParticleSystem spallPs = spallGo.AddComponent<ParticleSystem>();
            var sMain = spallPs.main;
            sMain.duration = 0.05f;
            sMain.loop = false;
            sMain.startLifetime = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            sMain.startSpeed = new ParticleSystem.MinMaxCurve(12f, 25f);
            sMain.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.055f);
            sMain.startColor = new Color(0.85f, 0.95f, 1f, 1f);
            var sEm = spallPs.emission;
            sEm.rateOverTime = 0;
            sEm.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 18) });
            var sShape = spallPs.shape;
            sShape.shapeType = ParticleSystemShapeType.Cone;
            sShape.angle = 40f;
            sShape.radius = 0.02f;
            var sPsr = spallGo.GetComponent<ParticleSystemRenderer>();
            sPsr.material = sparksMat;
            sPsr.renderMode = ParticleSystemRenderMode.Stretch;
            sPsr.lengthScale = 1.6f;
            sPsr.velocityScale = 0.12f;

            // PunctureDust (fine pulverized dust on solid surfaces, suppressed on character hits)
            GameObject dustGo = new GameObject("PunctureDust");
            dustGo.transform.SetParent(root.transform, false);
            ParticleSystem dustPs = dustGo.AddComponent<ParticleSystem>();
            var dMain = dustPs.main;
            dMain.duration = 0.08f;
            dMain.loop = false;
            dMain.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.55f);
            dMain.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2.5f);
            dMain.startSize = new ParticleSystem.MinMaxCurve(0.22f, 0.48f);
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

            // Dynamic impact light (sharp cyan-white flash)
            GameObject lightGo = new GameObject("ImpactLight");
            lightGo.transform.SetParent(root.transform, false);
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.88f, 0.96f, 1f);
            light.intensity = 5.5f;
            light.range = 5.5f;

            SaveAndDestroy(root, path);
        }

        private static void BuildImpact2(Material flashMat, Material shockwaveMat, Material sparksMat, Material dustMat)
        {
            string path = $"{VfxPrefabDir}/Impact_Sniper2.prefab";
            GameObject root = new GameObject("Impact_Sniper2");

            var cleanup = root.AddComponent<VFXAutoCleanup>();
            SetSerializedField(cleanup, "lifetime", 1.2f);
            SetSerializedField(cleanup, "lightDuration", 0.1f);

            // PlasmaCore (intense white-hot detonation crater flash)
            GameObject coreGo = new GameObject("PlasmaCore");
            coreGo.transform.SetParent(root.transform, false);
            ParticleSystem corePs = coreGo.AddComponent<ParticleSystem>();
            var cMain = corePs.main;
            cMain.duration = 0.08f;
            cMain.loop = false;
            cMain.startLifetime = 0.08f;
            cMain.startSpeed = 0f;
            cMain.startSize = 1.0f;
            cMain.startColor = new Color(1f, 0.96f, 0.88f, 1f);
            var cEm = corePs.emission;
            cEm.rateOverTime = 0;
            cEm.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 2) });
            var cPsr = coreGo.GetComponent<ParticleSystemRenderer>();
            cPsr.material = flashMat;
            cPsr.renderMode = ParticleSystemRenderMode.Billboard;

            // SurfaceShockwave (expanding planar shockwave ring)
            GameObject ringGo = new GameObject("SurfaceShockwave");
            ringGo.transform.SetParent(root.transform, false);
            ParticleSystem ringPs = ringGo.AddComponent<ParticleSystem>();
            var rMain = ringPs.main;
            rMain.duration = 0.1f;
            rMain.loop = false;
            rMain.startLifetime = 0.18f;
            rMain.startSpeed = 0f;
            rMain.startSize = 0.4f;
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
            rSol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 4.2f));
            var rPsr = ringGo.GetComponent<ParticleSystemRenderer>();
            rPsr.material = shockwaveMat;
            rPsr.renderMode = ParticleSystemRenderMode.Billboard;

            // IonSpall (violent molten spark spray)
            GameObject spallGo = new GameObject("IonSpall");
            spallGo.transform.SetParent(root.transform, false);
            ParticleSystem spallPs = spallGo.AddComponent<ParticleSystem>();
            var sMain = spallPs.main;
            sMain.duration = 0.08f;
            sMain.loop = false;
            sMain.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.35f);
            sMain.startSpeed = new ParticleSystem.MinMaxCurve(20f, 42f);
            sMain.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
            sMain.startColor = new Color(1f, 0.75f, 0.2f, 1f);
            var sEm = spallPs.emission;
            sEm.rateOverTime = 0;
            sEm.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 40) });
            var sShape = spallPs.shape;
            sShape.shapeType = ParticleSystemShapeType.Cone;
            sShape.angle = 55f;
            sShape.radius = 0.04f;
            var sPsr = spallGo.GetComponent<ParticleSystemRenderer>();
            sPsr.material = sparksMat;
            sPsr.renderMode = ParticleSystemRenderMode.Stretch;
            sPsr.lengthScale = 1.8f;
            sPsr.velocityScale = 0.12f;

            // Lingering embers spreading outward with gravity
            GameObject emberGo = new GameObject("LingeringEmbers");
            emberGo.transform.SetParent(root.transform, false);
            ParticleSystem emberPs = emberGo.AddComponent<ParticleSystem>();
            var eMain = emberPs.main;
            eMain.duration = 0.1f;
            eMain.loop = false;
            eMain.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.65f);
            eMain.startSpeed = new ParticleSystem.MinMaxCurve(5f, 12f);
            eMain.gravityModifier = 0.6f;
            eMain.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
            eMain.startColor = new Color(1f, 0.55f, 0.12f, 1f);
            var eEm = emberPs.emission;
            eEm.rateOverTime = 0;
            eEm.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 20) });
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

            // PunctureDebris (heavy crater dust and debris cloud, suppressed on character hit)
            GameObject dustGo = new GameObject("PunctureDebris");
            dustGo.transform.SetParent(root.transform, false);
            ParticleSystem dustPs = dustGo.AddComponent<ParticleSystem>();
            var dMain = dustPs.main;
            dMain.duration = 0.12f;
            dMain.loop = false;
            dMain.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 0.9f);
            dMain.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5.5f);
            dMain.startSize = new ParticleSystem.MinMaxCurve(0.45f, 0.95f);
            dMain.startColor = new Color(0.72f, 0.68f, 0.62f, 0.35f);
            var dEm = dustPs.emission;
            dEm.rateOverTime = 0;
            dEm.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 10) });
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

            // Dynamic impact light (intense crater glow)
            GameObject lightGo = new GameObject("ImpactLight");
            lightGo.transform.SetParent(root.transform, false);
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.62f, 0.15f);
            light.intensity = 9.5f;
            light.range = 9.0f;

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
                else if (value is Color c) prop.colorValue = c;
                else if (value is UnityEngine.Object obj) prop.objectReferenceValue = obj;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
