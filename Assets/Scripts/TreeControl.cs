using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeControl : MonoBehaviour {
	public enum TreeColor { 
		RED, GREEN, BLUE, NONE, WILDCARD
	};

	private static float approxTolerance = .001f;

	public int gridSize;
	public float gridSpacing;
	public GameObject branch;
	public float deleteSpeed;
	public TreeColor color;
	public Material[] colorMaterials;

	private GridSpline[,] pointsToBranches;
	private bool isDeleting = false;
	private bool isWaiting = false;
	private float endTime = 0;
	private Dictionary<GridSpline, float> branchesToDepths = new Dictionary<GridSpline, float> ();
    private AnimationCounter deleteCounter;

	void Update() {
		if (isDeleting) {
			DeleteAnimate (Time.time);
		}
	}

	public void Create() {
		float gridOffset = Mathf.Round(gridSize / 2) * gridSpacing;
		gameObject.transform.position = gameObject.transform.position + new Vector3(-1 * gridOffset, 0, 0);
		pointsToBranches = new GridSpline[gridSize, gridSize];

		List<Vector2> rootGridArray = new List<Vector2> ();
		rootGridArray.Add (new Vector2(gridSize / 2, -1));
		rootGridArray.Add (new Vector2(gridSize / 2, 0));
		rootGridArray.Add (new Vector2(gridSize / 2, 1));
		MakeBranch (rootGridArray, null);
	}

	public void Remove(AnimationCounter counter) {
        deleteCounter = counter;
		isDeleting = true;
	}

	public GridSpline AddPoint(Vector2 point, AnimationCounter counter) {
		int x = (int)point.x;
		int y = (int)point.y;

		GridSpline leftBranch = (x > 0) ? pointsToBranches [x - 1, y] : null;
		GridSpline rightBranch = (x < gridSize - 1) ? pointsToBranches [x + 1, y] : null;
		GridSpline bottomBranch = (y > 0) ? pointsToBranches [x, y - 1] : null;

		if (leftBranch != null) {
			if (IsLastPoint(leftBranch, x - 1, y)) {
				AddToBranch (leftBranch, point, counter);
				return leftBranch;
			} else {
				if (IsValidCoord (x - 1, y - 1) && leftBranch.currentPoints.Contains (new Vector2(x - 1, y - 1 ))) {
					List<Vector2> gridArray = new List<Vector2>();
					gridArray.Add (new Vector2(x - 1, y - 1));
					gridArray.Add (new Vector2(x - 1, y));
					gridArray.Add (new Vector2(x, y));
					return MakeBranch (gridArray, counter);
				} else {
					List<Vector2> gridArray = new List<Vector2> ();
					gridArray.Add (new Vector2 (x - 2, y));
					gridArray.Add (new Vector2 (x - 1, y));
					gridArray.Add (new Vector2 (x, y));
					return MakeBranch (gridArray, counter);
				}
			}
		} else if (rightBranch != null) {
			if (IsLastPoint(rightBranch, x + 1, y)) {
				AddToBranch (rightBranch, point, counter);
				return rightBranch;
			} else {
				if (IsValidCoord (x + 1, y - 1) && rightBranch.currentPoints.Contains (new Vector2(x + 1, y - 1))) {
					List<Vector2> gridArray = new List<Vector2> ();
					gridArray.Add (new Vector2(x + 1, y - 1));
					gridArray.Add (new Vector2(x + 1, y));
					gridArray.Add (new Vector2(x, y));
					return MakeBranch (gridArray, counter);
				} else {
					List<Vector2> gridArray = new List<Vector2> ();
					gridArray.Add (new Vector2(x + 2, y));
					gridArray.Add (new Vector2(x + 1, y));
					gridArray.Add (new Vector2(x, y));
					return MakeBranch (gridArray, counter);
				}
			}
		} else if (bottomBranch != null) {
			if (IsLastPoint(bottomBranch, x, y - 1)) {
				AddToBranch (bottomBranch, point, counter);
				return bottomBranch;
			} else {
				if (IsValidCoord (x - 1, y - 1) && bottomBranch.currentPoints.Contains (new Vector2(x - 1, y - 1)) &&
					IsValidCoord (x + 1, y - 1) && bottomBranch.currentPoints.Contains (new Vector2(x + 1, y - 1))) {
					int ind = bottomBranch.currentPoints.IndexOf(new Vector2(x, y - 1));
					float dir = 1;
					if (bottomBranch.currentPoints[ind + 1] == new Vector2(x + 1, y - 1)) {
						dir = -1;
					}
					List<Vector2> gridArray = new List<Vector2> ();
					gridArray.Add (new Vector2 (x + dir, y - 1));
					gridArray.Add (new Vector2 (x, y - 1));
					gridArray.Add (new Vector2 (x, y));
					return MakeBranch (gridArray, counter);
				} else {
					List<Vector2> gridArray = new List<Vector2> ();
					gridArray.Add (new Vector2 (x, y - 2));
					gridArray.Add (new Vector2 (x, y - 1));
					gridArray.Add (new Vector2 (x, y));
					return MakeBranch (gridArray, counter);
				}
			}
		}
		return null;
	}

	private void DeleteAnimate(float time) {
		if (isWaiting) {
			if (time > endTime) {
                deleteCounter.Dec();
				Destroy (gameObject);
			}
		} else {
			float maxDepth = 0;
			foreach (GridSpline deletingBranch in branchesToDepths.Keys) {
				maxDepth = Mathf.Max (maxDepth, GetDepth(deletingBranch));
			}
			foreach (GridSpline deletingBranch in branchesToDepths.Keys) {
				float deleteStartTime = time + 
					(maxDepth - GetDepth(deletingBranch)) / deleteSpeed;
				float deleteEndTime = 
					deleteStartTime + deletingBranch.cumulativeLengths[deletingBranch.cumulativeLengths.Count - 1]
					/ deleteSpeed;
				deletingBranch.Delete (deleteStartTime, deleteEndTime);
				endTime = Mathf.Max (endTime, deleteEndTime);
			}
			isWaiting = true;
		}
	}

	private void AddToBranch(GridSpline branch, Vector2 point, AnimationCounter counter) {
		int x = (int)point.x;
		int y = (int)point.y;
		branch.currentPoints.Add (point);
		branch.Reshape (branch.currentPoints, counter);
		pointsToBranches [x, y] = branch;
	}

	private GridSpline MakeBranch(List<Vector2> points, AnimationCounter counter) {
		GameObject newBranch = Instantiate (branch, gameObject.transform.position, Quaternion.identity);
		newBranch.transform.parent = gameObject.transform;
		GridSpline gridSpline = (GridSpline) newBranch.GetComponent<GridSpline> ();
		gridSpline.gridSpacing = gridSpacing;

		MeshRenderer renderer = newBranch.GetComponent<MeshRenderer> ();
		renderer.material = colorMaterials [(int)color];

		gridSpline.Reshape (points, counter);

		float baseDepth = 0;
		int baseX = (int)points [1].x;
		int baseY = (int)points [1].y;
		if (IsValidCoord(baseX, baseY) && pointsToBranches [baseX, baseY] != null) {
			GridSpline baseBranch = pointsToBranches [baseX, baseY];
			int i = gridSpline.splinePositions.Count - 1;
			while (i > 0 && !ApproxContains(baseBranch.splinePositions, gridSpline.splinePositions[i])) {
				i--;
			}
			Vector2 intersectionPoint = gridSpline.splinePositions [i];
			float offseted = 0;
			if (i == 0) {
				i = 1;
				offseted = gridSpline.turnRadius;
				Vector2 offset = (points [1] - points [0]) * gridSpline.turnRadius;
				intersectionPoint = gridSpline.splinePositions [1] - offset;
				if (!ApproxContains (baseBranch.splinePositions, intersectionPoint)) {
					offseted = -1 * gridSpline.turnRadius;
					intersectionPoint = gridSpline.splinePositions [1] + offset;
				}
			}
			int j = ApproxIndexOf (baseBranch.splinePositions, intersectionPoint);
			float depth = baseBranch.cumulativeLengths [j] - gridSpline.cumulativeLengths [i] + offseted;
			baseDepth = depth + branchesToDepths [baseBranch];
		}
		branchesToDepths [gridSpline] = baseDepth;

		foreach (Vector2 point in points) {
			int x = (int)point.x;
			int y = (int)point.y;
			if (IsValidCoord(x, y) && pointsToBranches [x, y] == null) {
				pointsToBranches [x, y] = gridSpline;
			}
		}

		return gridSpline;
	}

	private float GetDepth(GridSpline branch) {
		return branch.cumulativeLengths [branch.cumulativeLengths.Count - 1] + branchesToDepths [branch];
	}

	private bool IsValidCoord(int x, int y) {
		return (x >= 0 && x < gridSize && y >= 0 && y < gridSize);
	}

	private bool IsLastPoint(GridSpline branch, int x, int y) {
		Vector2 lastPoint = branch.currentPoints [branch.currentPoints.Count - 1];
		return (x == lastPoint.x && y == lastPoint.y);
	}

	private bool ApproxContains(List<Vector2> list, Vector2 point) {
		foreach (Vector2 point2 in list) {
			if ((point - point2).sqrMagnitude < approxTolerance)
				return true;
		}
		return false;
	}

	private int ApproxIndexOf(List<Vector2> list, Vector2 point) {
		for (int i = 0; i < list.Count; i++) {
			if ((point - list [i]).sqrMagnitude < approxTolerance) {
				return i;
			}
		}
		return -1;
	}
}
