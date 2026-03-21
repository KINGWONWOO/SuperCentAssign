using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// 자동 판매 NPC. ConverterMachine에서 수갑 10개를 가져와 DeskManager로 전달.
public class AutoSellNPC : MonoBehaviour
{
    [SerializeField] private ConverterMachine converterMachine;
    [SerializeField] private DeskManager deskManager;
    [SerializeField] private Transform pickupPoint;
    [SerializeField] private Transform deliveryPoint;

    private float moveSpeed;
    private int batchSize;
    private List<GameObject> carrying = new List<GameObject>();

    void Start()
    {
        GameSettings s = GameManager.Instance.Settings;
        moveSpeed = s.autoSellMoveSpeed;
        batchSize = s.autoSellBatchSize;
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

            // 수갑 전달
            foreach (GameObject hc in carrying)
            {
                if (hc != null)
                {
                    hc.SetActive(true);
                    deskManager.ReceiveHandcuff(hc);
                }
            }
            carrying.Clear();
        }
    }

    private IEnumerator WalkTo(Vector3 destination)
    {
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
    }
}
