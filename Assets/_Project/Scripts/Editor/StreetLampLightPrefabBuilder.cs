using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using CodeDrive.Environment;

namespace CodeDrive.EditorTools
{
    [InitializeOnLoad]
    public static class StreetLampLightPrefabBuilder
    {
        private const string PrefabDir = "Assets/_Project/Prefabs";
        private const string MaterialDir = "Assets/_Project/Materials";
        private const string PrefabPath = "Assets/_Project/Prefabs/StreetLight_DownBulb.prefab";
        private const string MatPath = "Assets/_Project/Materials/M_LampBulb_WarmGlow.mat";

        static StreetLampLightPrefabBuilder()
        {
            EditorApplication.delayCall += EnsureAssetsExist;
        }

        [MenuItem("Tools/CodeDrive/Rebuild Street Light Downward Bulb Prefab (Ultra Bright)")]
        public static void GeneratePrefabMenuItem()
        {
            CreateOrUpdatePrefab(true);
        }

        private static void EnsureAssetsExist()
        {
            if (!File.Exists(PrefabPath))
            {
                CreateOrUpdatePrefab(false);
            }
        }

        public static void CreateOrUpdatePrefab(bool logFeedback)
        {
            if (!Directory.Exists(PrefabDir)) Directory.CreateDirectory(PrefabDir);
            if (!Directory.Exists(MaterialDir)) Directory.CreateDirectory(MaterialDir);

            Color warmColor = new Color(1.0f, 0.85f, 0.55f, 1.0f);

            // 1. Emissive Material
            Material bulbMat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            if (bulbMat == null)
            {
                Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
                if (urpLitShader == null) urpLitShader = Shader.Find("Standard");

                bulbMat = new Material(urpLitShader)
                {
                    name = "M_LampBulb_WarmGlow"
                };
                AssetDatabase.CreateAsset(bulbMat, MatPath);
            }

            bulbMat.SetColor("_BaseColor", warmColor);
            bulbMat.SetColor("_Color", warmColor);
            bulbMat.EnableKeyword("_EMISSION");
            bulbMat.SetColor("_EmissionColor", warmColor * 6.0f);
            bulbMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(bulbMat);

            // 2. Build GameObject hierarchy
            GameObject rootGo = new GameObject("StreetLight_DownBulb");
            StreetLampLight lightCtrl = rootGo.AddComponent<StreetLampLight>();

            // 2a. Downward SpotLight (Strong downward cone)
            GameObject spotGo = new GameObject("SpotLight_Down");
            spotGo.transform.SetParent(rootGo.transform, false);
            spotGo.transform.localPosition = Vector3.zero;
            spotGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Point straight down

            Light spotLight = spotGo.AddComponent<Light>();
            spotLight.type = LightType.Spot;
            spotLight.color = warmColor;
            spotLight.intensity = 250f;
            spotLight.range = 22f;
            spotLight.spotAngle = 85f;
            spotLight.innerSpotAngle = 42f;
            spotLight.shadows = LightShadows.Soft;
            spotLight.shadowStrength = 0.85f;
            spotLight.cullingMask = ~0;
            spotLight.renderingLayerMask = ~0;
            spotLight.enabled = true;

            // 2b. Bulb Mesh (Visual glowing bulb cap)
            GameObject bulbMeshGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulbMeshGo.name = "Bulb_Mesh";
            bulbMeshGo.transform.SetParent(rootGo.transform, false);
            bulbMeshGo.transform.localPosition = Vector3.zero;
            bulbMeshGo.transform.localScale = new Vector3(0.35f, 0.12f, 0.35f);

            Collider col = bulbMeshGo.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            MeshRenderer meshRenderer = bulbMeshGo.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = bulbMat;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            // 2c. Wide Point Light (Ambient illumination around pole and road)
            GameObject ptGo = new GameObject("PointLight_Wide");
            ptGo.transform.SetParent(rootGo.transform, false);
            ptGo.transform.localPosition = new Vector3(0f, -0.1f, 0f);

            Light pointLight = ptGo.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = warmColor;
            pointLight.intensity = 60f;
            pointLight.range = 18f;
            pointLight.shadows = LightShadows.None;
            pointLight.cullingMask = ~0;
            pointLight.renderingLayerMask = ~0;
            pointLight.enabled = true;

            // Hook up references
            lightCtrl.FindReferences();
            lightCtrl.ApplySettings();

            // 3. Save as Prefab
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(rootGo, PrefabPath);
            Object.DestroyImmediate(rootGo);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (logFeedback)
            {
                Debug.Log($"<color=#50FA7B><b>[StreetLampLightPrefabBuilder]</b></color> Rebuilt prefab at: <b>{PrefabPath}</b>");
                Selection.activeObject = savedPrefab;
            }
        }
    }
}
