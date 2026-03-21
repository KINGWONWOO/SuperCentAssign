using UnityEngine;

// 플레이어가 Desk 위치에 진입하면 수갑을 DeskManager로 전달.
[RequireComponent(typeof(Collider))]
public class DeskZone : MonoBehaviour
{
    [SerializeField] private DeskManager deskManager;

    public void TransferHandcuff(PlayerStackManager stackManager)
    {
        if (!stackManager.HasItemOfType(ItemType.Handcuff)) return;
        if (deskManager == null) return;

        StackItem hc = stackManager.RemoveTopItemOfType(ItemType.Handcuff);
        if (hc != null)
            deskManager.ReceiveHandcuff(hc.gameObject);
    }
}
