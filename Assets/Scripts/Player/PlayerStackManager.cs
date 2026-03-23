using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerStackManager : MonoBehaviour
{
    [SerializeField] private Transform stackPoint;
    [SerializeField] private float itemSpacingY = 0.22f;
    [SerializeField] private float lerpSpeed = 12f;
    [SerializeField] private GameObject maxIndicatorObject;

    private int maxOreStack = 10;
    private List<StackItem> stackedItems = new List<StackItem>();

    // Ore는 maxOreStack 제한, Handcuff/Cash는 무제한
    public bool IsOreFull => CountOfType(ItemType.Ore) >= maxOreStack;
    public int Count => stackedItems.Count;

    void Start()
    {
        if (GameManager.Instance != null)
            maxOreStack = GameManager.Instance.Settings.maxOreStacks[0];
        SetMaxIndicator(false);
    }

    void Update()
    {
        // MAX 인디케이터 항상 IsOreFull 상태와 동기화
        if (maxIndicatorObject != null && maxIndicatorObject.activeSelf != IsOreFull)
            SetMaxIndicator(IsOreFull);
    }

    public void SetMaxOreStack(int max)
    {
        maxOreStack = max;
        SetMaxIndicator(IsOreFull);
    }

    // Ore 추가 (스택 제한 있음) — 가득 찼어도 false 반환하지 않음. 채굴은 계속되어야 함.
    // 반환값: 실제로 스택에 추가됐으면 true, 가득 차서 추가 안 됐으면 false
    public bool AddOre(GameObject orePrefab)
    {
        if (IsOreFull)
        {
            SetMaxIndicator(true);
            return false; // 스택에 쌓이지 않지만 광석은 사라짐 (MiningGrid에서 처리)
        }

        GameObject obj = Instantiate(orePrefab, stackPoint.position, Quaternion.identity);
        StackItem si = obj.GetComponent<StackItem>();
        if (si == null) si = obj.AddComponent<StackItem>();
        si.Initialize(ItemType.Ore);

        obj.transform.SetParent(stackPoint);
        stackedItems.Add(si);
        StartCoroutine(LerpToPosition(obj.transform, stackedItems.Count - 1));
        SetMaxIndicator(IsOreFull);
        return true;
    }

    // Handcuff/Cash 추가 — 무제한
    public bool AddItem(GameObject itemPrefab, ItemType type)
    {
        GameObject obj = Instantiate(itemPrefab, stackPoint.position, Quaternion.identity);
        StackItem si = obj.GetComponent<StackItem>();
        if (si == null) si = obj.AddComponent<StackItem>();
        si.Initialize(type);

        obj.transform.SetParent(stackPoint);
        stackedItems.Add(si);
        StartCoroutine(LerpToPosition(obj.transform, stackedItems.Count - 1));
        return true;
    }

    // 기존 StackItem 오브젝트를 스택에 편입 (ConverterMachine 출력 → 플레이어)
    public bool AddExistingItem(StackItem existingItem)
    {
        existingItem.transform.SetParent(stackPoint);
        stackedItems.Add(existingItem);
        StartCoroutine(LerpToPosition(existingItem.transform, stackedItems.Count - 1));
        return true;
    }

    public StackItem RemoveTopItemOfType(ItemType requiredType)
    {
        for (int i = stackedItems.Count - 1; i >= 0; i--)
        {
            if (stackedItems[i] == null) continue;
            if (stackedItems[i].ItemType != requiredType) continue;

            StackItem item = stackedItems[i];
            stackedItems.RemoveAt(i);
            item.transform.SetParent(null);
            RefreshPositions();
            SetMaxIndicator(IsOreFull);
            return item;
        }
        return null;
    }

    public StackItem RemoveTopItem()
    {
        for (int i = stackedItems.Count - 1; i >= 0; i--)
        {
            if (stackedItems[i] == null) continue;

            StackItem item = stackedItems[i];
            stackedItems.RemoveAt(i);
            item.transform.SetParent(null);
            RefreshPositions();
            SetMaxIndicator(IsOreFull);
            return item;
        }
        return null;
    }

    public bool HasItemOfType(ItemType type)
    {
        return stackedItems.Exists(i => i != null && i.ItemType == type);
    }

    public int CountOfType(ItemType type)
    {
        int count = 0;
        foreach (var item in stackedItems)
            if (item != null && item.ItemType == type) count++;
        return count;
    }

    private void SetMaxIndicator(bool active)
    {
        if (maxIndicatorObject != null)
            maxIndicatorObject.SetActive(active);
    }

    private void RefreshPositions()
    {
        for (int i = 0; i < stackedItems.Count; i++)
            if (stackedItems[i] != null)
                StartCoroutine(LerpToPosition(stackedItems[i].transform, i));
    }

    private IEnumerator LerpToPosition(Transform item, int index)
    {
        if (item == null) yield break;
        Vector3 target = GetLocalStackPosition(index);

        while (item != null && Vector3.Distance(item.localPosition, target) > 0.01f)
        {
            item.localPosition = Vector3.Lerp(item.localPosition, target, lerpSpeed * Time.deltaTime);
            item.localRotation = Quaternion.Slerp(item.localRotation, Quaternion.identity, lerpSpeed * Time.deltaTime);
            yield return null;
        }

        if (item != null)
        {
            item.localPosition = target;
            item.localRotation = Quaternion.identity;
        }
    }

    private Vector3 GetLocalStackPosition(int index)
    {
        // 전체 스택에서 타입별 인덱스를 구해 타입마다 다른 X 오프셋에 쌓음
        StackItem item = index < stackedItems.Count ? stackedItems[index] : null;
        ItemType type = item != null ? item.ItemType : ItemType.Ore;

        // 타입별 베이스 X 오프셋 (돌=왼쪽, 수갑=중앙, 돈=오른쪽)
        float baseX = type switch
        {
            ItemType.Ore      => -0.28f,
            ItemType.Handcuff =>  0.00f,
            ItemType.Cash     =>  0.28f,
            _                 =>  0.00f
        };

        // 같은 타입 내에서의 인덱스 (층 계산용)
        int sameTypeCount = 0;
        for (int i = 0; i < index; i++)
            if (stackedItems[i] != null && stackedItems[i].ItemType == type)
                sameTypeCount++;

        // 돈은 세로(Y) 대신 가로(Z) 방향으로 펼쳐 쌓음
        if (type == ItemType.Cash)
            return new Vector3(0.28f, 0.10f, sameTypeCount * 0.08f);

        return new Vector3(baseX, sameTypeCount * itemSpacingY, 0f);
    }
}
