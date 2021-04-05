using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Map.Map))]
public class WallEditor : Editor
{
    // Start is called before the first frame update
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("Generate Map Bounds"))
        {
            Map.Map script = target as Map.Map;
            script.MoveGroundTilesToGround();
            script.CalculateBounds();
            //script.CalculateHeightmap();
            script.CreateWallTiles();
        }

    }
}
