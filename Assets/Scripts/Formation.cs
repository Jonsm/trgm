﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Formation {
	private int[] drops;

	public Formation (int[] drops) {
		this.drops = drops;
	}

	public TreeControl.TreeColor[] GetColors() {
		int count = System.Enum.GetNames (typeof(TreeControl.TreeColor)).Length - 2;
		TreeControl.TreeColor[] colors = new TreeControl.TreeColor[count];
		for (int i = 0; i < count; i++) {
			colors [i] = (TreeControl.TreeColor)i;
		}
		Shuffle (colors);

		TreeControl.TreeColor[] colorDrops = new TreeControl.TreeColor[drops.Length];

		for (int i = 0; i < drops.Length; i++) {
			if (drops [i] == -1) {
				colorDrops [i] = TreeControl.TreeColor.NONE;
			} else if (drops [i] == -2) {
				colorDrops [i] = TreeControl.TreeColor.WILDCARD;
			} else {
				colorDrops [i] = colors [drops [i]];
			}
		}
		return colorDrops;
	}

	public void Shuffle(TreeControl.TreeColor[] array) {
        for (int i = 0; i < array.Length; i++) {
            int j = Random.Range(i, array.Length);
            TreeControl.TreeColor temp = array[i];
            array[i] = array[j];
            array[j] = temp;
        }
    }
}
