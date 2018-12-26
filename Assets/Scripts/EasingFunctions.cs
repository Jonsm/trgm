using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EasingFunction1D {
    public abstract float Ease(float t);

    public float Lerp(float start, float end, float t, float totalTime) {
        return start + (end - start) * Ease(t/totalTime);
    }
}

public interface EasingFunction2D {
    float Ease(float t, float x);
}

public class OvershootEase : EasingFunction1D {
    private float a;
    private float b;
    private float c;

    public OvershootEase (float overshootTime, float overshootAmount) {
        float p = overshootTime;
        float d = overshootAmount;
        a = -((-3 * d - Mathf.Pow(p, 4) + 4 * d * p + 4 * p - 3) / 
            (Mathf.Pow(p - 1, 2) * Mathf.Pow(p, 2)));
        b = -((2 * (d + Mathf.Pow(p, 4) -2 * d * Mathf.Pow(p, 2) - 2 * Mathf.Pow(p, 2) + 1)) / 
            (Mathf.Pow(p - 1, 2) * Mathf.Pow(p, 3)));
        c = -((-2 * d - Mathf.Pow(p, 3) + 3 * d * p + 3 * p - 2) /
            (Mathf.Pow(p - 1, 2) * Mathf.Pow(p, 3)));
    }

    public override float Ease(float t) {
        return a * Mathf.Pow(t, 2) + b * Mathf.Pow(t, 3) + c * Mathf.Pow(t, 4);
    }
}

public class BounceOnceEase : EasingFunction1D {
    private float bounceStart;
    private float a;

    public BounceOnceEase (float bounceStart, float bounceAmount) {
        this.bounceStart = bounceStart;
        a = 4 * bounceAmount / Mathf.Pow(1 - bounceStart, 2);
    }

    public override float Ease(float t) {
        if (t < bounceStart) {
            return t / bounceStart;
        } else {
            float t2 = t - bounceStart;
            return 1 - a * t2 * (1 - bounceStart - t2);
        }
    }
}