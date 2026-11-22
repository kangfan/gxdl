using System;
using MCPForUnity.Editor.Services;
using MCPForUnity.Editor.Services.Transport;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class GxdlMcpBootstrap
{
    static GxdlMcpBootstrap()
    {
        EditorApplication.delayCall += Connect;
    }

    private static async void Connect()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += Connect;
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        try
        {
            // The server is started separately; this does not change shared EditorPrefs.
            if (MCPServiceLocator.TransportManager.IsRunning(TransportMode.Http)) return;
            bool connected = await MCPServiceLocator.TransportManager.StartAsync(TransportMode.Http);
            if (connected) Debug.Log("[Gxdl MCP] HTTP bridge connected.");
            else Debug.LogWarning("[Gxdl MCP] Start Tools/start-unity-mcp.ps1, then use Window > MCP for Unity to connect.");
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[Gxdl MCP] " + exception.Message);
        }
    }
}

