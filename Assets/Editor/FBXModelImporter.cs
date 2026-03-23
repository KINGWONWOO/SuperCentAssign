// v1
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// modeling/object/ 하위 FBX를 Assets/Models/ 로 복사·임포트하고
/// URP Lit 머티리얼을 생성합니다.  씬에는 배치하지 않습니다.
/// </summary>
public static class FBXModelImporter
{
    // ── 경로 상수 ─────────────────────────────────────────────
    const string SRC_ROOT  = "modeling/object";          // 프로젝트 루트 기준
    const string DST_ROOT  = "Assets/Models";
    const string MAT_DIR   = "Assets/Models/Materials";

    // ── 모델 정의 ─────────────────────────────────────────────
    // (폴더명, FBX파일명, globalScale, isCharacter, 설명)
    // isCharacter=true  → tripo_convert .fbx, Y-up, m단위, 애니메이션 유지
    // isCharacter=false → base.fbx, Blender Z-up, cm단위 → globalScale=100, bakeAxisConversion
    static readonly ModelDef[] MODELS = new ModelDef[]
    {
        new ModelDef("convey",          "base.fbx",   1f, false, "컨베이어 머신"),
        new ModelDef("desk",            "base.fbx",   1f, false, "수갑 책상"),
        new ModelDef("fence",           "base.fbx",   1f, false, "채석장 울타리"),
        new ModelDef("gate",            "base.fbx",   1f, false, "수감자 스포너 게이트"),
        new ModelDef("handcuff",        "base.fbx",   1f, false, "수갑 소품"),
        new ModelDef("jail1",           "base.fbx",   1f, false, "20인 감옥"),
        new ModelDef("money",           "base.fbx",   1f, false, "돈 소품"),
        new ModelDef("plate",           "base.fbx",   1f, false, "수갑 트레이"),
        new ModelDef("rock",            "base.fbx",   1f, false, "바위"),
        // characters – tripo_convert .fbx, Tripo3D 기본 1m 높이 → 1.8m로 변환
        new ModelDef("miner",           null,         1.8f, true,  "자동 채굴 NPC"),
        new ModelDef("playcharacter",   null,         1.8f, true,  "플레이어 캐릭터"),
        new ModelDef("police",          null,         1.8f, true,  "경찰 NPC"),
        new ModelDef("prisoner_before", null,         1.8f, true,  "수감자 (수갑 전)"),
        new ModelDef("prisoner_after",  null,         1.8f, true,  "수감자 (수갑 후)"),
    };

    struct ModelDef
    {
        public string folder, fbxFile;
        public float  globalScale;
        public bool   isCharacter;
        public string desc;
        public ModelDef(string f, string fbx, float gs, bool ch, string d)
        { folder=f; fbxFile=fbx; globalScale=gs; isCharacter=ch; desc=d; }
    }

