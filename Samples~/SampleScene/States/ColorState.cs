using Behaviours.HFSM;
using Behaviours.HFSM.Runtime;
using UnityEngine;

public class ColorState : IBaseState
{
    public Color color = Color.white;

    public override void start(RuntimeStateData runtimeData)
    {
        base.start(runtimeData);

        SetColor(runtimeData, color);
    }

    private void SetColor(RuntimeStateData runtimeData, Color newColor)
    {
        var rootGameObject = runtimeData.runtimeNodeData.runtimeLayerData.gameObject;
        if (!rootGameObject)
        {
            Debug.LogError("Root GameObject is null. Cannot set color.");
            return;
        }
        
        var renderer = rootGameObject.GetComponent<Renderer>();
        if (!renderer)
        {
            Debug.LogError("Renderer component not found on the root GameObject. Cannot set color.");
            return;
        }
        
        // Set the color of the renderer
        renderer.material.color = newColor;
    }
}