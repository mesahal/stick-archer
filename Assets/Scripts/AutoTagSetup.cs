using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor script to automatically set up required tags and layers.
/// Run via menu: Tools → Setup Stick Archers Tags
/// </summary>
#if UNITY_EDITOR
public class AutoTagSetup : MonoBehaviour
{
    [MenuItem("Tools/Setup Stick Archers Tags")]
    static void SetupTagsAndLayers()
    {
        // Setup Tag
        string tagName = "Arena";
        if (!TagExists(tagName))
        {
            AddTag(tagName);
            Debug.Log($"[AutoTagSetup] Added tag: {tagName}");
        }
        else
        {
            Debug.Log($"[AutoTagSetup] Tag '{tagName}' already exists");
        }
        
        // Setup Layers
        string[] layerNames = { "Ground", "HitZone", "Arrow" };
        foreach (var layer in layerNames)
        {
            int layerIndex = GetLayerIndex(layer);
            if (layerIndex == -1)
            {
                AddLayer(layer);
                Debug.Log($"[AutoTagSetup] Added layer: {layer}");
            }
            else
            {
                Debug.Log($"[AutoTagSetup] Layer '{layer}' already exists at index {layerIndex}");
            }
        }
        
        Debug.Log("[AutoTagSetup] Tag and layer setup complete!");
    }
    
    static bool TagExists(string tag)
    {
        for (int i = 0; i < UnityEditorInternal.InternalEditorUtility.tags.Length; i++)
        {
            if (UnityEditorInternal.InternalEditorUtility.tags[i].Equals(tag))
                return true;
        }
        return false;
    }
    
    static void AddTag(string tag)
    {
        UnityEditorInternal.InternalEditorUtility.AddTag(tag);
    }
    
    static int GetLayerIndex(string layer)
    {
        for (int i = 0; i < 32; i++)
        {
            if (LayerMask.LayerToName(i).Equals(layer))
                return i;
        }
        return -1;
    }
    
    static void AddLayer(string layerName)
    {
        var tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layersProp = tagManager.FindProperty("layers");

        for (int i = 8; i < layersProp.arraySize; i++)
        {
            var element = layersProp.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(element.stringValue))
            {
                element.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return;
            }
        }
        Debug.LogWarning("[AutoTagSetup] No available layer slots!");
    }
    
    [MenuItem("Tools/Check Stick Archers Setup")]
    static void CheckSetup()
    {
        bool hasIssues = false;
        
        // Check tags
        if (!TagExists("Arena"))
        {
            Debug.LogError("[SetupCheck] Missing tag: 'Arena' - Run Tools → Setup Stick Archers Tags");
            hasIssues = true;
        }
        
        // Check layers
        string[] requiredLayers = { "Ground" };
        foreach (var layer in requiredLayers)
        {
            if (GetLayerIndex(layer) == -1)
            {
                Debug.LogError($"[SetupCheck] Missing layer: '{layer}' - Run Tools → Setup Stick Archers Tags");
                hasIssues = true;
            }
        }
        
        // Check prefab assignments
        var bootstrap = FindObjectOfType<GameArenaBootstrap>();
        if (bootstrap != null)
        {
            if (bootstrap.archerLocalPrefab == null)
            {
                Debug.LogError("[SetupCheck] GameArenaBootstrap.archerLocalPrefab is not assigned!");
                hasIssues = true;
            }
            if (bootstrap.arrowLocalPrefab == null)
            {
                Debug.LogError("[SetupCheck] GameArenaBootstrap.arrowLocalPrefab is not assigned!");
                hasIssues = true;
            }
        }
        
        // Check buttons
        var mainMenu = FindObjectOfType<MainMenuController>();
        if (mainMenu != null)
        {
            if (mainMenu.playOnlineButton == null)
            {
                Debug.LogWarning("[SetupCheck] MainMenuController.playOnlineButton is not assigned (will auto-create).");
            }
        }
        
        if (!hasIssues)
        {
            Debug.Log("[SetupCheck] ✓ All setup checks passed!");
        }
    }
}
#endif
