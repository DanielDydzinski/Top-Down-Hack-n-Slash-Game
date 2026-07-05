using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PotionBeltSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Tooltip("0-3, matches PlayerInventory's belt slot order")]
    [SerializeField] private int slotIndex;

    [Header("References")]
    [SerializeField] private Image icon;
    [SerializeField] private Text countText;
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private HealthPotionConsumer consumer;
    [Tooltip("Used to scale drag movement correctly in Screen Space - Camera canvases")]
    [SerializeField] private Canvas rootCanvas;

    private RectTransform iconRect;
    private Vector2 dragStartAnchoredPos;

    public int SlotIndex => slotIndex;

    void Awake()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (inventory == null && player != null) inventory = player.GetComponent<PlayerInventory>();
        if (consumer == null && player != null) consumer = player.GetComponent<HealthPotionConsumer>();
        if (icon != null) iconRect = icon.rectTransform;
    }

    void Update()
    {
        if (inventory == null) return;

        PlayerInventory.PotionSlot slot = inventory.GetSlot(slotIndex);
        bool hasPotion = slot != null && slot.potionType != null && slot.count > 0;

        if (icon != null)
        {
            icon.enabled = hasPotion;
            if (hasPotion) icon.sprite = slot.potionType.potionIcon;
        }

        if (countText != null)
            countText.text = (hasPotion && slot.count > 1) ? slot.count.ToString() : "";
    }

    // Wired from PlayerInputHandler's Alpha1-4 branches
    public void ConsumeFromInput()
    {
        consumer?.TryConsumePotion(slotIndex);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (iconRect == null) return;
        dragStartAnchoredPos = iconRect.anchoredPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (iconRect == null) return;
        iconRect.anchoredPosition += eventData.delta / (rootCanvas != null ? rootCanvas.scaleFactor : 1f);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Snap back - OnDrop (on whichever slot the pointer released over) already applied the swap
        if (iconRect != null) iconRect.anchoredPosition = dragStartAnchoredPos;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        PotionBeltSlotUI source = eventData.pointerDrag.GetComponent<PotionBeltSlotUI>();
        if (source == null || source == this) return;

        inventory?.SwapSlots(source.SlotIndex, slotIndex);
    }
}
