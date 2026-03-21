using UnityEngine;
using System.Collections.Generic;

public class ArrestZone : MonoBehaviour
{
    [SerializeField] private CellManager cellManager;

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

        // 대기 중인 죄수 중 한 명 체포
        PrisonerAI target = prisonersInZone.Find(p =>
            p != null && p.CurrentState == PrisonerState.Waiting);

        if (target == null) return;

        // 수갑 소비 (스택 + CurrencyManager)
        StackItem handcuff = stackManager.RemoveTopItemOfType(ItemType.Handcuff);
        if (handcuff != null) Destroy(handcuff.gameObject);
        CurrencyManager.Instance.SpendHandcuff();

        // 죄수 수감
        prisonersInZone.Remove(target);
        target.Arrest(cellManager);

        // 즉시 현금 보상
        CurrencyManager.Instance.AddCash(GameManager.Instance.Settings.cashPerPrisoner);
    }
}
