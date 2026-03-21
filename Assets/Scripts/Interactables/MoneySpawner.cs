using UnityEngine;

// DeskManager가 수갑 판매 시 SpawnMoney() 호출.
// 생성된 돈은 moneyDropZone(MoneyPickupZone)에 직접 쌓임.
public class MoneySpawner : MonoBehaviour
{
    [SerializeField] private GameObject moneyPrefab;
    [SerializeField] private Transform stackPoint;

    [Tooltip("생성된 돈을 쌓을 DropZone (MoneyPickupZone)")]
    [SerializeField] private DropZone moneyDropZone;

    private float stackSpacingY = 0.12f;
    private int fallbackCount = 0;

    public void SpawnMoney()
    {
        if (moneyPrefab == null) return;

        Transform pivot = stackPoint != null ? stackPoint : transform;
        Vector3 spawnPos = pivot.position + Vector3.up * 0.3f;
        GameObject obj = Instantiate(moneyPrefab, spawnPos, Quaternion.identity);

        if (moneyDropZone != null)
            moneyDropZone.PushItem(obj);
        else
        {
            obj.transform.SetParent(pivot);
            obj.transform.localPosition = Vector3.up * (fallbackCount * stackSpacingY);
            fallbackCount++;
        }
    }
}
