using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// place.png 배치도 기반 씬 자동 구성.
/// 메뉴: Game → Rebuild Scene
///
/// 좌표계 (place.png ↔ Unity 매핑):
///   PNG 좌 = -X,  PNG 우 = +X
///   PNG 위 = +Z,  PNG 아래 = -Z
///
/// 레이아웃:
///   우측     - 채굴 그리드(7×20, 세로, 펜스 그룹화)
///   상단중앙 - 돌→수갑 변환기, 수갑/돌 드롭존
///   좌측     - 수감자 도로(세로), 스포너, 경찰 책상
///   중앙     - 업그레이드 존 5종
///   하단     - 감옥 20명(회색, 입구 왼쪽) + 감옥 100명(녹색, 비활성)
/// </summary>
public static class SceneBuilder
{
    // ── 위치 좌표 (씬 실측값 기준, 2026-03-21) ───────────────────
    // 채굴 구역
    static readonly Vector3 GRID_CENTER       = new Vector3(20.46f, 0f,  4.26f);

    // 변환 시스템
    static readonly Vector3 ORE_DROP          = new Vector3( 5.42f, 0f, 11.68f);
    static readonly Vector3 CONVERTER         = new Vector3( 3.70f, 0f, 14.90f);
    static readonly Vector3 HANDCUFF_PICKUP   = new Vector3(-0.82f, 0f, 15.11f);

    // 수감자/책상 구역
    static readonly Vector3 PRISONER_SPAWN    = new Vector3(-10f,   0f, 18f);
    static readonly Vector3 PRISONER_WAIT     = new Vector3(-9.95f, 0f, 14.32f);
    static readonly Vector3 DESK_POS          = new Vector3( -8f,   0f, 11f);    // PoliceDeskArea
    static readonly Vector3 DESK_FRONT_POS    = new Vector3(-9.97f, 0f, 11.5f); // 수감자가 서는 책상 앞
    static readonly Vector3 MONEY_PICKUP_POS  = new Vector3(-7.4f,  0f,  8.3f);
    static readonly float   PRISONER_ROAD_X   = -10f;

    // 업그레이드 존 (씬 실측값)
    static readonly Vector3 UPG_DRILL         = new Vector3( 5.02f, 0f,  6.24f);
    static readonly Vector3 UPG_DRILLCAR      = new Vector3( 5.02f, 0f,  6.27f);
    static readonly Vector3 UPG_WORKER        = new Vector3( 5.06f, 0f,  2.71f);
    static readonly Vector3 UPG_AUTOSELL      = new Vector3(-4.54f, 0f, 10.86f);
    static readonly Vector3 UPG_PRISON        = new Vector3( 2.00f, 0f, -0.05f);

    // 감옥
    static readonly Vector3 PRISON_20_CENTER  = new Vector3( 3f,  0f, -7f);
    static readonly Vector3 PRISON_100_CENTER = new Vector3(15f,  0f, -7f);
    static readonly float   PRISON_20_W       = 8f;
    static readonly float   PRISON_100_W      = 16f;
    static readonly float   PRISON_DEPTH      = 10f;

    // 플레이어
    static readonly Vector3 PLAYER_START      = new Vector3(-1f, 0f, 7f);

    // ── 색상 팔레트 ───────────────────────────────────────────────
    static Color COL_ROCK       = new Color(0.62f, 0.58f, 0.54f);
    static Color COL_FENCE      = new Color(0.35f, 0.35f, 0.35f);
    static Color COL_CONVERTER  = new Color(0.85f, 0.45f, 0.10f);
    static Color COL_DESK       = new Color(0.85f, 0.55f, 0.25f);
    static Color COL_ZONE_ORE   = new Color(1.00f, 0.85f, 0.20f, 0.6f);
    static Color COL_ZONE_HC    = new Color(0.20f, 0.70f, 1.00f, 0.6f);
    static Color COL_ZONE_MONEY = new Color(0.20f, 0.90f, 0.30f, 0.6f);
    static Color COL_ROAD       = new Color(0.72f, 0.72f, 0.72f);
    static Color COL_PRISON_20  = new Color(0.72f, 0.72f, 0.72f);
    static Color COL_PRISON_100 = new Color(0.60f, 0.80f, 0.50f);
    static Color COL_UPG_DRILL  = new Color(0.30f, 0.70f, 1.00f);
    static Color COL_UPG_DRCAR  = new Color(0.80f, 0.30f, 1.00f);
    static Color COL_UPG_WORK   = new Color(1.00f, 0.55f, 0.10f);
    static Color COL_UPG_AUTO   = new Color(0.20f, 0.85f, 0.40f);
    static Color COL_UPG_PRIS   = new Color(0.90f, 0.30f, 0.30f);
    static Color COL_PLAYER     = new Color(0.20f, 0.50f, 1.00f);
    static Color COL_PRISONER   = new Color(1.00f, 0.55f, 0.10f);

    // ════════════════════════════════════════════════════════════
    // 진입점
    // ════════════════════════════════════════════════════════════
    [MenuItem("Game/Rebuild Scene")]
    public static void RebuildScene()
    {
        if (!EditorUtility.DisplayDialog("씬 재구성",
            "기존 씬 오브젝트를 모두 삭제하고 place.png 배치도 기준으로 재배치합니다.\n계속하시겠습니까?",
            "재구성", "취소")) return;

        ClearScene();
        var gs = LoadOrCreateGameSettings();
        var camObj = SetupCamera();
        SetupLighting();

        BuildFloorAndRoad();

        var grid     = BuildMiningGrid(gs);
        var converter = BuildConverter();
        BuildDropZones(converter);

        var prisonResult = BuildPrison();
        var deskResult   = BuildDesk();
        BuildPrisonerSpawner(deskResult.desk, prisonResult.cellManager);
        BuildMoneyPickupZone(deskResult.desk);

        BuildUpgradeZones(prisonResult.cellManager, grid, prisonResult.prison100);
        BuildOuterWalls();
        BuildManagers(gs);
        BuildMoneyHUD();
        var playerObj = BuildPlayer();

        // 카메라 팔로우 타겟 연결
        var follow = camObj.GetComponent<CameraFollow>();
        if (follow != null)
        {
            var soFollow = new SerializedObject(follow);
            soFollow.FindProperty("target").objectReferenceValue = playerObj.transform;
            soFollow.ApplyModifiedPropertiesWithoutUndo();
        }

        Debug.Log("[SceneBuilder] ✅ place.png 기반 씬 재구성 완료!");
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
    }

    // ════════════════════════════════════════════════════════════
    // 씬 정리
    // ════════════════════════════════════════════════════════════
    static void ClearScene()
    {
        foreach (var go in UnityEngine.SceneManagement.SceneManager
                                      .GetActiveScene().GetRootGameObjects())
        {
            string n = go.name;
            if (n == "EventSystem") continue;
            Object.DestroyImmediate(go);
        }
    }

