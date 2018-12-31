using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WildCardMarker : Marker
{
    public float animationTime;

    private TreeControl.TreeColor finalColor;
    private List<TreeControl.TreeColor> possibleColors;

    private bool isAnimating;
    private float startTime;

    private void Update() {
        if (isAnimating) {

        }
    }

    public TreeControl.TreeColor SetColor(List<TreeControl.TreeColor> possibleColors) {
        this.possibleColors = possibleColors;
        int rand = Random.Range(0, possibleColors.Count);
        finalColor = possibleColors[rand];
        return finalColor;
    }

    public IEnumerator Animate() {
        startTime = Time.time;
        isAnimating = true;
        while (Time.time - startTime < animationTime) {
            yield return new WaitForSeconds(.01f);
        }
    }
}
