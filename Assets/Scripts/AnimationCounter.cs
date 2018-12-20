using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationCounter {
    private int _counter = 0;

    public int counter {
        get { return _counter; }
    }

    public AnimationCounter() {
    }

    public AnimationCounter(int initialCount) {
        _counter = initialCount;
    }

    public void Inc() {
        _counter++;
    }

    public void Inc(int inc) {
        _counter+=inc;
    }

    public void Dec() {
        _counter--;
    }

    public void Dec(int dec) {
        _counter-=dec;
    }
}

