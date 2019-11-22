using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace WorldGenerator
{
    [CustomEditor(typeof(WG_Painter))]
    [System.Serializable]
    public class WG_Painter_Editor : Editor
    {
        public bool shouldRepaint;
        public Vector3 currentPoint;
        
        private WG_Painter wgPainter;

        private Vector2 lastPoint;
        private bool checkLastPoint = false;

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();

            string helpMessage = "Left click or drag to increase the height.\nShift-left click or shift-drag to decrease the height.\nCtrl-click or Ctrl-drag to delete points.";
            EditorGUILayout.HelpBox(helpMessage, MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Brush Size:");
            wgPainter.brushRadius = EditorGUILayout.Slider(wgPainter.brushRadius, 0.1f, 128.0f);
            EditorGUILayout.EndHorizontal();

            wgPainter.brushScale = EditorGUILayout.FloatField("Brush Scale", wgPainter.brushScale);

            wgPainter.brushColor = EditorGUILayout.ColorField("Brush Color", wgPainter.brushColor);

            wgPainter.drawPoints = EditorGUILayout.Toggle("Draw Points", wgPainter.drawPoints);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Points Disc Alpha:");
            wgPainter.pointsAlpha = EditorGUILayout.Slider(wgPainter.pointsAlpha, 0.0f, 1.0f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Points Center Size:");
            wgPainter.pointsSize = EditorGUILayout.Slider(wgPainter.pointsSize, 0.0f, 1.0f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Stroke Step:");
            wgPainter.strokeStep = EditorGUILayout.Slider(wgPainter.strokeStep, 1.0f, 512.0f);
            EditorGUILayout.EndHorizontal();

            wgPainter.wgBuilder = (WG_TerrainBuilder)EditorGUILayout.ObjectField(wgPainter.wgBuilder, typeof(WG_TerrainBuilder), true);
            
            if (GUILayout.Button("Clear points"))
            {
                wgPainter.Clear();
            }
            GUILayout.Label("Points count: " + wgPainter.GetPoinsCount().ToString());

            if (GUI.changed)
            {
                shouldRepaint = true;
                SceneView.RepaintAll();
            }
        }

        void OnSceneGUI()
        {
            Event guiEvent = Event.current;

            if (guiEvent.type == EventType.Repaint)
            {
                Draw();
            }
            else if (guiEvent.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            }
            else
            {
                HandleInput(guiEvent);
                if (shouldRepaint)
                {
                    HandleUtility.Repaint();
                }
            }
        }

        void HandleInput(Event guiEvent)
        {
            WG_Painter wgPainter = (WG_Painter)target;
            Ray mouseRay = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
            float drawPlainHeight = 0;
            float dstToDrawPlane = (drawPlainHeight - mouseRay.origin.y) / mouseRay.direction.y;
            Vector3 mousePosition = mouseRay.origin + dstToDrawPlane * mouseRay.direction;
            Vector2 position = new Vector2(mousePosition.x, mousePosition.z);
            
            if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.modifiers == EventModifiers.Shift)
            {
                HandleLeftClick(position, true);
            }

            if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.modifiers == EventModifiers.Control)
            {
                HandleRemoveClick(position);
            }
            if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.modifiers == EventModifiers.None)
            {
                HandleLeftClick(position, false);
            }
            if (guiEvent.type == EventType.MouseUp && guiEvent.button == 0)
            {
                HandleMouseUp();
            }
            if (guiEvent.type == EventType.MouseDrag && guiEvent.button == 0 && guiEvent.modifiers == EventModifiers.None)
            {
                HandleLeftDrag(position, false);
            }

            if (guiEvent.type == EventType.MouseDrag && guiEvent.button == 0 && guiEvent.modifiers == EventModifiers.Shift)
            {
                HandleLeftDrag(position, true);
            }

            if (guiEvent.type == EventType.MouseDrag && guiEvent.button == 0 && guiEvent.modifiers == EventModifiers.Control)
            {
                HandleRemoveDrag(position);
            }

            if (Vector3.Distance(currentPoint, mousePosition) > 0.01f)
            {
                currentPoint = mousePosition;
                shouldRepaint = true;
            }
        }

        void HandleMouseUp()
        {
            checkLastPoint = false;
        }

        void HandleRemoveClick(Vector2 position)
        {
			checkLastPoint = false;
            wgPainter.RemovePoints(position, wgPainter.brushRadius);
			TryUpdateMap();
        }

        void HandleRemoveDrag(Vector2 position)
        {
			checkLastPoint = false;
            wgPainter.RemovePoints(position, wgPainter.brushRadius);
			TryUpdateMap();
        }

        void HandleLeftClick(Vector2 position, bool isNegative)
        {
            checkLastPoint = true;
			lastPoint = position;
            wgPainter.AddPoint(position, wgPainter.brushRadius * wgPainter.brushScale, isNegative);
            shouldRepaint = true;
            TryUpdateMap();
        }

        void HandleLeftDrag(Vector2 position, bool isNegative)
        {
            if (!checkLastPoint || (checkLastPoint && Vector2.Distance(position, lastPoint) > wgPainter.strokeStep))
            {
                wgPainter.AddPoint(position, wgPainter.brushRadius * wgPainter.brushScale, isNegative);
                lastPoint = position;
            }
            checkLastPoint = true;
            TryUpdateMap();
        }

        void TryUpdateMap()
        {
            if (wgPainter.wgBuilder != null)
            {
                wgPainter.wgBuilder.UpdateMap();
            }
        }

        void Draw()
        {
            if (true || shouldRepaint)
            {
                Handles.color = wgPainter.brushColor;
                Handles.DrawSolidDisc(currentPoint, Vector3.up, wgPainter.brushRadius * wgPainter.brushScale);

                //draw points
                if (wgPainter.drawPoints)
                {
                    foreach (Disc disc in wgPainter.points)
                    {
                        if (disc.isNegative)
                        {
                            Handles.color = Color.red;
                            Handles.DrawSolidDisc(new Vector3(disc.center.x, 0.0f, disc.center.y), Vector3.up, wgPainter.pointsSize);
                            Handles.color = new Color(1.0f, 0.0f, 0.0f, wgPainter.pointsAlpha);
                            Handles.DrawSolidDisc(new Vector3(disc.center.x, 0.0f, disc.center.y), Vector3.up, disc.radius);
                        }
                        else
                        {
                            Handles.color = Color.green;
                            Handles.DrawSolidDisc(new Vector3(disc.center.x, 0.0f, disc.center.y), Vector3.up, wgPainter.pointsSize);
                            Handles.color = new Color(0.0f, 1.0f, 0.0f, wgPainter.pointsAlpha);
                            Handles.DrawSolidDisc(new Vector3(disc.center.x, 0.0f, disc.center.y), Vector3.up, disc.radius);
                        }
                    }
                }
            }

            shouldRepaint = false;
        }

        void OnEnable()
        {
            wgPainter = (WG_Painter)target;
            shouldRepaint = true;
            Tools.hidden = true;
        }

        void OnDisable()
        {
            Tools.hidden = false;
        }
    }

}
