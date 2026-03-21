using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// 광석 1개 → 수갑 1개, 2초 처리 시간
public class ConverterMachine : MonoBehaviour
{
    [SerializeField] private Transform outputPoint;
    [SerializeField] private GameObject handcuffPrefab;

    private int pendingOre = 0;
    private bool isProcessing = false;

    private List<GameObject> outputStack = new List<GameObject>();

    public bool HasOutput => outputStack.Count > 0;

    public void ReceiveOre(int count = 1)
    {
        pendingOre += count;
        if (!isProcessing)
            StartCoroutine(ProcessRoutine());
    }

    public GameObject TakeOutputHandcuff()
    {
        if (outputStack.Count == 0) return null;
        int last = outputStack.Count - 1;
        GameObject obj = outputStack[last];
        outputStack.RemoveAt(last);
        if (obj != null) obj.transform.SetParent(null);
        return obj;
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
        if (handcuffPrefab == null || outputPoint == null) return;

        float heightOffset = outputStack.Count * 0.18f;
        Vector3 spawnPos = outputPoint.position + Vector3.up * heightOffset;
        GameObject obj = Instantiate(handcuffPrefab, spawnPos, Quaternion.identity, outputPoint);
        outputStack.Add(obj);
    }
}
