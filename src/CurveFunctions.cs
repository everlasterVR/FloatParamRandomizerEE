using UnityEngine;

static class CurveFunctions
{
    #region Parametric smoother

    public static float EaseInOutExponent(float value, float midPoint)
    {
        float scaled = Mathf.Lerp(0, 3.5f, value);
        float curvature = AdjustedCurvature(scaled, midPoint);
        return 2 / (1 + curvature) - midPoint;
    }

    public static float BounceInOutExponent(float value, float midPoint)
    {
        float scaled = -Mathf.Lerp(0, 6.2f, value);
        float curvature = AdjustedCurvature(scaled, midPoint);
        return 2 / (1 + curvature) - midPoint;
    }

    // Adjusts the curvature such that a value of 0 is always a straight line regardless of midpoint
    static float AdjustedCurvature(float curvature, float midpoint) =>
        (curvature - 1) * (midpoint - 1) / (midpoint + 1);

    static float F1(float value, float n, float c) =>
        Mathf.Pow(value, c) / Mathf.Pow(n, c - 1);

    /// <summary>
    /// Eases or bounces value [0, 1] based on curvature and midpoint.
    /// Graph: https://www.desmos.com/calculator/7e1tgd7jqr (p = AdjustedCurvature)
    /// </summary>
    ///
    public static float ParametricSmoother(float value, float exponent, float midpoint)
    {
        if(value < 0)
        {
            return 0;
        }

        if(value > 1)
        {
            return 1;
        }

        if(value < midpoint)
        {
            return F1(value, midpoint, exponent);
        }

        return 1 - F1(1 - value, 1 - midpoint, exponent);
    }

    #endregion Parametric smoother
}
