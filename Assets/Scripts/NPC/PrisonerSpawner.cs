using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PrisonerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject prisonerPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform[] patrolWaypoints;

    private float spawnInterval;
    private int maxPrisoners;

    private List<GameObject> activePrisoners = new List<GameObject>();

    void Start()
    {
        GameSettings s = GameManager.Instance.Settings;
        spawnInterval = s.prisonerSpawnInterval;
        maxPrisoners = s.maxPrisonersAtOnce;

        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            // 죽은 오브젝트 정리
            activePrisoners.RemoveAll(p => p == null);

            if (activePrisoners.Count < maxPrisoners)
                SpawnPrisoner();
        }
    }

    private void SpawnPrisoner()
    {
        if (prisonerPrefab == null || spawnPoint == null) return;

        GameObject obj = Instantiate(prisonerPrefab, spawnPoint.position, spawnPoint.rotation);
        activePrisoners.Add(obj);

        // 웨이포인트 배열 전달
        PrisonerAI ai = obj.GetComponent<PrisonerAI>();
        if (ai != null && patrolWaypoints != null)
        {
            // PrisonerAI는 Inspector로 waypoints를 받지만
            // 런타임 주입이 필요한 경우 아래 메서드로 전달
            ai.SetWaypoints(patrolWaypoints);
        }
    }
}
