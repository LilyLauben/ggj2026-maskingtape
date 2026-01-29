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
    [SerializeField] private bool hasSelectedStartingPoint = false; // Once in cut mode, you move the mouse up and down to indicate
    // where you'd like to start the cut
    [SerializeField] private bool hasSelectedAngle = false; // After selecting starting point, you then select angle of cut

    [Header("Ripping UI")]
    public GameObject cutSlider;
    public GameObject cutAngleRotator;
    public GameObject mashText;

    [Header("Rip Controls")]
    [SerializeField] private float positionSensitivity = 0.5f;
    [SerializeField] private Vector2 positionBounds = new Vector2(-20f, 20f);
    [SerializeField] private float angleSensitivity = 0.5f;
    [SerializeField] private Vector2 angleBounds = new Vector2(-45f, 45f);
    [SerializeField] private int currentButtonPresses = 0;
    [SerializeField] private float mashTimerDuration = 2.0f; // Duration for the mash sequence in seconds
    [SerializeField] private int minButtonPresses = 2; // Minimum presses for a successful rip



    private void Awake()
    {
        playerInputActions = new PlayerInput();
        var playerInputMap = playerInputActions.Player;

        playerInputMap.PrimaryClick.canceled += _ => OnPrimaryClick();

        playerInputMap.MouseScroll.performed += ctx =>
        {
            Vector2 val = ctx.ReadValue<Vector2>();
            float scrollDelta = val.y;
            MoveTapeRoll(scrollDelta);
        };

        playerInputMap.InitiateCut.performed += _ => OnCutMode();

        // This action will be used to read the mouse delta for both positioning and angle selection
        playerInputActions.Player.CutAngle.Enable();
    }

    public void Start()
    {
        mainCamera = Camera.main;
        cutSlider.SetActive(false);
        cutAngleRotator.SetActive(false);
    }

    private void Update()
    {
        if (inCuttingMode)
        {
            Vector2 mouseDelta = playerInputActions.Player.CutAngle.ReadValue<Vector2>();

            if (!hasSelectedStartingPoint)
            {
                UpdateCutPosition(mouseDelta.y);
            }
            else if (!hasSelectedAngle)
            {
                UpdateCutAngle(mouseDelta.y);
            }
        }
    }

    private void UpdateCutPosition(float deltaY)
    {
        if (cutSlider == null) return;

        // Calculate the new Y position based on mouse delta and sensitivity
        float newY = cutSlider.transform.localPosition.y + deltaY * positionSensitivity;

        // Clamp the new position within the defined bounds
        newY = Mathf.Clamp(newY, positionBounds.x, positionBounds.y);

        // Apply the new local position
        cutSlider.transform.localPosition = new Vector3(cutSlider.transform.localPosition.x, newY, cutSlider.transform.localPosition.z);
    }

    private void UpdateCutAngle(float deltaY)
    {
        if (cutAngleRotator == null) return;

        // Calculate the new Z angle based on mouse delta and sensitivity
        // Adding to the current angle allows for continuous adjustment
        float currentAngle = cutAngleRotator.transform.localEulerAngles.z;
        // Normalize angle to be within -180 to 180 for correct clamping
        if (currentAngle > 180) currentAngle -= 360;

        float newAngle = currentAngle + deltaY * angleSensitivity;

        // Clamp the new angle within the defined bounds
        newAngle = Mathf.Clamp(newAngle, angleBounds.x, angleBounds.y);

        // Apply the new local rotation
        cutAngleRotator.transform.localEulerAngles = new Vector3(0, 0, newAngle);
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
            cutSlider.SetActive(true);
            inCuttingMode = true;
            return;
        }
        else //'C' does nothing if we are already in cut mode
        {
            return;
        }            
    }

    private void OnPrimaryClick()
    {
        if (!hasTapeBeenPlaced || !inCuttingMode) return; // Prevent multiple cuts without placing new tape
        if (!hasSelectedStartingPoint)
        {
            hasSelectedStartingPoint = true;
            cutAngleRotator.SetActive(true);
        }
        else if (!hasSelectedAngle)
        {
            hasSelectedAngle = true;
            mashText.SetActive(true);
            StartCoroutine(MashTimerCoroutine());
        }
        else
        {
            // This block runs during the mash timer
            currentButtonPresses++;
            Debug.Log($"Button presses: {currentButtonPresses}");
        }
    }

    private IEnumerator MashTimerCoroutine()
    {
        Debug.Log("Mash sequence started. Click!");
        yield return new WaitForSeconds(mashTimerDuration);
        Debug.Log($"Mash sequence ended. Total clicks: {currentButtonPresses}");

        // Start the cut based on the position of the cutSlider, angle of the cutRotator, and the amount of button presses
        StartCut();

        ResetCutting(); 
    }

    public void ResetCutting()
    {
        inCuttingMode = false;
        hasSelectedStartingPoint = false;
        hasSelectedAngle = false;
        currentButtonPresses = 0;
        
        mashText.SetActive(false);
        cutSlider.transform.localPosition = new Vector3(cutSlider.transform.localPosition.x, 0, cutSlider.transform.localPosition.z);
        cutSlider.SetActive(false);
        cutAngleRotator.transform.transform.localEulerAngles = new Vector3(0, 0, 0);
        cutAngleRotator.SetActive(false);
    }

    private void StartCut()
    {
        if (currentButtonPresses < minButtonPresses)
        {
            Debug.Log("Rip failed! Not enough button presses.");
            // No tape is generated, and the state is reset in ResetCutting()
            return;
        }

        float cutSliderY = cutSlider.transform.localPosition.y / 55f; // Scale factor to match tape roll movement
        float totalY = tapeRoll.transform.position.y + cutSliderY; //How tall the tape piece should be
        //55 is considered an "inch"
        
        float angle = cutAngleRotator.transform.localEulerAngles.z;
        Debug.Log($"Angle of cut is {angle}");
        List<Vector3> newJaggedEdge = GenerateJaggedEdge(angle, currentButtonPresses);

        // 4. Get the starting jagged edge from the previous tape piece
        List<Vector3> startJaggedEdge = new List<Vector3>();
        if (currentTape != null)
        {
            startJaggedEdge = currentTape.GetComponent<Tape>().GetLastJaggedEdge();
        }

        //Generate the tape piece
        GenerateTape(totalY, newJaggedEdge, startJaggedEdge);
        if (currentTape == null) throw new System.Exception("Failed to generate tape piece on cut.");

        //Update the tape roll to make it look like it was cut
        CreateTapeRollPiece(totalY);
    }

    private List<Vector3> GenerateJaggedEdge(float angle, int presses)
    {
        List<Vector3> jaggedEdge = new List<Vector3>();
        int segments = 10; // Number of points in the jagged edge
        float tapeWidth = tapePrefab.GetComponent<Tape>().getTapeWidth(); // The width of the tape
        float maxJaggedness = 0.2f + (presses * 0.05f); // More presses = more dramatic edge
        float slope = Mathf.Tan(angle * Mathf.Deg2Rad);
        float xDiff = tapeWidth / segments;
        for (int i = 0; i <= segments; i++)
        {
            // Calculate the x position, starting from the left edge of the tape
            float t = (float)i / segments;
            float x = Mathf.Lerp(-tapeWidth / 2, tapeWidth / 2, t);

            float y_baseline = x * slope;

            float y_jagged = Random.Range(-maxJaggedness, maxJaggedness);

            float y = y_baseline + y_jagged;

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