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

    // 워커 NPC가 채굴한 광석을 이 존에 시각적으로 쌓고 변환기로 전달
    public void WorkerDeliverOre(GameObject orePrefab)
    {
        if (zoneType != DropZoneType.OreToConverter || converterMachine == null || orePrefab == null) return;

        GameObject ore = Instantiate(orePrefab, (stackRoot != null ? stackRoot : transform).position, Quaternion.identity);
        PushItem(ore);
        converterMachine.ReceiveOre(1);

        // 변환 완료 후 리스트에서 제거 + 오브젝트 파괴
        float destroyDelay = GameManager.Instance != null
            ? GameManager.Instance.Settings.processTime + 0.3f
            : 2.5f;
        StartCoroutine(RemoveAfterDelay(ore, destroyDelay));
    }

    private System.Collections.IEnumerator RemoveAfterDelay(GameObject obj, float delay)
    {
        yield return new UnityEngine.WaitForSeconds(delay);
        if (obj != null)
        {
            items.Remove(obj);
            Destroy(obj);
            // 남은 아이템 위치 재정렬
            for (int i = 0; i < items.Count; i++)
                if (items[i] != null)
                    items[i].transform.localPosition = new Vector3(0f, i * stackSpacingY, 0f);
        }
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

        // 돈을 주울 때 CurrencyManager에 현금 추가 (HUD 반영)
        if (type == ItemType.Cash)
            CurrencyManager.Instance?.AddCash(
                GameManager.Instance.Settings.cashPerHandcuff);
    }
}
