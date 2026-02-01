using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class TapeController : MonoBehaviour
{
    [SerializeField] private GameObject tapePrefab;
    [SerializeField] private TapeRoll tapeRoll; // Assign your TapeRoll object in the Inspector
    [SerializeField] private List<GameObject> tapes = new List<GameObject>();
    [SerializeField] private GameObject currentTape = null;
    [SerializeField] private GameObject currentTapeRollPiece = null; //The fake tape piece that sits on top of the roll to show jagged edge
    private Camera mainCamera;
    private PlayerInput playerInputActions;
    [SerializeField] private bool hasTapeBeenPlaced = true; //Used to determine if the current cut tape piece has been cut. Starts at true.

    [SerializeField] private bool inCuttingMode = false;
    [SerializeField] private FMODUnity.EventReference tapeTearSound;

    [Header("Mouse Controls for Ripping")]
    public TapeBounds tapeBounds;
    public GameObject currentTapeBoundEntered = null;
    public Vector2 mouseEnterPosition;
    public Vector2 mouseExitPosition; // Store the exit position
    bool isMouseInsideBounds = false;
    public float mouseEnterTime;

    private void Awake()
    {
        playerInputActions = new PlayerInput();
        var playerInputMap = playerInputActions.Player;

        playerInputMap.MouseScroll.performed += ctx =>
        {
            Vector2 val = ctx.ReadValue<Vector2>();
            float scrollDelta = val.y;
            MoveTapeRoll(scrollDelta);
            RotateTape(scrollDelta);
        };

        playerInputMap.InitiateCut.performed += _ => OnCutMode();

        playerInputMap.PlaceTape.performed += _ => OnPlaceTape();

        // This action will be used to read the mouse delta for both positioning and angle selection
        playerInputActions.Player.CutAngle.Enable();

        tapeBounds.SetTapeController(this);
    }

    public void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if(GameManager.instance.GetState() == GameState.TAPING)
        {
            OnCutMode();
        }
        else
        {
            inCuttingMode = false;
            tapeRoll.gameObject.SetActive(false);
            if(currentTape != null)
            {
                Destroy(currentTape.gameObject);
            }
            currentTape = null;
        }
        if (isMouseInsideBounds)
        {
            mouseEnterTime += Time.deltaTime;
        }
        if(!hasTapeBeenPlaced && currentTape != null)
        {
            // Convert mouse position to world coordinates
            Vector2 screenPosition = Mouse.current.position.ReadValue();

            // Set distance from camera - adjust this value to control how far from camera the tape appears
            float distanceFromCamera = 8f; 

            // Convert screen position to world position
            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, distanceFromCamera));

            // Make the tape follow the mouse
            currentTape.GetComponent<Tape>().MoveTapeWithCursor(worldPosition);

        }
    }

    public void OnPlaceTape()
    {
        // Only process click if we have a tape that hasn't been placed yet
        if (!hasTapeBeenPlaced && currentTape != null)
        {
            PlaceTapeOnWall();
        }
    }
    private void PlaceTapeOnWall()
    {
        // Get mouse position
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        // Create a ray from the camera through the mouse position
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);

        // Perform raycast to find walls
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            // Check if we hit an object with the "Wall" tag
            if (hit.collider.CompareTag("Wall"))
            {
                // Place the tape at the hit point
                PlaceTapeAtPosition(hit.point, hit.normal);
                currentTape.transform.parent = hit.collider.transform; //Parent the tape to the wall it was placed on
                currentTape.layer = LayerMask.NameToLayer("Tape"); //Change layer to Tape
                hasTapeBeenPlaced = true;
                //currentTape = null;

            }
            else
            {
                Debug.Log("Clicked object is not tagged as 'Wall'. Found tag: " + hit.collider.tag);
            }
        }
        else
        {
            Debug.Log("No object hit by raycast");
        }
    }

    private void PlaceTapeAtPosition(Vector3 position, Vector3 surfaceNormal)
    {
        if (currentTape == null) return;

        // Move the tape to the hit position
        currentTape.transform.position = position;

        // Optional: Add a small offset from the wall to prevent z-fighting
        currentTape.transform.position += surfaceNormal * 0.01f;
    }
    public void UpdateTapeBound(TapeBounds tapeBound, Vector2 position)
    {
        if (inCuttingMode)
        {
            if (currentTapeBoundEntered == null)
            {
                currentTapeBoundEntered = tapeBound.gameObject;
                mouseEnterTime = 0f;
                mouseEnterPosition = position;
                isMouseInsideBounds = true;
                return;
            }
            if (currentTapeBoundEntered == tapeBound.gameObject)
            {
                return;
            }
            else
            {
                // Note: This will be called when moving between bounds, but the actual cut
                // will be triggered by OnMouseExitTapeBounds when leaving the tape area entirely
                return;
            }
        }
        else
        {
            currentTapeBoundEntered = null;
            isMouseInsideBounds = false;
            mouseEnterTime = 0f;
        }
    }

    public void OnMouseExitTapeBounds(Vector2 exitPosition)
    {
        if (inCuttingMode && currentTapeBoundEntered != null && isMouseInsideBounds && hasTapeBeenPlaced)
        {
            mouseExitPosition = exitPosition;
            StartCut(mouseEnterTime, mouseEnterPosition, mouseExitPosition);
            currentTapeBoundEntered = null;
            isMouseInsideBounds = false;
        }
    }

    private void OnEnable()
    {
        playerInputActions.Enable();
    }

    private void OnDisable()
    {
        playerInputActions.Disable();
    }

    private void OnCutMode()
    {
        Debug.Log("Hit C for Cut Mode");
        if (!inCuttingMode)
        {
            inCuttingMode = true;
            tapeRoll.gameObject.SetActive(true); //Make tape roll visible
            return;
        }
        else //'C' does nothing if we are already in cut mode
        {
            return;
        }
    }

    private void StartCut(float duration, Vector2 startPosition, Vector2 endPosition)
    {
        if (duration < 0.25f)
        {
            duration = 0.25f;
            mouseEnterTime = 0f;
        }
        if (duration > 2f)
        {
            Debug.Log("Rip failed! You took too long to swipe.");
            return;
        }

        // Calculate slope for jagged edge
        float slope = (endPosition.y - startPosition.y) / (endPosition.x - startPosition.x);

        // Calculate tape length based on tape roll position and mouse exit position
        float tapeLength = CalculateTapeLength(endPosition);
        Debug.Log($"Slope: {slope}, Duration: {duration}, Tape Length: {tapeLength}");
        // Determine the spawn position for the new tape piece
        Vector3 spawnPosition = GetNextTapeSpawnPosition();

        // Generate jagged edge in local coordinates relative to spawn position
        List<Vector3> newJaggedEdge = GenerateJaggedEdge(slope, duration, tapeLength, spawnPosition);

        // Get the starting jagged edge from the previous tape piece
        List<Vector3> startJaggedEdge = new List<Vector3>();
        if (currentTape != null)
        {
            Tape prevTape = currentTape.GetComponent<Tape>();
            List<Vector3> localStartJaggedEdge = prevTape.GetLastJaggedEdge();
            // Transform points from previous tape's local space to world space
            for (int i = 0; i < localStartJaggedEdge.Count; i++)
            {
                Vector3 worldPoint = currentTape.transform.TransformPoint(localStartJaggedEdge[i]);
                startJaggedEdge.Add(worldPoint);
            }
        }

        //Generate the tape piece, set currentTape to it
        GenerateTape(tapeLength, newJaggedEdge, startJaggedEdge, spawnPosition);
        if (currentTape == null) throw new System.Exception("Failed to generate tape piece on cut.");

        //Update the tape roll to make it look like it was cut
        if (!tapeTearSound.IsNull)
        {
            FMODUnity.RuntimeManager.PlayOneShot(tapeTearSound.Guid);
        }

        CreateTapeRollPiece(tapeLength);

        hasTapeBeenPlaced = false;
        inCuttingMode = false;
        //Turn off tape roll visibility until tape is placed
        tapeRoll.gameObject.SetActive(false);
    }

    private float CalculateTapeLength(Vector2 mouseExitScreenPosition)
    {
        // Get the tape roll's top position (where the tape starts)
        Transform tapeRollTop = tapeRoll.GetTapeRollTopPieceLocation();
        Vector3 tapeRollTopWorldPos = tapeRollTop.position;

        // Convert the tape roll top position to screen coordinates
        Vector2 tapeRollTopScreenPos = mainCamera.WorldToScreenPoint(tapeRollTopWorldPos);

        // Calculate the distance in screen space
        float screenDistance = tapeRollTopScreenPos.y - mouseExitScreenPosition.y;

        // Convert screen distance to world distance
        // We need to convert this properly based on camera distance and orthographic size
        float worldDistance;
        if (mainCamera.orthographic)
        {
            // For orthographic camera
            worldDistance = (screenDistance / Screen.height) * (mainCamera.orthographicSize * 2f);
        }
        else
        {
            // For perspective camera - need to account for depth
            float distanceToCamera = Vector3.Distance(mainCamera.transform.position, tapeRollTopWorldPos);
            float worldHeight = 2f * distanceToCamera * Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            worldDistance = (screenDistance / Screen.height) * worldHeight;
        }

        // Ensure minimum tape length
        return Mathf.Max(worldDistance, 0.5f);
    }

    private Vector3 GetNextTapeSpawnPosition()
    {
        // Calculate spawn position based on previous tape or default position
        if (currentTape != null)
        {
            // Position the new tape piece directly above the previous one
            Tape prevTape = currentTape.GetComponent<Tape>();
            List<Vector3> prevLastEdge = prevTape.GetLastJaggedEdge();
            if (prevLastEdge.Count > 0)
            {
                // Get the center point of the previous tape's last edge in world space
                Vector3 centerPoint = Vector3.zero;
                foreach (Vector3 point in prevLastEdge)
                {
                    centerPoint += currentTape.transform.TransformPoint(point);
                }
                centerPoint /= prevLastEdge.Count;
                return centerPoint;
            }
        }

        // Default position if no previous tape - use tape roll position as reference
        Transform tapeRollTop = tapeRoll.GetTapeRollTopPieceLocation();
        return new Vector3(tapeRollTop.position.x, tapeRollTop.position.y, tapeRollTop.position.z);
    }

    private List<Vector3> GenerateJaggedEdge(float slope, float duration, float heightOfTape, Vector3 basePosition)
    {
        List<Vector3> jaggedEdge = new List<Vector3>();
        int segments = 10; // Number of points in the jagged edge
        float tapeWidth = tapePrefab.GetComponent<Tape>().getTapeWidth(); // The width of the tape
        float maxJaggedness = 0.2f + (1 / (duration * 10)); // Faster = more dramatic edge

        for (int i = 0; i <= segments; i++)
        {
            // Calculate the x position, starting from the left edge of the tape
            float t = (float)i / segments;
            float x = Mathf.Lerp(-tapeWidth / 2, tapeWidth / 2, t);

            float y_baseline = x * slope;
            float y_jagged = Random.Range(-maxJaggedness, maxJaggedness);

            float y = y_baseline + heightOfTape;
            if (slope >= 0)
            {
                y += y_jagged;
            }
            else
            {
                y -= y_jagged;
            }

            // Generate in world coordinates
            Vector3 point = new Vector3(basePosition.x + x, basePosition.y + y, basePosition.z);
            jaggedEdge.Add(point);
        }
        return jaggedEdge;
    }

    private void GenerateTape(float heightOfTape, List<Vector3> endJaggedEdge, List<Vector3> startJaggedEdge, Vector3 spawnPosition)
    {
        currentTape = Instantiate(tapePrefab, transform);
        if (currentTape != null)
        {
            // Position the tape at the calculated spawn position
            currentTape.transform.position = spawnPosition;

            // Create the tape mesh with the jagged edges
            currentTape.GetComponent<Tape>().CreateTape(heightOfTape, endJaggedEdge, startJaggedEdge);
            tapes.Add(currentTape);
            hasTapeBeenPlaced = false;
        }
    }

    // This method uses the most recent cut piece of tape, takes its jagged edge (the bottom one) and inverts
    // it. This inverted jagged edge is then used as the starting edge for the new tape piece created on the tape roll.
    // The bottom of this new tape piece is just straight.

    public void CreateTapeRollPiece(float totalY)
    {
        if (currentTape == null) return;

        List<Vector3> currentBottomJaggedEdge = currentTape.GetComponent<Tape>().GetLastJaggedEdge();

        // Destroy the existing currentTapeRollPiece before creating a new one
        if (currentTapeRollPiece != null)
        {
            Destroy(currentTapeRollPiece);
        }

        currentTapeRollPiece = Instantiate(tapePrefab, transform);
        Vector3 startPos = new Vector3(0, 0, 0);
        Vector3 endPos = new Vector3(0, 3f, 0);

        // Position the tape roll piece first
        currentTapeRollPiece.transform.parent = tapeRoll.transform;
        currentTapeRollPiece.transform.localPosition = new Vector3(0, 0, -5.3f);
        currentTapeRollPiece.transform.localRotation = Quaternion.Euler(0, 0, 180);

        // Create the mesh using the jagged edge (already in local space)
        currentTapeRollPiece.GetComponent<Tape>().CreateTapeRollPiece(startPos, endPos, currentBottomJaggedEdge, totalY);
    }

    public void MoveTapeRoll(float scrollDelta)
    {
        if(!hasTapeBeenPlaced)
        {
            return; // Do not move the tape roll if the tape is following the mouse
        }
        // Adjust the tape roll's position based on scroll input
        tapeRoll.MoveTape(scrollDelta);
    }

    public void RotateTape(float scrollDelta)
    {
        if(currentTape != null && !hasTapeBeenPlaced)
        {
            float rotationSpeed = 5f;
            currentTape.transform.Rotate(0, 0, scrollDelta * rotationSpeed);
        }
    }
}