    // ─────────────────────────────────────────────────────────
    [MenuItem("Tools/FBX Model Importer/Reconfigure Scale Only")]
    public static void ReconfigureScale()
    {
        foreach (var m in MODELS)
        {
            string dstDir = $"{DST_ROOT}/{m.folder}";
            string fbxSrc = FindFBX(
                Path.Combine(Application.dataPath, "..", SRC_ROOT, m.folder).Replace('\\', '/'),
                m.fbxFile);
            if (fbxSrc == null) continue;
            string fbxDst = $"{dstDir}/{Path.GetFileName(fbxSrc)}";
            ConfigureFBX(fbxDst, m.globalScale, m.isCharacter);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[FBXImporter] ✅ 스케일 재설정 완료");
    }

    [MenuItem("Tools/FBX Model Importer/Import All Models")]
    public static void ImportAll()
    {
        EnsureDir(MAT_DIR);

        foreach (var m in MODELS)
        {
            string srcDir = Path.Combine(Application.dataPath, "..", SRC_ROOT, m.folder)
                               .Replace('\\', '/');
            string dstDir = $"{DST_ROOT}/{m.folder}";
            EnsureDir(dstDir);

            // ── 1. FBX 파일 복사 ────────────────────────────────
            string fbxSrc  = FindFBX(srcDir, m.fbxFile);
            if (fbxSrc == null)
            {
                Debug.LogWarning($"[FBXImporter] FBX not found in {srcDir}");
                continue;
            }
            string fbxDst = $"{dstDir}/{Path.GetFileName(fbxSrc)}";
            CopyToAssets(fbxSrc, fbxDst);

            // ── 2. 텍스처 복사 ──────────────────────────────────
            CopyTextures(srcDir, dstDir, m.isCharacter);

            // ── 3. FBX 임포트 설정 ──────────────────────────────
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureFBX(fbxDst, m.globalScale, m.isCharacter);

            // ── 4. URP Lit 머티리얼 생성 ────────────────────────
            CreateMaterial(m.folder, dstDir, m.isCharacter);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[FBXImporter] ✅ 모든 모델 임포트 완료");
    }

    // ─────────────────────────────────────────────────────────
    // FBX 임포트 설정
    // ─────────────────────────────────────────────────────────
    static void ConfigureFBX(string assetPath, float globalScale, bool isCharacter)
    {
        var imp = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (imp == null) return;

        imp.globalScale           = globalScale;
        imp.useFileScale          = true;

        if (!isCharacter)
        {
            // Blender Z-up → Unity Y-up 축 변환 bake
            imp.bakeAxisConversion    = true;
            imp.importAnimation       = false;
            imp.animationType         = ModelImporterAnimationType.None;
        }
        else
        {
            // 캐릭터: 축 변환 bake + 애니메이션 없음(트리포 변환 모델)
            imp.bakeAxisConversion    = true;
            imp.importAnimation       = false;
            imp.animationType         = ModelImporterAnimationType.Generic;
            imp.importBlendShapes     = true;
        }

        imp.importNormals         = ModelImporterNormals.Import;
        imp.importTangents        = ModelImporterTangents.CalculateMikk;
        imp.weldVertices          = true;
        imp.optimizeMeshPolygons  = true;
        imp.optimizeMeshVertices  = true;

        // 콜리전 없음 (런타임에서 BoxCollider/MeshCollider 직접 관리)
        imp.addCollider           = false;

        EditorUtility.SetDirty(imp);
        imp.SaveAndReimport();
        Debug.Log($"[FBXImporter] 설정 완료: {assetPath}  scale={globalScale}  bakeAxis=true");
    }

    // ─────────────────────────────────────────────────────────
    // URP Lit 머티리얼 생성
    // ─────────────────────────────────────────────────────────
    static void CreateMaterial(string modelName, string dstDir, bool isCharacter)
    {
        string matPath = $"{MAT_DIR}/{modelName}_mat.mat";

        Material mat;
        if (File.Exists(Path.Combine(Application.dataPath, "..",
                         matPath.Replace("Assets/", "Assets/"))))
        {
            mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null) mat = CreateNewMat(matPath);
        }
        else
        {
            mat = CreateNewMat(matPath);
        }

        if (!isCharacter)
        {
            // base.fbx 계열: texture_diffuse / normal / metallic
            SetTex(mat, "_BaseMap",          dstDir, "texture_diffuse");
            SetTex(mat, "_BumpMap",          dstDir, "texture_normal");
            SetTex(mat, "_MetallicGlossMap", dstDir, "texture_metallic");
            mat.SetFloat("_Smoothness", 0.4f);
        }
        else
        {
            // 캐릭터: .fbm 폴더 안의 jpg 사용
            string fbmTex = FindFBMTexture(dstDir);
            if (fbmTex != null)
                SetTexDirect(mat, "_BaseMap", fbmTex);
            mat.SetFloat("_Smoothness", 0.2f);
        }

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        Debug.Log($"[FBXImporter] 머티리얼 생성: {matPath}");
    }

    static Material CreateNewMat(string matPath)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var mat = new Material(shader);
        AssetDatabase.CreateAsset(mat, matPath);
        return mat;
    }

