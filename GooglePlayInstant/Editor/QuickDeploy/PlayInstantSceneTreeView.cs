using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GooglePlayInstant.Editor.QuickDeploy
{
    public class PlayInstantSceneTreeView : TreeView
    {
        private const int ToggleWidth = 18;

        public PlayInstantSceneTreeView(TreeViewState treeViewState)
            : base(treeViewState)
        {
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            extraSpaceBeforeIconAndLabel = ToggleWidth;

            Reload();
        }

        public class SceneItem : TreeViewItem
        {
            public bool enabled;
        }

        protected override TreeViewItem BuildRoot()
        {
            var scenes = GetAllScenes();

            var root = new TreeViewItem {id = 0, depth = -1, displayName = "Root"};

            var allItems = new List<TreeViewItem>();
            for (var i = 0; i < scenes.Length; i++)
            {
                allItems.Add(new SceneItem {id = i, depth = 0, displayName = scenes[i].path, enabled = true});
            }

            SetupParentsAndChildrenFromDepths(root, allItems);

            return root;
        }

        private static Scene[] GetAllScenes()
        {
            var scenes = new Scene[SceneManager.sceneCount];
            for (var i = 0; i < scenes.Length; i++)
                scenes[i] = SceneManager.GetSceneAt(i);

            return scenes;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            // Make a toggle button to the left of the label text
            Rect toggleRect = args.rowRect;
            toggleRect.x += GetContentIndent(args.item);
            toggleRect.width = ToggleWidth;

            var item = (SceneItem) args.item;

            item.enabled = EditorGUI.Toggle(toggleRect, item.enabled);

            // Default icon and label
            args.rowRect = args.rowRect;
            base.RowGUI(args);
        }
    }
}