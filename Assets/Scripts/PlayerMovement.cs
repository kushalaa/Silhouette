using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using TMPro;
using System;

public class PlayerMovement : MonoBehaviour
{
    public static readonly string POLY_TAG = "Poly";
    public static readonly string GHOST_POLY_TAG = "GhostPoly";
    public static readonly string GHOST_BOX_TAG = "GhostBox";
    
    public static event Action ButtonClickEvent;
    private GameObject selectedPoly = null;
    public Tilemap tileMap = null;
    public const float timeToMove = 0.2f;
    public const int gridSize = 10;
    public const float spinSpeed = 20;
    private bool isMoving = false;
    private Vector3 oldPos;
    private Vector3 targetPos;


    private Vector3Int UP = new Vector3Int(1, 0, 0);
    private Vector3Int DOWN = new Vector3Int(-1, 0, 0);
    private Vector3Int LEFT = new Vector3Int(0, 0, 1);
    private Vector3Int RIGHT = new Vector3Int(0, 0, -1);

    private Vector3 CLOCKWISE = 90 * Vector3.up;
    private Vector3 COUNTERCLOCKWISE = -90 * Vector3.up;

    public Button moveUpButton;
    public Button moveDownButton;
    public Button moveLeftButton;
    public Button moveRightButton;
    public Button rotateClockwiseButton;
    public Button rotateCounterclockwiseButton;
    public TextMeshProUGUI hintsCountText;

// Used in tutorial level to allow specific button to be enabled
    private Vector3Int allowMovement;
    private bool enableTutorial;
    private Vector3 allowRotate;
    private Dictionary<int, GameObject> polyToGhostMap;
    private float timeBetweenMoves = 0;

    public static int numHints = 3;
    private GameObject solutionManager;

    public System.Action checkForSolution;

    public GameObject SelectedPoly
    {
        get
        {
            return selectedPoly;
        }

        set
        {
            selectedPoly = value;
            CheckPossibleMoves();
        }
    }

    ColorBlock btnColor;
    ColorBlock resetBtnColor;
    Color normalClr;
    Color highLightedClr;
    Color pressedClr;
    Color selectedClr;
    Color disabledClr;
    string activeBtn;

    // Start is called before the first frame update
    void Start()
    {
        enableTutorial = false;
        polyToGhostMap = new Dictionary<int, GameObject>();
        var polys = GameObject.FindGameObjectsWithTag(POLY_TAG);
        foreach (var poly in polys)
        {
            var ghostPoly = Instantiate(poly);
            ghostPoly.tag = GHOST_POLY_TAG;
            ghostPoly.name = $"Ghost {poly.name}";

            for (int i = 0; i < ghostPoly.transform.childCount; ++i)
            {
                GameObject child = ghostPoly.transform.GetChild(i).gameObject;
                child.tag = GHOST_BOX_TAG;
                child.transform.localScale = child.transform.localScale * 0.5f; // halve the cube's scale to avoid edge collisions
                Renderer r = child.GetComponent<Renderer>();
                r.enabled = false;
            }

            foreach (Renderer r in ghostPoly.GetComponentsInChildren(typeof(Renderer)))
            {
                r.enabled = false;
            }

            polyToGhostMap.Add(poly.GetInstanceID(), ghostPoly);
        }
        CheckPossibleMoves();

        solutionManager = GameObject.Find("SolutionManager");

        btnColor = moveUpButton.colors;
        resetBtnColor = moveUpButton.colors;
        normalClr = new Color(0.8962264f, 0.7789694f, 0.5030705f);
        highLightedClr = new Color(1.0f, 0.9425556f, 0.8066038f);
        pressedClr = new Color(0.7830189f, 0.7165362f, 0.5503293f);
        selectedClr = new Color(0.8980393f, 0.7803922f, 0.5019608f);
        disabledClr = new Color(0.7843137f, 0.7843137f, 0.7843137f);        

        btnColor.highlightedColor = highLightedClr;
        btnColor.pressedColor = pressedClr;
        btnColor.selectedColor = selectedClr;
        btnColor.disabledColor = disabledClr; 

        resetBtnColor.normalColor = normalClr;
        resetBtnColor.highlightedColor = highLightedClr;
        resetBtnColor.pressedColor = pressedClr;
        resetBtnColor.selectedColor = selectedClr;
        resetBtnColor.disabledColor = disabledClr; 
    }


    private void Update()
    {
        timeBetweenMoves += Time.deltaTime;

        if (Input.GetKey(KeyCode.W) && CanMove(UP))
        {
            MoveBoxUp();

        }
        if (Input.GetKey(KeyCode.A) && CanMove(LEFT))
        {
            MoveBoxLeft();

        }
        if (Input.GetKey(KeyCode.S) && CanMove(DOWN))
        {
            MoveBoxDown();

        }
        if (Input.GetKey(KeyCode.D) && CanMove(RIGHT))
        {
            MoveBoxRight();

        }

        if (Input.GetKey(KeyCode.Q) && CanRotate(COUNTERCLOCKWISE))
        {
            CounterClockwiseRotate();

        }
        if (Input.GetKey(KeyCode.E) && CanRotate(CLOCKWISE))
        {
            ClockwiseRotate();
        }



        hintsCountText.text = "Hints Left: " + numHints;
    }

