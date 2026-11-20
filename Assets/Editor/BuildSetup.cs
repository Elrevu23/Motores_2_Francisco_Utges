using UnityEditor;
using UnityEngine;

// Configura la lista de escenas del build: Hub primero (es la que arranca el juego), después el mapa.
public static class BuildSetup
{
    [MenuItem("Extraccion/Configurar build")]
    public static void ConfigureScenes()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/Hub.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", true),
        };
        Debug.Log("Escenas del build: Hub (inicio) y SampleScene.");
    }
}
