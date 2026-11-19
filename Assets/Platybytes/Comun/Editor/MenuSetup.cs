using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Platybytes.Comun.EditorTools
{
    // Menú del editor: "Platybytes > Menú > Crear escena del menú".
    // 1. Crea la escena Menu con el componente MenuPrincipal.
    // 2. Crea la escena de Rescata la llave si todavía no existe.
    // 3. Ordena la lista de escenas de la build: Menu, PressTheButton, RescataLaLlave (y después las demás).
    public static class MenuSetup
    {
        const string RutaMenu = "Assets/Platybytes/Comun/Menu.unity";
        const string RutaPressTheButton = "Assets/Platybytes/PressTheButton/PressTheButton.unity";
        const string RutaRescataLaLlave = "Assets/Platybytes/RescataLaLlave/Escena/RescataLaLlave.unity";

        [MenuItem("Platybytes/Menú/Crear escena del menú")]
        static void Crear()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (!File.Exists(RutaMenu))
            {
                var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("Menu principal").AddComponent<MenuPrincipal>();
                EditorSceneManager.SaveScene(escena, RutaMenu);
            }

            if (!File.Exists(RutaRescataLaLlave))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(RutaRescataLaLlave));
                var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("Rescata la llave").AddComponent<Platybytes.Llave.RescataLaLlave>();
                EditorSceneManager.SaveScene(escena, RutaRescataLaLlave);
            }

            // Orden de la build: primero las tres escenas de la partida, después cualquier otra que ya hubiera
            string[] orden = { RutaMenu, RutaPressTheButton, RutaRescataLaLlave };
            List<EditorBuildSettingsScene> lista = new List<EditorBuildSettingsScene>();
            foreach (string ruta in orden)
            {
                if (File.Exists(ruta)) lista.Add(new EditorBuildSettingsScene(ruta, true));
                else Debug.LogWarning("MenuSetup: no encuentro la escena " + ruta);
            }
            foreach (EditorBuildSettingsScene e in EditorBuildSettings.scenes)
            {
                if (System.Array.IndexOf(orden, e.path) < 0) lista.Add(e);
            }
            EditorBuildSettings.scenes = lista.ToArray();

            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(RutaMenu);
            EditorUtility.DisplayDialog("Prison Escape",
                "Escena del menú lista y escenas ordenadas en la build:\n\n1. Menu\n2. PressTheButton\n3. RescataLaLlave\n\nPulsa Play para jugar la partida completa.",
                "¡Vamos!");
        }
    }
}
