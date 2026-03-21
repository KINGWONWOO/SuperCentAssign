using UnityEngine;

public class StackItem : MonoBehaviour
{
    public ItemType ItemType { get; private set; }

    public void Initialize(ItemType type)
    {
        ItemType = type;
    }
}
