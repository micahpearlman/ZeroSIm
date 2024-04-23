using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ZO.ImportExport {
    public class ZOExportOBJ {

        public enum Orientation {
            URDF,
            Unity // Unity Default direction
        }

        protected string _objString = null;
        public string OBJString {
            get { return _objString; }
            protected set {
                _objString = value;
            }
        }

        protected string _mtlLibraryString = null;
        public string MtlLibraryString {
            get { return _mtlLibraryString; }
            protected set {
                _mtlLibraryString = value;
            }
        }

        List<string> _textureAssetPaths = new List<string>();
        public List<string> TextureAssetPaths {
            get { return _textureAssetPaths; }
        }

        private int _startIndex = 0;

        protected void Start() {
            _startIndex = 0;
        }
        protected void End() {
            _startIndex = 0;
        }

        protected string MeshToString(Mesh mesh, ZOExportOBJ.Orientation orientation, Vector3 scale) {

            int numVertices = 0;

            StringBuilder sb = new StringBuilder();

            foreach (Vector3 vv in mesh.vertices) {
                Vector3 v = new Vector3(vv.x * scale.x, vv.y * scale.y, vv.z * scale.z);
                numVertices++;
                if (orientation == Orientation.Unity) {
                    sb.AppendLine($"v {v.x} {v.y} {v.z}");
                } else if (orientation == Orientation.URDF) {
                    sb.AppendLine($"v {v.z} {v.x} {v.y}");
                }
            }
            sb.AppendLine();
            foreach (Vector3 nn in mesh.normals) {
                Vector3 v = nn;
                if (orientation == Orientation.Unity) {
                    sb.AppendLine($"vn {v.x} {v.y} {v.z}");
                } else if (orientation == Orientation.URDF) {
                    sb.AppendLine($"vn {v.z} {v.x} {v.y}");
                }
            }

            for (int submesh = 0; submesh < mesh.subMeshCount; submesh++) {
                int[] triangles = mesh.GetTriangles(submesh);
                for (int i = 0; i < triangles.Length; i += 3) {
                    sb.AppendLine(string.Format("f {0}//{0} {1}//{1} {2}//{2}", // only exporting position and normal, no UV
                                        triangles[i] + 1 + _startIndex,
                                        triangles[i + 1] + 1 + _startIndex,
                                        triangles[i + 2] + 1 + _startIndex));

                }

            }

            _startIndex += numVertices;
            return sb.ToString();
        }


        protected string MeshFilterToString(MeshFilter meshFilter, Transform transform, bool applyLocalTransform, ZOExportOBJ.Orientation orientation) {
            Vector3 s = transform.localScale;
            Vector3 p = transform.localPosition;
            Quaternion r = transform.localRotation;

            if (applyLocalTransform == false) {
                s = Vector3.one;
                p = Vector3.zero;
                r = Quaternion.identity;
            }


            int numVertices = 0;
            Mesh mesh = meshFilter.sharedMesh;
            if (!mesh) {
                return "####Error####";
            }
            Material[] mats = meshFilter.GetComponent<Renderer>().sharedMaterials;

            StringBuilder sb = new StringBuilder();

            foreach (Vector3 vv in mesh.vertices) {
                Vector3 v = vv;
                if (applyLocalTransform == true) {
                    v = transform.TransformPoint(vv);
                }
                numVertices++;
                if (orientation == Orientation.Unity) {
                    sb.AppendLine($"v {v.x} {v.y} {v.z}");
                } else if (orientation == Orientation.URDF) {
                    sb.AppendLine($"v {v.z} {-v.x} {v.y}");
                }
            }
            sb.AppendLine();
            foreach (Vector3 nn in mesh.normals) {
                Vector3 v = r * nn;
                if (orientation == Orientation.Unity) {
                    sb.AppendLine($"vn {v.x} {v.y} {v.z}");
                } else if (orientation == Orientation.URDF) {
                    sb.AppendLine($"vn {v.z} {-v.x} {v.y}");
                }
            }
            sb.AppendLine();
            foreach (Vector3 v in mesh.uv) {
                sb.AppendLine($"vt {v.x} {v.y}");
            }

            for (int material = 0; material < mesh.subMeshCount; material++) {
                sb.Append("\n");
                sb.Append("usemtl ").Append(mats[material].name).Append("\n");
                sb.Append("usemap ").Append(mats[material].name).Append("\n");

                int[] triangles = mesh.GetTriangles(material);
                for (int i = 0; i < triangles.Length; i += 3) {

                    if (orientation == Orientation.Unity) {
                        sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n",
                                            triangles[i] + 1 + _startIndex,
                                            triangles[i + 1] + 1 + _startIndex,
                                            triangles[i + 2] + 1 + _startIndex));
                    } else if (orientation == Orientation.URDF) {
                        sb.Append(string.Format("f {2}/{2}/{2} {1}/{1}/{1} {0}/{0}/{0}\n",
                                            triangles[i] + 1 + _startIndex,
                                            triangles[i + 1] + 1 + _startIndex,
                                            triangles[i + 2] + 1 + _startIndex));
                    }


                }
            }

            _startIndex += numVertices;
            return sb.ToString();
        }

        protected string MaterialToString(Material material) {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("newmtl {0}", material.name).AppendLine();
            if (material.HasProperty("_Color")) {
                Color c = material.GetColor("_Color");
                sb.AppendFormat("Kd {0} {1} {2}", c.r, c.g, c.b).AppendLine();
            }
            if (material.HasProperty("_MainTex")) {
#if UNITY_EDITOR // AssetDatabase not available during runtime
                string assetPath = AssetDatabase.GetAssetPath(material.GetTexture("_MainTex"));
                string texName = Path.GetFileName(assetPath);
                sb.AppendFormat("map_Kd {0}", texName).AppendLine();
#endif // #if UNITY_EDITOR 

            }
            return sb.ToString();
        }

        protected string ProcessTransform(Transform transform, bool makeSubmeshes, bool applyLocalTransform, ZOExportOBJ.Orientation orientation) {
            StringBuilder meshString = new StringBuilder();

            meshString.Append("#" + transform.name
                              + "\n#-------"
                              + "\n");

            if (makeSubmeshes) {
                meshString.Append("g ").Append(transform.name).Append("\n");
            }

            MeshFilter meshFilter = transform.GetComponent<MeshFilter>();
            if (meshFilter != null) {
                meshString.Append(MeshFilterToString(meshFilter, transform, applyLocalTransform, orientation));
            }

            for (int i = 0; i < transform.childCount; i++) {
                meshString.Append(ProcessTransform(transform.GetChild(i), makeSubmeshes, applyLocalTransform, orientation));
            }

            return meshString.ToString();
        }


        public void BuildExportData(GameObject gameObject, bool makeSubmeshes, bool applyLocalTransform, ZOExportOBJ.Orientation orientation) {
            Debug.Assert(gameObject != null, "ERROR: invalid GameObject in BuildExport");

            string meshName = gameObject.name;

            Start();

            StringBuilder meshString = new StringBuilder();

            meshString.Append("#" + meshName + ".obj"
                              + "\n#" + System.DateTime.Now.ToLongDateString()
                              + "\n#" + System.DateTime.Now.ToLongTimeString()
                              + "\n#-------"
                              + "\n\n");

            meshString.AppendLine($"mtllib {meshName}.mtl");

            Transform transform = gameObject.transform;

            // Vector3 originalPosition = transform.position;
            // if (zeroPosition == true) {
            //     transform.position = Vector3.zero;
            // }

            if (!makeSubmeshes) {
                meshString.Append("g ").Append(transform.name).Append("\n");
            }
            meshString.Append(ProcessTransform(transform, makeSubmeshes, applyLocalTransform, orientation));

            OBJString = meshString.ToString();

            // transform.position = originalPosition;

            // export materials and textures
            StringBuilder mtlFileString = new StringBuilder();
            HashSet<string> materialNames = new HashSet<string>();
            MeshFilter meshFilter = transform.GetComponent<MeshFilter>();
            if (meshFilter != null) {
                Material[] materials = meshFilter.GetComponent<Renderer>().sharedMaterials;
                foreach (Material material in materials) {
                    if (materialNames.Contains(material.name) == false) {
                        string materialString = MaterialToString(material);
                        mtlFileString.Append(materialString);
                        materialNames.Add(material.name);

                        // handle texture
                        if (material.HasProperty("_MainTex")) {
#if UNITY_EDITOR // AssetDatabase not available during runtime                           
                            string assetPath = AssetDatabase.GetAssetPath(material.GetTexture("_MainTex"));
                            if (string.IsNullOrEmpty(assetPath) == false) {
                                TextureAssetPaths.Add(assetPath);
                            }
#endif // UNITY_EDITOR                            
                        }
                    }
                }
            }


            // go through the children materials
            for (int i = 0; i < transform.childCount; i++) {
                Transform child = transform.GetChild(i);
                meshFilter = child.GetComponent<MeshFilter>();

                if (meshFilter != null) {
                    Material[] materials = meshFilter.GetComponent<Renderer>().sharedMaterials;
                    foreach (Material material in materials) {
                        if (materialNames.Contains(material.name) == false) {
                            string materialString = MaterialToString(material);
                            mtlFileString.Append(materialString);
                            materialNames.Add(material.name);
                            // handle texture
                            if (material.HasProperty("_MainTex")) {
#if UNITY_EDITOR // AssetDatabase not available during runtime
                                string assetPath = AssetDatabase.GetAssetPath(material.GetTexture("_MainTex"));
                                if (string.IsNullOrEmpty(assetPath) == false) {
                                    TextureAssetPaths.Add(assetPath);
                                }
#endif // UNITY_EDITOR                                
                            }
                        }
                    }
                }
            }

            MtlLibraryString = mtlFileString.ToString();

            End();


        }

        public void ExportToDirectory(GameObject gameObject, string directoryPath, bool makeSubmeshes, bool applyLocalTransform, ZOExportOBJ.Orientation orientation) {
            BuildExportData(gameObject, makeSubmeshes, applyLocalTransform, orientation);

            // write out obj file
            string objFilePath = Path.Combine(directoryPath, $"{gameObject.name}.obj");
            using (StreamWriter sw = new StreamWriter(objFilePath)) {
                sw.Write(OBJString);
            }

            // write out material library file
            string mtlFilePath = Path.Combine(directoryPath, $"{gameObject.name}.mtl");
            using (StreamWriter sw = new StreamWriter(mtlFilePath)) {
                sw.Write(MtlLibraryString);
            }

            // copy the textures
            foreach (string sourceTexturePath in TextureAssetPaths) {
                File.Copy(sourceTexturePath, Path.Combine(directoryPath, Path.GetFileName(sourceTexturePath)), true);
            }

        }

        public void ExportMesh(Mesh mesh, string meshPath, ZOExportOBJ.Orientation orientation, Vector3 scale) {
            Start();

            StringBuilder meshString = new StringBuilder();

            meshString.Append("#" + Path.GetFileName(meshPath)
                              + "\n#" + System.DateTime.Now.ToLongDateString()
                              + "\n#" + System.DateTime.Now.ToLongTimeString()
                              + "\n#-------"
                              + "\n\n");

            meshString.Append(MeshToString(mesh, orientation, scale));

            OBJString = meshString.ToString();

            using (StreamWriter sw = new StreamWriter(meshPath)) {
                sw.Write(OBJString);
            }
        }
    }

}