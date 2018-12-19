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
    private Dictionary<TreeControl, AnimationCounter> treesToCounters = new Dictionary<TreeControl, AnimationCounter>();
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
                AnimationCounter counter = new AnimationCounter();
                foreach (TreeControl treeControl in treesToCounters.Keys) {
                    if (added.Contains(treeControl)) {
                        counter = treesToCounters[treeControl];
                    }
                }

                foreach (TreeControl treeControl in added) {
                    if (GameControl.animated) { 
                        GameControl.animationLock.Inc();
                    }
                    counter.Inc();
                    treeControl.AddPoint(GridToTreeCoords(point, treeControl), counter);
                }

                foreach (TreeControl treeControl in deletingSet) {
                    if (added.Contains(treeControl)) {
                        added.Remove(treeControl);
                    }
                }
                foreach (TreeControl treeControl in added) {
                    deletingSet.Add(treeControl);
                    treesToCounters[treeControl] = counter;
                }

                if (GameControl.animated) {
                    StartCoroutine(WaitRemove(counter, added));
                }
            } else {
                TreeControl[] treeArray = new TreeControl[1];
                added.CopyTo(treeArray);
                TreeControl addedTree = treeArray [0];
                AnimationCounter counter = new AnimationCounter();
                if (treesToCounters.ContainsKey(addedTree)) counter = treesToCounters[addedTree];
                else treesToCounters[addedTree] = counter;
                counter.Inc();
                addedTree.AddPoint (GridToTreeCoords (point, addedTree), counter);

                if (!addingSet.ContainsKey(addedTree)) addingSet[addedTree] = new List<int[]>();
                addingSet[addedTree].Add(new int[]{ x, y });
            }
        } else if (color != TreeControl.TreeColor.WILDCARD) {
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

    private IEnumerator WaitRemove(AnimationCounter counter, HashSet<TreeControl> trees) {
        while (counter.counter > 0) {
            yield return new WaitForSeconds (.01f);
        }
        RemoveTrees (trees);
    }

    public void UpdateGrid() {
        if (!GameControl.animated) {
            foreach (TreeControl treeControl in deletingSet) {
                Destroy(treeControl.gameObject);
            }
        }
        treesToCounters.Clear();
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
            if(treeControl != null) {
                newTrees[treeControl] = treesToRoots[treeControl];
            }
        }
        treesToRoots = newTrees;
    }

    private void RemoveTrees(HashSet<TreeControl> trees) {
        AnimationCounter counter = new AnimationCounter();
        foreach (TreeControl removingTree in trees) {
            counter.Inc();
            removingTree.Remove(counter);
        }
        StartCoroutine(DecrementAfterDelete(counter));
    }

    private IEnumerator DecrementAfterDelete (AnimationCounter counter) {
        while (counter.counter > 0) {
            yield return new WaitForSeconds(.01f);
        }
        if (GameControl.animated) {
            GameControl.animationLock.Dec();
        }
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
