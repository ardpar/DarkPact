using UnityEngine;
using UnityEditor;
using DarkPact.Core;

namespace DarkPact.Editor
{
    [CustomEditor(typeof(DungeonLayoutSO))]
    public class DungeonLayoutEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var layout = (DungeonLayoutSO)target;

            // Validation button
            EditorGUILayout.Space();
            if (GUILayout.Button("Validate Layout", GUILayout.Height(30)))
            {
                string error = layout.Validate();
                if (error == null)
                    EditorUtility.DisplayDialog("Validation", "Layout is valid! All rooms connected.", "OK");
                else
                    EditorUtility.DisplayDialog("Validation Error", error, "OK");
            }

            if (GUILayout.Button("Open Node Editor", GUILayout.Height(30)))
            {
                DungeonNodeEditorWindow.Open(layout);
            }

            EditorGUILayout.Space();
            DrawDefaultInspector();
        }
    }

    public class DungeonNodeEditorWindow : EditorWindow
    {
        DungeonLayoutSO _layout;
        Vector2 _scrollPos;
        Vector2 _dragOffset;
        int _draggingNode = -1;
        int _connectingFrom = -1;
        float _zoom = 1f;

        const float NodeWidth = 120;
        const float NodeHeight = 60;
        const float GridSize = 30;

        public static void Open(DungeonLayoutSO layout)
        {
            var window = GetWindow<DungeonNodeEditorWindow>("Dungeon Node Editor");
            window._layout = layout;
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        void OnGUI()
        {
            if (_layout == null)
            {
                EditorGUILayout.HelpBox("No layout selected. Select a DungeonLayoutSO and click 'Open Node Editor'.", MessageType.Info);
                return;
            }

            // Toolbar
            DrawToolbar();

            // Canvas area
            var canvasRect = new Rect(0, 22, position.width, position.height - 22);
            GUI.BeginGroup(canvasRect);

            // Background grid
            DrawGrid(canvasRect, GridSize * _zoom, new Color(0.2f, 0.2f, 0.2f, 0.4f));
            DrawGrid(canvasRect, GridSize * 5 * _zoom, new Color(0.2f, 0.2f, 0.2f, 0.8f));

            // Draw connections
            for (int i = 0; i < _layout.Connections.Count; i++)
            {
                var conn = _layout.Connections[i];
                if (conn.FromIndex >= 0 && conn.FromIndex < _layout.Rooms.Count &&
                    conn.ToIndex >= 0 && conn.ToIndex < _layout.Rooms.Count)
                {
                    var from = GetNodeCenter(conn.FromIndex);
                    var to = GetNodeCenter(conn.ToIndex);
                    Handles.color = Color.white;
                    Handles.DrawLine(from, to);

                    // Delete connection button at midpoint
                    var mid = (from + to) * 0.5f;
                    if (GUI.Button(new Rect(mid.x - 8, mid.y - 8, 16, 16), "×"))
                    {
                        Undo.RecordObject(_layout, "Delete Connection");
                        _layout.Connections.RemoveAt(i);
                        EditorUtility.SetDirty(_layout);
                        i--;
                    }
                }
            }

            // Draw connecting line
            if (_connectingFrom >= 0 && _connectingFrom < _layout.Rooms.Count)
            {
                Handles.color = Color.yellow;
                Handles.DrawLine(GetNodeCenter(_connectingFrom), Event.current.mousePosition);
                Repaint();
            }

            // Draw nodes
            for (int i = 0; i < _layout.Rooms.Count; i++)
                DrawNode(i);

            // Handle events
            HandleEvents(canvasRect);

            GUI.EndGroup();
        }

        void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            EditorGUILayout.LabelField(_layout.LayoutName ?? "Unnamed", EditorStyles.boldLabel, GUILayout.Width(150));

            if (GUILayout.Button("Add Room", EditorStyles.toolbarButton))
            {
                Undo.RecordObject(_layout, "Add Room");
                _layout.Rooms.Add(new DungeonLayoutSO.RoomNode
                {
                    Name = $"Room {_layout.Rooms.Count}",
                    EditorPosition = new Vector2(position.width / 2, position.height / 2) / _zoom,
                    Type = RoomType.Combat
                });
                EditorUtility.SetDirty(_layout);
            }

            if (GUILayout.Button("Validate", EditorStyles.toolbarButton))
            {
                string error = _layout.Validate();
                if (error == null)
                    Debug.Log("[Dungeon] Layout valid!");
                else
                    Debug.LogWarning("[Dungeon] " + error);
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.LabelField($"Rooms: {_layout.Rooms.Count}  Connections: {_layout.Connections.Count}", GUILayout.Width(200));

            EditorGUILayout.EndHorizontal();
        }

        void DrawNode(int index)
        {
            var room = _layout.Rooms[index];
            var pos = room.EditorPosition * _zoom;
            var rect = new Rect(pos.x - NodeWidth / 2, pos.y - NodeHeight / 2, NodeWidth, NodeHeight);

            // Node color by type
            Color nodeColor = room.Type switch
            {
                RoomType.Start => new Color(0.2f, 0.6f, 0.2f),
                RoomType.Boss => new Color(0.7f, 0.15f, 0.15f),
                RoomType.Treasure => new Color(0.7f, 0.6f, 0.1f),
                _ => new Color(0.25f, 0.25f, 0.35f)
            };

            // Highlight start/boss
            if (index == _layout.StartRoomIndex)
                nodeColor = new Color(0.1f, 0.7f, 0.1f);
            if (index == _layout.BossRoomIndex || (index == _layout.Rooms.Count - 1 && _layout.BossRoomIndex < 0))
                nodeColor = new Color(0.8f, 0.1f, 0.1f);

            EditorGUI.DrawRect(rect, nodeColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), Color.white);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), Color.white);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1, rect.height), Color.white);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1, rect.y, 1, rect.height), Color.white);

            // Labels
            var style = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.UpperCenter, normal = { textColor = Color.white }, fontSize = 10 };
            GUI.Label(new Rect(rect.x, rect.y + 2, rect.width, 16), room.Name ?? $"#{index}", style);

            var typeStyle = new GUIStyle(style) { fontSize = 9, fontStyle = FontStyle.Italic };
            GUI.Label(new Rect(rect.x, rect.y + 16, rect.width, 14), room.Type.ToString(), typeStyle);

            var diffStyle = new GUIStyle(style) { fontSize = 9 };
            GUI.Label(new Rect(rect.x, rect.y + 30, rect.width, 14), $"Diff: {room.Difficulty:F1}", diffStyle);

            // Connect button (right side)
            if (GUI.Button(new Rect(rect.xMax - 18, rect.y + NodeHeight / 2 - 8, 16, 16), "→"))
            {
                if (_connectingFrom < 0)
                    _connectingFrom = index;
                else if (_connectingFrom != index)
                {
                    // Complete connection
                    Undo.RecordObject(_layout, "Connect Rooms");
                    _layout.Connections.Add(new DungeonLayoutSO.RoomConnection
                    {
                        FromIndex = _connectingFrom,
                        ToIndex = index
                    });
                    EditorUtility.SetDirty(_layout);
                    _connectingFrom = -1;
                }
            }

            // Context menu (right-click)
            if (Event.current.type == EventType.ContextClick && rect.Contains(Event.current.mousePosition))
            {
                var menu = new GenericMenu();
                int capturedIndex = index;

                menu.AddItem(new GUIContent("Set as Start"), index == _layout.StartRoomIndex, () =>
                {
                    Undo.RecordObject(_layout, "Set Start");
                    _layout.StartRoomIndex = capturedIndex;
                    _layout.Rooms[capturedIndex].Type = RoomType.Start;
                    EditorUtility.SetDirty(_layout);
                });

                menu.AddItem(new GUIContent("Set as Boss"), index == _layout.BossRoomIndex, () =>
                {
                    Undo.RecordObject(_layout, "Set Boss");
                    _layout.BossRoomIndex = capturedIndex;
                    _layout.Rooms[capturedIndex].Type = RoomType.Boss;
                    EditorUtility.SetDirty(_layout);
                });

                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Type/Combat"), room.Type == RoomType.Combat, () => SetType(capturedIndex, RoomType.Combat));
                menu.AddItem(new GUIContent("Type/Treasure"), room.Type == RoomType.Treasure, () => SetType(capturedIndex, RoomType.Treasure));
                menu.AddItem(new GUIContent("Type/Start"), room.Type == RoomType.Start, () => SetType(capturedIndex, RoomType.Start));
                menu.AddItem(new GUIContent("Type/Boss"), room.Type == RoomType.Boss, () => SetType(capturedIndex, RoomType.Boss));

                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Delete Room"), false, () => DeleteRoom(capturedIndex));

                menu.ShowAsContext();
                Event.current.Use();
            }
        }

        void SetType(int index, RoomType type)
        {
            Undo.RecordObject(_layout, "Change Room Type");
            _layout.Rooms[index].Type = type;
            EditorUtility.SetDirty(_layout);
        }

        void DeleteRoom(int index)
        {
            Undo.RecordObject(_layout, "Delete Room");

            // Remove connections referencing this room
            _layout.Connections.RemoveAll(c => c.FromIndex == index || c.ToIndex == index);

            // Adjust indices in remaining connections
            for (int i = 0; i < _layout.Connections.Count; i++)
            {
                var c = _layout.Connections[i];
                if (c.FromIndex > index) c.FromIndex--;
                if (c.ToIndex > index) c.ToIndex--;
            }

            _layout.Rooms.RemoveAt(index);

            if (_layout.StartRoomIndex == index) _layout.StartRoomIndex = 0;
            else if (_layout.StartRoomIndex > index) _layout.StartRoomIndex--;

            if (_layout.BossRoomIndex == index) _layout.BossRoomIndex = -1;
            else if (_layout.BossRoomIndex > index) _layout.BossRoomIndex--;

            EditorUtility.SetDirty(_layout);
        }

        Vector2 GetNodeCenter(int index)
        {
            return _layout.Rooms[index].EditorPosition * _zoom;
        }

        void HandleEvents(Rect canvasRect)
        {
            var e = Event.current;

            switch (e.type)
            {
                case EventType.MouseDown when e.button == 0:
                    // Check if clicking on a node
                    for (int i = _layout.Rooms.Count - 1; i >= 0; i--)
                    {
                        var pos = _layout.Rooms[i].EditorPosition * _zoom;
                        var rect = new Rect(pos.x - NodeWidth / 2, pos.y - NodeHeight / 2, NodeWidth - 18, NodeHeight);
                        if (rect.Contains(e.mousePosition))
                        {
                            _draggingNode = i;
                            _dragOffset = e.mousePosition - pos;
                            e.Use();
                            break;
                        }
                    }

                    // Cancel connecting
                    if (_connectingFrom >= 0 && _draggingNode < 0)
                    {
                        _connectingFrom = -1;
                        e.Use();
                    }
                    break;

                case EventType.MouseDrag when _draggingNode >= 0:
                    Undo.RecordObject(_layout, "Move Room");
                    _layout.Rooms[_draggingNode].EditorPosition = (e.mousePosition - _dragOffset) / _zoom;
                    EditorUtility.SetDirty(_layout);
                    e.Use();
                    Repaint();
                    break;

                case EventType.MouseUp:
                    // Snap to grid
                    if (_draggingNode >= 0)
                    {
                        var room = _layout.Rooms[_draggingNode];
                        room.EditorPosition = new Vector2(
                            Mathf.Round(room.EditorPosition.x / GridSize) * GridSize,
                            Mathf.Round(room.EditorPosition.y / GridSize) * GridSize
                        );
                        EditorUtility.SetDirty(_layout);
                    }
                    _draggingNode = -1;
                    break;

                case EventType.ScrollWheel:
                    _zoom = Mathf.Clamp(_zoom - e.delta.y * 0.05f, 0.5f, 2f);
                    e.Use();
                    Repaint();
                    break;
            }
        }

        void DrawGrid(Rect rect, float spacing, Color color)
        {
            int widthDivs = Mathf.CeilToInt(rect.width / spacing);
            int heightDivs = Mathf.CeilToInt(rect.height / spacing);

            Handles.color = color;
            for (int i = 0; i <= widthDivs; i++)
                Handles.DrawLine(new Vector3(spacing * i, 0, 0), new Vector3(spacing * i, rect.height, 0));
            for (int j = 0; j <= heightDivs; j++)
                Handles.DrawLine(new Vector3(0, spacing * j, 0), new Vector3(rect.width, spacing * j, 0));
        }

        void OnSelectionChange()
        {
            if (Selection.activeObject is DungeonLayoutSO layout)
            {
                _layout = layout;
                Repaint();
            }
        }
    }
}
