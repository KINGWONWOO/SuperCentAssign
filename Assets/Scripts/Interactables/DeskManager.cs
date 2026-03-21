using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Desk에서 수갑을 수감자에게 판매하고 돈을 생성하는 매니저.
// PrisonerAI가 Desk에 도착하면 RegisterPrisoner()를 호출.
public class DeskManager : MonoBehaviour
{
    [SerializeField] private Transform deskStackPoint;
    [SerializeField] private MoneySpawner moneySpawner;
    [SerializeField] private PrisonerSpawner prisonerSpawner;

    private List<GameObject> deskHandcuffs = new List<GameObject>();
    private PrisonerAI currentPrisoner;
    private bool isSelling = false;

    public bool HasHandcuff => deskHandcuffs.Count > 0;

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

    public void RegisterPrisoner(PrisonerAI prisoner)
    {
        currentPrisoner = prisoner;
        if (!isSelling)
            StartCoroutine(SellRoutine());
    }

    public void OnPrisonerLeft()
    {
        currentPrisoner = null;
        prisonerSpawner?.OnSlotFreed();
    }

    private IEnumerator SellRoutine()
    {
        isSelling = true;
        float interval = GameManager.Instance.Settings.sellInterval;
        int cashPerHandcuff = GameManager.Instance.Settings.cashPerHandcuff;

        while (currentPrisoner != null)
        {
            // 수갑 재고 대기
            if (deskHandcuffs.Count == 0)
            {
                yield return new WaitForSeconds(0.2f);
                continue;
            }

            yield return new WaitForSeconds(interval);

            if (currentPrisoner == null) break;

            // 수갑 하나 제거
            int last = deskHandcuffs.Count - 1;
            GameObject hc = deskHandcuffs[last];
            deskHandcuffs.RemoveAt(last);
            if (hc != null) Destroy(hc);

            // 돈 생성 및 현금 추가
            moneySpawner?.SpawnMoney();
            CurrencyManager.Instance.AddCash(cashPerHandcuff);

            // 수감자에게 수갑 전달
            currentPrisoner.ReceiveHandcuff();
        }

        isSelling = false;
    }
}
