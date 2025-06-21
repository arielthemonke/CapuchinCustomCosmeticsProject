using UnityEditor;
using UnityEngine;
using System.IO;
using System.IO.Compression;

public class CapuCosmeticBuilder : EditorWindow
{
    GameObject inputObject;
    string bundleName = "capucosmetic";
    string metadataJson = "";
    string outputFolder = "Builds/Capucosmetics";
    CapuCosmeticMetadata metadata = new CapuCosmeticMetadata();

    [MenuItem("CapuCosmetics/Build Cosmetic")]
    public static void ShowWindow()
    {
        GetWindow<CapuCosmeticBuilder>("CapuCosmetic Builder");
    }

    void OnGUI()
    {
        GUILayout.Label("CapuCosmetic Builder", EditorStyles.boldLabel);

        inputObject = (GameObject)EditorGUILayout.ObjectField("Input GameObject", inputObject, typeof(GameObject));
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
        GUILayout.Space(10);
        GUILayout.Label("Metadata Editing", EditorStyles.boldLabel);
        metadata.name = EditorGUILayout.TextField("Name", metadata.name);
        metadata.author = EditorGUILayout.TextField("Author", metadata.author);
        metadata.version = EditorGUILayout.IntField("Version", metadata.version);
        metadata.description = EditorGUILayout.TextField("Description", metadata.description);
        metadata.syncToLeftHand = EditorGUILayout.Toggle("Sync to LeftHand", metadata.syncToLeftHand);
        metadata.syncToRightHand = EditorGUILayout.Toggle("Sync to RightHand", metadata.syncToRightHand);
        GUILayout.Space(10);

        GUI.enabled = inputObject != null;
        if (GUILayout.Button("Build .capucosmetic file"))
        {
            BuildCapuCosmetic();
        }
        GUI.enabled = true;
    }

    void BuildCapuCosmetic()
    {
        string tempDir = "Assets/Temp";
        string prefabPath = $"{tempDir}/{bundleName}.prefab";
        string bundleOutput = $"{tempDir}/bundle";
        string metadataPath = $"{tempDir}/metadata.json";
        string finalCapuCosmetic = $"{outputFolder}/{metadata.name}.capucosmetic";

        if (Directory.Exists(tempDir))
            Directory.Delete(tempDir, true);
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(bundleOutput);
        Directory.CreateDirectory(outputFolder);

        PrefabUtility.SaveAsPrefabAsset(inputObject, prefabPath);
        AssetImporter importer = AssetImporter.GetAtPath(prefabPath);
        importer.assetBundleName = bundleName;
        
        AssetDatabase.Refresh();
        
        BuildPipeline.BuildAssetBundles(bundleOutput, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);

        string json = JsonUtility.ToJson(metadata, true);
        File.WriteAllText(metadataPath, json);

        if (File.Exists(finalCapuCosmetic)) 
            File.Delete(finalCapuCosmetic);

        using (var archive = ZipFile.Open(finalCapuCosmetic, ZipArchiveMode.Create))
        {
            string bundleFilePath = Path.Combine(bundleOutput, bundleName);
            if (File.Exists(bundleFilePath))
            {
                archive.CreateEntryFromFile(bundleFilePath, bundleName);
                Debug.Log($"Added bundle file {bundleFilePath}");
            }
            else
            {
                Debug.LogError($"Bundle file not found at: {bundleFilePath}");
                string[] files = Directory.GetFiles(bundleOutput);
                Debug.Log("Files in bundle output:");
                foreach (string file in files)
                {
                    Debug.Log($"- {file} ");
                }
            }
            archive.CreateEntryFromFile(metadataPath, "metadata.json");
        }

        AssetDatabase.DeleteAsset(prefabPath);
        if (Directory.Exists(tempDir))
            Directory.Delete(tempDir, true);

        if (File.Exists(finalCapuCosmetic))
        {
            Debug.Log($"Successfully created {finalCapuCosmetic}(Yipeee)");
            using (var archive = ZipFile.OpenRead(finalCapuCosmetic))
            {
                Debug.Log("Contents of created .capucosmetic file:");
                foreach (var entry in archive.Entries)
                {
                    Debug.Log($" - {entry.Name} ({entry.Length} bytes)");
                }
            }
            
            EditorUtility.DisplayDialog("Success", "CapuCosmetic created at:\n" + finalCapuCosmetic, "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Error", "Failed to create CapuCosmetic file", "OK");
        }
    }
}

[System.Serializable]
public class CapuCosmeticMetadata
{
    public string name = "My cool CapuCosmetic!";
    public string author = "yourname";
    public int version = 1;
    public string description = "my very cool Capuchin custom cosmetic!";
    public bool syncToRightHand = false;
    public bool syncToLeftHand = false;
}