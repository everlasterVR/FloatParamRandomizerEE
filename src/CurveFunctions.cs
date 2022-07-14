using UnityEngine;

public static class CurveFunctions
{
    /// <summary>
    /// Smoothes value [0, 1] based on curvature and midpoint.
    /// Curvature:
    ///     * values in the range [-3, 1] produces an increasing curve, with -1/3 representing a straight line
    ///     * values below -1/3 result in "bounce"
    ///     * values above -1/3 result in smoothing
    ///     * 0 is close to Mathf.SmoothStep amount of smoothing
    /// Graph: https://www.desmos.com/calculator/1i1nvvwttp
    /// </summary>
    ///
    public static float ParametricSmoother(float value, float curvature, float midpoint)
    {
        if(value < 0)
        {
            return 0;
        }

        if(value > 1)
        {
            return 1;
        }

        float c = 2 / (1 - curvature) - midpoint;
        if(value < midpoint)
        {
            return F1(value, midpoint, c);
        }

        return 1 - F1(1 - value, 1 - midpoint, c);
    }

    private static float F1(float value, float n, float c) =>
        Mathf.Pow(value, c) / Mathf.Pow(n, c - 1);
}
