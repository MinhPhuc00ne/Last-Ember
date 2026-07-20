using UnityEngine;

namespace Antigravity
{
    [CreateAssetMenu(fileName = "NewItemData", menuName = "Inventory/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Item Info")]
        public string itemName = "New Item";
        public Sprite icon;
        public GameObject prefab;
    }
}
