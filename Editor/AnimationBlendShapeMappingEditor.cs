#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Chreez.Editor.Tools.Util;

namespace Chreez.Editor.Tools
{
    public class BlendShapeMapping
    {
        public string Source { get; set; }
        public string Target { get; set; }
    }

    public class AnimationBlendShapeMappingEditor : EditorWindow
    {
        private static readonly int columnWidth = 200;
        private static readonly int buttonHeight = 22;
		
		private static readonly double automaticMappingSensitivity = 0.3;

        private Animator animatorReference;
        private SkinnedMeshRenderer skinnedMeshRendererReference;
        private AnimationClip animationClip;

        private Dictionary<string, int> _blendShapeMappings = new Dictionary<string, int>();
        private List<string> _blendShapeOptions = new List<string>();

        private Vector2 scrollPos = Vector2.zero;

        [MenuItem("Window/Animation BlendShape Mapping Editor")]
        private static void ShowWindow()
        {
            GetWindow<AnimationBlendShapeMappingEditor>(TextResource.Title);
        }

        private void OnSelectionChange()
        {
            if (Selection.activeObject is AnimationClip)
            {
                animationClip = (AnimationClip)Selection.activeObject;
            }
            else
            {
                animationClip = null;
                _blendShapeMappings.Clear();
            }
            Repaint();
        }

        private void OnInspectorUpdate()
        {
            _blendShapeOptions = GetBlendShapeOptions(skinnedMeshRendererReference);
            Repaint();
        }

        private void OnGUI()
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos, Styles.ScrollViewStyle);

            // Title
            GUILayout.Label(TextResource.Label_Title, Styles.TitleStyle);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // Information
            GUILayout.Box(TextResource.Text_Intro, Styles.TextBoxStyle);

            if (animationClip)
            {
                // Animation Clip
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Animation Clip:", GUILayout.Width(columnWidth));
                animationClip = ((AnimationClip)EditorGUILayout.ObjectField(
                    animationClip,
                    typeof(AnimationClip),
                    true));
                EditorGUILayout.EndHorizontal();

                // Facial Skinned Mesh Renderer
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Facial SkinnedMeshRenderer:", GUILayout.Width(columnWidth));
                skinnedMeshRendererReference = ((SkinnedMeshRenderer)EditorGUILayout.ObjectField(
                    skinnedMeshRendererReference,
                    typeof(SkinnedMeshRenderer),
                    true));
                EditorGUILayout.EndHorizontal();

                // Character Animator
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Character Animator (optional):", GUILayout.Width(columnWidth));

                animatorReference = ((Animator)EditorGUILayout.ObjectField(
                    animatorReference,
                    typeof(Animator),
                    true));

                EditorGUILayout.EndHorizontal();

                // Table Blendshape Mapping
                DrawMappingsTable();
            }
            else
            {
                GUILayout.Label("Please select an AnimationClip from Assets...");
            }

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            DrawCredits();

