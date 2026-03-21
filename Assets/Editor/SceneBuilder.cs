using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// place.png 배치도 기반 씬 자동 구성.
/// 메뉴: Game → Rebuild Scene
///
/// 레이아웃 요약 (place.png 기준):
///   우측 - 돌 7x20 채굴 구역 (펜스 둘러쌈)
///   상단 - 돌→수갑 변환기, 수갑 받아가는 곳
///   좌측 - 수감자 스포너, 이동경로(도로), 경찰 책상
///   중앙 - 채굴/인력 업그레이드, 자동판매 업그레이드
///   하단 - 감옥 20명(업그레이드 전) + 100명(업그레이드 후)
/// </summary>
public static class SceneBuilder
{
    // ── 게임 오브젝트 위치 (place.png 기준) ──────────────────────
    // 우측 채굴 구역
    static readonly Vector3 GRID_CENTER      = new Vector3(12f, 0f, 11f);   // 7×20 그리드 중심
    static readonly Vector3 ORE_DROP         = new Vector3(4f,  0f, 13f);   // 돌 두는 곳
    static readonly Vector3 CONVERTER        = new Vector3(4f,  0f, 17f);   // 돌→수갑 변환기
    static readonly Vector3 HANDCUFF_PICKUP  = new Vector3(-1f, 0f, 17f);   // 수갑 받아가는 곳

    // 좌측 수감자 구역
    static readonly Vector3 PRISONER_SPAWN   = new Vector3(-9f, 0f, 17f);   // 수감자 스포너
    static readonly Vector3 PRISONER_WAIT    = new Vector3(-6f, 0f, 13f);   // Desk 사이 대기
    static readonly Vector3 DESK_POS         = new Vector3(-4f, 0f, 13f);   // 경찰 책상
    static readonly Vector3 MONEY_SPAWNER_POS= new Vector3(-2f, 0f, 13f);   // 돈 스포너
    static readonly Vector3 MONEY_PICKUP_POS = new Vector3(-2f, 0f, 11f);   // 돈 받아가는 곳
    static readonly float   PRISONER_ROAD_X  = -9f;                         // 수감자 이동경로 X

    // 업그레이드 존
    static readonly Vector3 UPG_DRILL        = new Vector3(4f,  0f, 9f);    // 채광 업그레이드(드릴)
    static readonly Vector3 UPG_DRILLCAR     = new Vector3(6f,  0f, 9f);    // 채광 업그레이드(드릴 차)
    static readonly Vector3 UPG_WORKER       = new Vector3(4f,  0f, 6f);    // 인력 업그레이드
    static readonly Vector3 UPG_AUTOSELL     = new Vector3(-4f, 0f, 10f);   // 자동 판매 업그레이드
    static readonly Vector3 UPG_PRISON       = new Vector3(0f,  0f, 2f);    // 감옥 확장 업그레이드

    // 감옥
    static readonly Vector3 PRISON_20_CENTER = new Vector3(-3f, 0f, -3f);   // 감옥 20명 (회색)
    static readonly Vector3 PRISON_100_CENTER= new Vector3(7f,  0f, -3f);   // 감옥 100명 (녹색)

    // 플레이어
    static readonly Vector3 PLAYER_START     = new Vector3(-1f, 1f, 7f);

    // ── 색상 팔레트 ───────────────────────────────────────────────
    static Color COL_ROCK      = new Color(0.62f, 0.58f, 0.54f);
    static Color COL_FENCE     = new Color(0.35f, 0.35f, 0.35f);
    static Color COL_CONVERTER = new Color(0.85f, 0.45f, 0.10f);
    static Color COL_DESK      = new Color(0.85f, 0.55f, 0.25f);
    static Color COL_ZONE_ORE  = new Color(1.00f, 0.85f, 0.20f, 0.6f);
    static Color COL_ZONE_HC   = new Color(0.20f, 0.70f, 1.00f, 0.6f);
    static Color COL_ZONE_MONEY= new Color(0.20f, 0.90f, 0.30f, 0.6f);
    static Color COL_ZONE_DESK = new Color(1.00f, 0.60f, 0.10f, 0.6f);
    static Color COL_ROAD      = new Color(0.72f, 0.72f, 0.72f);
    static Color COL_PRISON_20 = new Color(0.72f, 0.72f, 0.72f);
    static Color COL_PRISON_100= new Color(0.60f, 0.80f, 0.50f);
    static Color COL_UPG_DRILL = new Color(0.30f, 0.70f, 1.00f);
    static Color COL_UPG_DRCAR = new Color(0.80f, 0.30f, 1.00f);
    static Color COL_UPG_WORK  = new Color(1.00f, 0.55f, 0.10f);
    static Color COL_UPG_AUTO  = new Color(0.20f, 0.85f, 0.40f);
    static Color COL_UPG_PRIS  = new Color(0.90f, 0.30f, 0.30f);
    static Color COL_PLAYER    = new Color(0.20f, 0.50f, 1.00f);
    static Color COL_PRISONER  = new Color(1.00f, 0.55f, 0.10f);

