using UnityEngine;
using System.Collections.Generic;

public class DropZone : MonoBehaviour
{
    [SerializeField] private DropZoneType zoneType;

    [Tooltip("OreToConverter 타입일 때 사용")]
    [SerializeField] private ConverterMachine converterMachine;

    [Tooltip("HandcuffPickup/MoneyPickup: 아이템이 쌓일 기준점 (없으면 transform)")]
    [SerializeField] private Transform stackRoot;

    private readonly List<GameObject> items = new List<GameObject>();
    private const float stackSpacingY = 0.18f;

    public bool HasItems => items.Count > 0;

    // ConverterMachine / MoneySpawner가 호출 — 변환 완료된 아이템을 이 존에 쌓음
    public void PushItem(GameObject obj)
    {
        if (obj == null) return;
        Transform root = stackRoot != null ? stackRoot : transform;
        obj.transform.SetParent(root);
        obj.transform.localPosition = new Vector3(0f, items.Count * stackSpacingY, 0f);
        obj.transform.localRotation = Quaternion.identity;
        items.Add(obj);
    }

    // AutoSellNPC 등 외부에서 직접 꺼낼 때 사용
    public GameObject TakeItem()
    {
        if (items.Count == 0) return null;
        int last = items.Count - 1;
        GameObject obj = items[last];
        items.RemoveAt(last);
        if (obj != null) obj.transform.SetParent(null);
        return obj;
    }

    private GameObject PopItem() => TakeItem();

    public void ProcessTransfer(PlayerStackManager stackManager)
    {
        switch (zoneType)
        {
            case DropZoneType.OreToConverter: DeliverOre(stackManager);                     break;
            case DropZoneType.HandcuffPickup: PickupItem(stackManager, ItemType.Handcuff);  break;
            case DropZoneType.MoneyPickup:    PickupItem(stackManager, ItemType.Cash);      break;
        }
    }

    private void DeliverOre(PlayerStackManager stackManager)
    {
        if (!stackManager.HasItemOfType(ItemType.Ore)) return;
        if (converterMachine == null) return;
        StackItem ore = stackManager.RemoveTopItemOfType(ItemType.Ore);
        if (ore != null) { converterMachine.ReceiveOre(1); Destroy(ore.gameObject); }
    }

    private void PickupItem(PlayerStackManager stackManager, ItemType type)
    {
        if (!HasItems) return;
        GameObject obj = PopItem();
        if (obj == null) return;
        StackItem si = obj.GetComponent<StackItem>();
        if (si == null) si = obj.AddComponent<StackItem>();
        si.Initialize(type);
        stackManager.AddExistingItem(si);
    }
}
