using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Platybytes.Llave.Editor
{
    // Menú del editor: "Platybytes > Rescata la llave > Crear escena".
    // Crea la escena del minijuego con un único objeto que lleva el componente RescataLaLlave
    // y la añade a la lista de escenas de la build SIN quitar las de tus compañeros.
    public static class RescataLaLlaveSetup
    {
        public const string RutaEscena = "Assets/Platybytes/RescataLaLlave/Escena/RescataLaLlave.unity";

        [MenuItem("Platybytes/Rescata la llave/Crear escena")]
        static void CrearEscena()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (!File.Exists(RutaEscena))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(RutaEscena));
                var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("Rescata la llave").AddComponent<RescataLaLlave>();
                EditorSceneManager.SaveScene(escena, RutaEscena);
            }

            // Añadir a Build Settings si no está ya
            List<EditorBuildSettingsScene> lista = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool yaEsta = false;
            foreach (EditorBuildSettingsScene e in lista)
            {
                if (e.path == RutaEscena) yaEsta = true;
            }
            if (!yaEsta)
            {
                lista.Add(new EditorBuildSettingsScene(RutaEscena, true));
                EditorBuildSettings.scenes = lista.ToArray();
            }

            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(RutaEscena);
            EditorUtility.DisplayDialog("Rescata la llave",
                "Escena lista en:\n" + RutaEscena + "\n\nPulsa Play para jugar. En el ordenador: flechas o WASD.",
                "¡Vamos!");
        }
    }
}