            GUILayout.EndScrollView();
        }

        private void DrawCredits()
        {
            if (GUILayout.Button(TextResource.Text_Credits, Styles.Credits, GUILayout.ExpandWidth(false)))
            {
                Application.OpenURL("www.github.com/chr33z");
            }
            var lastRect = GUILayoutUtility.GetLastRect();
            lastRect.y += lastRect.height - 1;
            lastRect.height = 1;
            GUI.Box(lastRect, "");
        }

        private void DrawMappingsTable()
        {
            if (!animationClip || !skinnedMeshRendererReference)
                return;

            GUILayout.Space(20);

            // Button Apply Blendshape Mapping
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = CanApplyBlendShapeMapping();
            GUILayout.Label("", GUILayout.Width(columnWidth));
            if (GUILayout.Button(
                new GUIContent(
                    "Apply & Save Blendshape Mapping",
                    TextResource.Tooltip_ApplyMapping
                ),
                Styles.ApplyMappingButtonStyle,
                new GUILayoutOption[]
                {
                    GUILayout.Height((int)(buttonHeight * 1.5)),
                }
                ))
            {
                ApplyBlendShapeMapping();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // Button Automatic Mapping
            GUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("", GUILayout.Width(columnWidth));
            if (GUILayout.Button(
                new GUIContent(
                    "Create Automatic Mapping",
                    TextResource.Tooltip_AutomaticMapping
                ),
                new GUILayoutOption[]
                {
                    GUILayout.Height(buttonHeight)
                }
                ))
            {
                CreateAutomaticMapping();
            }
            EditorGUILayout.EndHorizontal();

            // Button Clear Mapping
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("", GUILayout.Width(columnWidth));
            if (GUILayout.Button(
                new GUIContent(
                    "Clear Current Mapping",
                    TextResource.Tooltip_ClearMapping
                ), GUILayout.Height(buttonHeight)
                ))
            {
                ClearMapping();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // Table Header
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Animation Blendshapes", Styles.TableHeaderStyle, GUILayout.Width(columnWidth));
            GUILayout.Label("Mapped Blendshapes", Styles.TableHeaderStyle, GUILayout.Width(columnWidth));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // Mappings Table
            var curveBindings = AnimationUtility.GetCurveBindings(animationClip);
            foreach (var curveBinding in curveBindings)
            {
                if (!curveBinding.propertyName.Contains("blendShape"))
                    continue;

                var source = curveBinding.propertyName.Replace("blendShape.", "");

                if (!_blendShapeMappings.ContainsKey(source))
                {
                    _blendShapeMappings[source] = 0;
                }

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(source, GUILayout.Width(columnWidth));
                var selectedIndex = _blendShapeMappings[source];
                _blendShapeMappings[source] = EditorGUILayout.Popup(selectedIndex, _blendShapeOptions.ToArray());
                EditorGUILayout.EndHorizontal();
            }
        }

        private bool CanApplyBlendShapeMapping()
        {
            return _blendShapeMappings.Values.Any(val => val > 0);
        }

        private void ApplyBlendShapeMapping()
        {
            var availableBlendshapes = skinnedMeshRendererReference.sharedMesh.blendShapeCount;
            var changedBlendshapes = _blendShapeMappings.ToArray().Where(mapping => mapping.Value > 0).Count();

            if (!ShowConfirmApplyBlendShapeMapping(changedBlendshapes, availableBlendshapes))
            {
                return;
            }

            var curveBindings = AnimationUtility.GetCurveBindings(animationClip);
            var relativePath = GetPathFromSkinnedMeshToAnimator();
            for (var i = 0; i < curveBindings.Length; i++)
            {
                var curveBinding = curveBindings[i];
                var key = curveBinding.propertyName.Replace("blendShape.", "");
                var mappingName = GetBlendShapeMappingName(key);

                if (_blendShapeMappings.ContainsKey(key) && !string.IsNullOrEmpty(mappingName))
                {
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(animationClip, curveBinding);

                    if (curve != null)
                    {
                        AnimationUtility.SetEditorCurve(animationClip, curveBinding, null);
                        curveBinding.propertyName = "blendShape." + mappingName;
                        curveBinding.path = relativePath;
                        AnimationUtility.SetEditorCurve(animationClip, curveBinding, curve);
                    }
                }
            }
        }

        private bool ShowConfirmApplyBlendShapeMapping(int changedBlendshapes, int totalBlendshapes)
        {
            var title = "Apply Blendshape Mapping";
            var message = string.Format(TextResource.Template_ApplyDialogMessage, changedBlendshapes, totalBlendshapes);
            return EditorUtility.DisplayDialog(title, message, "Apply Mapping", "Cancel");
        }

        private void CreateAutomaticMapping()
        {
            if (!animationClip || !skinnedMeshRendererReference)
                return;

            var blendShapesInAnimation = _blendShapeMappings.Keys.ToArray();
            var blendShapesInCharacter = GetBlendShapeNames(skinnedMeshRendererReference);

            foreach (var blendShapeInAnim in blendShapesInAnimation)
            {
                var bestMatch = MatchToNameInList(blendShapeInAnim, blendShapesInCharacter, automaticMappingSensitivity);
                if (!string.IsNullOrEmpty(bestMatch))
                {
                    _blendShapeMappings[blendShapeInAnim] = _blendShapeOptions.IndexOf(bestMatch);
                }
            }
        }

        private static string MatchToNameInList(string targetName, List<string> availableBlendShapeNames, double tolerance)
        {
            string bestMatch = "";
            double maxScore = double.MinValue;

            foreach (var blendShapeName in availableBlendShapeNames)
            {
                double score = targetName.DiceCoefficient(blendShapeName);

                if (score > maxScore)
                {
                    maxScore = score;
                    bestMatch = blendShapeName;
                }
            }

            if (maxScore > tolerance)
            {
                return bestMatch;
            }
            else
            {
                return "";
            }
        }

        private void ClearMapping()
        {
            _blendShapeMappings.Clear();
        }

        private string GetPathFromSkinnedMeshToAnimator()
        {
            if (!animatorReference || !skinnedMeshRendererReference)
                return skinnedMeshRendererReference.name;

            var obj = skinnedMeshRendererReference.gameObject;

            string path = obj.name;
            while (obj.transform.parent != null && !obj.transform.parent.name.Equals(animatorReference.gameObject.name))
            {
                obj = obj.transform.parent.gameObject;
                path = obj.name + "/" + path;
            }
            return path;
        }

        private string GetBlendShapeMappingName(string key)
        {
            return _blendShapeOptions[_blendShapeMappings[key]];
        }

        private List<string> GetBlendShapeOptions(SkinnedMeshRenderer skinnedMeshRenderer)
        {
            var options = new List<string> { "None" };
            options.AddRange(GetBlendShapeNames(skinnedMeshRenderer));
            return options;
        }

        private List<string> GetBlendShapeNames(SkinnedMeshRenderer skinnedMeshRenderer)
        {
            if (!skinnedMeshRendererReference)
            {
                return new List<string>();
            }

            var mesh = skinnedMeshRenderer.sharedMesh;
            return Enumerable.Range(0, mesh.blendShapeCount).Select(i => mesh.GetBlendShapeName(i)).ToList();
        }
    }

    internal class Styles
    {
        internal static GUIStyle TableHeaderStyle {
            get {
                var tableHeaderStyle = new GUIStyle(GUI.skin.label);
                tableHeaderStyle.fontStyle = FontStyle.Bold;
                return tableHeaderStyle;
            }
        }

        internal static GUIStyle TitleStyle {
            get {
                var titleStyle = new GUIStyle(GUI.skin.label);
                titleStyle.fontStyle = FontStyle.Bold;
                titleStyle.alignment = TextAnchor.MiddleCenter;
                titleStyle.margin = new RectOffset(10, 10, 10, 0);
                return titleStyle;
            }
        }

        internal static GUIStyle ScrollViewStyle {
            get {
                var scrollViewStyle = new GUIStyle(GUI.skin.scrollView);
                scrollViewStyle.padding = new RectOffset(10, 10, 10, 10);
                return scrollViewStyle;
            }
        }

        internal static GUIStyle TextBoxStyle {
            get {
                var textBoxStyle = new GUIStyle(GUI.skin.box);
                textBoxStyle.alignment = TextAnchor.MiddleLeft;
                textBoxStyle.margin = new RectOffset(10, 10, 0, 10);
                textBoxStyle.padding = new RectOffset(10, 10, 10, 10);
                return textBoxStyle;
            }
        }

        internal static GUIStyle ApplyMappingButtonStyle {
            get {
                var style = new GUIStyle(GUI.skin.button);
                style.fontStyle = FontStyle.Bold;
                style.normal.textColor = new Color(0.14f, 0.5f, 0.34f);
                style.hover.textColor = new Color(0.24f, 0.6f, 0.44f);
                return style;
            }
        }

        internal static GUIStyle Credits {
            get {
                var style = new GUIStyle(GUI.skin.label);
                style.fontSize = (int)(style.fontSize * 0.9);
                style.hover.textColor = new Color(0x00 / 255f, 0x78 / 255f, 0xDA / 255f, 1f);
                return style;
            }
        }
    }

    internal class TextResource
    {
        internal static string Title = "Animation Blendshape Mapping Editor";
        internal static string Label_Title = "Animation Blendshape Mapper";
        internal static string Tooltip_AutomaticMapping = "Try to automatically match animation blendshapes with the characters blendshapes using fuzzy name matching.";
        internal static string Tooltip_ApplyMapping = "Apply the blendshape mapping to the animation and save it. Overwrites the existing blendshape mapping.";
        internal static string Tooltip_ClearMapping = "Reset all mappings.";

        internal static string Text_Intro =
            @"This editor helps mapping blendshapes of animations to a character and corrects the animation hierarchy. After selecting the Animator and Skinned Mesh Renderer containing the blendshapes you can adjust the mappings below.";

        internal static string Text_Credits = "2020 @ Christopher Gebhardt | https://github.com/chr33z";

        internal static string Template_ApplyDialogMessage = "This action will change {0} of {1} available blendshapes in the selected character and overwrite this animation. Do you want to do proceed?";
    }
}

#endif
