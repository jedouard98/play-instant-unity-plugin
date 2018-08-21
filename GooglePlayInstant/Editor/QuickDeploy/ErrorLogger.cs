// Copyright 2018 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using UnityEditor;

namespace GooglePlayInstant.Editor.QuickDeploy
{
    public static class ErrorLogger
    {
        private const string DialogButtonText = "OK";
        
        public static void DisplayError(string title, string message)
        {
            EditorUtility.DisplayDialog(title, message, DialogButtonText);
        }
//
//        public static void DisplayError(string title, string[] messages)
//        {
//            
//            EditorUtility.DisplayDialog(title, message, DialogButtonText);
//        }
        
        
//        
//        public static void DisplayUrlError(string message)
//        {
//            EditorUtility.DisplayDialog("Invalid Default URL", message, "OK");
//        }
    }
}