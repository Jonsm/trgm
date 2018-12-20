using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridSpline : MonoBehaviour {
	public float gridSpacing;
	public float turnRadius;
	public float turnSegments;
	public float branchWidth;
	public float endOffset;
	public float startOffset;
	public float animationSpeed;

	[HideInInspector]
	public List<Vector2> currentPoints;
	[HideInInspector]
	public bool isAnimating = false;
	[HideInInspector]
	public List<float> cumulativeLengths;
	[HideInInspector]
	public List<Vector2> splinePositions = new List<Vector2>();

	private SplineDraw splineDraw;
	private List<float> splineWidths;
	private float startingLength = 0;
	private int currentPosIndex;
	private float animationStartTime = -100;
	private float totalAnimationTime;
	private int animationDir = 1;

	void Awake () {
		splineDraw = gameObject.GetComponent<SplineDraw> () as SplineDraw;
	}

	void Update() {
		if (isAnimating && Time.time - animationStartTime < totalAnimationTime) {
			Animate (Time.time - animationStartTime);
		} else if (isAnimating) {
			isAnimating = false;

			if (animationDir == 1) {
				GameControl.animationLock.Dec();
				splineDraw.Reshape (splinePositions, splineWidths);
			} else {
				Destroy (gameObject);
			}
		}
	}

	public void Delete(float startDeleteTime, float endDeleteTime) {
		animationSpeed = cumulativeLengths[cumulativeLengths.Count - 1] / (endDeleteTime - startDeleteTime);
		animationDir = -1;
		startingLength = cumulativeLengths[cumulativeLengths.Count - 1];
		totalAnimationTime = endDeleteTime - startDeleteTime;
		currentPosIndex = splinePositions.Count - 1;
		StartCoroutine (DeleteWait (startDeleteTime - Time.time, startDeleteTime));
	}

	public void Reshape(List<Vector2> points) {
		List<Vector2> positions = new List<Vector2> ();
		Vector2 direction = new Vector2 (points [1].x - points [0].x, points [1].y - points [0].y);
		positions.Add(GridToVector (points [0]) + startOffset * gridSpacing * direction);

		for (int i = 1; i < points.Count - 1; i++) {
			Vector2 newDirection = new Vector2 (points [i+1].x - points [i].x, points [i+1].y - points [i].y);
			if (direction == newDirection) {
				positions.Add (GridToVector (points [i]));
			} else {
				Vector2 start = GridToVector (points [i]) - direction * turnRadius;
				Vector2 end = GridToVector (points [i]) + newDirection * turnRadius;
				Vector2 turnCenter = start + newDirection * turnRadius;
				Vector2 max1 = start - turnCenter;
				Vector2 max2 = end - turnCenter;

				for (int j = 0; j < turnSegments; j++) {
					float theta = Mathf.PI * j / 2 / turnSegments;
					positions.Add (turnCenter + max1 * Mathf.Cos (theta) + max2 * Mathf.Sin (theta));
				}
				positions.Add (end);
			}
			direction = newDirection;
		}
		positions.Add (GridToVector (points [points.Count - 1]) - endOffset * gridSpacing * direction);

		List<float> widths = new List<float> ();
		for (int i = 0; i < positions.Count; i++) {
			widths.Add (branchWidth);
		}
			
		startingLength = .1f;
		if (cumulativeLengths != null && cumulativeLengths.Count > 0) {
			startingLength = cumulativeLengths [cumulativeLengths.Count - 1];
		}
		cumulativeLengths = GetCumulativeLengths (positions);
		currentPosIndex = Mathf.Max(splinePositions.Count - 2, 0);
		splinePositions = positions;
		splineWidths = widths;
		currentPoints = points;

		isAnimating = true;
		animationStartTime = Time.time;
		totalAnimationTime = (cumulativeLengths[cumulativeLengths.Count - 1] - startingLength) / animationSpeed;
	}

	private void Animate(float time) {
		float currentLength = startingLength + animationDir * time * animationSpeed;
		float distAlongCurrent = currentLength - cumulativeLengths [currentPosIndex];

		if (animationDir == 1) {
			while (currentPosIndex < cumulativeLengths.Count - 2 &&
			       distAlongCurrent > cumulativeLengths [currentPosIndex + 1] - cumulativeLengths [currentPosIndex]) {
				currentPosIndex++;
				distAlongCurrent = currentLength - cumulativeLengths [currentPosIndex];
			}
		} else {
			while (currentPosIndex > 0 && distAlongCurrent < 0) {
				currentPosIndex--;
				distAlongCurrent = currentLength - cumulativeLengths [currentPosIndex];
			}
		}

		List<Vector2> partialPoints = new List<Vector2> ();
		for (int i = 0; i <= currentPosIndex; i++) {
			partialPoints.Add (splinePositions [i]);
		}

		Vector2 lastPoint = Vector2.Lerp(splinePositions[currentPosIndex], splinePositions[currentPosIndex + 1],
			distAlongCurrent / (cumulativeLengths[currentPosIndex + 1] - cumulativeLengths[currentPosIndex]));

		if (partialPoints.Count == 1) {
			partialPoints.Add ((partialPoints [0] + lastPoint) / 2);
		}
		partialPoints.Add (lastPoint);

		List<float> partialWidths = new List<float> ();
		for (int i = 0; i < partialPoints.Count; i++) {
			partialWidths.Add (splineWidths [i]);
		}
		splineDraw.Reshape (partialPoints, partialWidths);
	}

	private IEnumerator DeleteWait(float waitTime, float startTime) {
		yield return new WaitForSeconds(waitTime);
		animationStartTime = startTime;
		isAnimating = true;
	}

	private Vector2 GridToVector(Vector2 point) {
		return gridSpacing * point;
	}

	private List<float> GetCumulativeLengths(List<Vector2> positions) {
		List<float> cumulativeLengths = new List<float> ();
		cumulativeLengths.Add (0);
		for (int i = 1; i < positions.Count; i++) {
			cumulativeLengths.Add (cumulativeLengths [i - 1] + (positions [i] - positions [i - 1]).magnitude);
		}
		return cumulativeLengths;
	}
}
