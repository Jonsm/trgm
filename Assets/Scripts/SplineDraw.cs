using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplineDraw : MonoBehaviour {
	public MeshFilter meshFilter;
	public int segments;
	public float terminatorLength;
    public int terminatorSegments;


    private Mesh mesh;
    private float[] terminatorWidths;
    private float[] terminatorLengths;

	void Awake () {
		mesh = meshFilter.mesh;
		Create ();
	}
	
	void Create () {
		Vector3[] vertices = new Vector3[(segments + 1) * 2];
		int[] triangles = new int[segments * 6];
		for (int i = 0; i < segments; i++) {
			triangles [6*i] = 2*i + 1;
			triangles [6*i + 1] = 2*i + 2;
			triangles [6*i + 2] = 2*i;
			triangles [6*i + 3] = 2*i + 3;
			triangles [6*i + 4] = 2*i + 2;
			triangles [6*i + 5] = 2*i + 1;
		}
			
		mesh.vertices = vertices;
		mesh.triangles = triangles;

        terminatorWidths = new float[terminatorSegments];
        terminatorLengths = new float[terminatorSegments];
        for (int i = 0; i < terminatorSegments; i++) {
            terminatorWidths[i] = Mathf.Cos((i + 1) * Mathf.PI / terminatorSegments / 2);
            terminatorLengths[i] = Mathf.Sin((i + 1) * Mathf.PI / terminatorSegments / 2) -
                Mathf.Sin(i * Mathf.PI / terminatorSegments / 2);
        }
        terminatorWidths[terminatorSegments - 1] += .01f;
    }

	public void Reshape(List<Vector2> positions, List<float> widths) {
		int buffer = segments - positions.Count + 1 - terminatorSegments;
		float startCos = (positions [1].x - positions [0].x) / (positions [1] - positions [0]).magnitude;
		float startSin = (positions [1].y - positions [0].y) / (positions [1] - positions [0]).magnitude;
		Vector2 startWidthDisp = new Vector2 (-1 * startSin, startCos) * widths[0] / 2f;

		Vector3[] vertices = mesh.vertices;
		for (int i = 0; i <= buffer; i++) {
			vertices [2 * i] = positions [0] - startWidthDisp;
			vertices [2 * i + 1] = positions [0] + startWidthDisp;
		}

		Vector2 widthDisp = startWidthDisp;
		for (int i = 1; i < positions.Count - 1; i++) {
			Vector2 v1 = positions [i] - positions [i - 1];
			Vector2 v2 = positions [i + 1] - positions [i];
			Vector2 bisector = (v1.normalized - v2.normalized).normalized;
			if (Mathf.Approximately (bisector.magnitude, 0)) {
				bisector = new Vector2 (v1.y, -1 * v1.x).normalized;
			}
			if (Vector2.Dot (bisector, widthDisp) < 0)
				bisector = -1 * bisector;
			widthDisp = bisector * widths [i] / 2;

			vertices [2 * (buffer + i)] = positions [i] - widthDisp;
			vertices [2 * (buffer + i) + 1] = positions [i] + widthDisp;
		}

		int last = positions.Count - 1;
		float endCos = (positions [last].x - positions [last-1].x) / (positions [last] - positions [last-1]).magnitude;
		float endSin = (positions [last].y - positions [last-1].y) / (positions [last] - positions [last-1]).magnitude;
		Vector2 endWidthDisp = new Vector2 (-1 * endSin, endCos) * widths [last] / 2;
		Vector2 endDirection = (positions [last] - positions [last - 1]).normalized;
		if (Vector2.Dot (endWidthDisp, widthDisp) < 0)
			endWidthDisp = -1 * endWidthDisp;
		vertices [vertices.Length - 2 - 2 * terminatorSegments] = positions [last] - endWidthDisp;
		vertices [vertices.Length - 1 - 2 * terminatorSegments] = positions [last] + endWidthDisp;

        Vector2 terminatorOffset = positions[last];
        for (int i = 0; i < terminatorSegments; i++) {
            int j = vertices.Length - 2 * (terminatorSegments - i);
            terminatorOffset += endDirection * terminatorLengths[i] * widths[last]/2;
            vertices[j] = terminatorOffset - endWidthDisp * terminatorWidths[i];
            vertices[j + 1] = terminatorOffset + endWidthDisp * terminatorWidths[i];
        }

        mesh.vertices = vertices;
		mesh.RecalculateBounds ();
	}
}
