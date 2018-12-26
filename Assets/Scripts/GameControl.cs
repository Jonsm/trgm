using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameControl : MonoBehaviour {
    public static AnimationCounter animationLock = new AnimationCounter();
    public static bool debugMode = true;

    private static Formation[] formationsList = new Formation[]{
        new Formation(new int[] {0, 1, 0}),
        new Formation(new int[] {0, -1, 0}),
        new Formation(new int[] {0, 1, 2}),
        new Formation(new int[] {0, -1, 1}),
        new Formation(new int[] {0, -2, 1}),
        new Formation(new int[] {0, -2, 0})
    };

    public List<GameObject> markerObjects;
    public TreeGrid treeGrid;

    public float dropTime;
    public float dropAnimationTime;
    public float freeFallSpeed;
    public float bounceTime;
    public float bounceAmount;

    public float sideMoveRate;

    private Dictionary<TreeControl.TreeColor, GameObject> colorsToMarkers = 
        new Dictionary<TreeControl.TreeColor, GameObject>();

    private int width;
    private int height;
    private float gridSpacing;

    private float lastDropTime;
    private bool isDropping;
    private EasingFunction1D droppingFunction;

    private float freeFallStartTime;
    private float freeFallStartHeight;
    private float freeFallStopHeight;
    private bool isFreeFall;
    private bool stoppingFall;
    private EasingFunction1D stoppingEase;

    private float lastMoveTime;

    private bool keyboardLock;

    private Dictionary<GameObject, Vector2> markers = new Dictionary<GameObject, Vector2>();
    private float currentHeight;
    private int gridHeight;

    void Start() {
        TreeControl.TreeColor[] colors = (TreeControl.TreeColor[])System.Enum.GetValues(typeof(TreeControl.TreeColor));
        for (int i = 0; i < colors.Length; i++) {
            colorsToMarkers[colors[i]] = markerObjects[i];
        }

        width = treeGrid.width;
        height = treeGrid.height;
        gridSpacing = treeGrid.gridSpacing;
        droppingFunction = new BounceOnceEase(1 - bounceTime / dropAnimationTime, bounceAmount);
        float freeFallBounceStart = 1 / (1 + freeFallSpeed * bounceTime);
        stoppingEase = new BounceOnceEase(freeFallBounceStart, bounceAmount);
    }

    private void Update() {
        if (animationLock.counter == 0) {
            GenerateNewMarkers();
            keyboardLock = false;
            isFreeFall = false;
        } else {
            if (!keyboardLock) {
                if (Input.GetKey(KeyCode.DownArrow)) {
                    if (!isDropping && !isFreeFall) {
                        freeFallStartTime = Time.time;
                        freeFallStartHeight = currentHeight;
                        isFreeFall = true;
                    }
                } else {
                    if (Time.time - lastMoveTime > 1 / sideMoveRate) {
                        if (Input.GetKey(KeyCode.LeftArrow)) {
                            SideMove(-1);
                        }
                        if (Input.GetKey(KeyCode.RightArrow)) {
                            SideMove(1);
                        }
                    }

                    if (isFreeFall && !stoppingFall) {
                        stoppingFall = true;
                        freeFallStopHeight = currentHeight;
                    }
                }
            }

            if (Time.time - lastDropTime > dropTime && !isFreeFall) {
                isDropping = true;
                lastDropTime = Time.time;
            }

            if (isFreeFall) {
                if (stoppingFall) {
                    StopFallAnimation();
                } else {
                    FreeFallAnimation();
                }
            } else if (isDropping) {
                DropAnimation();
            }

            UpdateHeights();
        }

    }

    private void GenerateNewMarkers() {
        treeGrid.UpdateGrid();
        animationLock.Inc();
        currentHeight = height * gridSpacing - 1;
        gridHeight = height - 1;

        Formation formation = formationsList [Random.Range (0, formationsList.Length)];
        TreeControl.TreeColor[] dropColors = formation.GetColors ();
        Vector2 center = new Vector2(0, currentHeight);
        for (int i = 0; i < dropColors.Length; i++) {
            if (dropColors[i] != TreeControl.TreeColor.NONE) {
                int gridX = i - Mathf.FloorToInt(.5f * dropColors.Length);
                Vector2 position = center + Vector2.right * gridSpacing * gridX;
                GameObject marker = Instantiate(colorsToMarkers[dropColors[i]], position, Quaternion.identity);
                markers[marker] = new Vector2(gridX + width / 2, gridHeight);
                animationLock.Inc();
            }
        }
        lastDropTime = Time.time;
        treeGrid.DelayedRemove();
    }

    private void SideMove (int dir) {
        lastMoveTime = Time.time;
        Vector2 disp = new Vector2(dir, 0);
        List<GameObject> markerList = new List<GameObject>(markers.Keys);
        foreach (GameObject marker in markerList) {
            if ((markers[marker].x + dir >= width || markers[marker].x + dir < 0) ||
                treeGrid.Occupied(markers[marker] + disp))
                return;
        }

        foreach (GameObject marker in markerList) {
            marker.transform.position = marker.transform.position + (Vector3)(gridSpacing * disp);
            markers[marker] = markers[marker] + disp;
        }
    }

    private void DropAnimation() {
        if (Time.time - lastDropTime < dropAnimationTime) {
            float startY = gridHeight * gridSpacing;
            float endY = (gridHeight - 1) * gridSpacing;
            float t = Time.time - lastDropTime;
            currentHeight = droppingFunction.Lerp(startY, endY, t, dropAnimationTime);
        } else {
            UpdateGridHeight();
            isDropping = false;
        }
    }

    private void FreeFallAnimation() {
        currentHeight = freeFallStartHeight - freeFallSpeed * gridSpacing * (Time.time - freeFallStartTime);
        if (currentHeight < (gridHeight - 1) * gridSpacing) UpdateGridHeight();
    }

    private void StopFallAnimation() {
        float startOffset = (gridHeight * gridSpacing - freeFallStopHeight) / (gridSpacing * freeFallSpeed);
        float startTime = (freeFallStartHeight - freeFallStopHeight) / 
        (gridSpacing * freeFallSpeed) + freeFallStartTime;
        float totalTime = 1 / freeFallSpeed + bounceTime;
        float startHeight = gridHeight * gridSpacing;
        float endHeight = (gridHeight - 1) * gridSpacing;
        float t = startOffset + Time.time - startTime;

        if (t < totalTime) {
            currentHeight = stoppingEase.Lerp(startHeight, endHeight, t, totalTime);
        } else {
            currentHeight = endHeight;
            isFreeFall = false;
            stoppingFall = false;
            lastDropTime = Time.time - dropAnimationTime;
            UpdateGridHeight();
        }
    }

    private void UpdateHeights() {
        foreach (GameObject marker in markers.Keys) {
            marker.transform.position = new Vector2(marker.transform.position.x, currentHeight);
        }
        MakeGridChecks();
    }

    private void UpdateGridHeight() {
        gridHeight = gridHeight - 1;
        currentHeight = gridHeight * gridSpacing;
        List<GameObject> markerList = new List<GameObject>(markers.Keys);
        foreach (GameObject marker in markerList) {
            markers[marker] = new Vector2(markers[marker].x, gridHeight);
        }
        MakeGridChecks();
    }

    private void MakeGridChecks() {
        List<GameObject> markerList = new List<GameObject>(markers.Keys);
        foreach (GameObject markerObj in markerList) {
            Marker marker = markerObj.GetComponent<Marker>();
            int check = treeGrid.CheckNeighbors(markers[markerObj], marker);
            if (check != 0) {
                animationLock.Dec();
                markers.Remove(markerObj);
                Destroy(markerObj);

                if (!keyboardLock) {
                    keyboardLock = true;
                    if (!isFreeFall || stoppingFall) {
                        stoppingFall = false;
                        isFreeFall = true;
                        freeFallStartTime = Time.time;
                        freeFallStartHeight = currentHeight;
                    }
                }
            }
        }
    }
}
