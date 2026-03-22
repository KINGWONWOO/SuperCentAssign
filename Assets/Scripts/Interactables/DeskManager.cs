using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// 플레이어가 DeskZone에 수갑을 올려두면 책상 위에 쌓임.
// 수감자(PrisonerAI)가 도착해 RegisterPrisoner() 호출 → SellRoutine이 자동 판매.
// 판매 시 moneyPrefab이 수감자 위치에서 MoneyPickupZone으로 날아가 쌓임.
public class DeskManager : MonoBehaviour
{
    [SerializeField] private Transform deskStackPoint;
    [SerializeField] private GameObject moneyPrefab;
    [SerializeField] private DropZone moneyPickupZone;

    [Header("돈 비행 설정")]
    [SerializeField] private float moneyFlySpeed = 8f;   // 단위/초
    [SerializeField] private float moneyArcHeight = 1.5f; // 포물선 최대 높이

    private List<GameObject> deskHandcuffs = new List<GameObject>();
    private PrisonerAI currentPrisoner;
    private bool isSelling = false;

    public bool HasHandcuff => deskHandcuffs.Count > 0;

    // 플레이어가 DeskZone에 진입 시 DeskZone이 호출 — 수갑을 책상 위에 쌓음
    public void ReceiveHandcuff(GameObject handcuffObj)
    {
        if (handcuffObj == null) return;
        float heightOffset = deskHandcuffs.Count * 0.15f;
        Transform pivot = deskStackPoint != null ? deskStackPoint : transform;
        handcuffObj.transform.SetParent(pivot);
        handcuffObj.transform.localPosition = new Vector3(0f, heightOffset, 0f);
        handcuffObj.transform.localRotation = Quaternion.identity;
        deskHandcuffs.Add(handcuffObj);
    }

    // 수감자가 책상 앞에 도착하면 호출 → 판매 루틴 시작
    public void RegisterPrisoner(PrisonerAI prisoner)
    {
        currentPrisoner = prisoner;
        if (!isSelling)
            StartCoroutine(SellRoutine());
    }

    // 수감자가 처리 완료 후 호출 — currentPrisoner 초기화
    public void OnPrisonerLeft()
    {
        currentPrisoner = null;
    }

    private IEnumerator SellRoutine()
    {
        isSelling = true;
        float interval = GameManager.Instance.Settings.sellInterval;

        while (currentPrisoner != null)
        {
            if (deskHandcuffs.Count == 0)
            {
                yield return new WaitForSeconds(0.2f);
                continue;
            }

            yield return new WaitForSeconds(interval);

            if (currentPrisoner == null) break;

            // 책상 위 수갑 제거
            int last = deskHandcuffs.Count - 1;
            GameObject hc = deskHandcuffs[last];
            deskHandcuffs.RemoveAt(last);
            if (hc != null) Destroy(hc);

            // 수감자 위치에서 MoneyPickupZone으로 돈이 날아감
            SpawnFlyingMoney();

            // 수감자에게 수갑 전달 알림
            currentPrisoner.ReceiveHandcuff();
        }

        isSelling = false;
    }

    // 수감자 머리 위에서 돈 오브젝트를 생성해 MoneyPickupZone으로 날려 보냄
    private void SpawnFlyingMoney()
    {
        if (moneyPrefab == null || moneyPickupZone == null || currentPrisoner == null) return;

        Vector3 fromPos = currentPrisoner.transform.position + Vector3.up * 1.5f;
        GameObject obj = Instantiate(moneyPrefab, fromPos, Quaternion.identity);
        StartCoroutine(FlyToZone(obj, fromPos));
    }

    // 포물선 궤도로 목표 존까지 날아간 뒤 PushItem으로 쌓음
    private IEnumerator FlyToZone(GameObject obj, Vector3 from)
    {
        if (obj == null) yield break;

        // 목표: moneyPickupZone의 다음 스택 꼭대기 위치 (근사)
        Transform zoneRoot = moneyPickupZone.transform;
        Vector3 to = zoneRoot.position + Vector3.up * 0.5f;

        float dist     = Vector3.Distance(from, to);
        float duration = dist / moneyFlySpeed;
        float elapsed  = 0f;

        while (elapsed < duration)
        {
            if (obj == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // 선형 보간 + 포물선 Y
            Vector3 flat = Vector3.Lerp(from, to, t);
            flat.y += moneyArcHeight * Mathf.Sin(t * Mathf.PI);
            obj.transform.position = flat;

            yield return null;
        }

        if (obj == null) yield break;

        // 도착 → 존에 편입
        obj.transform.position = to;
        moneyPickupZone.PushItem(obj);
    }
}