    [MenuItem("Game/Rebuild Scene")]
    public static void RebuildScene()
    {
        if (!EditorUtility.DisplayDialog("씬 재구성",
            "기존 씬 오브젝트를 모두 삭제하고 place.png 배치도 기준으로 재배치합니다.\n계속하시겠습니까?",
            "재구성", "취소")) return;

        ClearScene();
        var gs = LoadOrCreateGameSettings();
        SetupCamera();
        SetupLighting();

        // 환경
        BuildFloorAndRoad();

        // 채굴 구역 (우측 대형 구역)
        var grid = BuildMiningGrid(gs);

        // 변환 시스템
        var converter = BuildConverter();
        BuildDropZones(converter, null); // moneySpawner는 아래서 연결

        // 수감자 / 감옥 시스템 (순서 중요)
        var cellManager = BuildPrison();
        var deskResult  = BuildDesk();
        BuildPrisonerSpawner(deskResult.desk, cellManager, deskResult.moneySpawner);

        // 돈 픽업 존 연결
        BuildMoneyPickupZone(deskResult.moneySpawner);

        // 업그레이드 존
        BuildUpgradeZones(cellManager, grid);

        // 매니저 싱글톤
        BuildManagers(gs);

        // 플레이어
        BuildPlayer();

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
            if (n == "Canvas" || n == "EventSystem") continue;
            Object.DestroyImmediate(go);
        }
    }

    // ════════════════════════════════════════════════════════════
    // 카메라 & 조명
    // ════════════════════════════════════════════════════════════
    static void SetupCamera()
    {
        var cam = new GameObject("Main Camera"); cam.tag = "MainCamera";
        cam.AddComponent<AudioListener>();
        var c = cam.AddComponent<Camera>();
        c.fieldOfView = 55f;
        c.farClipPlane = 200f;
        cam.transform.position = new Vector3(-1f, 22f, -2f);
        cam.transform.rotation = Quaternion.Euler(58f, 0f, 0f);
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
        // 전체 바닥
        var floor = MakeCube("Floor",
            new Vector3(-1f, -0.55f, 8f),
            new Vector3(28f, 0.1f, 28f),
            new Color(0.88f, 0.86f, 0.82f));
        floor.GetComponent<Collider>().enabled = false;

        // 수감자 이동 도로 (좌측 세로)
        var road = MakeCube("PrisonerRoad",
            new Vector3(PRISONER_ROAD_X, -0.50f, 9f),
            new Vector3(2.5f, 0.12f, 18f),
            COL_ROAD);
        road.GetComponent<Collider>().enabled = false;

        // 도로 점선 (중앙선)
        for (int i = 0; i < 8; i++)
        {
            var line = MakeCube($"RoadLine_{i}",
                new Vector3(PRISONER_ROAD_X, -0.48f, 2f + i * 2.2f),
                new Vector3(0.12f, 0.12f, 1.0f),
                Color.white);
            line.GetComponent<Collider>().enabled = false;
        }
    }

    // ════════════════════════════════════════════════════════════
    // 채굴 그리드 (우측 대형 구역 + 펜스)
    // ════════════════════════════════════════════════════════════
    static MiningGrid BuildMiningGrid(GameSettings gs)
    {
        // GameSettings의 gridSpacingX/Z 기반으로 크기 계산
        int cols = 7; int rows = 20;
        float sx = 1.2f; float sz = 1.2f;
        float totalW = (cols - 1) * sx + sx + 1f;   // ≈ 9.4
        float totalD = (rows - 1) * sz + sz + 1f;   // ≈ 25.8

        // 그리드 바닥 (진회색)
        var gridFloor = MakeCube("MiningGridFloor",
            new Vector3(GRID_CENTER.x, -0.52f, GRID_CENTER.z),
            new Vector3(totalW, 0.12f, totalD),
            new Color(0.50f, 0.48f, 0.44f));
        gridFloor.GetComponent<Collider>().enabled = false;

        // ── 펜스 (4면) ──────────────────────────────────────────
        float hw = totalW * 0.5f;
        float hd = totalD * 0.5f;

        // 앞/뒤 가로 펜스
        for (int side = 0; side < 2; side++)
        {
            float z = GRID_CENTER.z + (side == 0 ? -hd : hd);
            for (int c = -3; c <= 3; c++)
            {
                var post = MakeCube($"FencePost_FB_{side}_{c}",
                    new Vector3(GRID_CENTER.x + c * (totalW / 6f), 0.6f, z),
                    new Vector3(0.15f, 1.4f, 0.15f), COL_FENCE);
            }
            var rail = MakeCube($"FenceRail_FB_{side}",
                new Vector3(GRID_CENTER.x, 0.9f, z),
                new Vector3(totalW, 0.12f, 0.12f), COL_FENCE);
            var rail2 = MakeCube($"FenceRail2_FB_{side}",
                new Vector3(GRID_CENTER.x, 0.4f, z),
                new Vector3(totalW, 0.12f, 0.12f), COL_FENCE);
        }

        // 좌/우 세로 펜스
        for (int side = 0; side < 2; side++)
        {
            float x = GRID_CENTER.x + (side == 0 ? -hw : hw);
            for (int r = -5; r <= 5; r++)
            {
                var post = MakeCube($"FencePost_LR_{side}_{r}",
                    new Vector3(x, 0.6f, GRID_CENTER.z + r * (totalD / 10f)),
                    new Vector3(0.15f, 1.4f, 0.15f), COL_FENCE);
            }
            var rail = MakeCube($"FenceRail_LR_{side}",
                new Vector3(x, 0.9f, GRID_CENTER.z),
                new Vector3(0.12f, 0.12f, totalD), COL_FENCE);
            var rail2 = MakeCube($"FenceRail2_LR_{side}",
                new Vector3(x, 0.4f, GRID_CENTER.z),
                new Vector3(0.12f, 0.12f, totalD), COL_FENCE);
        }

        // ── MiningGrid 컴포넌트 ──────────────────────────────────
        var gridObj = new GameObject("MiningGrid");
        gridObj.transform.position = GRID_CENTER;

        var rockPrefab = GetOrCreateRockPrefab();
        var orePrefab  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/OrePrefab.prefab");

        var mg = gridObj.AddComponent<MiningGrid>();
        var so = new SerializedObject(mg);
        so.FindProperty("rockPrefab").objectReferenceValue = rockPrefab;
        so.FindProperty("orePrefab").objectReferenceValue  = orePrefab;
        so.ApplyModifiedPropertiesWithoutUndo();

        // 레이블
        MakeLabel(gridObj.transform, "채굴 구역", new Vector3(0f, 2.5f, -hd - 1f),
            Color.white, 4f);

        return mg;
    }

    // ════════════════════════════════════════════════════════════
    // 변환기 (돌 → 수갑)
    // ════════════════════════════════════════════════════════════
    static ConverterMachine BuildConverter()
    {
        var root = new GameObject("ConverterMachine");
        root.transform.position = CONVERTER;

        // 본체
        MakeChildCube(root, "Body",    Vector3.zero,
            new Vector3(2.5f, 1.2f, 2f), COL_CONVERTER);

        // 컨베이어 벨트 (작은 롤러들)
        for (int i = 0; i < 5; i++)
        {
            var roller = MakeChildCube(root, $"Roller_{i}",
                new Vector3(-1.0f + i * 0.5f, 0.65f, 0.3f),
                new Vector3(0.12f, 0.12f, 1.2f),
                new Color(0.25f, 0.25f, 0.25f));
        }
        MakeChildCube(root, "Belt",
            new Vector3(0f, 0.62f, 0.3f),
            new Vector3(2.4f, 0.08f, 1.0f),
            new Color(0.15f, 0.15f, 0.15f));

        // 드릴(삼각형 대신 앞뒤로 좁아지는 모양)
        MakeChildCube(root, "DrillHead",
            new Vector3(1.0f, 0.6f, 0f),
            new Vector3(0.8f, 0.5f, 0.5f),
            new Color(0.50f, 0.50f, 0.55f));

        // 출력 포인트
        var outputPt = new GameObject("OutputPoint");
        outputPt.transform.SetParent(root.transform);
        outputPt.transform.localPosition = new Vector3(-1.0f, 0.8f, 0f);

        var hcPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/HandcuffPrefab.prefab");

        MakeLabel(root.transform, "돌→수갑 변환기",
            new Vector3(0f, 1.8f, 0f), COL_CONVERTER, 3f);

        var cm = root.AddComponent<ConverterMachine>();
        var so = new SerializedObject(cm);
        so.FindProperty("outputPoint").objectReferenceValue   = outputPt.transform;
        so.FindProperty("handcuffPrefab").objectReferenceValue = hcPrefab;
        so.ApplyModifiedPropertiesWithoutUndo();

        return cm;
    }

    // ════════════════════════════════════════════════════════════
    // 드롭 존 (광석 투입 + 수갑 픽업)
    // ════════════════════════════════════════════════════════════
    static void BuildDropZones(ConverterMachine converter, MoneySpawner moneySpawner)
    {
        // 돌 두는 곳 (OreToConverter)
        BuildDropZone("OreDropZone", ORE_DROP,
            new Vector3(3.5f, 2f, 3f),
            DropZoneType.OreToConverter,
            converter, null,
            COL_ZONE_ORE, "돌 두는 곳");

        // 수갑 받아가는 곳 (HandcuffPickup) - 대시 박스 느낌
        BuildDropZone("HandcuffPickupZone", HANDCUFF_PICKUP,
            new Vector3(4f, 2f, 2.5f),
            DropZoneType.HandcuffPickup,
            converter, null,
            COL_ZONE_HC, "수갑 받아가는 곳");
    }

    // 돈 픽업 존 별도 (MoneySpawner 참조 필요)
    static void BuildMoneyPickupZone(MoneySpawner moneySpawner)
    {
        BuildDropZone("MoneyPickupZone", MONEY_PICKUP_POS,
            new Vector3(2.5f, 2f, 2f),
            DropZoneType.MoneyPickup,
            null, moneySpawner,
            COL_ZONE_MONEY, "돈 받아가는 곳");
    }

    static void BuildDropZone(string name, Vector3 pos, Vector3 size,
        DropZoneType type, ConverterMachine conv, MoneySpawner ms,
        Color color, string label)
    {
        var obj = new GameObject(name);
        obj.transform.position = pos;

        var bc = obj.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = size;

        // 바닥 타일 (반투명)
        var tile = MakeChildCube(obj, "Tile",
            new Vector3(0f, -0.5f, 0f),
            new Vector3(size.x, 0.12f, size.z),
            color);
        tile.GetComponent<Collider>().enabled = false;

        // 테두리 점선 (4코너)
        float hw = size.x * 0.5f - 0.1f;
        float hd = size.z * 0.5f - 0.1f;
        Color lineColor = new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f);

        foreach (var corner in new[] {
            new Vector2(-hw, -hd), new Vector2(hw, -hd),
            new Vector2(-hw,  hd), new Vector2(hw,  hd) })
        {
            MakeChildCube(obj, "Corner",
                new Vector3(corner.x, -0.48f, corner.y),
                new Vector3(0.25f, 0.12f, 0.25f), lineColor).GetComponent<Collider>().enabled = false;
        }

        MakeLabel(obj.transform, label, new Vector3(0f, 0.6f, 0f), color * 1.2f, 2.5f);

        var dz = obj.AddComponent<DropZone>();
        var so = new SerializedObject(dz);
        so.FindProperty("zoneType").enumValueIndex = (int)type;
        if (conv != null)
            so.FindProperty("converterMachine").objectReferenceValue = conv;
        if (ms != null)
            so.FindProperty("moneySpawner").objectReferenceValue = ms;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ════════════════════════════════════════════════════════════
    // 경찰 책상 + DeskManager + MoneySpawner
    // ════════════════════════════════════════════════════════════
    struct DeskResult { public DeskManager desk; public MoneySpawner moneySpawner; }
    static DeskResult BuildDesk()
    {
        // ── 책상 비주얼 ─────────────────────────────────────────
        var deskRoot = new GameObject("PoliceDeskArea");
        deskRoot.transform.position = DESK_POS;

        // 책상 상판
        MakeChildCube(deskRoot, "Top",
            new Vector3(0f, 0.4f, 0f),
            new Vector3(2.5f, 0.12f, 1.5f),
            COL_DESK);

        // 책상 다리 4개
        foreach (var leg in new[] {
            new Vector3(-1.0f, 0f, -0.6f), new Vector3(1.0f, 0f, -0.6f),
            new Vector3(-1.0f, 0f,  0.6f), new Vector3(1.0f, 0f,  0.6f) })
        {
            MakeChildCube(deskRoot, "Leg",
                leg + new Vector3(0f, -0.2f, 0f),
                new Vector3(0.15f, 0.8f, 0.15f),
                new Color(0.6f, 0.4f, 0.2f));
        }

        // DeskZone 트리거
        var dzoneObj = new GameObject("DeskZone");
        dzoneObj.transform.position = DESK_POS;
        var bc = dzoneObj.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = new Vector3(3.5f, 2f, 2f);

        MakeLabel(deskRoot.transform, "경찰 책상",
            new Vector3(0f, 1.2f, 0f), COL_DESK, 2.8f);

        // ── 수갑 스택 포인트 ─────────────────────────────────────
        var stackPt = new GameObject("DeskStackPoint");
        stackPt.transform.SetParent(deskRoot.transform);
        stackPt.transform.localPosition = new Vector3(0f, 0.5f, 0f);

        // ── 돈 스포너 (Desk 우측) ────────────────────────────────
        var msObj = new GameObject("MoneySpawner");
        msObj.transform.position = MONEY_SPAWNER_POS;

        // 돈 스포너 비주얼 (작은 박스)
        MakeChildCube(msObj, "Body",
            Vector3.zero,
            new Vector3(1f, 0.6f, 1f),
            new Color(0.8f, 0.8f, 0.1f));
        MakeLabel(msObj.transform, "돈", new Vector3(0f, 0.8f, 0f),
            Color.yellow, 2.5f);

        var msPt = new GameObject("StackPoint");
        msPt.transform.SetParent(msObj.transform);
        msPt.transform.localPosition = new Vector3(0f, 0.4f, 0f);

        var cashPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/CashPrefab.prefab");

        var ms = msObj.AddComponent<MoneySpawner>();
        var msSO = new SerializedObject(ms);
        msSO.FindProperty("moneyPrefab").objectReferenceValue = cashPrefab;
        msSO.FindProperty("stackPoint").objectReferenceValue  = msPt.transform;
        msSO.ApplyModifiedPropertiesWithoutUndo();

        // ── DeskManager ──────────────────────────────────────────
        var dmObj = new GameObject("DeskManager");
        dmObj.transform.position = DESK_POS;

        var dm = dmObj.AddComponent<DeskManager>();
        var dmSO = new SerializedObject(dm);
        dmSO.FindProperty("deskStackPoint").objectReferenceValue = stackPt.transform;
        dmSO.FindProperty("moneySpawner").objectReferenceValue   = ms;
        // prisonerSpawner → BuildPrisonerSpawner에서 연결
        dmSO.ApplyModifiedPropertiesWithoutUndo();

        // ── DeskZone 연결 ─────────────────────────────────────────
        var dz = dzoneObj.AddComponent<DeskZone>();
        var dzSO = new SerializedObject(dz);
        dzSO.FindProperty("deskManager").objectReferenceValue = dm;
        dzSO.ApplyModifiedPropertiesWithoutUndo();

        return new DeskResult { desk = dm, moneySpawner = ms };
    }

    // ════════════════════════════════════════════════════════════
    // 감옥 (20명 + 100명 확장 구역)
    // ════════════════════════════════════════════════════════════
    static CellManager BuildPrison()
    {
        var prisonRoot = new GameObject("Prison");
        prisonRoot.transform.position = Vector3.zero;

        // ── 감옥 20명 (업그레이드 전) ─────────────────────────────
        var p20 = BuildPrisonBlock(prisonRoot, "Prison_20",
            PRISON_20_CENTER, 8f, 10f, COL_PRISON_20, "감옥\n20명\n(업그레이드 전)");

        // ── 감옥 100명 (업그레이드 후, 비활성) ────────────────────
        var p100 = BuildPrisonBlock(prisonRoot, "Prison_100",
            PRISON_100_CENTER, 16f, 10f, COL_PRISON_100, "감옥\n100명\n(업그레이드 후)");
        p100.SetActive(false); // PrisonExpand 업그레이드 시 활성화

        // 철창 (앞면)
        BuildBars(prisonRoot, PRISON_20_CENTER + new Vector3(0f, 0f, 5f), 8f);
        BuildBars(prisonRoot, PRISON_100_CENTER + new Vector3(0f, 0f, 5f), 16f);

        // 카운터 텍스트
        var counterObj = new GameObject("CellCounter");
        counterObj.transform.position = PRISON_20_CENTER + new Vector3(0f, 3.5f, 0f);
        var counter = counterObj.AddComponent<TMPro.TextMeshPro>();
        counter.text = "0/20";
        counter.fontSize = 6f;
        counter.alignment = TMPro.TextAlignmentOptions.Center;
        counter.color = Color.white;

        // 셀 루트
        var cellRoot = new GameObject("CellRoot");
        cellRoot.transform.SetParent(prisonRoot.transform);
        cellRoot.transform.position = PRISON_20_CENTER + new Vector3(0f, 0f, -2f);

        // CellManager
        var cm = prisonRoot.AddComponent<CellManager>();
        var so = new SerializedObject(cm);
        so.FindProperty("cellRoot").objectReferenceValue        = cellRoot.transform;
        so.FindProperty("cellCounterText").objectReferenceValue = counter;
        so.ApplyModifiedPropertiesWithoutUndo();

        // 업그레이드 완료 시 활성화할 오브젝트 목록은 UpgradeZone에서 처리

        return cm;
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
            new Vector3(width, 0.15f, depth),
            color);
        floor.GetComponent<Collider>().enabled = false;

        // 벽 3면 (뒤, 좌, 우)
        Color wallColor = new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f);
        MakeChildCube(obj, "WallBack",
            new Vector3(0f, 1f, -depth * 0.5f),
            new Vector3(width, 2.2f, 0.2f), wallColor).GetComponent<Collider>().enabled = false;
        MakeChildCube(obj, "WallLeft",
            new Vector3(-width * 0.5f, 1f, 0f),
            new Vector3(0.2f, 2.2f, depth), wallColor).GetComponent<Collider>().enabled = false;
        MakeChildCube(obj, "WallRight",
            new Vector3(width * 0.5f, 1f, 0f),
            new Vector3(0.2f, 2.2f, depth), wallColor).GetComponent<Collider>().enabled = false;

        MakeLabel(obj.transform, label, new Vector3(0f, 2f, 0f), Color.white, 3.5f);

        return obj;
    }

    static void BuildBars(GameObject parent, Vector3 center, float width)
    {
        int count = Mathf.CeilToInt(width / 0.7f);
        for (int i = 0; i < count; i++)
        {
            float x = center.x - width * 0.5f + i * (width / count) + (width / count) * 0.5f;
            MakeChildCube(parent, $"Bar_{x:F0}",
                new Vector3(x, center.y + 1.0f, center.z),
                new Vector3(0.12f, 2.2f, 0.12f),
                new Color(0.3f, 0.3f, 0.3f));
        }
        // 가로 봉
        MakeChildCube(parent, "HBar_Top",
            new Vector3(center.x, center.y + 2.0f, center.z),
            new Vector3(width, 0.12f, 0.12f),
            new Color(0.3f, 0.3f, 0.3f));
    }

    // ════════════════════════════════════════════════════════════
    // 수감자 스포너
    // ════════════════════════════════════════════════════════════
    static void BuildPrisonerSpawner(DeskManager desk, CellManager cellManager,
                                      MoneySpawner moneySpawner)
    {
        // 스포너 비주얼 (주황 박스)
        var spawnerRoot = new GameObject("PrisonerSpawner");
        spawnerRoot.transform.position = PRISONER_SPAWN;

        MakeChildCube(spawnerRoot, "Body",
            Vector3.zero,
            new Vector3(2f, 1f, 2f),
            new Color(0.95f, 0.60f, 0.10f));
        MakeLabel(spawnerRoot.transform, "수감자\n스포너",
            new Vector3(0f, 1.5f, 0f), COL_PRISONER, 3f);

        // 포인트 오브젝트들
        var spawnPt = MakeMarker("SpawnPoint",  spawnerRoot, Vector3.zero);
        var waitPt  = MakeMarker("WaitPosition", null, PRISONER_WAIT);
        var deskPt  = MakeMarker("DeskPosition", null,
                          DESK_POS + new Vector3(0f, 0f, -1.5f));

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

        // DeskManager에 prisonerSpawner 역참조
        var dmSO = new SerializedObject(desk);
        dmSO.FindProperty("prisonerSpawner").objectReferenceValue = ps;
        dmSO.ApplyModifiedPropertiesWithoutUndo();
    }

    // ════════════════════════════════════════════════════════════
    // 업그레이드 존 (5종, place.png 위치 기준)
    // ════════════════════════════════════════════════════════════
    static void BuildUpgradeZones(CellManager cellManager, MiningGrid miningGrid)
    {
        var root = new GameObject("_UpgradeZones");

        // Prison_100 오브젝트 참조 (PrisonExpand 완료 시 활성화)
        var prison100 = GameObject.Find("Prison_100");

        // 채광 업그레이드 - 드릴 ($20)
        BuildUpgradeZone(root.transform, "UpgDrill", UPG_DRILL,
            UpgradeType.Drill, "$20\n드릴", COL_UPG_DRILL,
            cellManager, miningGrid, null);

        // 채광 업그레이드 - 드릴 차 ($50) - 드릴 업그레이드 후 생성 (기본 비활성)
        var upg_drillcar = BuildUpgradeZone(root.transform, "UpgDrillCar", UPG_DRILLCAR,
            UpgradeType.DrillCar, "$50\n드릴 차", COL_UPG_DRCAR,
            cellManager, miningGrid, null);
        upg_drillcar.SetActive(false);

        // 인력 업그레이드 ($40) - 드릴 업그레이드 후 생성 (기본 비활성)
        var upg_worker = BuildUpgradeZone(root.transform, "UpgWorker", UPG_WORKER,
            UpgradeType.WorkerHire, "$40\n인력 고용", COL_UPG_WORK,
            cellManager, miningGrid, null);
        upg_worker.SetActive(false);

        // 자동 판매 업그레이드 ($50) - 드릴 업그레이드 후 생성 (기본 비활성)
        var upg_autosell = BuildUpgradeZone(root.transform, "UpgAutoSell", UPG_AUTOSELL,
            UpgradeType.AutoSell, "$50\n자동 판매", COL_UPG_AUTO,
            cellManager, miningGrid, null);
        upg_autosell.SetActive(false);

        // 감옥 확장 업그레이드 ($50) - 드릴 차 업그레이드 후 생성 (기본 비활성)
        var upg_prison = BuildUpgradeZone(root.transform, "UpgPrison", UPG_PRISON,
            UpgradeType.PrisonExpand, "$50\n감옥 확장", COL_UPG_PRIS,
            cellManager, miningGrid, prison100);
        upg_prison.SetActive(false);

        // 드릴 업그레이드 완료 시 활성화할 오브젝트 연결
        var drillZone = root.transform.Find("UpgDrill");
        if (drillZone != null)
        {
            var uz = drillZone.GetComponent<UpgradeZone>();
            var uzSO = new SerializedObject(uz);
            var arr = uzSO.FindProperty("objectsToActivateOnComplete");
            arr.ClearArray();
            arr.InsertArrayElementAtIndex(0);
            arr.GetArrayElementAtIndex(0).objectReferenceValue = upg_drillcar;
            arr.InsertArrayElementAtIndex(1);
            arr.GetArrayElementAtIndex(1).objectReferenceValue = upg_worker;
            arr.InsertArrayElementAtIndex(2);
            arr.GetArrayElementAtIndex(2).objectReferenceValue = upg_autosell;
            uzSO.ApplyModifiedPropertiesWithoutUndo();
        }

        // 드릴 차 업그레이드 완료 시 활성화
        var drillcarZone = root.transform.Find("UpgDrillCar");
        if (drillcarZone != null)
        {
            var uz = drillcarZone.GetComponent<UpgradeZone>();
            var uzSO = new SerializedObject(uz);
            var arr = uzSO.FindProperty("objectsToActivateOnComplete");
            arr.ClearArray();
            arr.InsertArrayElementAtIndex(0);
            arr.GetArrayElementAtIndex(0).objectReferenceValue = upg_prison;
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

        // 트리거
        var bc = obj.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = new Vector3(2.2f, 2f, 2.2f);

        // 플랫폼
        MakeChildCube(obj, "Platform",
            new Vector3(0f, -0.5f, 0f),
            new Vector3(2.2f, 0.12f, 2.2f),
            color).GetComponent<Collider>().enabled = false;

        // 화살표 (아래→위 방향)
        MakeChildCube(obj, "ArrowShaft",
            new Vector3(0f, -0.46f, 0f),
            new Vector3(0.35f, 0.12f, 1.0f),
            Color.white).GetComponent<Collider>().enabled = false;
        MakeChildCube(obj, "ArrowHead",
            new Vector3(0f, -0.44f, 0.55f),
            new Vector3(0.8f, 0.12f, 0.4f),
            Color.white).GetComponent<Collider>().enabled = false;

        // 채움 바
        var fillBar = MakeChildCube(obj, "FillBar",
            new Vector3(1.3f, 0f, 0f),
            new Vector3(0.3f, 0.001f, 0.3f),
            Color.green);
        fillBar.GetComponent<Collider>().enabled = false;

        // 비용 텍스트
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
        so.FindProperty("upgradeType").enumValueIndex = (int)upgradeType;
        so.FindProperty("costText").objectReferenceValue  = tmp;
        so.FindProperty("fillBar").objectReferenceValue   = fillBar.transform;
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
    static void BuildPlayer()
    {
        var player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = PLAYER_START;

        // 몸통 (캡슐)
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(player.transform);
        body.transform.localPosition = new Vector3(0f, 1f, 0f);
        body.GetComponent<Renderer>().sharedMaterial = MakeMat(COL_PLAYER);
        Object.DestroyImmediate(body.GetComponent<Collider>());

        // 머리
        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(player.transform);
        head.transform.localPosition = new Vector3(0f, 2.2f, 0f);
        head.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
        head.GetComponent<Renderer>().sharedMaterial = MakeMat(new Color(1.0f, 0.85f, 0.65f));
        Object.DestroyImmediate(head.GetComponent<Collider>());

        // 물리 컴포넌트
        var rb = player.AddComponent<Rigidbody>();
        rb.isKinematic = true; rb.useGravity = false;

        var cc = player.AddComponent<CharacterController>();
        cc.height = 2f; cc.radius = 0.5f; cc.center = new Vector3(0f, 1f, 0f);

        // 상호작용 구체
        var sc = player.AddComponent<SphereCollider>();
        sc.isTrigger = true; sc.radius = 2.0f; sc.center = new Vector3(0f, 1f, 0f);

        // 스택 포인트 (등 위치)
        var stackPt = new GameObject("StackPoint");
        stackPt.transform.SetParent(player.transform);
        stackPt.transform.localPosition = new Vector3(0f, 1.0f, -0.6f);

        // MAX 인디케이터
        var maxInd = new GameObject("MaxIndicator");
        maxInd.transform.SetParent(player.transform);
        maxInd.transform.localPosition = new Vector3(0f, 3.0f, 0f);
        var maxText = maxInd.AddComponent<TMPro.TextMeshPro>();
        maxText.text = "MAX";
        maxText.fontSize = 4f;
        maxText.color = Color.red;
        maxText.alignment = TMPro.TextAlignmentOptions.Center;
        maxInd.SetActive(false);

        // 도구 비주얼 (곡괭이 대역)
        var toolObj = new GameObject("ToolModel_Pickaxe");
        toolObj.transform.SetParent(player.transform);
        toolObj.transform.localPosition = new Vector3(0.5f, 1.2f, 0.3f);
        var toolVis = GameObject.CreatePrimitive(PrimitiveType.Cube);
        toolVis.name = "PickaxeVis";
        toolVis.transform.SetParent(toolObj.transform);
        toolVis.transform.localPosition = Vector3.zero;
        toolVis.transform.localScale = new Vector3(0.1f, 0.7f, 0.1f);
        toolVis.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);
        toolVis.GetComponent<Renderer>().sharedMaterial = MakeMat(new Color(0.4f, 0.4f, 0.4f));
        Object.DestroyImmediate(toolVis.GetComponent<Collider>());

        // 컴포넌트 부착
        var psm = player.AddComponent<PlayerStackManager>();
        var psmSO = new SerializedObject(psm);
        psmSO.FindProperty("stackPoint").objectReferenceValue        = stackPt.transform;
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
    }

    static GameObject MakeMarker(string name, GameObject parent, Vector3 worldPos)
    {
        var obj = new GameObject(name);
        if (parent != null)
            obj.transform.SetParent(parent.transform);
        obj.transform.position = worldPos;
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