    // ════════════════════════════════════════════════════════════
    // 카메라 & 조명
    // ════════════════════════════════════════════════════════════
    static GameObject SetupCamera()
    {
        var cam = new GameObject("Main Camera"); cam.tag = "MainCamera";
        cam.AddComponent<AudioListener>();
        var c = cam.AddComponent<Camera>();
        c.fieldOfView = 50f;
        c.farClipPlane = 300f;

        // 아이소메트릭 45° 뷰
        cam.transform.rotation = Quaternion.Euler(50f, 45f, 0f);
        cam.transform.position = PLAYER_START + new Vector3(-7f, 12f, -7f);

        var follow = cam.AddComponent<CameraFollow>();
        var soFollow = new SerializedObject(follow);
        soFollow.FindProperty("offset").vector3Value = new Vector3(-7f, 12f, -7f);
        soFollow.ApplyModifiedPropertiesWithoutUndo();

        return cam;
    }

    static void SetupLighting()
    {
        var sun = new GameObject("Directional Light");
        var l = sun.AddComponent<Light>();
        l.type = LightType.Directional;
        l.intensity = 1.1f;
        l.color = Color.white;
        sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    // ════════════════════════════════════════════════════════════
    // 바닥 & 수감자 도로
    // ════════════════════════════════════════════════════════════
    static void BuildFloorAndRoad()
    {
        // 투명 지면 Collider (플레이어가 서는 실제 충돌면, y=0)
        var ground = new GameObject("Ground");
        var gc = ground.AddComponent<BoxCollider>();
        gc.size = new Vector3(120f, 0.1f, 120f);
        gc.center = new Vector3(0f, -0.05f, 0f); // 상단 표면 = y=0

        // 전체 바닥 (시각적 장식, Collider 비활성)
        var floor = MakeCube("Floor",
            new Vector3(4f, -0.55f, 5f),
            new Vector3(38f, 0.1f, 36f),
            new Color(0.88f, 0.86f, 0.82f));
        floor.GetComponent<Collider>().enabled = false;

        // ── 수감자 이동 도로 그룹 ──────────────────────────────────
        var roadGroup = new GameObject("PrisonerRoadGroup");
        roadGroup.transform.position = new Vector3(PRISONER_ROAD_X, 0f, 4f);

        // 세로 도로 (위→아래)
        var road = MakeChildCube(roadGroup, "RoadVertical",
            new Vector3(0f, -0.50f, 0f),
            new Vector3(2.5f, 0.12f, 30f),
            COL_ROAD);
        road.GetComponent<Collider>().enabled = false;

        // 수평 연결로 (도로→감옥 입구)
        var roadH = MakeChildCube(roadGroup, "RoadHorizontal",
            new Vector3((PRISON_20_CENTER.x - 4f - PRISONER_ROAD_X) * 0.5f, -0.50f,
                        PRISON_20_CENTER.z - roadGroup.transform.position.z),
            new Vector3(Mathf.Abs(PRISON_20_CENTER.x - 4f - PRISONER_ROAD_X), 0.12f, 2.5f),
            COL_ROAD);
        roadH.GetComponent<Collider>().enabled = false;

        // 도로 점선
        for (int i = 0; i < 10; i++)
        {
            var line = MakeChildCube(roadGroup, $"RoadLine_{i}",
                new Vector3(0f, -0.48f, -10f + i * 2.8f),
                new Vector3(0.12f, 0.12f, 1.0f),
                Color.white);
            line.GetComponent<Collider>().enabled = false;
        }
    }

    // ════════════════════════════════════════════════════════════
    // 채굴 그리드 (우측, 세로 배치, 모두 MiningGridGroup 자식으로 그룹화)
    // ════════════════════════════════════════════════════════════
    static MiningGrid BuildMiningGrid(GameSettings gs)
    {
        int cols = 7; int rows = 20;
        float sx = 1.2f; float sz = 1.2f;
        // 실제 암석 스팬 (gridOrigin 센터링 기반)
        float rockW = (cols - 1) * sx;  // 7.2
        float rockD = (rows - 1) * sz;  // 22.8
        // 울타리는 암석 영역보다 약간 넓게
        float totalW = rockW + sx + 1.2f;  // 9.6
        float totalD = rockD + sz + 1.2f;  // 25.2
        float hw = totalW * 0.5f;
        float hd = totalD * 0.5f;

        // ── MiningGridGroup (이 오브젝트를 옮기면 전체 이동) ────────
        var group = new GameObject("MiningGridGroup");
        group.transform.position = GRID_CENTER;
        group.transform.rotation = Quaternion.Euler(0f, 90f, 0f); // 입구가 플레이어(-Z 방향) 쪽을 향하게

        // 그리드 바닥
        var gridFloor = MakeChildCube(group, "MiningGridFloor",
            new Vector3(0f, -0.52f, 0f),
            new Vector3(totalW, 0.12f, totalD),
            new Color(0.50f, 0.48f, 0.44f));
        gridFloor.GetComponent<Collider>().enabled = false;

        // ── 울타리 (모두 group 자식) ─────────────────────────────────
        // 앞/뒤 울타리: side=0(입구 쪽)은 코너 기둥만, side=1(안쪽)은 전체
        for (int side = 0; side < 2; side++)
        {
            float lz = side == 0 ? -hd : hd;
            if (side == 0)
            {
                // 입구 쪽: 코너 기둥 2개만 (c=-3, c=3), 레일 없음
                MakeChildCube(group, $"FencePost_FB_{side}_{-3}",
                    new Vector3(-3 * (totalW / 6f), 0.6f, lz),
                    new Vector3(0.15f, 1.4f, 0.15f), COL_FENCE);
                MakeChildCube(group, $"FencePost_FB_{side}_{3}",
                    new Vector3( 3 * (totalW / 6f), 0.6f, lz),
                    new Vector3(0.15f, 1.4f, 0.15f), COL_FENCE);
            }
            else
            {
                for (int c = -3; c <= 3; c++)
                    MakeChildCube(group, $"FencePost_FB_{side}_{c}",
                        new Vector3(c * (totalW / 6f), 0.6f, lz),
                        new Vector3(0.15f, 1.4f, 0.15f), COL_FENCE);
                MakeChildCube(group, $"FenceRail_FB_{side}",
                    new Vector3(0f, 0.9f, lz),
                    new Vector3(totalW, 0.12f, 0.12f), COL_FENCE);
                MakeChildCube(group, $"FenceRail2_FB_{side}",
                    new Vector3(0f, 0.4f, lz),
                    new Vector3(totalW, 0.12f, 0.12f), COL_FENCE);
            }
        }

        // 좌/우 세로 울타리 (Z방향으로 뻗음)
        // side=0: 전체 기둥(r=0~10) + 레일
        // side=1: 내부 기둥만(r=1~9, 코너는 FB 기둥이 대신) + 레일
        for (int side = 0; side < 2; side++)
        {
            float lx = side == 0 ? -hw : hw;
            int postCount = 11;
            int rStart = side == 1 ? 1 : 0;
            int rEnd   = side == 1 ? postCount - 2 : postCount - 1;
            for (int r = rStart; r <= rEnd; r++)
                MakeChildCube(group, $"FencePost_LR_{side}_{r}",
                    new Vector3(lx, 0.6f, -hd + r * (totalD / (postCount - 1))),
                    new Vector3(0.15f, 1.4f, 0.15f), COL_FENCE);
            MakeChildCube(group, $"FenceRail_LR_{side}",
                new Vector3(lx, 0.9f, 0f),
                new Vector3(0.12f, 0.12f, totalD), COL_FENCE);
            MakeChildCube(group, $"FenceRail2_LR_{side}",
                new Vector3(lx, 0.4f, 0f),
                new Vector3(0.12f, 0.12f, totalD), COL_FENCE);
        }

        // 레이블 (그룹 자식)
        MakeLabel(group.transform, "채굴 구역 (7×20)",
            new Vector3(0f, 2.5f, -hd - 1.5f), Color.white, 3.5f);

        // 그리드 코너 기둥 — 항상 보이는 주황 기둥으로 채굴 영역 시각화
        Color col_corner = new Color(1f, 0.5f, 0f);
        MakeChildCube(group, "GridCorner_00", new Vector3(-hw,  1.2f, -hd), new Vector3(0.25f, 2.5f, 0.25f), col_corner);
        MakeChildCube(group, "GridCorner_10", new Vector3( hw,  1.2f, -hd), new Vector3(0.25f, 2.5f, 0.25f), col_corner);
        MakeChildCube(group, "GridCorner_01", new Vector3(-hw,  1.2f,  hd), new Vector3(0.25f, 2.5f, 0.25f), col_corner);
        MakeChildCube(group, "GridCorner_11", new Vector3( hw,  1.2f,  hd), new Vector3(0.25f, 2.5f, 0.25f), col_corner);

        // ── MiningGrid 컴포넌트 (group 자식) ─────────────────────────
        var gridObj = new GameObject("MiningGrid");
        // worldPositionStays=false → localRotation이 identity 유지 → 부모 90° 회전 상속
        gridObj.transform.SetParent(group.transform, false);
        // y=0.275 = 돌 프리팹 높이(0.55)의 절반 → 돌 바닥이 지면(y=0)에 닿음
        gridObj.transform.localPosition = new Vector3(0f, 0.275f, 0f);

        var rockPrefab = GetOrCreateRockPrefab();
        var orePrefab  = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/OrePrefab.prefab");

        var mg = gridObj.AddComponent<MiningGrid>();
        var so = new SerializedObject(mg);
        so.FindProperty("rockPrefab").objectReferenceValue = rockPrefab;
        so.FindProperty("orePrefab").objectReferenceValue  = orePrefab;
        so.ApplyModifiedPropertiesWithoutUndo();

        return mg;
    }

    // ════════════════════════════════════════════════════════════
    // 변환기 (돌 → 수갑)
    // ════════════════════════════════════════════════════════════
    static ConverterMachine BuildConverter()
    {
        var root = new GameObject("ConverterMachine");
        root.transform.position = CONVERTER;

        MakeChildCube(root, "Body", Vector3.zero,
            new Vector3(2.5f, 1.2f, 2f), COL_CONVERTER);

        for (int i = 0; i < 5; i++)
            MakeChildCube(root, $"Roller_{i}",
                new Vector3(-1.0f + i * 0.5f, 0.65f, 0.3f),
                new Vector3(0.12f, 0.12f, 1.2f),
                new Color(0.25f, 0.25f, 0.25f));

        MakeChildCube(root, "Belt",
            new Vector3(0f, 0.62f, 0.3f),
            new Vector3(2.4f, 0.08f, 1.0f),
            new Color(0.15f, 0.15f, 0.15f));
        MakeChildCube(root, "DrillHead",
            new Vector3(1.0f, 0.6f, 0f),
            new Vector3(0.8f, 0.5f, 0.5f),
            new Color(0.50f, 0.50f, 0.55f));

        var hcPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/HandcuffPrefab.prefab");

        MakeLabel(root.transform, "돌→수갑 변환기",
            new Vector3(0f, 1.8f, 0f), COL_CONVERTER, 3f);

        var cm = root.AddComponent<ConverterMachine>();
        var so = new SerializedObject(cm);
        so.FindProperty("handcuffPrefab").objectReferenceValue = hcPrefab;
        so.ApplyModifiedPropertiesWithoutUndo();

        return cm;
    }

    // ════════════════════════════════════════════════════════════
    // 드롭 존
    // ════════════════════════════════════════════════════════════
    static void BuildDropZones(ConverterMachine converter)
    {
        BuildDropZone("OreDropZone", ORE_DROP,
            new Vector3(3.5f, 2f, 3f),
            DropZoneType.OreToConverter, COL_ZONE_ORE, "돌 두는 곳",
            conv: converter);

        var handcuffDZ = BuildDropZone("HandcuffPickupZone", HANDCUFF_PICKUP,
            new Vector3(4f, 2f, 2.5f),
            DropZoneType.HandcuffPickup, COL_ZONE_HC, "수갑 받아가는 곳");

        // ConverterMachine → HandcuffPickupZone 연결
        var convSO = new SerializedObject(converter);
        convSO.FindProperty("handcuffDropZone").objectReferenceValue = handcuffDZ;
        convSO.ApplyModifiedPropertiesWithoutUndo();
    }

    // DeskManager에 moneyPrefab + moneyPickupZone 연결
    static void BuildMoneyPickupZone(DeskManager deskManager)
    {
        var moneyDZ = BuildDropZone("MoneyPickupZone", MONEY_PICKUP_POS,
            new Vector3(2.5f, 2f, 2f),
            DropZoneType.MoneyPickup, COL_ZONE_MONEY, "돈 받아가는 곳");

        var cashPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/CashPrefab.prefab");

        var dmSO = new SerializedObject(deskManager);
        dmSO.FindProperty("moneyPrefab").objectReferenceValue      = cashPrefab;
        dmSO.FindProperty("moneyPickupZone").objectReferenceValue  = moneyDZ;
        dmSO.ApplyModifiedPropertiesWithoutUndo();
    }

    static DropZone BuildDropZone(string name, Vector3 pos, Vector3 size,
        DropZoneType type, Color color, string label,
        ConverterMachine conv = null)
    {
        var obj = new GameObject(name);
        obj.transform.position = pos;

        var bc = obj.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = size;

        var tile = MakeChildCube(obj, "Tile",
            new Vector3(0f, -0.5f, 0f),
            new Vector3(size.x, 0.12f, size.z), color);
        tile.GetComponent<Collider>().enabled = false;

        float hw2 = size.x * 0.5f - 0.1f;
        float hd2 = size.z * 0.5f - 0.1f;
        Color lineCol = new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f);
        foreach (var corner in new[] {
            new Vector2(-hw2, -hd2), new Vector2(hw2, -hd2),
            new Vector2(-hw2,  hd2), new Vector2(hw2,  hd2) })
        {
            MakeChildCube(obj, "Corner",
                new Vector3(corner.x, -0.48f, corner.y),
                new Vector3(0.25f, 0.12f, 0.25f), lineCol)
                .GetComponent<Collider>().enabled = false;
        }

        // HandcuffPickup / MoneyPickup 전용: 아이템이 쌓이는 기준점
        Transform stackRoot = null;
        if (type == DropZoneType.HandcuffPickup || type == DropZoneType.MoneyPickup)
        {
            var sr = new GameObject("StackRoot");
            sr.transform.SetParent(obj.transform);
            sr.transform.localPosition = new Vector3(0f, -0.4f, 0f);
            stackRoot = sr.transform;
        }

        MakeLabel(obj.transform, label, new Vector3(0f, 0.6f, 0f), color * 1.2f, 2.5f);

        var dz = obj.AddComponent<DropZone>();
        var so = new SerializedObject(dz);
        so.FindProperty("zoneType").enumValueIndex = (int)type;
        if (conv != null)       so.FindProperty("converterMachine").objectReferenceValue = conv;
        if (stackRoot != null)  so.FindProperty("stackRoot").objectReferenceValue = stackRoot;
        so.ApplyModifiedPropertiesWithoutUndo();

        return dz;
    }

