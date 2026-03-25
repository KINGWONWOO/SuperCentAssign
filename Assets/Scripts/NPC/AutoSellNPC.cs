using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// 자동 판매 NPC. ConverterMachine에서 수갑 10개를 가져와 DeskManager로 전달.
// 프리팹에서 인스턴스화될 때 참조가 없으면 Start()에서 씬에서 자동 탐색.
public class AutoSellNPC : MonoBehaviour
{
    [SerializeField] private ConverterMachine converterMachine;
    [SerializeField] private DeskManager deskManager;
    [SerializeField] private Transform pickupPoint;
    [SerializeField] private Transform deliveryPoint;

    private float moveSpeed;
    private int batchSize;
    private List<GameObject> carrying = new List<GameObject>();
    private Animator animator;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    void Start()
    {
        // 씬에서 자동 탐색 (프리팹에서 생성 시 참조 미할당 상태를 처리)
        if (converterMachine == null)
            converterMachine = FindObjectOfType<ConverterMachine>();
        if (deskManager == null)
            deskManager = FindObjectOfType<DeskManager>();

        // pickupPoint: HandcuffPickupZone 위치
        if (pickupPoint == null)
        {
            var pickupZone = GameObject.Find("HandcuffPickupZone");
            if (pickupZone != null) pickupPoint = pickupZone.transform;
        }
        // deliveryPoint: DeskManager 또는 PoliceDeskArea
        if (deliveryPoint == null)
        {
            var deskObj = GameObject.Find("DeskManager") ?? GameObject.Find("PoliceDeskArea");
            if (deskObj != null) deliveryPoint = deskObj.transform;
        }

        if (converterMachine == null || deskManager == null || pickupPoint == null || deliveryPoint == null)
        {
            Debug.LogError("[AutoSellNPC] 필수 참조 탐색 실패 – 씬 오브젝트를 확인하세요.");
            return;
        }

        GameSettings s = GameManager.Instance.Settings;
        moveSpeed = s.autoSellMoveSpeed;
        batchSize = s.autoSellBatchSize;
        animator = GetComponentInChildren<Animator>(true);
        StartCoroutine(AutoSellRoutine());
    }

    private IEnumerator AutoSellRoutine()
    {
        while (true)
        {
            // Pickup 위치로 이동
            yield return StartCoroutine(WalkTo(pickupPoint.position));

            // 수갑 집기 (최대 batchSize개)
            int picked = 0;
            while (picked < batchSize && converterMachine.HasOutput)
            {
                GameObject hc = converterMachine.TakeOutputHandcuff();
                if (hc != null) { carrying.Add(hc); hc.SetActive(false); picked++; }
                yield return null;
            }

            if (carrying.Count == 0) { yield return new WaitForSeconds(1f); continue; }

            // Delivery 위치로 이동
            yield return StartCoroutine(WalkTo(deliveryPoint.position));

            // 수갑을 플레이어와 동일한 dropInterval 간격으로 한 개씩 전달
            float dropInterval = GameManager.Instance.Settings.dropInterval;
            foreach (GameObject hc in carrying)
            {
                if (hc != null)
                {
                    hc.SetActive(true);
                    deskManager.ReceiveHandcuff(hc);
                }
                yield return new WaitForSeconds(dropInterval);
            }
            carrying.Clear();
        }
    }

    private IEnumerator WalkTo(Vector3 destination)
    {
        animator?.SetFloat(SpeedHash, 1f);
        while (Vector3.Distance(transform.position, destination) > 0.2f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, destination, moveSpeed * Time.deltaTime);
            Vector3 dir = (destination - transform.position); dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir);
            yield return null;
        }
        transform.position = destination;
        animator?.SetFloat(SpeedHash, 0f);
    }
}
