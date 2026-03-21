using UnityEngine;

// DeskManager가 수갑 판매 시 SpawnMoney() 호출.
// 생성된 돈은 moneyDropZone(MoneyPickupZone)에 직접 쌓임.
public class MoneySpawner : MonoBehaviour
{
    [SerializeField] private GameObject moneyPrefab;

    [Tooltip("생성된 돈을 쌓을 DropZone (MoneyPickupZone)")]
    [SerializeField] private DropZone moneyDropZone;

    public void SpawnMoney()
    {
        if (moneyPrefab == null || moneyDropZone == null) return;
        GameObject obj = Instantiate(moneyPrefab, transform.position + Vector3.up, Quaternion.identity);
        moneyDropZone.PushItem(obj);
    }
}
