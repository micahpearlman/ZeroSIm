﻿
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ZO.ImportExport;

namespace ZO.Export {

    public class ZOExportOBJEditor : EditorWindow {
        public bool _exportSubMeshes = true;
        public bool _applyLocalTransform = false;
        public ZOExportOBJ.Orientation _orientation = ZOExportOBJ.Orientation.URDF;
        private string _selectedNames;

        [MenuItem("Zero Sim/Export OBJ...")]
        public static void DoOBJExport() {
            //EditorWindow.GetWindow<ZOExportOBJ>();
            ZOExportOBJEditor window = ScriptableObject.CreateInstance<ZOExportOBJEditor>();
            window.ShowUtility();

        }

        private void OnGUI() {
            _exportSubMeshes = EditorGUILayout.Toggle("Export Sub Meshes: ", _exportSubMeshes);
            _applyLocalTransform = EditorGUILayout.Toggle("Apply Local Transform: ", _applyLocalTransform);
            _orientation = (ZOExportOBJ.Orientation)EditorGUILayout.EnumPopup("Export orientation:", _orientation);

            foreach (var t in Selection.objects) {
                _selectedNames += t.name + " ";
            }
            EditorGUILayout.LabelField("Selected Objects: ", _selectedNames);

            _selectedNames = "";

            EditorGUI.BeginDisabledGroup(Selection.gameObjects.Length == 0);
            if (GUILayout.Button("Export OBJ")) {
                string meshName = Selection.gameObjects[0].name;
                string fileDirectory = EditorUtility.OpenFolderPanel("Export .OBJ to directory", "", "");

                DoExport(_exportSubMeshes, fileDirectory, _applyLocalTransform, _orientation);
            }
            EditorGUI.EndDisabledGroup();

        }

        private void OnInspectorUpdate() {
            Repaint();
        }

        public static void DoExport(bool makeSubmeshes, string directoryPath, bool applyLocalTransform, ZOExportOBJ.Orientation orientation, GameObject gameObject = null) {

            if (gameObject == null && Selection.gameObjects.Length == 0) {
                Debug.Log("ERROR: Didn't Export Any Meshes; Nothing was selected!");
                return;
            }

            if (gameObject == null) {
                gameObject = Selection.gameObjects[0];
            }
            ZOExportOBJ exportOBJ = new ZOExportOBJ();
            exportOBJ.ExportToDirectory(gameObject, directoryPath, makeSubmeshes, applyLocalTransform, orientation);
        }

    }
}