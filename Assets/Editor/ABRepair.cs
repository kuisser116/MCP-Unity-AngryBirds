using UnityEditor;
using UnityEngine;

/// <summary>
/// Reimporta una sola vez los assets de AngryBirds para limpiar el
/// "Internal error - unexpected guid mismatch" que deja Unity cuando los
/// archivos se reemplazan en disco con el editor abierto.
/// </summary>
[InitializeOnLoad]
public static class ABRepair
{
    const string Marker = "Assets/AngryBirds/.art_repair_done";
    const string ScenePath = "Assets/Scenes/AngryBirds.unity";

    static ABRepair()
    {
        EditorApplication.delayCall += Repair;
    }

    static void Repair()
    {
        if (System.IO.File.Exists(Marker)) return;

        AssetDatabase.ImportAsset("Assets/AngryBirds",
            ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

        if (AssetDatabase.LoadAssetAtPath<Object>(ScenePath) != null || System.IO.File.Exists(ScenePath))
        {
            AssetDatabase.ImportAsset(ScenePath,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }

        System.IO.File.WriteAllText(Marker, "ok");
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        Debug.Log("[ABRepair] Assets de AngryBirds reimportados: error de guid limpiado.");
    }
}
