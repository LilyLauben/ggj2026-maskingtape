using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PaintRollerController : MonoBehaviour
{
    private Transform paintRoller;
    [SerializeField]
    private Camera mainCam;
    [SerializeField]
    private PaintWithMouse wallController;

    [SerializeField]
    private float paintingPosZ;
    [SerializeField]
    private float holdingPosZ;
    [SerializeField]
    private float moveSpeed = 1f;
    [SerializeField]
    private float rotationSpeed = 1f;

    private bool canPaint;
    private bool isPainting;
    private Quaternion targetRotation = Quaternion.identity;

    void Start()
    {
        paintRoller = this.transform;
        GameManager.instance.OnGameStateChanged += CheckPaint;
        CheckPaint(GameManager.instance.GetState());
    }

    void Update()
    {
        if (canPaint)
        {
            isPainting = Mouse.current.leftButton.isPressed;

            Vector3 mousePos = Mouse.current.position.ReadValue();
            Ray ray = mainCam.ScreenPointToRay(mousePos);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100.0f, LayerMask.GetMask("Wall")))
            {
                MoveToPos(hit.point);
                if (isPainting)
                {
                    Ray paintRay = new Ray(paintRoller.position, Vector3.forward);
                    Debug.DrawRay(paintRoller.position, Vector3.forward);
                    RaycastHit paintHit;
                    if (Physics.Raycast(paintRay, out paintHit, 100.0f, LayerMask.GetMask("Wall")))
                    {
                        Debug.Log("hit");
                        wallController.PaintWall(paintHit.textureCoord, 0f);
                    }
                }
            }
        }
    }

    private void MoveToPos(Vector3 pos)
    {
        //TODO: clamp edges
        if (isPainting)
        {
            Vector3 targetPos = new Vector3(pos.x, pos.y, paintingPosZ);

            // Vector3 targetPosNoRot = new Vector3(pos.x, pos.y, paintRoller.position.z);
            // Vector3 targetDirection = targetPosNoRot - paintRoller.position;
            // targetDirection.z = 0;
            // if (targetDirection != Vector3.zero)
            // {
            //     targetRotation = Quaternion.LookRotation(targetDirection, Vector3.back);
            // }
            // transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            Vector2 lerpPos = Vector2.Lerp(paintRoller.position, targetPos, moveSpeed * Time.deltaTime);
            paintRoller.position = new Vector3(lerpPos.x, lerpPos.y, paintingPosZ);
        }
        else
        {
            paintRoller.position = new Vector3(pos.x, pos.y, holdingPosZ);
        }
    }

    private void CheckPaint(GameState _state)
    {
        canPaint = _state == GameState.PAINTING;
        paintRoller.gameObject.SetActive(canPaint);
    }
}
