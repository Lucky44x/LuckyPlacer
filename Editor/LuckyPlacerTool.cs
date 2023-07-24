using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lucky44.Placer
{
    [EditorTool("Lucky Placer")]
    public class LuckyPlacerTool : EditorTool
    {
        GUIStyle AreaStyle = new GUIStyle();

        Assembly assembly = Assembly.Load("UnityEditor.dll");
        Type gridSettings;
        PropertyInfo gridSize;

        private void OnEnable()
        {
            AreaStyle.normal.background = MakeTex(1, 1, new Color(0, 0, 0, 0.5f));
            //Load offset data

            gridSettings = assembly.GetType("UnityEditor.GridSettings");
            gridSize = gridSettings.GetProperty("size");
        }

        private void OnDisable()
        {
            //Save offset data
        }

        [Shortcut("Activate Lucky Placer Tool", typeof(SceneView), KeyCode.P)]
        private static void ToolShortcut()
        {
            ToolManager.SetActiveTool<LuckyPlacerTool>();
        }

        public override void OnActivated()
        {
            LuckyPlacerWindow.Open();
            localPosOffset = Vector3.zero;
            localRotOffset = Vector3.zero;
        }

        //Vars
        private rotDir rotateAround = rotDir.y;
        private space rotSpace = space.local;

        private Vector3 localPosOffset = Vector3.zero;

        private Vector3 finalPosOffset = Vector3.zero;
        private Vector3 localRotOffset = Vector3.zero;
        private Vector3 normal;
        private GameObject localPrefab;

        private Vector3 originalPos = Vector3.zero;

        private bool b_autoParenting = false;

        private float gridX, gridY, gridZ;
        private bool b_gridSnap;
        private bool b_angleSnap;
        private float angleSnap = 0;

        #region toggleButtons
        private bool xSel, ySel = true, zSel;
        #endregion

        private Vector3 internalEulerAngles = Vector3.zero;

        public override void OnToolGUI(EditorWindow window)
        {
            if (window is SceneView sceneView)
            {
                Handles.BeginGUI();
                using (new GUILayout.HorizontalScope())
                {
                    using (new GUILayout.VerticalScope())
                    {
                        #region toggleButtons
                        bool xClicked, yClicked, zClicked;
                        #endregion

                        int size = 200;
                        if (b_gridSnap)
                            size += 45;
                        if (b_angleSnap)
                            size += 45;

                        GUILayout.BeginArea(new Rect(50, 10, 110, size), AreaStyle);
                        GUILayout.Label("Lucky", EditorStyles.boldLabel);
                        GUILayout.Label("           Placer", EditorStyles.boldLabel);

                        b_autoParenting = EditorGUILayout.ToggleLeft("Auto-Parent", b_autoParenting, GUILayout.Width(100));

                        GUILayout.Space(5);

                        GUILayout.BeginHorizontal();
                        string text = rotSpace == space.local ? "World" : "Local";
                        if (GUILayout.Button(text))
                        {
                            if (rotSpace == space.local)
                                rotSpace = space.world;
                            else
                                rotSpace = space.local;
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        xClicked = GUILayout.Toggle(xSel, "X", "Button", GUILayout.Width(30)) != xSel;
                        yClicked = GUILayout.Toggle(ySel, "Y", "Button", GUILayout.Width(30)) != ySel;
                        zClicked = GUILayout.Toggle(zSel, "Z", "Button", GUILayout.Width(30)) != zSel;

                        if (xClicked)
                        {
                            xSel = true;
                            ySel = false;
                            zSel = false;

                            rotateAround = rotDir.x;
                        }
                        else if (yClicked)
                        {
                            ySel = true;
                            xSel = false;
                            zSel = false;

                            rotateAround = rotDir.y;
                        }
                        else if (zClicked)
                        {
                            zSel = true;
                            ySel = false;
                            xSel = false;

                            rotateAround = rotDir.z;
                        }

                        GUILayout.EndHorizontal();

                        GUILayout.Space(5);
                        
                        b_gridSnap = EditorGUILayout.ToggleLeft("Snap To Grid", b_gridSnap, GUILayout.Width(100));

                        if (b_gridSnap)
                        {
                            EditorGUILayout.BeginHorizontal();

                            EditorGUILayout.BeginVertical();
                            GUILayout.Label("X", EditorStyles.boldLabel);
                            gridX = EditorGUILayout.FloatField(gridX, GUILayout.Width(30));
                            EditorGUILayout.EndVertical();

                            EditorGUILayout.BeginVertical();
                            GUILayout.Label("Y", EditorStyles.boldLabel);
                            gridY = EditorGUILayout.FloatField(gridY, GUILayout.Width(30));
                            EditorGUILayout.EndVertical();

                            EditorGUILayout.BeginVertical();
                            GUILayout.Label("Z", EditorStyles.boldLabel);
                            gridZ = EditorGUILayout.FloatField(gridZ, GUILayout.Width(30));
                            EditorGUILayout.EndVertical();

                            EditorGUILayout.EndHorizontal();

                            if (gridX <= 0)
                                gridX = 0.000001f;
                            if (gridY <= 0)
                                gridY = 0.000001f;
                            if (gridZ <= 0)
                                gridZ = 0.000001f;

                            gridSize.SetValue("size", new Vector3(gridX, gridY, gridZ));
                        }

                        GUILayout.Space(5);

                        b_angleSnap = EditorGUILayout.ToggleLeft("Snap To Angle", b_angleSnap, GUILayout.Width(100));

                        if (b_angleSnap)
                        {
                            EditorGUILayout.BeginVertical();
                            GUILayout.Label("Angle", EditorStyles.boldLabel);
                            angleSnap = EditorGUILayout.FloatField(angleSnap, GUILayout.Width(30));
                            EditorGUILayout.EndVertical();
                        }

                        GUILayout.Space(5);

                        if (GUILayout.Button("Reset pos"))
                        {
                            finalPosOffset = Vector3.zero;
                        }
                        if (GUILayout.Button("Reset rot"))
                        {
                            if (LuckyPlacer.Instance.previewModel != null)
                            {
                                LuckyPlacer.Instance.previewModel.transform.rotation = LuckyPlacer.Instance.selected.transform.rotation;
                            }
                        }

                        GUILayout.EndArea();
                    }
                }
                Handles.EndGUI();
            }

            if (localPrefab != LuckyPlacer.Instance.selected)
            {
                localPrefab = LuckyPlacer.Instance.selected;
                localPosOffset = Vector3.zero;
                finalPosOffset = Vector3.zero;
                localRotOffset = Vector3.zero;
                ySel = true;
                xSel = false;
                zSel = false;
            }

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            if (LuckyPlacer.Instance.previewModel != null)
            {
                Transform hit_parent = null;
                Vector3 hit_pos = Vector3.zero;
                Vector3 hit_normal = Vector3.zero;
                bool hit_object = false;

                Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    hit_parent = hit.transform;
                    hit_normal = hit.normal;
                    hit_pos = hit.point;
                    hit_object = true;
                }
                else
                {
                    Plane hPlane = new Plane(Vector3.up, Vector3.zero);
                    float distance = 0;
                    if (hPlane.Raycast(ray, out distance))
                    {
                        hit_pos = ray.GetPoint(distance);
                    }
                }

                switch (Event.current.type)
                {
                    case EventType.MouseDown:
                        if (Event.current.button == 0)
                        {
                            GameObject g = Instantiate(LuckyPlacer.Instance.selected, LuckyPlacer.Instance.previewModel.transform.position, LuckyPlacer.Instance.previewModel.transform.rotation);
                            g.name = g.name.Substring(0, g.name.Length - 7);
                            if (b_autoParenting)
                            {
                                g.transform.parent = hit_parent;
                            }
                            Undo.RegisterCreatedObjectUndo(g, $"Created {g.name}");
                        }
                        break;

                    case EventType.KeyDown:
                        if (Event.current.alt)
                        {
                            if (originalPos == Vector3.zero)
                            {
                                originalPos = LuckyPlacer.Instance.previewModel.transform.position;
                                normal = hit_normal;
                            }
                        }
                        if (Event.current.keyCode == KeyCode.X)
                        {
                            if(hit_object)
                                Undo.DestroyObjectImmediate(hit.transform.gameObject);
                        }
                        break;

                    case EventType.KeyUp:
                        if (!Event.current.alt)
                        {
                            finalPosOffset += localPosOffset;
                            localPosOffset = Vector3.zero;
                            originalPos = Vector3.zero;
                        }
                        break;
                }

                if (Event.current.alt)
                {
                    Handles.color = Color.yellow;
                    Handles.DrawDottedLine(originalPos, originalPos + localPosOffset, 5);
                    Handles.Label(originalPos + localPosOffset / 2, Vector3.Distance(originalPos, originalPos + localPosOffset).ToString());

                    localPosOffset += normal * -Event.current.delta.y * .01f;
                    LuckyPlacer.Instance.previewModel.transform.position = originalPos + localPosOffset;
                    return;
                }

                if (Event.current.shift)
                {
                    Vector3 up = LuckyPlacer.Instance.previewModel.transform.up;

                    switch (rotateAround)
                    {
                        case rotDir.x:
                            up = rotSpace == space.local ? LuckyPlacer.Instance.previewModel.transform.right : Vector3.right;
                            Handles.color = Color.red;
                            break;
                        case rotDir.y:
                            Handles.color = Color.green;
                            up = rotSpace == space.local ? up : Vector3.up;
                            break;
                        case rotDir.z:
                            Handles.color = Color.blue;
                            up = rotSpace == space.local ? LuckyPlacer.Instance.previewModel.transform.forward : Vector3.forward;
                            break;
                    }

                    internalEulerAngles += up * Event.current.delta.x;

                    Vector3 eulerAngles = new Vector3(internalEulerAngles.x, internalEulerAngles.y, internalEulerAngles.z);

                    if (b_angleSnap)
                    {
                        float[] angles = new float[] { internalEulerAngles.x, internalEulerAngles.y, internalEulerAngles.z };
                        for (int i = 0; i < angles.Length; i++)
                        {
                            float closestSnapped = (Mathf.RoundToInt(angles[i] / angleSnap) * angleSnap);
                            angles[i] = closestSnapped;
                        }
                        eulerAngles.x = angles[0];
                        eulerAngles.y = angles[1];
                        eulerAngles.z = angles[2];
                    }

                    LuckyPlacer.Instance.previewModel.transform.eulerAngles = eulerAngles;
                    localRotOffset = LuckyPlacer.Instance.previewModel.transform.localEulerAngles;

                    //Draw Visuals
                    Handles.DrawWireDisc(LuckyPlacer.Instance.previewModel.transform.position, up, 2);
                    float maxVal = Mathf.Max(Mathf.Abs(localRotOffset.x), Mathf.Abs(localRotOffset.y), Mathf.Abs(localRotOffset.z));
                    Handles.Label(LuckyPlacer.Instance.previewModel.transform.position, maxVal.ToString());

                    return;
                }

                //LuckyPlacer.Instance.previewModel.transform.rotation = Quaternion.LookRotation(Vector3.forward, hit.normal);
                //Draw Visuals
                Handles.color = Color.white;
                Handles.DrawWireDisc(hit_pos, hit_normal, 1, 4);
                Handles.DrawDottedLine(hit_pos, LuckyPlacer.Instance.previewModel.transform.position, 5);

                Vector3 newPos = hit_pos + LuckyPlacer.Instance.selected.transform.position + finalPosOffset;

                if (b_gridSnap)
                {
                    Vector3 snappedPosition = Vector3.zero;
                    snappedPosition.x = Mathf.Round(newPos.x / gridX) * gridX;
                    snappedPosition.y = Mathf.Round(newPos.y / gridY) * gridY;
                    snappedPosition.z = Mathf.Round(newPos.z / gridZ) * gridZ;

                    //Debug.Log($"X: ({newPos.x}/{gridX}) * {gridX} = {snappedPosition.x}");
                    //Debug.Log(snappedPosition.ToString());

                    LuckyPlacer.Instance.previewModel.transform.position = snappedPosition;
                }
                else
                {
                    LuckyPlacer.Instance.previewModel.transform.position = newPos;
                }
            }
        }

        public override void OnWillBeDeactivated()
        {
            LuckyPlacerWindow.close();
            LuckyPlacer.Instance.selected = null;

            if (LuckyPlacer.Instance.previewModel != null)
                GameObject.DestroyImmediate(LuckyPlacer.Instance.previewModel);
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];

            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();

            return result;
        }
    }

    public enum rotDir
    {
        x,
        y,
        z
    }

    public enum space
    {
        world,
        local
    }
}