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

    public int CheckNeighbors(Vector2 point, Marker marker) {
        int x = (int)point.x;
        int y = (int)point.y;
        TreeControl.TreeColor color = marker.color;

        if (y > 0) {
            if (CanAdd (x - 1, y, color) || CanAdd (x + 1, y, color) || CanAdd (x, y - 1, color)) {
                AddPoint (point, marker);
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
                AddPoint (point, marker);
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

    public bool Occupied(Vector2 point) {
        int x = Mathf.RoundToInt(point.x);
        int y = Mathf.RoundToInt(point.y);
        return (pointsToTrees[x, y] != null);
    }

    private void AddPoint(Vector2 point, Marker marker) {
        int x = (int)point.x;
        int y = (int)point.y;
        TreeControl.TreeColor color = marker.color;

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

            List<TreeControl> addingList = new List<TreeControl>(added);
            if (color == TreeControl.TreeColor.WILDCARD) {
                Dictionary<TreeControl.TreeColor, List<TreeControl>> colorsToTrees =
                    new Dictionary<TreeControl.TreeColor, List<TreeControl>>();
                foreach (TreeControl treeControl in added) {
                    if (!colorsToTrees.ContainsKey(treeControl.color)) {
                        colorsToTrees[treeControl.color] = new List<TreeControl>();
                    }
                    colorsToTrees[treeControl.color].Add(treeControl);
                }

                List<TreeControl.TreeColor> colorsList = new List<TreeControl.TreeColor>(colorsToTrees.Keys);
                WildCardMarker wildMarker = (WildCardMarker)marker;
                TreeControl.TreeColor wildColor = wildMarker.SetColor(colorsList);
                addingList = colorsToTrees[wildColor];
            }

            if (addingList.Count > 1) {
                foreach (TreeControl treeControl in addingList) {
                    GameControl.animationLock.Inc();
                    deletingSet.Add(treeControl);
                }
                AddToTrees(addingList, marker, point);
            } else {
                TreeControl addedTree = addingList [0];
                GameControl.animationLock.Inc();
                AddToTrees(addingList, marker, point);

                if (!addingSet.ContainsKey(addedTree)) addingSet[addedTree] = new List<int[]>();
                addingSet[addedTree].Add(new int[]{ x, y });
            }
        } else if (color != TreeControl.TreeColor.WILDCARD) {
            GameControl.animationLock.Inc();
            MakeTree (x, color);
        }
    }

    private void AddToTrees(List<TreeControl> trees, Marker marker, Vector2 point) {
        if (marker.color == TreeControl.TreeColor.WILDCARD) {
            WildCardMarker wildMarker = (WildCardMarker)marker;
            StartCoroutine(WaitWildCard(wildMarker, trees, point));
        } else {
            foreach (TreeControl addedTree in trees) {
                addedTree.AddPoint(GridToTreeCoords(point, addedTree));
            }
        }
    }

    private IEnumerator WaitWildCard (WildCardMarker marker, List<TreeControl> trees, Vector2 point) {
        yield return marker.Animate();
        foreach (TreeControl addedTree in trees) {
            addedTree.AddPoint(GridToTreeCoords(point, addedTree));
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