    public void setAllowMovement(Vector3Int key) {
        allowMovement = key;
    }

    public void setEnableTutorial(bool val) {
        enableTutorial = val;
    }
    
    public void setAllowRotate(Vector3 nxt) {
        allowRotate = nxt;
    }
    /******* Move *******/

    // Computed once after a move or rotate
    // "Tests" which moves are possible and sets the interactive attribute on each respective button
    private void CheckPossibleMoves()
    {
        moveUpButton.interactable = CanMove(UP);
        moveDownButton.interactable = CanMove(DOWN);
        moveLeftButton.interactable = CanMove(LEFT);
        moveRightButton.interactable = CanMove(RIGHT);
        rotateClockwiseButton.interactable = CanRotate(CLOCKWISE);
        rotateCounterclockwiseButton.interactable = CanRotate(COUNTERCLOCKWISE);
    }

    public void MoveBoxUp() //positive x
    {
        if (!isMoving && selectedPoly != null)
        {
            AnalyticsSender.SendTimeBetweenMovesEvent(Mathf.RoundToInt(timeBetweenMoves));
            timeBetweenMoves = 0;
            StartCoroutine(MoveBox(UP));
            Debug.Log("MOVE UP");
            // ButtonClickEvent?.Invoke();
        }
    }

    public void MoveBoxDown() //negative x
    {
        if (!isMoving && selectedPoly != null)
        {
            AnalyticsSender.SendTimeBetweenMovesEvent(Mathf.RoundToInt(timeBetweenMoves));
            timeBetweenMoves = 0;
            StartCoroutine(MoveBox(DOWN));
            Debug.Log("MOVE DOWN");
            // ButtonClickEvent?.Invoke();
        }
    }

    public void MoveBoxLeft() //positive z
    {
        if (!isMoving && selectedPoly != null)
        {
            AnalyticsSender.SendTimeBetweenMovesEvent(Mathf.RoundToInt(timeBetweenMoves));
            timeBetweenMoves = 0;
            StartCoroutine(MoveBox(LEFT));
            Debug.Log("MOVE LEFT");
            // ButtonClickEvent?.Invoke();
        }
    }

    public void MoveBoxRight() //negative z
    {
        if (!isMoving && selectedPoly != null)
        {
            AnalyticsSender.SendTimeBetweenMovesEvent(Mathf.RoundToInt(timeBetweenMoves));
            timeBetweenMoves = 0;
            StartCoroutine(MoveBox(RIGHT));
            Debug.Log("MOVE RIGHT");
            // ButtonClickEvent?.Invoke();
        }
    }

