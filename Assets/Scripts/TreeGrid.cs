using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeGrid : MonoBehaviour {
    public GameObject tree;
    public float gridSpacing;
    public int width;
    public int height;

    private TreeControl[,] pointsToTrees;
    private Dictionary<TreeControl, int> treesToRoots = new Dictionary<TreeControl, int>();
    private bool isDeleting;
    private HashSet<TreeControl> deletingSet = new HashSet<TreeControl>();
    private Dictionary<TreeControl, List<int[]>> addingSet = new Dictionary<TreeControl, List<int[]>>();

    void Start() {
        pointsToTrees = new TreeControl[width, height];
    }

    public int CheckNeighbors(Vector2 point, TreeControl.TreeColor color) {
        int x = (int)point.x;
        int y = (int)point.y;

        if (y > 0) {
            if (CanAdd (x - 1, y, color) || CanAdd (x + 1, y, color) || CanAdd (x, y - 1, color)) {
                AddPoint (point, color);
                return 1;
            } else if (pointsToTrees [x, y - 1] != null && pointsToTrees [x, y - 1].color != color) {
                return -1;
            } else {
                return 0;
            }
        } else {
            if (color == TreeControl.TreeColor.WILDCARD) {
                return -1;
            } else {
                AddPoint (point, color);
                return 1;
            }
        }
    }

    public void DelayedRemove() {
        StartCoroutine(DelayedRemoveCoroutine());
    }

    public void UpdateGrid() {
        deletingSet.Clear();

        foreach (TreeControl treeControl in addingSet.Keys) {
            List<int[]> coords = addingSet[treeControl];
            foreach (int[] coord in coords) {
                pointsToTrees[coord[0], coord[1]] = treeControl;
                if (coord[1] == 0) {
                    treesToRoots[treeControl] = coord[0];
                }
            }
        }
        addingSet.Clear();

        Dictionary<TreeControl, int> newTrees = new Dictionary<TreeControl, int>();
        foreach (TreeControl treeControl in treesToRoots.Keys) {
            if (treeControl != null) {
                newTrees[treeControl] = treesToRoots[treeControl];
            }
        }
        treesToRoots = newTrees;
    }

    private void AddPoint(Vector2 point, TreeControl.TreeColor color) {
        int x = (int)point.x;
        int y = (int)point.y;

        if (y > 0) {
            HashSet<TreeControl> added = new HashSet<TreeControl> ();
            if (CanAdd(x - 1, y, color)) {
                added.Add (pointsToTrees [x - 1, y]);
            }
            if (CanAdd(x + 1, y, color)) {
                added.Add (pointsToTrees [x + 1, y]);
            }
            if (CanAdd(x, y - 1, color)) {
                added.Add (pointsToTrees [x, y - 1]);
            }

            if (added.Count > 1) {
                foreach (TreeControl treeControl in added) {
                    GameControl.animationLock.Inc();
                    treeControl.AddPoint(GridToTreeCoords(point, treeControl));
                    deletingSet.Add(treeControl);
                }
            } else {
                TreeControl[] treeArray = new TreeControl[1];
                added.CopyTo(treeArray);
                TreeControl addedTree = treeArray [0];
                GameControl.animationLock.Inc();
                addedTree.AddPoint (GridToTreeCoords (point, addedTree));

                if (!addingSet.ContainsKey(addedTree)) addingSet[addedTree] = new List<int[]>();
                addingSet[addedTree].Add(new int[]{ x, y });
            }
        } else if (color != TreeControl.TreeColor.WILDCARD) {
            GameControl.animationLock.Inc();
            MakeTree (x, color);
        }
    }

    private bool CanAdd(int x, int y, TreeControl.TreeColor color) {
        return (x >= 0 && y >= 0 && x < width && y < height &&
            pointsToTrees [x, y] != null && (pointsToTrees [x, y].color == color ||
                color == TreeControl.TreeColor.WILDCARD));
    }

    private Vector2 GridToTreeCoords(Vector2 point, TreeControl tree) {
        return new Vector2 (point.x - treesToRoots [tree] + tree.gridSize / 2, point.y);
    }

    private IEnumerator DelayedRemoveCoroutine() {
        while (GameControl.animationLock.counter > 1) {
            yield return new WaitForSeconds(.01f);
        }
        AnimationCounter finishCounter = new AnimationCounter(deletingSet.Count);
        foreach (TreeControl treeControl in deletingSet) {
            treeControl.Remove(finishCounter);
        }

        while (finishCounter.counter > 0) {
            yield return new WaitForSeconds(.01f);
        }
        GameControl.animationLock.Dec();
    }

    private void MakeTree(int x, TreeControl.TreeColor color) {
        Vector2 position = new Vector2 ((x - (float)width / 2 + .5f) * gridSpacing, 0);
        GameObject newTree = Instantiate (tree, position, Quaternion.identity);
        TreeControl newTreeControl = newTree.GetComponent<TreeControl> ();
        newTreeControl.color = color;
        newTreeControl.gridSpacing = gridSpacing;
        newTreeControl.gridSize = (int)(Mathf.Max (width, height) * 2);
        newTreeControl.Create ();
        addingSet[newTreeControl] = new List<int[]>();
        addingSet[newTreeControl].Add(new int[] { x, 0 });
        addingSet[newTreeControl].Add(new int[] { x, 1 });
    }
}
