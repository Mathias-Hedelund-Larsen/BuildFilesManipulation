using System.IO;
using UnityEngine;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using System.Collections.Generic;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
#if PREFABULOUS_ENABLED
using HephaestusForge.PrefabManagement;
#endif

namespace HephaestusForge
{
    namespace BuildFilesManupalation
    {
        public class ManipulateFilesBeforeBuild : IPreprocessBuildWithReport
        {
            private const string FILE_NAMES_PATH = "FileNames.txt";
            private const string SET_BEFORE_BUILD = "_onlyForBuild";
            private const string LOOK_FOR_STRING_CONTAINING = "_onlyForInspector";
            public int callbackOrder => 0;

            public void OnPreprocessBuild(BuildReport report)
            {
                var activeScenePath = EditorSceneManager.GetActiveScene().path;
                var untitled = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                var prefabGuids = AssetDatabase.FindAssets($"t:Prefab");
                var assetGuids = AssetDatabase.FindAssets($"t:{typeof(ScriptableObject)}");
                var sceneGuids = AssetDatabase.FindAssets($"t:SceneAsset");

                ManipulateFiles(prefabGuids, out bool prefabChangesMade);
                ManipulateFiles(assetGuids, out bool assetChangesMade);
                ManipulateFiles(sceneGuids, out bool sceneChangesMade);

                if (prefabChangesMade || assetChangesMade || sceneChangesMade)
                {
                    EditorSceneManager.OpenScene(activeScenePath, OpenSceneMode.Single);
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                }
            }

            private void ManipulateFiles(string[] guids, out bool changesMade)
            {
                string dataToWrite = string.Empty;
                changesMade = false;

                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    string readerData = string.Empty;

                    using (StreamReader reader = new StreamReader(path))
                    {
                        readerData = reader.ReadToEnd();

                        if (readerData.Contains(LOOK_FOR_STRING_CONTAINING))
                        {
                            var textSplit = new List<string>(readerData.Split('\n'));

                            for (int t = textSplit.Count - 1; t >= 0; t--)
                            {
                                if (textSplit[t].Contains(LOOK_FOR_STRING_CONTAINING))
                                {
                                    string placeHolder = textSplit[t];
                                    textSplit.RemoveAt(t);

                                    List<int> arrayElements = new List<int>();

                                    for (int x = t; x < textSplit.Count; x++)
                                    {
                                        if (textSplit[x].Contains(" - "))
                                        {
                                            arrayElements.Add(x);
                                        }
                                        else if (textSplit[x].Contains(SET_BEFORE_BUILD))
                                        {
                                            var setBeforeBuildSplit = textSplit[x].Split('_').ToList();

#if PREFABULOUS_ENABLED
                                            if (setBeforeBuildSplit[2] == "Prefabulous")
                                            {
                                                var prefabulous = AssetDatabase.LoadAssetAtPath<Prefabulous>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets($"t:{typeof(Prefabulous)}")[0]));
                                                var guid = placeHolder.Split(':')[1].Remove(0, 1);
                                                var fieldName = textSplit[x].Split(':')[0];
                                                fieldName = fieldName.Remove(0, 1);
                                                fieldName = fieldName.Insert(4, "-");
                                                string data = $"{fieldName}: {prefabulous.GetAssetIndex(guid)}";
                                                textSplit[x] = data;
                                            }
#endif
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }

                                    for (int z = arrayElements.Count - 1; z >= 0; z--)
                                    {
                                        textSplit.RemoveAt(arrayElements[z]);
                                    }
                                }
                            }

                            dataToWrite = string.Join("\n", textSplit);

                            changesMade = true;
                        }
                    }

                    if (dataToWrite != string.Empty)
                    {
                        using (StreamWriter writer = new StreamWriter(File.Open(path, FileMode.Create)))
                        {
                            writer.Write(dataToWrite);
                            dataToWrite = string.Empty;
                        }

                        string fileNamesPath = $"{Application.persistentDataPath}/{FILE_NAMES_PATH}";

                        if (!File.Exists(fileNamesPath))
                        {
                            using (File.Create(fileNamesPath)) { }
                        }

                        using (StreamWriter writer = new StreamWriter(new FileStream(fileNamesPath, FileMode.Append, FileAccess.Write)))
                        {
                            writer.Write($"{Path.GetFileName(path)}\n");
                        }

                        string persistantPath = $"{Application.persistentDataPath}/{Path.GetFileName(path)}";

                        using (StreamWriter writer = new StreamWriter(File.Open(persistantPath, FileMode.Create)))
                        {
                            writer.Write($"{path}\n");
                            writer.Write(readerData);
                        }
                    }
                }
            }

            [PostProcessBuild(0)]
            private static void GetOriginalFiles(BuildTarget target, string pathToBuiltProject)
            {
                EditorApplication.delayCall += () =>
                {
                    string fileNamesPath = $"{Application.persistentDataPath}/{FILE_NAMES_PATH}";

                    if (File.Exists(fileNamesPath))
                    {
                        List<string> filesToLoad = new List<string>();

                        using (StreamReader reader = new StreamReader(fileNamesPath))
                        {
                            string fileNames = reader.ReadToEnd();

                            var fileNamesSplit = fileNames.Split('\n');

                            for (int i = 0; i < fileNamesSplit.Length; i++)
                            {
                                if (fileNamesSplit[i] != string.Empty)
                                {
                                    filesToLoad.Add($"{Application.persistentDataPath}/{fileNamesSplit[i]}");
                                }
                            }
                        }

                        File.Delete(fileNamesPath);

                        for (int i = 0; i < filesToLoad.Count; i++)
                        {
                            using (StreamReader reader = new StreamReader(filesToLoad[i]))
                            {
                                string fileData = reader.ReadToEnd();

                                var fileDataSplit = fileData.Split('\n').ToList();

                                string assetPath = fileDataSplit[0];
                                fileDataSplit.RemoveAt(0);

                                string assetData = string.Join("\n", fileDataSplit);

                                using (StreamWriter writer = new StreamWriter(File.Open(assetPath, FileMode.Create)))
                                {
                                    writer.Write(assetData);
                                }
                            }

                            File.Delete(filesToLoad[i]);
                        }

                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                    }
                };
            }
        }
    }
}
