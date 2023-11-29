using UnityEngine;
using UnityEngine.UI;

static class MVRScriptExtensions
{
    public static UIDynamic NewSpacer(
        this MVRScript script,
        float height,
        bool rightSide = false
    )
    {
        if(height <= 0)
        {
            return null;
        }

        var spacer = script.CreateSpacer(rightSide);
        spacer.height = height;
        return spacer;
    }

    public static Transform InstantiateButton(this MVRScript script, Transform parent = null) =>
        Instantiate(script.manager.configurableButtonPrefab, parent);

    static Transform Instantiate(Transform prefab, Transform parent = null)
    {
        var transform = Object.Instantiate(prefab, parent, false);
        Object.Destroy(transform.GetComponent<LayoutElement>());
        return transform;
    }
}
