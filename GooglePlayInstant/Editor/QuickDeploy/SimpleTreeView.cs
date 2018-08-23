﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GooglePlayInstant.Editor.QuickDeploy
{
    public class SimpleTreeView : TreeView
    {
        public SimpleTreeView(TreeViewState treeViewState)
            : base(treeViewState)
        {
            
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            extraSpaceBeforeIconAndLabel = 20;
//            EditorGUI.Toggle(new Rect(0,0,0,0), true);
            
            Reload();
        }

        private ArrayList test;
        
        protected override TreeViewItem BuildRoot ()
        {
            // BuildRoot is called every time Reload is called to ensure that TreeViewItems 
            // are created from data. Here we create a fixed set of items. In a real world example,
            // a data model should be passed into the TreeView and the items created from the model.

            // This section illustrates that IDs should be unique. The root item is required to 
            // have a depth of -1, and the rest of the items increment from that.
            
            Scene[] allScenes = new Scene[SceneManager.sceneCount];
            for (int i = 0; i < allScenes.Length; i++)
                allScenes[i] = SceneManager.GetSceneAt(i); 
            
            var root = new TreeViewItem {id = 0, depth = -1, displayName = "Root"};
//            var allItems = new List<TreeViewItem>
//            {
//                new TreeViewItem {id = 1, depth = 0, displayName = "Animals"},
//                new TreeViewItem {id = 2, depth = 1, displayName = "Mammals"},
//                new TreeViewItem {id = 3, depth = 2, displayName = "Tiger"},
//                new TreeViewItem {id = 4, depth = 2, displayName = "Elephant"},
//                new TreeViewItem {id = 5, depth = 2, displayName = "Okapi"},
//                new TreeViewItem {id = 6, depth = 2, displayName = "Armadillo"},
//                new TreeViewItem {id = 7, depth = 1, displayName = "Reptiles"},
//                new TreeViewItem {id = 8, depth = 2, displayName = "Crocodile"},
//                new TreeViewItem {id = 9, depth = 2, displayName = "Lizard"},
//            };

            var allItems = new List<TreeViewItem>();
            for (int i = 0; i < allScenes.Length; i++)
            {
                allItems.Add(new TreeViewItem {id = i, depth = 0, displayName = allScenes[i].name});
                var dependencies = AssetDatabase.GetDependencies(allScenes[i].path, true);
                for (int j = 0; j < dependencies.Length; j++)
                {
                    if (Path.GetFileNameWithoutExtension(dependencies[j]) == allScenes[i].name || Path.GetExtension(dependencies[j]) == ".cs")
                    {
                        continue;
                    }

                    allItems.Add(new TreeViewItem {id = i, depth = 1, displayName = Path.GetFileNameWithoutExtension(dependencies[j])});
                }
            }

            
            // Utility method that initializes the TreeViewItem.children and .parent for all items.
            SetupParentsAndChildrenFromDepths (root, allItems);
            
            // Return root of the tree
            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            EditorGUI.Toggle(new Rect(0,0,0,0), true);
        }
    }
}