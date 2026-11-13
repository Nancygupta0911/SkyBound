using UnityEditor;
using UnityEngine;

public static class ImportTmp
{
    public static string Run()
    {
        string settings = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        if (AssetDatabase.LoadAssetAtPath<Object>(settings) != null)
            return "TMP essentials already present";

        TMPro.TMP_PackageResourceImporter.ImportResources(true, false, false);
        AssetDatabase.Refresh();
        bool ok = AssetDatabase.LoadAssetAtPath<Object>(settings) != null;
        return "TMP essentials imported=" + ok;
    }
}
