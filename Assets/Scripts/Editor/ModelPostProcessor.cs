using UnityEditor;
using UnityEngine;

public class ModelPostProcessor : AssetPostprocessor
{
    private void OnPostprocessModel(GameObject g)
    {
        return;
        var modelImporter = assetImporter as ModelImporter;
        if (modelImporter?.defaultClipAnimations == null) return;

        var clipAnimations = modelImporter.defaultClipAnimations;
        for (var index = 0; index < clipAnimations.Length; index++)
        {
            clipAnimations[index].name = g.name;
        }

        modelImporter.clipAnimations = clipAnimations;
        modelImporter.SaveAndReimport();
    }
}
