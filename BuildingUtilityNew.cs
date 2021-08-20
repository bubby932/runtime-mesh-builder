namespace VigorXR.Utilities.UNSTABLE
{
    using UnityEngine;
    using UnityEngine.ProBuilder;
    using UnityEngine.ProBuilder.MeshOperations;

    using Unity.RuntimeSceneSerialization;
    using System.Linq;
    using UnityEngine.SceneManagement;
    using System.IO;
    using System.Collections.Generic;
    using UnityEngine.Rendering;
    using System;
    using UnityEditor.ProBuilder;
    using ProBuilder.Examples;
    using Parabox.CSG;
    using Newtonsoft.Json;

    public class BuildingUtilityNew : MonoBehaviour
    {
        ProBuilderMesh pb;

        
        List<ObjectData> data = new List<ObjectData>();

        SelectionObject selection;

        List<Face> faceSelection = new List<Face>();

        List<Vector3> faceSelectionNormals = new List<Vector3>();

        RaycastHit FaceRayHit;

        [SerializeField] Material[] ObjectMaterials;

        [Range(0, 4)]
        [SerializeField] int materialSelection;

        MeshAndFace m_Selection;

        DragState m_DragState = new DragState();

        public GameObject other;

        [System.Obsolete]
        void OnGUI()
        {

            if (GUILayout.Button("Create probuilder object"))
            {
                pb = ShapeGenerator.CreateShape(ShapeType.Cube, PivotLocation.Center);

                pb.ToTriangles(pb.faces);

                pb.ToMesh();

                pb.Refresh();

                pb.gameObject.AddComponent<MeshCollider>();

                pb.gameObject.AddComponent<SerializedObject>();

                pb.gameObject.AddComponent<SelectionObject>();

                selection = pb.gameObject.GetComponent<SelectionObject>();

                selection.GetSerializedObject.ObjectSerializationType = SerializedType.PlayerMeshObject;

                MeshUtility.CollapseSharedVertices(pb.GetComponent<MeshFilter>().mesh);

                pb.Refresh();
            }

            if (GUILayout.Button("Create empty object"))
            {

                var _go = CreateEmptyObject();

                selection = _go.GetComponent<SelectionObject>();
            }

            if (GUILayout.Button("ProBuilder Only: Extrude Face"))
            {
                if(selection.GetSerializedObject.ObjectSerializationType == SerializedType.PlayerMeshObject)
                {
                    var _pb = selection.gameObject.GetComponent<ProBuilderMesh>();

                    _pb.Extrude(faceSelection.ToArray() ?? new Face[] { _pb.faces.First()}, ExtrudeMethod.FaceNormal, 1f);

                    _pb.ToTriangles(_pb.faces);
                    
                    _pb.ToMesh();

                    _pb.Refresh();

                    MeshUtility.CollapseSharedVertices(_pb.GetComponent<MeshFilter>().mesh);

                    _pb.GetComponent<MeshCollider>().sharedMesh = null;
                    _pb.GetComponent<MeshCollider>().sharedMesh = _pb.GetComponent<MeshFilter>().mesh;
                }
                else
                {
                    Debug.LogAssertion("Attempted to extrude face on a non-mesh object!");
                }
                
            }

            if(GUILayout.Button("Destroy Selection"))
            {
                Destroy(selection.gameObject);
            }

            if (GUILayout.Button("Serialize All Objects"))
            {
                var _objs = FindObjectsOfType<SerializedObject>();

                foreach (var item in _objs)
                {
                    data.Add(item.PrepareForSerialization());
                }
                var json = JsonConvert.SerializeObject(data, settings: new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                }, formatting: Formatting.Indented);

                var path = $"{Application.persistentDataPath}/Vigor XR Room Data.json";

                StreamWriter writer = new StreamWriter(path, false);



                writer.WriteLine(json);

                writer.Close();
            }

            if (GUILayout.Button("Deserialize All Objects"))
            {
                
                var prev_objs = FindObjectsOfType<SerializedObject>();

                var path = $"{Application.persistentDataPath}/Vigor XR Room Data.json";

                var json = File.ReadAllText(path);

                JsonConvert.PopulateObject(json, data);

                foreach (var item in prev_objs)
                {
                    Destroy(item.gameObject);
                }

                foreach(var item in data)
                {
                    var obj = new GameObject("Vigor Custom Object");

                    var serial = obj.AddComponent<SerializedObject>();
                    var sel = obj.AddComponent<SelectionObject>();

                    if(item.ObjectType == SerializedType.PlayerMeshObject)
                    {
                        var _pb = obj.AddComponent<ProBuilderMesh>();
                        var col = obj.AddComponent<MeshCollider>();
                    }

                    serial.Rebuild(item);
                }
            }

            if (GUILayout.Button("Set Object Selection From Camera"))
            {

                Physics.Raycast(origin: Camera.main.transform.position, direction: Camera.main.transform.forward, out RaycastHit hitInfo, maxDistance: 50f);

                selection = hitInfo.collider.gameObject.GetComponent<SelectionObject>();


            }

            if (GUILayout.Button("Add Face Selection From Camera"))
            {
                Physics.Raycast(origin: Camera.main.transform.position, direction: Camera.main.transform.forward, out RaycastHit hitInfo, maxDistance: 50f);

                if(selection != hitInfo.collider.gameObject.GetComponent<SelectionObject>()) selection = hitInfo.collider.gameObject.GetComponent<SelectionObject>();

                faceSelection.Add(selection.GetComponent<ProBuilderMesh>().faces[hitInfo.triangleIndex]);

                FaceRayHit = hitInfo;

                faceSelectionNormals.Add(hitInfo.normal);
            }

            if (GUILayout.Button("Remove Face Selection From Camera"))
            {
                Physics.Raycast(origin: Camera.main.transform.position, direction: Camera.main.transform.forward, out RaycastHit hitInfo, maxDistance: 50f);

                if (selection != hitInfo.collider.gameObject.GetComponent<SelectionObject>()) selection = hitInfo.collider.gameObject.GetComponent<SelectionObject>();

                faceSelection.Remove(selection.GetComponent<ProBuilderMesh>().faces[hitInfo.triangleIndex]);

                faceSelectionNormals.Remove(hitInfo.normal);
            }

            if(GUILayout.Button("Clear Selection"))
            {
                selection = null;
                faceSelection = new List<Face>();
            }

            if (GUILayout.Button("Move Selected Faces up by 1m on Y axis"))
            {

                Vector3 offset = new Vector3(0, 1, 0);
                MoveFace(selection.GetComponent<ProBuilderMesh>(), faceSelection, offset);
                
            }

            if(GUILayout.Button("Set face material"))
            {
                    selection.GetComponent<ProBuilderMesh>().SetMaterial(faceSelection.ToArray(), ObjectMaterials[materialSelection]);
            }

            if(GUILayout.Button("Rotate selected faces 90 Degrees"))
            {

                RotateSelectedFaces(new Vector3(1, 0, 0), 45f);
            }

            if(GUILayout.Button("Scale selected faces 2x"))
            {
                ScaleSelectedFaces(new Vector3(2,2,2), false);
            }

            if (GUILayout.Button("Scale selected faces 0.5x"))
            {
                ScaleSelectedFaces(new Vector3(.5f, .5f, .5f), false);
            }

            if(GUILayout.Button("difference bool"))
            {
                CSG_Model result = Parabox.CSG.Boolean.Subtract(selection.gameObject, other);

                pb = selection.GetComponent<ProBuilderMesh>();

                selection.GetComponent<MeshFilter>().sharedMesh = result.mesh;

                selection.GetComponent<MeshRenderer>().sharedMaterials = result.materials.ToArray();

                pb.Refresh();

                pb.GetComponent<MeshCollider>().sharedMesh = null;
                pb.GetComponent<MeshCollider>().sharedMesh = pb.GetComponent<MeshFilter>().mesh;
            }
        }


        private GameObject CreateEmptyObject()
        {
            var _gO = new GameObject("Empty Player Object");

            _gO.AddComponent(typeof(SerializedObject));
            _gO.GetComponent<SerializedObject>().ObjectSerializationType = SerializedType.PlayerEmptyObject;
            _gO.AddComponent(typeof(SelectionObject));
            return _gO;
        }

        /// <summary>
        /// Scales the face selection.
        /// </summary>
        /// <param name="factor">scale factor, 0 = 0 scale, 1 = no scale difference, 2 = 200% scale, etc.</param>
        /// <param name="all">whether the origin of scaling should be individual (false) or between all faces (true) </param>
        void ScaleSelectedFaces(Vector3 factor, bool all)
        {
            List<int> modifiedVertices = new List<int>();
            ProBuilderMesh _pb = selection.GetComponent<ProBuilderMesh>();
            Vertex[] _vs = _pb.GetVertices();

            _pb.ToTriangles(_pb.faces);

            List<Vector3> vs = new List<Vector3>();

            foreach (var _item in _vs)
            {
                vs.Add(_item.position);
            }

            var av = UnityEngine.ProBuilder.Math.Average(vs.ToArray());

            av -= _pb.transform.position;

            Matrix4x4 _m;

            _m = Matrix4x4.TRS(Vector3.zero - _pb.transform.position, Quaternion.Euler(new Vector3(0, 0, 0)), factor);



            //_m.SetTRS(Vector3.zero - av, Quaternion.Euler(0,0,0), factor);


            //_m = Matrix4x4.identity;

            //_m = Matrix4x4.Scale(factor);

            foreach (var item in faceSelection)
            {
                

                var vertices = item.indexes;

                var indices = _pb.sharedVertices;

                foreach (var _item in vertices)
                {
                    var pos = _m.MultiplyVector(_vs[_item].position); //_m.MultiplyPoint(_pb.transform.TransformPoint(_vs[_item].position));

                    
                    pos -= _pb.transform.position;

                    _vs[_item].position = pos;
                }


                
                _pb.SetVertices(_vs);

                

                _pb.ToTriangles(_pb.faces);
                _pb.ToMesh();

                _pb.Refresh();

                pb.GetComponent<MeshCollider>().sharedMesh = null;
                pb.GetComponent<MeshCollider>().sharedMesh = pb.GetComponent<MeshFilter>().mesh;




            }

            _pb.ToTriangles(_pb.faces);
            _pb.ToMesh();

            _pb.Refresh();

            pb.GetComponent<MeshCollider>().sharedMesh = null;
            pb.GetComponent<MeshCollider>().sharedMesh = pb.GetComponent<MeshFilter>().mesh;

            modifiedVertices.Clear();
        }

        void RotateSelectedFaces(Vector3 axis, float rotation)
        {
            var pb = selection.GetComponent<ProBuilderMesh>();

            List<int> ModifiedVertices = new List<int>();
            foreach (var item in faceSelection)
            {
                Face face = item;
                // only grab the unique indices - so (0,1,2,1,3,2) becomes (0,1,2,3)
                var indices = face.distinctIndexes;

                // store the origin points of each vertex we'll be moving
                List<Vector3> vertexOrigins = new List<Vector3>();
                foreach(var _item in indices)
                {
                    vertexOrigins.Add(pb.VerticesInWorldSpace()[_item]);
                }
                var vertexCenter = UnityEngine.ProBuilder.Math.Average(vertexOrigins);

                Quaternion faceRotation = Quaternion.Euler(axis * rotation);

                for (int i = 0; i < indices.Count; i++)
                {

                    if (!ModifiedVertices.Contains(indices[i]))
                    {
                        Vector3 v = vertexOrigins[i] - vertexCenter;

                        v = faceRotation * v;

                        v += vertexCenter;

                        // Using SetSharedVertexPosition guarantees that all vertices
                        // that are shared among the indices are also rotated.  It 
                        // also applies the pb.vertices array to the mesh.
                        pb.SetSharedVertexPosition(indices[i], v);

                        ModifiedVertices.Add(indices[i]);
                    }
                    
                }
            }

            ModifiedVertices.Clear();

            pb.ToMesh();
            pb.Refresh();

            pb.GetComponent<MeshCollider>().sharedMesh = null;
            pb.GetComponent<MeshCollider>().sharedMesh = pb.GetComponent<MeshFilter>().mesh;

        }


        //Credit:
        //Unity / ProBuilder > Examples > Runtime Mesh Modification
        //Vigor XR / Bubby932
        //Any code within this section was not made entirely by myself or anyone on the Vigor XR Development Team.
        //Any code here should not be reused without this message above.
        //This code HAS been modified by myself and others.
        //  ~Bubby
        #region Modified Unoriginal Code
        void MoveFace(ProBuilderMesh pbm, List<Face> faces, Vector3 dir)
        {
            List<int> ModifiedVertices = new List<int>();


            foreach(var item in faces)
            {
                var m_Selection = new MeshAndFace();
                m_Selection.mesh = pbm;
                m_Selection.face = item;


                m_DragState.meshState = new MeshState(m_Selection.mesh, m_Selection.face.distinctIndexes);

                var distance = dir.magnitude;

                var mesh = pbm;

                var indices = m_DragState.meshState.indices;
                var vertices = m_DragState.meshState.vertices;
                var origins = m_DragState.meshState.origins;
                // Constraint is in world coordinates, but we need model space when applying changes to mesh values.
                var direction = mesh.transform.InverseTransformDirection(dir);

                for (int i = 0, c = indices.Count; i < c; i++)
                {
                    if(!ModifiedVertices.Contains(indices[i]))
                    {
                        vertices[indices[i]] = origins[i] + direction * distance;
                        ModifiedVertices.Add(indices[i]);
                    }
                }

                mesh.positions = vertices;
                mesh.ToMesh();
                mesh.Refresh();

                pbm.GetComponent<MeshCollider>().sharedMesh = null;
                pbm.GetComponent<MeshCollider>().sharedMesh = pbm.GetComponent<MeshFilter>().mesh;
            }

            ModifiedVertices.Clear();
        }
        #endregion

    }

    //Credit:
    //Unity / ProBuilder > Examples > Runtime Mesh Modification
    //Any code within this section was not made by myself or anyone on the Vigor XR Development Team.
    //Any code here should not be reused without this message above.
    //  ~Bubby
    #region Unoriginal Code
    class MeshState
    {
        public ProBuilderMesh mesh;
        public Vector3[] vertices;
        public Vector3[] origins;
        public List<int> indices;

        public MeshState(ProBuilderMesh mesh, IList<int> selectedIndices)
        {
            this.mesh = mesh;
            vertices = mesh.positions.ToArray();
            indices = mesh.GetCoincidentVertices(selectedIndices);
            origins = new Vector3[indices.Count];

            for (int i = 0, c = indices.Count; i < c; i++)
                origins[i] = vertices[indices[i]];
        }
    }

    struct MeshAndFace
    {
        public ProBuilderMesh mesh;
        public Face face;
    }

    class DragState
    {
        public bool active;
        public Ray constraint;
        public float offset;
        public MeshState meshState;
    }

    #endregion
}