    // ════════════════════════════════════════════════════════════
    // 경찰 책상 + DeskManager + MoneySpawner
    // ════════════════════════════════════════════════════════════
    struct DeskResult { public DeskManager desk; }

    static DeskResult BuildDesk()
    {
        var deskRoot = new GameObject("PoliceDeskArea");
        deskRoot.transform.position = DESK_POS;
        deskRoot.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

        MakeChildCube(deskRoot, "Top",
            new Vector3(0f, 0.4f, 0f),
            new Vector3(2.5f, 0.12f, 1.5f), COL_DESK);

        foreach (var leg in new[] {
            new Vector3(-1.0f, 0f, -0.6f), new Vector3(1.0f, 0f, -0.6f),
            new Vector3(-1.0f, 0f,  0.6f), new Vector3(1.0f, 0f,  0.6f) })
        {
            MakeChildCube(deskRoot, "Leg",
                leg + new Vector3(0f, -0.2f, 0f),
                new Vector3(0.15f, 0.8f, 0.15f),
                new Color(0.6f, 0.4f, 0.2f));
        }

        MakeLabel(deskRoot.transform, "경찰 책상",
            new Vector3(0f, 1.2f, 0f), COL_DESK, 2.8f);

        var stackPt = new GameObject("DeskStackPoint");
        stackPt.transform.SetParent(deskRoot.transform);
        stackPt.transform.localPosition = new Vector3(0f, 0.5f, 0f);

        // DeskZone: 책상과 수감자 사이 — 플레이어가 수갑을 내려놓는 영역
        var dzoneObj = new GameObject("DeskZone");
        dzoneObj.transform.position = new Vector3(
            (DESK_POS.x + DESK_FRONT_POS.x) * 0.5f, 0f, DESK_FRONT_POS.z);
        var bc = dzoneObj.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = new Vector3(4f, 2f, 3f);

        var dmObj = new GameObject("DeskManager");
        dmObj.transform.position = DESK_POS;
        var dm = dmObj.AddComponent<DeskManager>();
        var dmSO = new SerializedObject(dm);
        dmSO.FindProperty("deskStackPoint").objectReferenceValue = stackPt.transform;
        // moneyPrefab / moneyPickupZone → BuildMoneyPickupZone에서 연결
        dmSO.ApplyModifiedPropertiesWithoutUndo();

        var dz = dzoneObj.AddComponent<DeskZone>();
        var dzSO = new SerializedObject(dz);
        dzSO.FindProperty("deskManager").objectReferenceValue = dm;
        dzSO.ApplyModifiedPropertiesWithoutUndo();

        return new DeskResult { desk = dm };
    }

