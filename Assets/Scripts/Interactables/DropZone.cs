using UnityEngine;

public class DropZone : MonoBehaviour
{
    [SerializeField] private DropZoneType zoneType;

    [Tooltip("OreToConverter 타입일 때 사용")]
    [SerializeField] private ConverterMachine converterMachine;

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
        }
    }

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

    private void PickupHandcuff(PlayerStackManager stackManager)
    {
        if (stackManager.IsFull) return;
        if (converterMachine == null || !converterMachine.HasOutput) return;

        // 기계 출력 스택에서 오브젝트 꺼내기
        GameObject handcuffObj = converterMachine.TakeOutputHandcuff();
        if (handcuffObj == null) return;

        // 플레이어 스택에 추가 (StackItem 컴포넌트 직접 설정 후 AddItem 우회)
        StackItem si = handcuffObj.GetComponent<StackItem>();
        if (si == null) si = handcuffObj.AddComponent<StackItem>();
        si.Initialize(ItemType.Handcuff);

        // PlayerStackManager 내부 리스트에 직접 편입 (별도 Instantiate 없이)
        stackManager.AddExistingItem(si);

        // CurrencyManager 동기화
        CurrencyManager.Instance.AddHandcuffs(1);
    }
}
