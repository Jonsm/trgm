using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameControl : MonoBehaviour {
	public static int animationLock = 0;
	public static bool animated = true;

	private static Formation[] formationsList = new Formation[]{
		new Formation(new int[] {0, 1, 0}),
		new Formation(new int[] {0, -1, 0}),
		new Formation(new int[] {0, 1, 2}),
		new Formation(new int[] {0, -1, 1})
		//new Formation(new int[] {0, -2, 1}),
		//new Formation(new int[] {0, -2, 0})
	};

	public TreeGrid treeGrid;
	public float moveDelay;
	public float rateAfterDelay;
	public GameObject marker;

	private float gridSpacing;
	private int width;
	private int height;
	private int currentX;

	private float lastPressTime;
	private float nextMoveTime;
	private bool validPress = false;

	private List<GameObject> markers;

	void Start() {
		gridSpacing = treeGrid.gridSpacing;
		width = treeGrid.width;
		height = treeGrid.height;
		MakeDrops ();
	}

	void Update() {
		if (animationLock == 0) {
			if (markers == null) {
				MakeDrops ();
			}

			if (Input.GetKeyDown (KeyCode.LeftArrow)) {
				InitiateMove (-1);
			} else if (Input.GetKeyDown (KeyCode.RightArrow)) {
				InitiateMove (1);
			}

			if (Time.time - lastPressTime > moveDelay && Time.time > nextMoveTime) {
				nextMoveTime += 1.0f / rateAfterDelay;
				if (Input.GetKey (KeyCode.LeftArrow)) {
					if (validPress) {
						MoveMarker (-1);
					} else {
						InitiateMove (-1);
					}
				} else if (Input.GetKey (KeyCode.RightArrow)) {
					if (validPress) {
						MoveMarker (1);
					} else {
						InitiateMove (1);
					}
				}
			}

			if (Input.GetKeyDown (KeyCode.Space)) {
				Drop ();
			}
		} else {
			validPress = false;
		}
	}

	private void InitiateMove(int dir) {
		MoveMarker (dir);
		lastPressTime = Time.time;
		nextMoveTime = Time.time + moveDelay;
		validPress = true;
	}

	private void MoveMarker(int dir) {
		if (currentX + dir >= 0 && currentX + dir < width) {
			currentX = currentX + dir;
			foreach (GameObject markerObj in markers) {
				if (markerObj != null) {
					markerObj.transform.position = markerObj.transform.position + dir * gridSpacing * Vector3.right;
				}
			}
		}
	}

	private void Drop() {
		for (int i = 0; i < markers.Count; i++) {
			if (markers [i] != null) {
				if (animated) {
					animationLock++;
				}

				int x = currentX + i - Mathf.FloorToInt (.5f * markers.Count);
				Marker markerControl = (Marker)markers [i].GetComponent<Marker> ();
				markerControl.Drop (height, x);
			}
		}

		markers = null;
	}

	private void MakeDrops() {
		Formation formation = formationsList [Random.Range (0, formationsList.Length)];
		TreeControl.TreeColor[] dropColors = formation.GetColors ();

		markers = new List<GameObject> ();
		Vector3 center = new Vector2 (0, height * gridSpacing);
		for (int i = 0; i < dropColors.Length; i++) {
			if (dropColors [i] != TreeControl.TreeColor.NONE) {
				Vector3 position = center + Vector3.right *
					gridSpacing * (i - Mathf.FloorToInt (.5f * dropColors.Length));
				GameObject currentMarker = Instantiate (marker, position, Quaternion.identity);
				Marker markerControl = (Marker)currentMarker.GetComponent<Marker> ();
				markerControl.SetColor (dropColors [i]);
				markers.Add (currentMarker);
			} else {
				markers.Add (null);
			}
		}

		currentX = width / 2;
	}
}
