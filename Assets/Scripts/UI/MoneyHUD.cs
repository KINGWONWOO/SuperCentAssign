using UnityEngine;
using TMPro;

// 화면 우측 상단에 현재 보유 금액을 표시하는 HUD.
// CurrencyManager.OnCashChanged 이벤트를 구독하여 실시간 업데이트.
public class MoneyHUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI cashText;

    void Start()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCashChanged += UpdateDisplay;
            UpdateDisplay(CurrencyManager.Instance.Cash);
        }
        else
        {
            UpdateDisplay(0);
        }
    }

    void OnEnable()
    {
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCashChanged += UpdateDisplay;
    }

    void OnDisable()
    {
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCashChanged -= UpdateDisplay;
    }

    private void UpdateDisplay(int amount)
    {
        if (cashText != null)
            cashText.text = $"${amount}";
    }
}
