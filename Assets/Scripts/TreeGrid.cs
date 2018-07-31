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
	private List<GameObject> deletingList = new List<GameObject>();

	void Update() {
		if (isDeleting) {
			int nullCount = 0;
			foreach (GameObject obj in deletingList) {
				if (obj == null)
					nullCount++;
			}
			if (nullCount == deletingList.Count) {
				deletingList.Clear ();
				isDeleting = false;
				GameControl.animationLock--;
			}
		}
	}

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
				print ("deleting"); //THERES A BUG
				foreach (GameObject obj in deletingList) {
					TreeControl objTree = (TreeControl)obj.GetComponent<TreeControl>();
					if (added.Contains (objTree)) {
						added.Remove (objTree);
					}
				}
				GameControl.animationLock += added.Count;
				foreach (TreeControl removingTree in added) {
					deletingList.Add (removingTree.gameObject);
					removingTree.AddPoint (GridToTreeCoords (point, removingTree));
				}
				StartCoroutine(WaitRemove(added, point));
			} else {
				TreeControl[] treeArray = new TreeControl[1];
				added.CopyTo(treeArray);
				TreeControl addedTree = treeArray [0];
				addedTree.AddPoint (GridToTreeCoords (point, addedTree));
				pointsToTrees [x, y] = addedTree;
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

	private IEnumerator WaitRemove(HashSet<TreeControl> trees, Vector2 point) {
		while (GameControl.animationLock > 1) {
			yield return new WaitForSeconds (.01f);
		}
		RemoveTrees (trees, point);
	}

	private void RemoveTrees(HashSet<TreeControl> trees, Vector2 point) {
		isDeleting = true;
		foreach (TreeControl removingTree in trees) {
//			deletingList.Add (removingTree.gameObject);
			removingTree.Remove ();
			for (int x = 0; x < width; x++) {
				for (int y = 0; y < height; y++) {
					if (pointsToTrees [x, y] == removingTree) {
						pointsToTrees [x, y] = null;
					}
				}
				treesToRoots.Remove (removingTree);
			}
		}
	}

	private void MakeTree(int x, TreeControl.TreeColor color) {
		Vector2 position = new Vector2 ((x - (float)width / 2 + .5f) * gridSpacing, 0);
		GameObject newTree = Instantiate (tree, position, Quaternion.identity);
		TreeControl newTreeControl = (TreeControl)newTree.GetComponent<TreeControl> ();
		newTreeControl.color = color;
		newTreeControl.gridSpacing = gridSpacing;
		newTreeControl.gridSize = (int)(Mathf.Max (width, height) * 2);
		newTreeControl.Create ();
		pointsToTrees [x, 0] = newTreeControl;
		pointsToTrees [x, 1] = newTreeControl;
		treesToRoots [newTreeControl] = x;
	}
}
