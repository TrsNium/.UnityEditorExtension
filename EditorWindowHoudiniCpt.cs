using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;
using System.Linq;


public class EditorWindowHoudiniCpt : EditorWindow
{
    string parentObjectName="";
    GameObject prefab;

    [MenuItem("Window/HoudiniCpt")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        EditorWindowHoudiniCpt window = (EditorWindowHoudiniCpt)EditorWindow.GetWindow(typeof(EditorWindowHoudiniCpt));
        window.Show();
    }

    private string readFile()
    {
        string path = EditorUtility.OpenFilePanel("Load Point info", "", "json");
        if (string.IsNullOrEmpty(path))
            return null;

        var reader = new StreamReader(path, System.Text.Encoding.GetEncoding("utf-8"));
        string textData = reader.ReadToEnd();
        reader.Close();

        return textData;
    }

    private Vector3 convertVector3(float[] array){
        if(array.Length != 3){
            throw new System.Exception("array length must be 3");
        }
        return new Vector3(array[0], array[1], array[2]);
    }

    private GameObject spawnObject(PointAttrib attrib){
        Vector3 p = convertVector3(attrib.P);
        Vector3 n = convertVector3(attrib.N);
        Vector3 up = convertVector3(attrib.up);

        Quaternion rotation = new Quaternion();
        rotation.SetLookRotation(n, up);

        GameObject obj = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        obj.transform.position = p;
        obj.transform.localRotation = rotation;
        return obj;
    }

    private void spawnObjects(PointAttrib[] attribs){
        if (0<attribs.Length){
            GameObject parent = new GameObject(parentObjectName);
            foreach(PointAttrib attrib in attribs){
                try{
                    GameObject child = spawnObject(attrib);
                    child.transform.parent = parent.transform;
                }catch(System.Exception){
                    Debug.LogWarning($"Cannot instance object {attrib.id}");
                    continue;
                }
            }
        }
    }

    void OnGUI()
    {

        GUILayout.Label("Base Settings", EditorStyles.boldLabel);
        parentObjectName = EditorGUILayout.TextField("Parent Object Name", parentObjectName);
        prefab = EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), true) as GameObject;
        if (GUILayout.Button("Copy to Point with JsonFile")){
            var data = readFile();
            if (data != null && prefab != null && parentObjectName != ""){
                // deserialize text
                PointAttrib[] attribs = JsonHelper.FromJson<PointAttrib>(data);
                // instance Object
                spawnObjects(attribs);
            }
        }
    }


    [Serializable]
    private class PointAttrib
    {
        public int id;
        public float[] P;
        public float[] up;
        public float[] N;
    }

    // ref. https://takap-tech.com/entry/2021/02/02/222406
    private static class JsonHelper
    {
        public static T[] FromJson<T>(string json)
        {
            string dummy_json = $"{{\"{DummyNode<T>.ROOT_NAME}\": {json}}}";
            var obj = JsonUtility.FromJson<DummyNode<T>>(dummy_json);
            return obj.array;
        }

        public static string ToJson<T>(IEnumerable<T> collection)
        {
            string json = JsonUtility.ToJson(new DummyNode<T>(collection));
            int start = DummyNode<T>.ROOT_NAME.Length + 4;
            int len = json.Length - start - 1;
            return json.Substring(start, len);
        }

        [Serializable]
        private struct DummyNode<T>
        {
            public const string ROOT_NAME = nameof(array);
            public T[] array;
            public DummyNode(IEnumerable<T> collection) => this.array = collection.ToArray();
        }
    }
}
