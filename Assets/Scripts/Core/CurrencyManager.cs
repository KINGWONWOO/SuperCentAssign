using UnityEngine;
using System;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    public int Cash { get; private set; }
    public int Handcuffs { get; private set; }

    public event Action<int> OnCashChanged;
    public event Action<int> OnHandcuffsChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void AddCash(int amount)
    {
        if (amount <= 0) return;
        Cash += amount;
        OnCashChanged?.Invoke(Cash);
    }

    public bool SpendCash(int amount)
    {
        if (Cash < amount) return false;
        Cash -= amount;
        OnCashChanged?.Invoke(Cash);
        return true;
    }

    public void AddHandcuffs(int amount)
    {
        if (amount <= 0) return;
        Handcuffs += amount;
        OnHandcuffsChanged?.Invoke(Handcuffs);
    }

    public bool SpendHandcuff()
    {
        if (Handcuffs < 1) return false;
        Handcuffs--;
        OnHandcuffsChanged?.Invoke(Handcuffs);
        return true;
    }
}