    // ─────────────────────────────────────────────────────────
    // 텍스처 복사
    // ─────────────────────────────────────────────────────────
    static void CopyTextures(string srcDir, string dstDir, bool isCharacter)
    {
        if (!isCharacter)
        {
            string[] wanted = { "texture_diffuse", "texture_normal",
                                 "texture_metallic", "texture_roughness",
                                 "texture_pbr", "shaded" };
            foreach (var name in wanted)
            {
                foreach (var ext in new[] { ".png", ".jpg", ".jpeg" })
                {
                    string src = Path.Combine(srcDir, name + ext).Replace('\\', '/');
                    if (!File.Exists(src)) continue;
                    string dst = $"{dstDir}/{name}{ext}";
                    CopyToAssets(src, dst);
                }
            }
        }
        else
        {
            // .fbm 폴더 안 텍스처 복사
            foreach (var subDir in Directory.GetDirectories(srcDir))
            {
                if (!Path.GetFileName(subDir).EndsWith(".fbm")) continue;
                string fbmDst = $"{dstDir}/{Path.GetFileName(subDir)}";
                EnsureDir(fbmDst);
                foreach (var f in Directory.GetFiles(subDir))
                {
                    string ext = Path.GetExtension(f).ToLower();
                    if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
                        CopyToAssets(f.Replace('\\', '/'), $"{fbmDst}/{Path.GetFileName(f)}");
                }
            }
        }
    }

    // ─────────────────────────────────────────────────────────
    // 유틸
    // ─────────────────────────────────────────────────────────
    static string FindFBX(string dir, string preferredName)
    {
        if (!Directory.Exists(dir)) return null;
        // 지정 이름 우선
        if (preferredName != null)
        {
            string exact = Path.Combine(dir, preferredName).Replace('\\', '/');
            if (File.Exists(exact)) return exact;
        }
        // tripo_convert*.fbx 검색
        foreach (var f in Directory.GetFiles(dir, "*.fbx"))
            if (Path.GetFileName(f).StartsWith("tripo_convert")) return f.Replace('\\', '/');
        // 그 외 첫 번째 fbx
        var all = Directory.GetFiles(dir, "*.fbx");
        return all.Length > 0 ? all[0].Replace('\\', '/') : null;
    }

    static string FindFBMTexture(string dstDir)
    {
        if (!Directory.Exists(Path.Combine(Application.dataPath, "..", dstDir.Replace("Assets/", "Assets/"))))
            dstDir = dstDir; // pass-through
        foreach (var sub in Directory.GetDirectories(
                     Path.Combine(Application.dataPath, "..", dstDir)))
        {
            if (!Path.GetFileName(sub).EndsWith(".fbm")) continue;
            foreach (var f in Directory.GetFiles(sub))
            {
                string ext = Path.GetExtension(f).ToLower();
                if (ext == ".png" || ext == ".jpg")
                {
                    // Assets 상대 경로로 변환
                    string rel = f.Replace('\\', '/');
                    int idx = rel.IndexOf("Assets/");
                    return idx >= 0 ? rel.Substring(idx) : null;
                }
            }
        }
        return null;
    }

    static void CopyToAssets(string src, string dstAssetPath)
    {
        string dstAbs = Path.Combine(Application.dataPath, "..", dstAssetPath).Replace('\\', '/');
        if (File.Exists(dstAbs)) return; // 이미 존재하면 스킵
        Directory.CreateDirectory(Path.GetDirectoryName(dstAbs));
        File.Copy(src, dstAbs, false);
    }

    static void EnsureDir(string assetPath)
    {
        string abs = Path.Combine(Application.dataPath, "..", assetPath).Replace('\\', '/');
        Directory.CreateDirectory(abs);
    }

    static void SetTex(Material mat, string prop, string dstDir, string texName)
    {
        foreach (var ext in new[] { ".png", ".jpg", ".jpeg" })
        {
            string path = $"{dstDir}/{texName}{ext}";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null && mat.HasProperty(prop))
            {
                mat.SetTexture(prop, tex);
                // 노멀맵 타입 설정
                if (prop == "_BumpMap")
                {
                    var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (ti != null && ti.textureType != TextureImporterType.NormalMap)
                    {
                        ti.textureType = TextureImporterType.NormalMap;
                        ti.SaveAndReimport();
                    }
                    mat.SetFloat("_BumpScale", 1f);
                    mat.EnableKeyword("_NORMALMAP");
                }
                return;
            }
        }
    }

    static void SetTexDirect(Material mat, string prop, string assetPath)
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (tex != null && mat.HasProperty(prop))
            mat.SetTexture(prop, tex);
    }
}
