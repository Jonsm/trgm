using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WildCardMarker : Marker
{
    private TreeControl.TreeColor finalColor;
    private List<TreeControl.TreeColor> possibleColors;

    public TreeControl.TreeColor SetColor(List<TreeControl.TreeColor> possibleColors) {
        this.possibleColors = possibleColors;
        int rand = Random.Range(0, possibleColors.Count);
        finalColor = possibleColors[rand];
        return finalColor;
    }
}
