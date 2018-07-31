using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Marker : MonoBehaviour {
	public Material[] colorMaterials;
	public Material wildcardMaterial;
	public float dropSpeed;

	private TreeControl.TreeColor currentColor;
	private TreeGrid treeGrid;
	private int currentX;
	private int currentY;
	private bool isDropping = false;

	void Start() {
		GameObject treeGridObj = GameObject.FindGameObjectWithTag ("TreeGrid");
		treeGrid = (TreeGrid)treeGridObj.GetComponent<TreeGrid> ();
	}

	void Update() {
		if (isDropping) {
			gameObject.transform.position += dropSpeed * Time.deltaTime * Vector3.down;
			float actualY = gameObject.transform.position.y / treeGrid.gridSpacing;
			while (actualY <= currentY) {
				int check = treeGrid.CheckNeighbors(new Vector2(currentX, currentY), currentColor);
				if (check == 1) {
					Destroy (gameObject);
				} else if (check == -1) {
					GameControl.animationLock--;
					Destroy (gameObject);
				}
				currentY--;
			}
		}
	}

	public void SetColor(TreeControl.TreeColor color) {
		MeshRenderer renderer = gameObject.GetComponent<MeshRenderer> () as MeshRenderer;
		currentColor = color;

		if (color == TreeControl.TreeColor.WILDCARD) {
			renderer.material = wildcardMaterial;
		} else {
			renderer.material = colorMaterials [(int)color];
		}
	}

	public void Drop(int height, int currentX) { //the bass
		currentY = height - 1;
		this.currentX = currentX;
		isDropping = true;
	}
}
