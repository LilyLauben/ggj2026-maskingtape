using System.Collections;
using System.Collections.Generic;
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
    public TapeBounds tapeLeftBounds;
    public TapeBounds tapeRightBounds;
    public GameObject currentTapeBoundEntered = null;
    public Vector2 mouseEnterPosition;
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
        };

        playerInputMap.InitiateCut.performed += _ => OnCutMode();

        // This action will be used to read the mouse delta for both positioning and angle selection
        playerInputActions.Player.CutAngle.Enable();

        tapeLeftBounds.SetTapeController(this);
        tapeRightBounds.SetTapeController(this);
    }

    public void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (isMouseInsideBounds)
        {
            mouseEnterTime += Time.deltaTime;
        }
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
                StartCut(mouseEnterTime, mouseEnterPosition, position);
                currentTapeBoundEntered = null;
                isMouseInsideBounds = false;
            }
        }
        else
        {
            currentTapeBoundEntered = null;
            isMouseInsideBounds = false;
            mouseEnterTime = 0f;
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
            hasTapeBeenPlaced = true; // THIS IS FOR TESTING PURPOSES ONLY! THIS SHOULD ONLY BE TRUE WHEN YOU 
            //ACTUALLY PLACE IT
            inCuttingMode = true;
            return;
        }
        else //'C' does nothing if we are already in cut mode
        {
            return;
        }            
    }

    private void StartCut(float duration, Vector2 startPosition, Vector2 endPosition)
    {
        if(duration < 0.25f)
        {
            duration = 0.25f;
            mouseEnterTime = 0f;
        }
        if (duration > 2f)
        {
            Debug.Log("Rip failed! You took too long to swipe.");
        }
        Vector2 startPosConverted = new Vector2(startPosition.x, (startPosition.y / Screen.height) * 10f);
        Vector2 endPosConverted = new Vector2(endPosition.x, (endPosition.y / Screen.height) * 10f);

        float slope = (endPosition.y - startPosition.y) / (endPosition.x - startPosition.x);
        float tapeLength = tapeRoll.transform.position.y - Mathf.Min(startPosConverted.y, endPosConverted.y);

        List <Vector3> newJaggedEdge = GenerateJaggedEdge(slope, duration, tapeLength);

        // 4. Get the starting jagged edge from the previous tape piece
        List<Vector3> startJaggedEdge = new List<Vector3>();
        if (currentTape != null)
        {
            startJaggedEdge = currentTape.GetComponent<Tape>().GetLastJaggedEdge();
        }

        //Generate the tape piece
        GenerateTape(tapeLength, newJaggedEdge, startJaggedEdge);
        if (currentTape == null) throw new System.Exception("Failed to generate tape piece on cut.");

        //Update the tape roll to make it look like it was cut
        if(!tapeTearSound.IsNull)
        {
            FMODUnity.RuntimeManager.PlayOneShot(tapeTearSound.Guid);
        }

        CreateTapeRollPiece(tapeLength);
    }

    private List<Vector3> GenerateJaggedEdge(float slope, float duration, float heightOfTape)
    {
        List<Vector3> jaggedEdge = new List<Vector3>();
        int segments = 10; // Number of points in the jagged edge
        float tapeWidth = tapePrefab.GetComponent<Tape>().getTapeWidth(); // The width of the tape
        float maxJaggedness = 0.2f + (1 / (duration * 10)); // Faster = more dramatic edge
        float xDiff = tapeWidth / segments;
        for (int i = 0; i <= segments; i++)
        {
            // Calculate the x position, starting from the left edge of the tape
            float t = (float)i / segments;
            float x = Mathf.Lerp(-tapeWidth / 2, tapeWidth / 2, t);

            float y_baseline = x * slope;

            float y_jagged = Random.Range(-maxJaggedness, maxJaggedness);

            float y = y_baseline + heightOfTape;
            if(slope >= 0)
            {
                y += y_jagged;
            }
            else
            {
                y -= y_jagged;
            }

            // Create a point and rotate it by the cut angle
            Vector3 point = new Vector3(x, y, 0);
            jaggedEdge.Add(point);
        }
        return jaggedEdge;
    }

    private void GenerateTape(float heightOfTape, List<Vector3> endJaggedEdge, List<Vector3> startJaggedEdge)
    {
        currentTape = Instantiate(tapePrefab, transform);
        if (currentTape != null)
        {
            // We need a new CreateTape method that accepts a jagged edge for the end
            currentTape.GetComponent<Tape>().CreateTape(heightOfTape, endJaggedEdge, startJaggedEdge);
            tapes.Add(currentTape);
            hasTapeBeenPlaced = false;
            currentTape.transform.position = new Vector3(mainCamera.transform.position.x - 5f, mainCamera.transform.position.y, 0);
        }
    }

    // This method uses the most recent cut piece of tape, takes its jagged edge (the bottom one) and inverts
    // it. This inverted jagged edge is then used as the starting edge for the new tape piece created on the tape roll.
    // The bottom of this new tape piece is just straight.
    
    public void CreateTapeRollPiece(float totalY)
    {
        List<Vector3> currentBottomJaggedEdge = currentTape.GetComponent<Tape>().GetLastJaggedEdge();
        List<Vector3> invertedJaggedEdge = new List<Vector3>();
        for (int i = 0; i < currentBottomJaggedEdge.Count; i++)
        {
            invertedJaggedEdge.Add(currentBottomJaggedEdge[i]);
        }
        // Destroy the existing currentTapeRollPiece before creating a new one
        Destroy(currentTapeRollPiece);

        currentTapeRollPiece = Instantiate(tapePrefab, transform);
        Vector3 startPos = new Vector3(0, 0, 0);
        Vector3 endPos = new Vector3(0, 3f, 0);
        currentTapeRollPiece.GetComponent<Tape>().CreateTapeRollPiece(startPos, endPos, invertedJaggedEdge, totalY);
        currentTapeRollPiece.transform.parent = tapeRoll.transform;
        currentTapeRollPiece.transform.Rotate(0, 0, 180, Space.Self);
        currentTapeRollPiece.transform.localPosition = new Vector3(currentTapeRollPiece.transform.position.x, currentTapeRollPiece.transform.position.y, -5.3f);
    }

    public void MoveTapeRoll(float scrollDelta)
    {
        // Adjust the tape roll's position based on scroll input
        tapeRoll.MoveTape(scrollDelta);
    }
}