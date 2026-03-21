using UnityEngine;
using System.Collections.Generic;

// Desk 옆에 배치. DeskManager가 수갑 판매 시 SpawnMoney() 호출.
// 플레이어가 MoneyPickup DropZone에 진입하면 TakeMoney()로 가져감.
public class MoneySpawner : MonoBehaviour
{
    [SerializeField] private GameObject moneyPrefab;
    [SerializeField] private Transform stackPoint;
    [SerializeField] private float stackSpacingY = 0.12f;

    private List<GameObject> moneyStack = new List<GameObject>();

    public bool HasMoney => moneyStack.Count > 0;

    public void SpawnMoney()
    {
        if (moneyPrefab == null) return;

        Transform pivot = stackPoint != null ? stackPoint : transform;
        float height = moneyStack.Count * stackSpacingY;
        Vector3 pos = pivot.position + Vector3.up * height;
        GameObject obj = Instantiate(moneyPrefab, pos, Quaternion.identity, pivot);
        moneyStack.Add(obj);
    }

    public GameObject TakeMoney()
    {
        if (moneyStack.Count == 0) return null;
        int last = moneyStack.Count - 1;
        GameObject obj = moneyStack[last];
        moneyStack.RemoveAt(last);
        if (obj != null) obj.transform.SetParent(null);
        return obj;
    }
}
