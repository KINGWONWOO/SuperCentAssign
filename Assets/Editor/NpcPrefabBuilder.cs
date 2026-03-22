using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 플레이어와 동일한 외형의 NPC 프리팹을 생성하고 씬의 UpgradeZone에 연결.
/// 메뉴: Game → Build NPC Prefabs
/// </summary>
public static class NpcPrefabBuilder
{
    private const string PREFAB_DIR = "Assets/Prefabs";

    [MenuItem("Game/Build NPC Prefabs")]
    public static void BuildNpcPrefabs()
    {
        // 플레이어 외형 재료 가져오기
        var player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("[NpcPrefabBuilder] Player not found in scene.");
            return;
        }

        var bodyTf   = player.transform.Find("Body");
        var headTf   = player.transform.Find("Head");
        if (bodyTf == null || headTf == null)
        {
            Debug.LogError("[NpcPrefabBuilder] Player Body/Head children not found.");
            return;
        }

        // 플레이어 색상 추출
        var bodyMr = bodyTf.GetComponent<MeshRenderer>();
        Color bodyColor = bodyMr != null ? bodyMr.sharedMaterial.color : new Color(0.2f, 0.5f, 1f);

        if (!Directory.Exists(PREFAB_DIR))
            Directory.CreateDirectory(PREFAB_DIR);

        // ── WorkerNPC 프리팹 ──────────────────────────────────────
        string workerPath = PREFAB_DIR + "/WorkerNPCPrefab.prefab";
        GameObject workerGo = BuildNpcGameObject("WorkerNPC", bodyColor);
        workerGo.AddComponent<WorkerNPC>();
        SavePrefab(workerGo, workerPath);
        Object.DestroyImmediate(workerGo);

        // ── AutoSellNPC 프리팹 ────────────────────────────────────
        string autoPath = PREFAB_DIR + "/AutoSellNPCPrefab.prefab";
        GameObject autoGo = BuildNpcGameObject("AutoSellNPC", bodyColor);
        autoGo.AddComponent<AutoSellNPC>();
        SavePrefab(autoGo, autoPath);
        Object.DestroyImmediate(autoGo);

        AssetDatabase.Refresh();

        // ── UpgradeZone에 프리팹 연결 ─────────────────────────────
        AssignToUpgradeZones(workerPath, autoPath);

        Debug.Log("[NpcPrefabBuilder] Done! WorkerNPC & AutoSellNPC prefabs created and assigned.");
        EditorUtility.DisplayDialog("완료", "WorkerNPC / AutoSellNPC 프리팹 생성 및 UpgradeZone 연결 완료!", "확인");
    }

    // 플레이어와 동일한 Body+Head 구조의 NPC 루트 오브젝트 생성
    private static GameObject BuildNpcGameObject(string name, Color bodyColor)
    {
        var root = new GameObject(name);

        // Body (캡슐)
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0f, 1f, 0f);
        body.transform.localScale    = Vector3.one;
        Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());

        var bodyMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        bodyMat.color = bodyColor;
        body.GetComponent<MeshRenderer>().sharedMaterial = bodyMat;

        // Head (구)
        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(root.transform);
        head.transform.localPosition = new Vector3(0f, 2.2f, 0f);
        head.transform.localScale    = new Vector3(0.6f, 0.6f, 0.6f);
        Object.DestroyImmediate(head.GetComponent<SphereCollider>());

        var headMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        headMat.color = bodyColor;
        head.GetComponent<MeshRenderer>().sharedMaterial = headMat;

        return root;
    }

    private static void SavePrefab(GameObject go, string path)
    {
        bool success;
        PrefabUtility.SaveAsPrefabAsset(go, path, out success);
        if (!success)
            Debug.LogError($"[NpcPrefabBuilder] Failed to save prefab: {path}");
    }

    private static void AssignToUpgradeZones(string workerPrefabPath, string autoSellPrefabPath)
    {
        var workerPrefab   = AssetDatabase.LoadAssetAtPath<GameObject>(workerPrefabPath);
        var autoSellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(autoSellPrefabPath);

        var upgradeZones = Object.FindObjectsOfType<UpgradeZone>();
        foreach (var uz in upgradeZones)
        {
            var so = new SerializedObject(uz);

            // UpgradeType 확인 — private이므로 SerializedProperty로 접근
            var upgradeTypeProp = so.FindProperty("upgradeType");
            if (upgradeTypeProp == null) continue;

            int typeVal = upgradeTypeProp.enumValueIndex;
            // UpgradeType enum: Drill=0, DrillCar=1, WorkerHire=2, AutoSell=3, PrisonExpand=4

            if (typeVal == 2 && workerPrefab != null) // WorkerHire
            {
                so.FindProperty("workerNpcPrefab").objectReferenceValue = workerPrefab;
                so.ApplyModifiedProperties();
                Debug.Log($"[NpcPrefabBuilder] WorkerNPCPrefab assigned to {uz.name}");
            }
            else if (typeVal == 3 && autoSellPrefab != null) // AutoSell
            {
                so.FindProperty("autoSellNpcPrefab").objectReferenceValue = autoSellPrefab;
                so.ApplyModifiedProperties();
                Debug.Log($"[NpcPrefabBuilder] AutoSellNPCPrefab assigned to {uz.name}");
            }
        }

        // 씬 저장
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}