    private IEnumerator MoveBox(Vector3Int dir)
    {
        ButtonClickEvent?.Invoke();
        isMoving = true;
        PlayerData.NumberOfMoves += 1;

        float elapsedTime = 0;

        oldPos = selectedPoly.transform.position;
        targetPos = oldPos + dir * gridSize;

        while (elapsedTime < timeToMove)
        {
            selectedPoly.transform.position = Vector3.Lerp(oldPos, targetPos, (elapsedTime / timeToMove));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // the final position should be exactly targetPos at the end of the animation
        selectedPoly.transform.position = targetPos;
        isMoving = false;
        CheckPossibleMoves();
        checkForSolution?.Invoke();
    }

    /******* Rotate *******/

    private IEnumerator RotateBox(Vector3 dir)
    {
        ButtonClickEvent?.Invoke();
        isMoving = true;
        PlayerData.NumberOfRotations += 1;

        float elapsedTime = 0;

        Quaternion startRotation = selectedPoly.transform.rotation;
        Quaternion targetRotation = selectedPoly.transform.rotation * Quaternion.Euler(dir);
        while (elapsedTime < timeToMove)
        {
            selectedPoly.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsedTime / timeToMove);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        selectedPoly.transform.rotation = targetRotation;
        isMoving = false;
        CheckPossibleMoves();
        checkForSolution?.Invoke();
    }


    public void ClockwiseRotate()
    {
        if (!isMoving && selectedPoly != null)
        {
            AnalyticsSender.SendTimeBetweenMovesEvent(Mathf.RoundToInt(timeBetweenMoves));
            timeBetweenMoves = 0;
            StartCoroutine(RotateBox(CLOCKWISE));
            Debug.Log("MOVE CLOCKWISE");
            // ButtonClickEvent?.Invoke();
        }
    }

    public void CounterClockwiseRotate()
    {
        if (!isMoving && selectedPoly != null)
        {
            AnalyticsSender.SendTimeBetweenMovesEvent(Mathf.RoundToInt(timeBetweenMoves));
            timeBetweenMoves = 0;
            StartCoroutine(RotateBox(COUNTERCLOCKWISE));
            Debug.Log("MOVE ANTICLOCKWISE");
            // ButtonClickEvent?.Invoke();
        }
    }

    /******* Collision *******/

    // tests if the given Box transform is colliding with something
    // true if colliding with a wall or another box
    private bool IsCubeColliding(Transform ghostTransform)
    {
        GameObject ghostPoly = ghostTransform.gameObject;

        for (int i = 0; i < ghostTransform.childCount; ++i)
        {
            GameObject ghostBox = ghostTransform.GetChild(i).gameObject;
            var hits = Physics.BoxCastAll(ghostBox.transform.position, Vector3.one, ghostBox.transform.forward, ghostBox.transform.rotation, 0);
            foreach (var hit in hits)
            {
                GameObject hitParent = hit.transform?.parent?.gameObject ?? null;

                // ignore colliding with itself, a ghost cube, or the shadows
                if (hit.transform.gameObject.CompareTag(Wall.SHADOW_TAG) || hitParent == selectedPoly || hit.transform.gameObject.CompareTag(GHOST_BOX_TAG))
                {
                    continue;
                }

                return true;
            }
        }
        return false;
    }

    private bool CanMove(Vector3Int dir)
    {
        if (selectedPoly == null)
        {
            return false;
        }

        int instanceID = selectedPoly.GetInstanceID();
        var ghostPoly = polyToGhostMap[instanceID];
        ghostPoly.transform.position = selectedPoly.transform.position;
        ghostPoly.transform.rotation = selectedPoly.transform.rotation;

        // apply the target position to the poly and test for collisions
        var targetPos = selectedPoly.transform.position + dir * gridSize;
        ghostPoly.transform.position = targetPos;

        return !IsCubeColliding(ghostPoly.transform) && (allowMovement == dir || !enableTutorial);
    }

    private bool CanRotate(Vector3 dir)
    {
        if (selectedPoly == null)
        {
            return false;
        }

        int instanceID = selectedPoly.GetInstanceID();
        var ghostPoly = polyToGhostMap[instanceID];
        ghostPoly.transform.position = selectedPoly.transform.position;
        ghostPoly.transform.rotation = selectedPoly.transform.rotation;

        // apply the target rotation to the poly and test for collisions
        Quaternion targetRotation = selectedPoly.transform.rotation * Quaternion.Euler(dir);
        ghostPoly.transform.rotation = targetRotation;

        return !IsCubeColliding(ghostPoly.transform) && (allowRotate == dir || !enableTutorial);
    }


    /******* Hints *******/
    public void UseAHint()
    {
        if (numHints > 0 && solutionManager.GetComponent<Hints>().ShowAHint())
        {
            numHints -= 1;
        }
    }

    public void IncHintsCount()
    {
        numHints++;
    }

    public void BlinkBtn(string actBtn, string rstBtn) {

        activeBtn = actBtn;
        ResetBtnColors(rstBtn);
        CancelInvoke();
        InvokeRepeating("Blink", 0.0f, 0.5f);
    }

    public void ResetBtnColors(string resetBtn) {
        switch(resetBtn)
        {
            case "up":
                moveUpButton.colors = resetBtnColor;
                break;
            case "down":
                moveDownButton.colors = resetBtnColor;
                break;
            case "left":
                moveLeftButton.colors = resetBtnColor;
                break;
            case "right":
                moveRightButton.colors = resetBtnColor;
                break;
            case "clockwise":
                rotateClockwiseButton.colors = resetBtnColor;
                break;
            case "antiClockwise":
                rotateCounterclockwiseButton.colors = resetBtnColor;
                break;
            default:
                break;
        }
    }
 
    public void ResetMovementControl() {
        CancelInvoke();
        btnColor.normalColor = normalClr;
        moveUpButton.colors = btnColor;
        moveDownButton.colors = btnColor;
        moveLeftButton.colors = btnColor;
        moveRightButton.colors = btnColor;
        rotateClockwiseButton.colors = btnColor;
        rotateCounterclockwiseButton.colors = btnColor;
    }

    public void Blink() {
        moveUpButton.interactable = false;
        moveDownButton.interactable = false;
        moveLeftButton.interactable = false;
        moveRightButton.interactable = false;
        rotateClockwiseButton.interactable = false;
        rotateCounterclockwiseButton.interactable = false;
        if(btnColor.normalColor == normalClr) {
            btnColor.normalColor = highLightedClr;
        }
        else {
            btnColor.normalColor = normalClr;
        }
        switch(activeBtn)
        {
            case "up":
                moveUpButton.interactable = true;
                moveUpButton.colors = btnColor;
                break;
            case "down":
                moveDownButton.interactable = true;
                moveDownButton.colors = btnColor;
                break;
            case "left":
                moveLeftButton.interactable = true;
                moveLeftButton.colors = btnColor;
                break;
            case "right":
                moveRightButton.interactable = true;
                moveRightButton.colors = btnColor;
                break;
            case "clockwise":
                rotateClockwiseButton.interactable = true;
                rotateClockwiseButton.colors = btnColor;
                break;
            case "antiClockwise":
                rotateCounterclockwiseButton.interactable = true;
                rotateCounterclockwiseButton.colors = btnColor;
                break;
        }
    }
}
