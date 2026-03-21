using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerStackManager : MonoBehaviour
{
    [SerializeField] private Transform stackPoint;
    [SerializeField] private int maxStack = 15;
    [SerializeField] private float itemSpacingY = 0.22f;
    [SerializeField] private float lerpSpeed = 12f;

    private List<StackItem> stackedItems = new List<StackItem>();

    public bool IsFull => stackedItems.Count >= maxStack;
    public int Count => stackedItems.Count;

    public void SetMaxStack(int max) { maxStack = max; }

    // 이미 씬에 존재하는 StackItem을 스택에 편입 (Instantiate 없이)
    public bool AddExistingItem(StackItem existingItem)
    {
        if (IsFull) return false;

        existingItem.transform.SetParent(stackPoint);
        stackedItems.Add(existingItem);
        StartCoroutine(LerpToPosition(existingItem.transform, stackedItems.Count - 1));
        return true;
    }

    public bool AddItem(GameObject itemPrefab, ItemType type)
    {
        if (IsFull) return false;

        GameObject obj = Instantiate(itemPrefab, stackPoint.position, Quaternion.identity);
        StackItem si = obj.GetComponent<StackItem>();
        if (si == null) si = obj.AddComponent<StackItem>();
        si.Initialize(type);

        obj.transform.SetParent(stackPoint);
        stackedItems.Add(si);

        int targetIndex = stackedItems.Count - 1;
        StartCoroutine(LerpToPosition(obj.transform, targetIndex));
        return true;
    }

    public StackItem RemoveTopItemOfType(ItemType requiredType)
    {
        for (int i = stackedItems.Count - 1; i >= 0; i--)
        {
            if (stackedItems[i] == null) continue;
            if (stackedItems[i].ItemType == requiredType)
            {
                StackItem item = stackedItems[i];
                stackedItems.RemoveAt(i);
                item.transform.SetParent(null);
                RefreshPositions();
                return item;
            }
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

    private void RefreshPositions()
    {
        for (int i = 0; i < stackedItems.Count; i++)
        {
            if (stackedItems[i] != null)
                StartCoroutine(LerpToPosition(stackedItems[i].transform, i));
        }
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
        int itemsPerRow = 3;
        int layer = index / itemsPerRow;
        int posInRow = index % itemsPerRow;
        float xOffset = (posInRow - 1) * 0.2f;
        return new Vector3(xOffset, layer * itemSpacingY, 0f);
    }
}
