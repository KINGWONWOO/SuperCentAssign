using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Top Panel")]
    [SerializeField] private TextMeshProUGUI cashText;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        CurrencyManager.Instance.OnCashChanged += UpdateCash;
        UpdateCash(CurrencyManager.Instance.Cash);
    }

    void OnDestroy()
    {
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCashChanged -= UpdateCash;
    }

    private void UpdateCash(int value)
    {
        if (cashText != null)
            cashText.text = $"${value}";
    }
}
