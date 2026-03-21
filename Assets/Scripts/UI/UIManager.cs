using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Top Panel")]
    [SerializeField] private TextMeshProUGUI cashText;
    [SerializeField] private TextMeshProUGUI handcuffText;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        CurrencyManager.Instance.OnCashChanged += UpdateCash;
        CurrencyManager.Instance.OnHandcuffsChanged += UpdateHandcuffs;
        UpdateCash(CurrencyManager.Instance.Cash);
        UpdateHandcuffs(CurrencyManager.Instance.Handcuffs);
    }

    void OnDestroy()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCashChanged -= UpdateCash;
            CurrencyManager.Instance.OnHandcuffsChanged -= UpdateHandcuffs;
        }
    }

    private void UpdateCash(int value)
    {
        if (cashText != null)
            cashText.text = $"${value}";
    }

    private void UpdateHandcuffs(int value)
    {
        if (handcuffText != null)
            handcuffText.text = $"x{value}";
    }
}
