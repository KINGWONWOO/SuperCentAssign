using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ConverterMachine : MonoBehaviour
{
    [SerializeField] private Transform outputPoint;
    [SerializeField] private GameObject handcuffPrefab;

    private float processTime;
    private int orePerHandcuff;
    private int maxOutput;

    private int storedOre = 0;
    private bool isProcessing = false;

    private List<GameObject> outputStack = new List<GameObject>();

    public bool HasOutput => outputStack.Count > 0;

    void Start()
    {
        GameSettings s = GameManager.Instance.Settings;
        processTime = s.processTime;
        orePerHandcuff = s.orePerHandcuff;
        maxOutput = s.maxConverterOutput;
    }

    public void ReceiveOre(int count = 1)
    {
        storedOre += count;
        if (!isProcessing)
            StartCoroutine(ProcessRoutine());
    }

    public GameObject TakeOutputHandcuff()
    {
        if (outputStack.Count == 0) return null;
        int last = outputStack.Count - 1;
        GameObject obj = outputStack[last];
        outputStack.RemoveAt(last);
        return obj;
    }

    private IEnumerator ProcessRoutine()
    {
        isProcessing = true;

        while (storedOre >= orePerHandcuff)
        {
            yield return new WaitForSeconds(processTime);

            if (storedOre < orePerHandcuff) break;
            if (outputStack.Count >= maxOutput) { yield return null; continue; }

            storedOre -= orePerHandcuff;
            SpawnOutputItem();
        }

        isProcessing = false;
    }

    private void SpawnOutputItem()
    {
        if (handcuffPrefab == null || outputPoint == null) return;

        float heightOffset = outputStack.Count * 0.15f;
        Vector3 spawnPos = outputPoint.position + Vector3.up * heightOffset;
        GameObject obj = Instantiate(handcuffPrefab, spawnPos, Quaternion.identity, outputPoint);
        outputStack.Add(obj);
    }
}
