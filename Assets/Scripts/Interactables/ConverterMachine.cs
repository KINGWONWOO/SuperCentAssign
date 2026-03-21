using UnityEngine;
using System.Collections;

// 광석 1개 → 수갑 1개, processTime 처리 시간.
// 완성된 수갑은 handcuffDropZone(HandcuffPickupZone)에 직접 쌓임.
public class ConverterMachine : MonoBehaviour
{
    [SerializeField] private GameObject handcuffPrefab;

    [Tooltip("완성된 수갑을 쌓을 DropZone (HandcuffPickupZone)")]
    [SerializeField] private DropZone handcuffDropZone;

    private int pendingOre = 0;
    private bool isProcessing = false;

    // AutoSellNPC 호환용
    public bool HasOutput => handcuffDropZone != null && handcuffDropZone.HasItems;
    public GameObject TakeOutputHandcuff() => handcuffDropZone?.TakeItem();

    public void ReceiveOre(int count = 1)
    {
        pendingOre += count;
        if (!isProcessing)
            StartCoroutine(ProcessRoutine());
    }

    private IEnumerator ProcessRoutine()
    {
        isProcessing = true;
        float processTime = GameManager.Instance.Settings.processTime;

        while (pendingOre > 0)
        {
            yield return new WaitForSeconds(processTime);
            pendingOre--;
            SpawnHandcuff();
        }

        isProcessing = false;
    }

    private void SpawnHandcuff()
    {
        if (handcuffPrefab == null || handcuffDropZone == null) return;
        GameObject obj = Instantiate(handcuffPrefab, transform.position + Vector3.up, Quaternion.identity);
        handcuffDropZone.PushItem(obj);
    }
}
