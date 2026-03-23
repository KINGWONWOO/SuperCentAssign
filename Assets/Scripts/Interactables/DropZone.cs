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
        if (ore == null) return;

        // 드랍존에 쌓기 → 기계로 날아가는 연출
        StartCoroutine(OreDropAndFly(ore.gameObject));
    }

    private System.Collections.IEnumerator OreDropAndFly(GameObject oreObj)
    {
        // 1단계: 드랍존 pile에 물리적으로 쌓임
        PushItem(oreObj);

        yield return new WaitForSeconds(0.5f);
        if (oreObj == null) yield break;

        // pile에서 제거 후 월드 좌표 언파렌트
        items.Remove(oreObj);
        Vector3 startPos = oreObj.transform.position;
        oreObj.transform.SetParent(null);
        oreObj.transform.position = startPos;

        // 2단계: 기계 쪽으로 포물선 비행
        Vector3 endPos = converterMachine.transform.position + Vector3.up * 0.5f;
        float duration = 0.35f;
        float elapsed = 0f;

        while (elapsed < duration && oreObj != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 pos = Vector3.Lerp(startPos, endPos, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * 0.8f;
            oreObj.transform.position = pos;
            yield return null;
        }

        if (oreObj != null)
        {
            converterMachine.ReceiveOre(1);
            Destroy(oreObj);
        }

        // 나머지 pile 재정렬
        for (int i = 0; i < items.Count; i++)
            if (items[i] != null)
                items[i].transform.localPosition = new Vector3(0f, i * stackSpacingY, 0f);
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
