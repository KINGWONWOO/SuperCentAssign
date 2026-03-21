using UnityEngine;
using System.Collections.Generic;

public class ArrestZone : MonoBehaviour
{
    [SerializeField] private CellManager cellManager;

    [Tooltip("체포 후 플레이어 등에 올라갈 현금 프리팹")]
    [SerializeField] private GameObject cashPrefab;

    private List<PrisonerAI> prisonersInZone = new List<PrisonerAI>();

    void OnTriggerEnter(Collider other)
    {
        PrisonerAI prisoner = other.GetComponent<PrisonerAI>();
        if (prisoner != null && prisoner.CurrentState == PrisonerState.Waiting)
            prisonersInZone.Add(prisoner);
    }

    void OnTriggerExit(Collider other)
    {
        PrisonerAI prisoner = other.GetComponent<PrisonerAI>();
        if (prisoner != null)
            prisonersInZone.Remove(prisoner);
    }

    public void TryArrest(PlayerStackManager stackManager)
    {
        if (cellManager != null && cellManager.IsFull) return;
        if (!stackManager.HasItemOfType(ItemType.Handcuff)) return;

        PrisonerAI target = prisonersInZone.Find(
            p => p != null && p.CurrentState == PrisonerState.Waiting);
        if (target == null) return;

        // 수갑 소비: 스택에서 제거 + CurrencyManager 차감
        StackItem handcuff = stackManager.RemoveTopItemOfType(ItemType.Handcuff);
        if (handcuff != null) Destroy(handcuff.gameObject);
        CurrencyManager.Instance.SpendHandcuff();

        // 죄수 수감 처리
        prisonersInZone.Remove(target);
        target.Arrest(cellManager);

        // 현금 획득: 플레이어 등에 현금 아이템 추가 + CurrencyManager 반영
        GameSettings settings = GameManager.Instance.Settings;
        if (cashPrefab != null)
            stackManager.AddItem(cashPrefab, ItemType.Cash);
        CurrencyManager.Instance.AddCash(settings.cashPerPrisoner);
    }
}
