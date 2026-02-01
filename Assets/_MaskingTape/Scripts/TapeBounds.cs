using UnityEngine;
using UnityEngine.EventSystems;

public class TapeBounds : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private TapeController tapeController;

    public void SetTapeController(TapeController controller)
    {
        tapeController = controller;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        tapeController.UpdateTapeBound(this, eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log($"The mouse exited {gameObject.name}'s bounds at position: {eventData.position}");
        // Notify the tape controller of the exit position
        tapeController.OnMouseExitTapeBounds(eventData.position);
    }
}