    // ════════════════════════════════════════════════════════════
    // 감옥 (20명 + 100명 확장, 입구가 왼쪽 -X 방향)
    // ════════════════════════════════════════════════════════════
    struct PrisonResult { public CellManager cellManager; public GameObject prison100; }

    static PrisonResult BuildPrison()
    {
        var prisonRoot = new GameObject("Prison");
        prisonRoot.transform.position = Vector3.zero;

        // ── 감옥 20명 (기본, 회색) ──────────────────────────────────
        BuildPrisonBlock(prisonRoot, "Prison_20",
            PRISON_20_CENTER, PRISON_20_W, PRISON_DEPTH, COL_PRISON_20,
            "감옥\n20명\n(업그레이드 전)");

        // 철창: 왼쪽 면 (-X 방향, 수감자 도로와 마주봄)
        float p20_leftX = PRISON_20_CENTER.x - PRISON_20_W * 0.5f;
        BuildBarsLeft(prisonRoot,
            new Vector3(p20_leftX, 0f, PRISON_20_CENTER.z),
            PRISON_DEPTH);

        // ── 감옥 100명 (업그레이드 후, 녹색, 비활성) ────────────────
        var p100 = BuildPrisonBlock(prisonRoot, "Prison_100",
            PRISON_100_CENTER, PRISON_100_W, PRISON_DEPTH, COL_PRISON_100,
            "감옥\n100명\n(업그레이드 후)");
        p100.SetActive(false); // PrisonExpand 업그레이드 완료 시 활성화

        // 철창 100: 왼쪽 면
        float p100_leftX = PRISON_100_CENTER.x - PRISON_100_W * 0.5f;
        var bars100 = new GameObject("Bars_100");
        bars100.transform.SetParent(p100.transform);
        bars100.transform.position = Vector3.zero;
        BuildBarsLeft(bars100,
            new Vector3(p100_leftX, 0f, PRISON_100_CENTER.z),
            PRISON_DEPTH);
        // bars100도 p100 활성화 시 함께 보임 (부모가 p100)

        // 카운터 텍스트
        var counterObj = new GameObject("CellCounter");
        counterObj.transform.position = PRISON_20_CENTER + new Vector3(0f, 3.5f, 0f);
        var counter = counterObj.AddComponent<TMPro.TextMeshPro>();
        counter.text = "0/20";
        counter.fontSize = 5f;
        counter.alignment = TMPro.TextAlignmentOptions.Center;
        counter.color = Color.white;

        // 셀 루트 (CellManager가 감방 위치를 생성하는 기준) — 초록 구체로 씬 뷰에 표시
        var cellRoot = MakeMarker("CellRoot", prisonRoot,
                                  PRISON_20_CENTER + new Vector3(0f, 0f, -2f),
                                  new Color(0.2f, 1f, 0.3f));
        // MarkerVis를 씬에서 이동한 위치로 고정
        cellRoot.transform.Find("MarkerVis").localPosition = new Vector3(0f, 0f, 1.79f);

        // 감옥 밖 FIFO 대기줄 시작 위치 — 철창(-X면) 바로 앞, +Z 방향으로 줄 섬
        float p20LeftX = PRISON_20_CENTER.x - PRISON_20_W * 0.5f - 1.5f;
        var queueStart = MakeMarker("OutsideQueueStart", prisonRoot,
                                    new Vector3(p20LeftX, 0f, PRISON_20_CENTER.z),
                                    new Color(1f, 0.2f, 1f)); // 보라
        queueStart.transform.rotation = Quaternion.LookRotation(Vector3.forward);

        var cm = prisonRoot.AddComponent<CellManager>();
        var so = new SerializedObject(cm);
        so.FindProperty("cellRoot").objectReferenceValue          = cellRoot.transform;
        so.FindProperty("outsideQueueStart").objectReferenceValue = queueStart.transform;
        so.FindProperty("cellCounterText").objectReferenceValue   = counter;
        so.ApplyModifiedPropertiesWithoutUndo();

        return new PrisonResult { cellManager = cm, prison100 = p100 };
    }

