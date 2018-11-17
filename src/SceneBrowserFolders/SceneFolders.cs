using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using Studio;
using UnityEngine;

namespace SceneBrowserFolders
{
    public class SceneFolders
    {
        private static string _currentRelativeFolder;
        private static string _currentFolder;

        private static bool _scrollTreeToSelected;
        private static SceneLoadScene _studioInitObject;

        private readonly HashSet<string> _openedObjects = new HashSet<string>();

        private Vector2 _treeScrollPosition;

        public SceneFolders()
        {
            HarmonyInstance.Create(SceneBrowserFolders.Guid + "." + nameof(SceneFolders)).PatchAll(typeof(SceneFolders));
        }

        public static string CurrentFolder
        {
            get => _currentFolder;
            set
            {
                var lowVal = Path.GetFullPath(value.ToLower().TrimEnd('\\'));
                if (_currentFolder == lowVal) return;

                _currentFolder = lowVal;
                _currentRelativeFolder = _currentFolder.Length > Utils.GetUserDataPath().Length ? _currentFolder.Substring(Utils.GetUserDataPath().Length) : "";

                if (_studioInitObject != null)
                    RefreshList();
            }
        }

        private static void RefreshList()
        {
            var sls = typeof(SceneLoadScene);
            sls.GetMethod("OnClickCancel", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(_studioInitObject, null);
            sls.GetMethod("InitInfo", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(_studioInitObject, null);
            sls.GetMethod("SetPage", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(_studioInitObject, new object[] { SceneLoadScene.page });
        }

        private static string GetDefaultPath() => Path.Combine(Utils.GetUserDataPath(), @"studio\scene");

        public void OnGui()
        {
            if (_studioInitObject != null)
            {
                var screenRect = new Rect((int)(Screen.width / 11.3f), (int)(Screen.height / 90f), (int)(Screen.width / 2.5f), (int)(Screen.height / 5f));
                Utils.DrawSolidWindowBackground(screenRect);
                GUILayout.Window(362, screenRect, TreeWindow, "Select folder with scenes to view");
            }
        }

        [HarmonyPatch(typeof(SceneLoadScene), "InitInfo")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> StudioInitInfoPatch(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (string.Equals(instruction.operand as string, "studio/scene", StringComparison.OrdinalIgnoreCase))
                {
                    //0x7E	ldsfld <field>	Push the value of the static field on the stack.
                    instruction.opcode = OpCodes.Ldsfld;
                    instruction.operand = typeof(SceneFolders).GetField(nameof(_currentRelativeFolder), BindingFlags.NonPublic | BindingFlags.Static);
                }

                yield return instruction;
            }
        }

        [HarmonyPatch(typeof(SceneLoadScene), "InitInfo")]
        [HarmonyPostfix]
        public static void StudioInitInfoPost(SceneLoadScene __instance)
        {
            _studioInitObject = __instance;
            if (CurrentFolder == null)
                CurrentFolder = GetDefaultPath();
            _scrollTreeToSelected = true;
        }

        private void TreeWindow(int id)
        {
            ExpandToCurrentFolder();

            GUILayout.BeginHorizontal();
            {
                _treeScrollPosition = GUILayout.BeginScrollView(_treeScrollPosition, GUI.skin.box,
                    GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
                {
                    DisplayObjectTreeHelper(new DirectoryInfo(GetDefaultPath()), 0);
                }
                GUILayout.EndScrollView();

                GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(200));
                {
                    if (GUILayout.Button("Refresh scene thumbnails"))
                        RefreshList();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Open current folder in explorer"))
                        Process.Start("explorer.exe", $"\"{CurrentFolder}\"");
                    if (GUILayout.Button("Open character folder in explorer"))
                        Process.Start("explorer.exe", $"\"{Path.Combine(Utils.GetUserDataPath(), "chara")}\"");
                    if (GUILayout.Button("Open main game folder in explorer"))
                        Process.Start("explorer.exe", $"\"{Path.GetDirectoryName(Utils.GetUserDataPath())}\"");
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }

        private void ExpandToCurrentFolder()
        {
            var path = CurrentFolder;
            var defaultPath = GetDefaultPath();
            _openedObjects.Add(defaultPath);
            while (!string.IsNullOrEmpty(path) && path.Length > defaultPath.Length)
            {
                _openedObjects.Add(path);
                path = Path.GetDirectoryName(path)?.TrimEnd('\\');
            }
        }

        private void DisplayObjectTreeHelper(DirectoryInfo dir, int indent)
        {
            var fullNameLower = dir.FullName.ToLower();
            var subDirs = dir.GetDirectories();

            if (indent == 0 && subDirs.Length == 0)
            {
                GUILayout.BeginVertical();
                {
                    GUILayout.Label(@"You can organize your scenes into folders and use this window to browse them.");
                    GUILayout.Space(5);
                    GUILayout.Label(@"Folders placed inside your UserData\Studio\scene folder will appear on this list.");
                }
                GUILayout.EndVertical();
                return;
            }

            var c = GUI.color;
            if (fullNameLower == CurrentFolder)
            {
                GUI.color = Color.cyan;
                if (_scrollTreeToSelected && Event.current.type == EventType.Repaint)
                {
                    _scrollTreeToSelected = false;
                    _treeScrollPosition.y = GUILayoutUtility.GetLastRect().y - 50;
                }
            }

            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(indent * 20f);

                GUILayout.BeginHorizontal();
                {
                    if (subDirs.Length > 0)
                    {
                        if (GUILayout.Toggle(_openedObjects.Contains(fullNameLower), "", GUILayout.ExpandWidth(false)))
                            _openedObjects.Add(fullNameLower);
                        else
                            _openedObjects.Remove(fullNameLower);
                    }
                    else
                    {
                        GUILayout.Space(20f);
                    }

                    if (GUILayout.Button(dir.Name, GUI.skin.label, GUILayout.ExpandWidth(true), GUILayout.MinWidth(100)))
                    {
                        if (string.Equals(CurrentFolder, fullNameLower, StringComparison.OrdinalIgnoreCase))
                        {
                            if (_openedObjects.Contains(fullNameLower) == false)
                                _openedObjects.Add(fullNameLower);
                            else
                                _openedObjects.Remove(fullNameLower);
                        }
                        CurrentFolder = fullNameLower;
                    }

                    GUI.color = c;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndHorizontal();

            if (_openedObjects.Contains(fullNameLower))
            {
                foreach (var subDir in subDirs.OrderBy(x => x.Name))
                    DisplayObjectTreeHelper(subDir, indent + 1);
            }
        }
    }
}