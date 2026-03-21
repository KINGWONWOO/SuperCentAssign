using UnityEngine;

public class DropZone : MonoBehaviour
{
    [SerializeField] private DropZoneType zoneType;

    [Tooltip("OreToConverter / HandcuffPickup 타입일 때 사용")]
    [SerializeField] private ConverterMachine converterMachine;

    [Tooltip("MoneyPickup 타입일 때 사용")]
    [SerializeField] private MoneySpawner moneySpawner;

    public void ProcessTransfer(PlayerStackManager stackManager)
    {
        switch (zoneType)
        {
            case DropZoneType.OreToConverter:
                DeliverOre(stackManager);
                break;

            case DropZoneType.HandcuffPickup:
                PickupHandcuff(stackManager);
                break;

            case DropZoneType.MoneyPickup:
                PickupMoney(stackManager);
                break;
        }
    }

    // 광석을 빠르게 Converter로 보냄 (oreDropSpeed 간격은 PlayerInteraction에서 제어)
    private void DeliverOre(PlayerStackManager stackManager)
    {
        if (!stackManager.HasItemOfType(ItemType.Ore)) return;
        if (converterMachine == null) return;

        StackItem ore = stackManager.RemoveTopItemOfType(ItemType.Ore);
        if (ore != null)
        {
            converterMachine.ReceiveOre(1);
            Destroy(ore.gameObject);
        }
    }

    // 수갑 픽업 — 무제한 (IsFull 체크 없음)
    private void PickupHandcuff(PlayerStackManager stackManager)
    {
        if (converterMachine == null || !converterMachine.HasOutput) return;

        GameObject handcuffObj = converterMachine.TakeOutputHandcuff();
        if (handcuffObj == null) return;

        StackItem si = handcuffObj.GetComponent<StackItem>();
        if (si == null) si = handcuffObj.AddComponent<StackItem>();
        si.Initialize(ItemType.Handcuff);

        stackManager.AddExistingItem(si);
    }

    // 돈 픽업 — 플레이어 스택에 현금 아이템 추가
    private void PickupMoney(PlayerStackManager stackManager)
    {
        if (moneySpawner == null || !moneySpawner.HasMoney) return;

        GameObject moneyObj = moneySpawner.TakeMoney();
        if (moneyObj == null) return;

        StackItem si = moneyObj.GetComponent<StackItem>();
        if (si == null) si = moneyObj.AddComponent<StackItem>();
        si.Initialize(ItemType.Cash);

        stackManager.AddExistingItem(si);
    }
}
