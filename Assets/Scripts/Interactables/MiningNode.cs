using UnityEngine;
using System.Collections;

public class MiningNode : MonoBehaviour
{
    [SerializeField] private GameObject orePrefab;
    [SerializeField] private ParticleSystem hitParticle;
    [SerializeField] private MeshRenderer[] nodeMeshes;

    private int maxHP;
    private float respawnTime;
    private int currentHP;

    public bool IsActive { get; private set; } = true;

    void Start()
    {
        GameSettings s = GameManager.Instance.Settings;
        maxHP = s.defaultNodeHP;
        respawnTime = s.respawnTime;
        currentHP = maxHP;

        if (nodeMeshes == null || nodeMeshes.Length == 0)
            nodeMeshes = GetComponentsInChildren<MeshRenderer>();
    }

    public void Mine(PlayerStackManager stackManager)
    {
        if (!IsActive) return;

        hitParticle?.Play();

        if (orePrefab != null)
            stackManager.AddItem(orePrefab, ItemType.Ore);

        currentHP--;
        if (currentHP <= 0)
            StartCoroutine(DepleteRoutine());
    }

    private IEnumerator DepleteRoutine()
    {
        IsActive = false;
        SetVisible(false);

        yield return new WaitForSeconds(respawnTime);

        currentHP = maxHP;
        IsActive = true;
        SetVisible(true);
    }

    private void SetVisible(bool visible)
    {
        foreach (var mr in nodeMeshes)
            if (mr != null) mr.enabled = visible;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = visible;
    }
}