    static GameObject BuildPrisonBlock(GameObject parent, string name,
        Vector3 center, float width, float depth, Color color, string label)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent.transform);
        obj.transform.position = center;

        // 바닥
        var floor = MakeChildCube(obj, "Floor",
            new Vector3(0f, -0.5f, 0f),
            new Vector3(width, 0.15f, depth), color);
        floor.GetComponent<Collider>().enabled = false;

        // 벽 3면 (뒤+우+앞, 왼쪽은 철창으로 대체)
        Color wallCol = new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f);
        // 뒤 벽 (+X면)
        MakeChildCube(obj, "WallBack",
            new Vector3(width * 0.5f, 1f, 0f),
            new Vector3(0.2f, 2.2f, depth), wallCol)
            .GetComponent<Collider>().enabled = false;
        // 앞 벽 (+Z면)
        MakeChildCube(obj, "WallFront",
            new Vector3(0f, 1f, depth * 0.5f),
            new Vector3(width, 2.2f, 0.2f), wallCol)
            .GetComponent<Collider>().enabled = false;
        // 뒤 벽 (-Z면)
        MakeChildCube(obj, "WallRear",
            new Vector3(0f, 1f, -depth * 0.5f),
            new Vector3(width, 2.2f, 0.2f), wallCol)
            .GetComponent<Collider>().enabled = false;

        MakeLabel(obj.transform, label, new Vector3(0f, 2.5f, 0f), Color.white, 3.5f);

        return obj;
    }

    /// <summary>왼쪽 면(-X)에 Z방향으로 철창을 배치</summary>
    static void BuildBarsLeft(GameObject parent, Vector3 center, float depth)
    {
        int count = Mathf.CeilToInt(depth / 0.65f);
        for (int i = 0; i < count; i++)
        {
            float z = center.z - depth * 0.5f
                      + i * (depth / count) + (depth / count) * 0.5f;
            MakeChildCube(parent, $"Bar_{i}",
                new Vector3(center.x, center.y + 1.0f, z),
                new Vector3(0.12f, 2.2f, 0.12f),
                new Color(0.3f, 0.3f, 0.3f));
        }
        // 가로 봉 (Z방향)
        MakeChildCube(parent, "HBar_Top",
            new Vector3(center.x, center.y + 2.0f, center.z),
            new Vector3(0.12f, 0.12f, depth),
            new Color(0.3f, 0.3f, 0.3f));
    }

    // ════════════════════════════════════════════════════════════
    // 수감자 스포너 + 이동 경로 마커
    // ════════════════════════════════════════════════════════════
    static void BuildPrisonerSpawner(DeskManager desk, CellManager cellManager)
    {
        // ── 스포너 본체 ─────────────────────────────────────────
        var spawnerRoot = new GameObject("PrisonerSpawner");
        spawnerRoot.transform.position = PRISONER_SPAWN;

        MakeChildCube(spawnerRoot, "Body", Vector3.zero,
            new Vector3(2f, 1f, 2f), new Color(0.95f, 0.60f, 0.10f));
        MakeLabel(spawnerRoot.transform, "수감자\n스포너",
            new Vector3(0f, 1.5f, 0f), COL_PRISONER, 3f);

        // ── 이동 경로 마커 그룹 (PrisonerPath) ─────────────────
        // 기존 마커 제거 후 새로 생성 (Rebuild Scene 시 자동 처리됨)
        // 경로: ① SpawnPoint(빨강) → ② WaitPosition(노랑) → ③ DeskPosition(하늘)
        var pathRoot = new GameObject("PrisonerPath");

        var spawnPt = MakePathMarker("SpawnPoint",   pathRoot, PRISONER_SPAWN,
                                     new Color(1f, 0.15f, 0.15f), "① 스폰");
        var waitPt  = MakePathMarker("WaitPosition", pathRoot, PRISONER_WAIT,
                                     new Color(1f, 0.85f, 0f),    "② 대기");
        var deskPt  = MakePathMarker("DeskPosition", pathRoot, DESK_FRONT_POS,
                                     new Color(0.15f, 0.75f, 1f), "③ 책상앞");

        // 경로 연결 화살표
        MakePathArrow(pathRoot, PRISONER_SPAWN, PRISONER_WAIT,  new Color(1f, 0.6f, 0.1f));
        MakePathArrow(pathRoot, PRISONER_WAIT,  DESK_FRONT_POS, new Color(0.3f, 0.85f, 1f));

        // ── PrisonerSpawner 컴포넌트 연결 ──────────────────────
        var prisonerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/PrisonerPrefab.prefab");

        var ps = spawnerRoot.AddComponent<PrisonerSpawner>();
        var so = new SerializedObject(ps);
        so.FindProperty("prisonerPrefab").objectReferenceValue = prisonerPrefab;
        so.FindProperty("spawnPoint").objectReferenceValue     = spawnPt.transform;
        so.FindProperty("waitPosition").objectReferenceValue   = waitPt.transform;
        so.FindProperty("deskPosition").objectReferenceValue   = deskPt.transform;
        so.FindProperty("deskManager").objectReferenceValue    = desk;
        so.FindProperty("cellManager").objectReferenceValue    = cellManager;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // 번호 라벨이 달린 경로 마커
    static GameObject MakePathMarker(string name, GameObject parent,
                                      Vector3 worldPos, Color color, string label)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent.transform);
        obj.transform.position = worldPos;

        // 마커 구체 (런타임에도 보임)
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "MarkerVis";
        sphere.transform.SetParent(obj.transform);
        sphere.transform.localPosition = Vector3.zero;
        sphere.transform.localScale    = Vector3.one * 0.55f;
        sphere.GetComponent<Renderer>().sharedMaterial = MakeMat(color);
        Object.DestroyImmediate(sphere.GetComponent<Collider>());

        // 기둥 (마커 위치 강조)
        var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "Pole";
        pole.transform.SetParent(obj.transform);
        pole.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        pole.transform.localScale    = new Vector3(0.06f, 0.6f, 0.06f);
        pole.GetComponent<Renderer>().sharedMaterial = MakeMat(color * 0.6f);
        Object.DestroyImmediate(pole.GetComponent<Collider>());

        // 번호 라벨 (카메라 방향)
        MakeLabel(obj.transform, label, new Vector3(0f, 1.5f, 0f), color, 2.8f);

        return obj;
    }

    // 두 지점을 잇는 얇은 경로 화살표 (씬 뷰 + 런타임 가시화)
    static void MakePathArrow(GameObject parent, Vector3 from, Vector3 to, Color color)
    {
        Vector3 dir    = to - from;
        float   length = dir.magnitude;
        Vector3 mid    = (from + to) * 0.5f + Vector3.up * 0.05f;

        var cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cyl.name = "PathLine";
        cyl.transform.SetParent(parent.transform);
        cyl.transform.position  = mid;
        cyl.transform.rotation  = Quaternion.LookRotation(dir) * Quaternion.Euler(90f, 0f, 0f);
        cyl.transform.localScale = new Vector3(0.07f, length * 0.5f, 0.07f);
        cyl.GetComponent<Renderer>().sharedMaterial = MakeMat(new Color(color.r, color.g, color.b, 0.6f));
        Object.DestroyImmediate(cyl.GetComponent<Collider>());
    }

    // ════════════════════════════════════════════════════════════
    // 업그레이드 존 (5종)
    // prison100: BuildPrison()에서 직접 받아 비활성 오브젝트 참조 문제 해결
    // ════════════════════════════════════════════════════════════
    static void BuildUpgradeZones(CellManager cellManager, MiningGrid miningGrid,
                                   GameObject prison100)
    {
        var root = new GameObject("_UpgradeZones");

        var upgDrill = BuildUpgradeZone(root.transform, "UpgDrill", UPG_DRILL,
            UpgradeType.Drill, "$20\n드릴", COL_UPG_DRILL,
            cellManager, miningGrid, null);

        var upgDrillCar = BuildUpgradeZone(root.transform, "UpgDrillCar", UPG_DRILLCAR,
            UpgradeType.DrillCar, "$50\n드릴 차", COL_UPG_DRCAR,
            cellManager, miningGrid, null);
        upgDrillCar.SetActive(false);

        var upgWorker = BuildUpgradeZone(root.transform, "UpgWorker", UPG_WORKER,
            UpgradeType.WorkerHire, "$40\n인력 고용", COL_UPG_WORK,
            cellManager, miningGrid, null);
        upgWorker.SetActive(false);

        var upgAutoSell = BuildUpgradeZone(root.transform, "UpgAutoSell", UPG_AUTOSELL,
            UpgradeType.AutoSell, "$50\n자동 판매", COL_UPG_AUTO,
            cellManager, miningGrid, null);
        upgAutoSell.SetActive(false);

        // PrisonExpand: prison100를 직접 참조 (비활성 오브젝트도 안전하게 전달)
        var upgPrison = BuildUpgradeZone(root.transform, "UpgPrison", UPG_PRISON,
            UpgradeType.PrisonExpand, "$50\n감옥 확장", COL_UPG_PRIS,
            cellManager, miningGrid, prison100);
        upgPrison.SetActive(false);

        // 드릴 업그레이드 완료 → 드릴차 + 인력 + 자판 활성화
        {
            var uz = upgDrill.GetComponent<UpgradeZone>();
            var uzSO = new SerializedObject(uz);
            var arr = uzSO.FindProperty("objectsToActivateOnComplete");
            arr.ClearArray();
            arr.InsertArrayElementAtIndex(0); arr.GetArrayElementAtIndex(0).objectReferenceValue = upgDrillCar;
            arr.InsertArrayElementAtIndex(1); arr.GetArrayElementAtIndex(1).objectReferenceValue = upgWorker;
            arr.InsertArrayElementAtIndex(2); arr.GetArrayElementAtIndex(2).objectReferenceValue = upgAutoSell;
            uzSO.ApplyModifiedPropertiesWithoutUndo();
        }

        // 드릴차 업그레이드 완료 → 감옥 확장 활성화
        {
            var uz = upgDrillCar.GetComponent<UpgradeZone>();
            var uzSO = new SerializedObject(uz);
            var arr = uzSO.FindProperty("objectsToActivateOnComplete");
            arr.ClearArray();
            arr.InsertArrayElementAtIndex(0); arr.GetArrayElementAtIndex(0).objectReferenceValue = upgPrison;
            uzSO.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    static GameObject BuildUpgradeZone(Transform parent, string name, Vector3 pos,
        UpgradeType upgradeType, string label, Color color,
        CellManager cellManager, MiningGrid miningGrid, GameObject activateOnComplete)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.transform.position = pos;

        var bc = obj.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = new Vector3(2.2f, 2f, 2.2f);

        MakeChildCube(obj, "Platform",
            new Vector3(0f, -0.5f, 0f),
            new Vector3(2.2f, 0.12f, 2.2f), color)
            .GetComponent<Collider>().enabled = false;

        MakeChildCube(obj, "ArrowShaft",
            new Vector3(0f, -0.46f, 0f),
            new Vector3(0.35f, 0.12f, 1.0f), Color.white)
            .GetComponent<Collider>().enabled = false;
        MakeChildCube(obj, "ArrowHead",
            new Vector3(0f, -0.44f, 0.55f),
            new Vector3(0.8f, 0.12f, 0.4f), Color.white)
            .GetComponent<Collider>().enabled = false;

        var fillBar = MakeChildCube(obj, "FillBar",
            new Vector3(1.3f, 0f, 0f),
            new Vector3(0.3f, 0.001f, 0.3f), Color.green);
        fillBar.GetComponent<Collider>().enabled = false;

        var textObj = new GameObject("CostText");
        textObj.transform.SetParent(obj.transform);
        textObj.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        var tmp = textObj.AddComponent<TMPro.TextMeshPro>();
        tmp.text = label;
        tmp.fontSize = 3f;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = color * 1.3f;

        var uz = obj.AddComponent<UpgradeZone>();
        var so = new SerializedObject(uz);
        so.FindProperty("upgradeType").enumValueIndex  = (int)upgradeType;
        so.FindProperty("costText").objectReferenceValue   = tmp;
        so.FindProperty("fillBar").objectReferenceValue    = fillBar.transform;
        so.FindProperty("miningGrid").objectReferenceValue = miningGrid;
        if (upgradeType == UpgradeType.PrisonExpand)
            so.FindProperty("cellManager").objectReferenceValue = cellManager;
        if (activateOnComplete != null)
        {
            var arr = so.FindProperty("objectsToActivateOnComplete");
            arr.ClearArray();
            arr.InsertArrayElementAtIndex(0);
            arr.GetArrayElementAtIndex(0).objectReferenceValue = activateOnComplete;
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        return obj;
    }

    // ════════════════════════════════════════════════════════════
    // 매니저 싱글톤
    // ════════════════════════════════════════════════════════════
    static void BuildManagers(GameSettings gs)
    {
        var mgr = new GameObject("_Managers");
        var gm = mgr.AddComponent<GameManager>();
        var gmSO = new SerializedObject(gm);
        gmSO.FindProperty("settings").objectReferenceValue = gs;
        gmSO.ApplyModifiedPropertiesWithoutUndo();
        mgr.AddComponent<CurrencyManager>();
        mgr.AddComponent<UIManager>();
    }

    // ════════════════════════════════════════════════════════════
    // 플레이어
    // ════════════════════════════════════════════════════════════
    static GameObject BuildPlayer()
    {
        var player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = PLAYER_START;

        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(player.transform);
        body.transform.localPosition = new Vector3(0f, 1f, 0f);
        body.GetComponent<Renderer>().sharedMaterial = MakeMat(COL_PLAYER);
        Object.DestroyImmediate(body.GetComponent<Collider>());

        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(player.transform);
        head.transform.localPosition = new Vector3(0f, 2.2f, 0f);
        head.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
        head.GetComponent<Renderer>().sharedMaterial =
            MakeMat(new Color(1.0f, 0.85f, 0.65f));
        Object.DestroyImmediate(head.GetComponent<Collider>());

        var rb = player.AddComponent<Rigidbody>();
        rb.isKinematic = true; rb.useGravity = false;

        var cc = player.AddComponent<CharacterController>();
        cc.height = 2f; cc.radius = 0.5f; cc.center = new Vector3(0f, 1f, 0f);

        var sc = player.AddComponent<SphereCollider>();
        sc.isTrigger = true; sc.radius = 2.0f; sc.center = new Vector3(0f, 1f, 0f);

        var stackPt = new GameObject("StackPoint");
        stackPt.transform.SetParent(player.transform);
        stackPt.transform.localPosition = new Vector3(0f, 1.0f, -0.6f);

        var maxInd = new GameObject("MaxIndicator");
        maxInd.transform.SetParent(player.transform);
        maxInd.transform.localPosition = new Vector3(0f, 3.0f, 0f);
        var maxText = maxInd.AddComponent<TMPro.TextMeshPro>();
        maxText.text = "MAX"; maxText.fontSize = 4f;
        maxText.color = Color.red;
        maxText.alignment = TMPro.TextAlignmentOptions.Center;
        maxInd.SetActive(false);

        var toolObj = new GameObject("ToolModel_Pickaxe");
        toolObj.transform.SetParent(player.transform);
        toolObj.transform.localPosition = new Vector3(0.5f, 1.2f, 0.3f);
        var toolVis = GameObject.CreatePrimitive(PrimitiveType.Cube);
        toolVis.name = "PickaxeVis";
        toolVis.transform.SetParent(toolObj.transform);
        toolVis.transform.localPosition = Vector3.zero;
        toolVis.transform.localScale = new Vector3(0.1f, 0.7f, 0.1f);
        toolVis.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);
        toolVis.GetComponent<Renderer>().sharedMaterial =
            MakeMat(new Color(0.4f, 0.4f, 0.4f));
        Object.DestroyImmediate(toolVis.GetComponent<Collider>());

        var psm = player.AddComponent<PlayerStackManager>();
        var psmSO = new SerializedObject(psm);
        psmSO.FindProperty("stackPoint").objectReferenceValue         = stackPt.transform;
        psmSO.FindProperty("maxIndicatorObject").objectReferenceValue = maxInd;
        psmSO.ApplyModifiedPropertiesWithoutUndo();

        player.AddComponent<PlayerController>();
        player.AddComponent<PlayerAnimation>();

        var ptm = player.AddComponent<PlayerToolManager>();
        var ptmSO = new SerializedObject(ptm);
        var toolModels = ptmSO.FindProperty("toolModels");
        toolModels.ClearArray();
        toolModels.InsertArrayElementAtIndex(0);
        toolModels.GetArrayElementAtIndex(0).objectReferenceValue = toolObj;
        ptmSO.ApplyModifiedPropertiesWithoutUndo();

        player.AddComponent<PlayerInteraction>();
        return player;
    }

    // ════════════════════════════════════════════════════════════
    // 외벽 (L자형 — 액터를 효율적으로 감싸는 형태)
    // ════════════════════════════════════════════════════════════
    // 폴리곤 꼭짓점 (XZ): (-12,20)→(8,20)→(8,14)→(35,14)→(35,-14)→(-12,-14)
    // 왼쪽 구역(X -12~8): 수감자/컨버터/수갑픽업 포함, Z -14~20 전체
    // 오른쪽 구역(X 8~35): 채석장/감옥 포함, Z -14~14
    static readonly float WALL_H = 3.5f;
    static readonly float WALL_T = 0.6f;

    // L-shape 꼭짓점 좌표
    static readonly float WX_LEFT  = -12f;
    static readonly float WX_STEP  =   8f;
    static readonly float WX_RIGHT =  35f;
    static readonly float WZ_SOUTH = -14f;
    static readonly float WZ_STEP  =  14f;
    static readonly float WZ_NORTH =  20f;

    [MenuItem("Game/Add Outer Walls")]
    public static void BuildOuterWalls()
    {
        var existing = GameObject.Find("OuterWalls");
        if (existing != null) Object.DestroyImmediate(existing);

        var root = new GameObject("OuterWalls");
        Color wallCol = new Color(0.55f, 0.52f, 0.48f); // 석재 회색
        Color capCol  = new Color(0.42f, 0.38f, 0.34f); // 상단 장식 띠
        Color postCol = new Color(0.38f, 0.34f, 0.30f); // 코너 기둥

        float hH  = WALL_H * 0.5f;
        float capH = 0.18f;
        float capY = WALL_H + capH * 0.5f;
        float T   = WALL_T;
        float cT  = T + 0.12f; // 캡 두께

        // ─── 6개 벽 세그먼트 ────────────────────────────────────────
        // 1. 서쪽 벽: X=-12, Z(-14~20), 전체 높이
        float westSpanZ = WZ_NORTH - WZ_SOUTH;
        float westCZ    = (WZ_NORTH + WZ_SOUTH) * 0.5f;
        MakeChildCube(root, "Wall_West",
            new Vector3(WX_LEFT, hH, westCZ),
            new Vector3(T, WALL_H, westSpanZ), wallCol);
        MakeChildCube(root, "Cap_West",
            new Vector3(WX_LEFT, capY, westCZ),
            new Vector3(cT, capH, westSpanZ + cT), capCol);

        // 2. 북쪽-왼쪽 벽: Z=20, X(-12~8)
        float northLSpanX = WX_STEP - WX_LEFT;
        float northLCX    = (WX_LEFT + WX_STEP) * 0.5f;
        MakeChildCube(root, "Wall_North_Left",
            new Vector3(northLCX, hH, WZ_NORTH),
            new Vector3(northLSpanX, WALL_H, T), wallCol);
        MakeChildCube(root, "Cap_North_Left",
            new Vector3(northLCX, capY, WZ_NORTH),
            new Vector3(northLSpanX + cT, capH, cT), capCol);

        // 3. 계단 벽 (내부 노치): X=8, Z(14~20)
        float stepSpanZ = WZ_NORTH - WZ_STEP;
        float stepCZ    = (WZ_NORTH + WZ_STEP) * 0.5f;
        MakeChildCube(root, "Wall_Step",
            new Vector3(WX_STEP, hH, stepCZ),
            new Vector3(T, WALL_H, stepSpanZ), wallCol);
        MakeChildCube(root, "Cap_Step",
            new Vector3(WX_STEP, capY, stepCZ),
            new Vector3(cT, capH, stepSpanZ + cT), capCol);

        // 4. 북쪽-오른쪽 벽: Z=14, X(8~35)
        float northRSpanX = WX_RIGHT - WX_STEP;
        float northRCX    = (WX_RIGHT + WX_STEP) * 0.5f;
        MakeChildCube(root, "Wall_North_Right",
            new Vector3(northRCX, hH, WZ_STEP),
            new Vector3(northRSpanX, WALL_H, T), wallCol);
        MakeChildCube(root, "Cap_North_Right",
            new Vector3(northRCX, capY, WZ_STEP),
            new Vector3(northRSpanX + cT, capH, cT), capCol);

        // 5. 동쪽 벽: X=35, Z(-14~14)
        float eastSpanZ = WZ_STEP - WZ_SOUTH;
        float eastCZ    = (WZ_STEP + WZ_SOUTH) * 0.5f;
        MakeChildCube(root, "Wall_East",
            new Vector3(WX_RIGHT, hH, eastCZ),
            new Vector3(T, WALL_H, eastSpanZ), wallCol);
        MakeChildCube(root, "Cap_East",
            new Vector3(WX_RIGHT, capY, eastCZ),
            new Vector3(cT, capH, eastSpanZ + cT), capCol);

        // 6. 남쪽 벽: Z=-14, X(-12~35)
        float southSpanX = WX_RIGHT - WX_LEFT;
        float southCX    = (WX_RIGHT + WX_LEFT) * 0.5f;
        MakeChildCube(root, "Wall_South",
            new Vector3(southCX, hH, WZ_SOUTH),
            new Vector3(southSpanX, WALL_H, T), wallCol);
        MakeChildCube(root, "Cap_South",
            new Vector3(southCX, capY, WZ_SOUTH),
            new Vector3(southSpanX + cT, capH, cT), capCol);

        // ─── 6개 꼭짓점 코너 기둥 ─────────────────────────────────
        float postSize = T + 0.2f;
        float postH    = WALL_H + capH + 0.05f;
        float postHH   = postH * 0.5f;
        Vector3 postScale = new Vector3(postSize, postH, postSize);
        foreach (var (name, x, z) in new (string, float, float)[]
        {
            ("Post_NW",  WX_LEFT,  WZ_NORTH),
            ("Post_NS",  WX_STEP,  WZ_NORTH),
            ("Post_SW",  WX_STEP,  WZ_STEP ),
            ("Post_NE",  WX_RIGHT, WZ_STEP ),
            ("Post_SE",  WX_RIGHT, WZ_SOUTH),
            ("Post_WSW", WX_LEFT,  WZ_SOUTH),
        })
        {
            MakeChildCube(root, name,
                new Vector3(x, postHH, z), postScale, postCol);
        }

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[SceneBuilder] L자형 외벽 생성 완료");
    }

    // ════════════════════════════════════════════════════════════
    // 헬퍼
    // ════════════════════════════════════════════════════════════
    static Material MakeMat(Color color)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        return mat;
    }

    static GameObject MakeCube(string name, Vector3 pos, Vector3 scale, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = MakeMat(color);
        return go;
    }

    static GameObject MakeChildCube(GameObject parent, string name,
        Vector3 localPos, Vector3 scale, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        go.transform.localRotation = Quaternion.identity;
        go.GetComponent<Renderer>().sharedMaterial = MakeMat(color);
        return go;
    }

    static void MakeLabel(Transform parent, string text, Vector3 localPos,
        Color color, float fontSize = 3f)
    {
        var obj = new GameObject("Label");
        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPos;
        obj.transform.localRotation = Quaternion.identity;
        var tmp = obj.AddComponent<TMPro.TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        // 카메라 시점에서 정면으로 보이게 하는 빌보드 컴포넌트
        obj.AddComponent<CameraBillboard>();
    }

    // ════════════════════════════════════════════════════════════
    // 머니 HUD (Screen Space Overlay Canvas, 우측 상단)
    // ════════════════════════════════════════════════════════════
    static void BuildMoneyHUD()
    {
        // Canvas
        var canvasObj = new GameObject("MoneyHUD_Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = UnityEngine.RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // 배경 패널 (반투명 검정)
        var panelObj = new GameObject("Panel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        var panelImg = panelObj.AddComponent<UnityEngine.UI.Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.55f);
        var panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot     = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-16f, -16f);
        panelRect.sizeDelta = new Vector2(180f, 60f);

        // 텍스트
        var textObj = new GameObject("CashText");
        textObj.transform.SetParent(panelObj.transform, false);
        var tmp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = "$0";
        tmp.fontSize = 32f;
        tmp.fontStyle = TMPro.FontStyles.Bold;
        tmp.color = new Color(1f, 0.9f, 0.1f); // 금색
        tmp.alignment = TMPro.TextAlignmentOptions.MidlineRight;
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 0f);
        textRect.offsetMax = new Vector2(-8f, 0f);

        // MoneyHUD 컴포넌트
        var hud = canvasObj.AddComponent<MoneyHUD>();
        var hudSO = new UnityEditor.SerializedObject(hud);
        hudSO.FindProperty("cashText").objectReferenceValue = tmp;
        hudSO.ApplyModifiedPropertiesWithoutUndo();
    }

    // 씬 뷰에서 드래그로 위치를 조정할 수 있는 컬러 마커 구체
    static GameObject MakeMarker(string name, GameObject parent, Vector3 worldPos,
                                 Color? color = null)
    {
        var obj = new GameObject(name);
        if (parent != null) obj.transform.SetParent(parent.transform);
        obj.transform.position = worldPos;

        // 작은 구체 비주얼 (에디터 전용 - 런타임에는 비활성)
        var vis = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        vis.name = "MarkerVis";
        vis.transform.SetParent(obj.transform);
        vis.transform.localPosition = Vector3.zero;
        vis.transform.localScale = Vector3.one * 0.4f;
        vis.GetComponent<Renderer>().sharedMaterial =
            MakeMat(color ?? new Color(1f, 0.8f, 0f));
        Object.DestroyImmediate(vis.GetComponent<Collider>());

        // 런타임에는 렌더러만 끔 (Transform은 유지해야 경로로 사용됨)
        vis.tag = "EditorOnly";

        return obj;
    }

    // ════════════════════════════════════════════════════════════
    // 프리팹 / 에셋 헬퍼
    // ════════════════════════════════════════════════════════════
    static GameObject GetOrCreateRockPrefab()
    {
        string path = "Assets/Prefabs/RockPrefab.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Rock";
        go.transform.localScale = new Vector3(0.85f, 0.55f, 0.85f);
        go.GetComponent<Renderer>().sharedMaterial = MakeMat(COL_ROCK);
        go.AddComponent<RockNode>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static GameSettings LoadOrCreateGameSettings()
    {
        var gs = AssetDatabase.LoadAssetAtPath<GameSettings>(
            "Assets/Settings/GameSettings.asset");
        if (gs != null) return gs;

        gs = ScriptableObject.CreateInstance<GameSettings>();
        if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            AssetDatabase.CreateFolder("Assets", "Settings");
        AssetDatabase.CreateAsset(gs, "Assets/Settings/GameSettings.asset");
        AssetDatabase.SaveAssets();
        return gs;
    }
}
