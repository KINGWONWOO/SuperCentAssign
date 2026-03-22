using UnityEngine;
using System.Collections;

public class RockNode : MonoBehaviour
{
    [SerializeField] private MeshRenderer[] nodeMeshes;
    [SerializeField] private Collider nodeCollider;

    public bool IsActive { get; private set; } = true;
    public Vector2Int GridCoord { get; set; }

    void Awake()
    {
        if (nodeMeshes == null || nodeMeshes.Length == 0)
            nodeMeshes = GetComponentsInChildren<MeshRenderer>();
        if (nodeCollider == null)
            nodeCollider = GetComponent<Collider>();
        // CharacterController는 trigger 콜라이더를 통과 → 돌에 막히지 않음
        if (nodeCollider != null)
            nodeCollider.isTrigger = true;
    }

    // 플레이어 채굴 — 광석 추가 시도 후 돌 사라짐 (스택 가득 차도 돌은 사라짐)
    public void Mine(PlayerStackManager stackManager, GameObject orePrefab)
    {
        if (!IsActive) return;
        IsActive = false;
        SetVisible(false);
        StartCoroutine(RespawnRoutine());

        // 스택 가득 찼어도 AddOre 호출 — 내부에서 false 반환하지만 돌은 이미 사라짐
        if (stackManager != null && orePrefab != null)
            stackManager.AddOre(orePrefab);
    }

    // 워커 NPC 채굴 — 광석을 Converter로 직접 전달 (스택 없음)
    public bool MineForWorker()
    {
        if (!IsActive) return false;
        IsActive = false;
        SetVisible(false);
        StartCoroutine(RespawnRoutine());
        return true;
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(GameManager.Instance.Settings.rockRespawnTime);
        IsActive = true;
        SetVisible(true);
    }

    private void SetVisible(bool visible)
    {
        foreach (var mr in nodeMeshes)
            if (mr != null) mr.enabled = visible;
        if (nodeCollider != null)
            nodeCollider.enabled = visible;
    }
}
