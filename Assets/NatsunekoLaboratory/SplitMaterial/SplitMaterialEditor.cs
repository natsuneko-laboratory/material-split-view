// ------------------------------------------------------------------------------------------
//  Copyright (c) Natsuneko. All rights reserved.
//  Licensed under the License Zero Parity 7.0.0 and MIT (contributions) with exception License Zero Patron 1.0.0. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------

using System;
using System.Linq.Expressions;

using UnityEditor;
using UnityEditor.UIElements;

using UnityEngine;
using UnityEngine.UIElements;

using Object = UnityEngine.Object;

namespace Assets.NatsunekoLaboratory.SplitMaterial
{
    public class SplitMaterialEditor : EditorWindow
    {
        private const string StyleGuid = "c1a0feed5272ba44bbdf76b167359241";
        private const string XamlGuid = "9e9d5dc723cfd0744aa9a3c60eb3e49d";

        [SerializeField]
        private Material _left;

        private Editor _leftEditor;
        private Vector2 _leftScroll = Vector2.zero;

        [SerializeField]
        private Material _right;

        private Editor _rightEditor;
        private Vector2 _rightScroll;

        private SerializedObject _so;

        [MenuItem("Window/Natsuneko Laboratory/Split Material")]
        public static void ShowMenu()
        {
            var window = GetWindow<SplitMaterialEditor>();
            window.titleContent = new GUIContent("Material Split Viewer");

            window.Show();
        }

        // ReSharper disable once InconsistentNaming
        private void CreateGUI()
        {
            _so = new SerializedObject(this);
            _so.Update();

            var root = rootVisualElement;
            root.styleSheets.Add(LoadAssetByGuid<StyleSheet>(StyleGuid));

            var xaml = LoadAssetByGuid<VisualTreeAsset>(XamlGuid);
            var tree = xaml.CloneTree();
            tree.Bind(_so);
            root.Add(tree);

            BindingObjectReference(w => _left, w => true, material =>
            {
                if (material != null)
                    _leftEditor = Editor.CreateEditor(material);
            });
            BindingObjectReference(w => _right, w => true, material =>
            {
                if (material != null)
                    _rightEditor = Editor.CreateEditor(material);
            });

            var leftGuiContainer = new IMGUIContainer(() =>
            {
                if (_leftEditor == null)
                    return;

                _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll);

                using (new EditorGUILayout.VerticalScope())
                {
                    _leftEditor.DrawHeader();
                    _leftEditor.OnInspectorGUI();
                }

                EditorGUILayout.EndScrollView();
            });
            var rightGuiContainer = new IMGUIContainer(() =>
            {
                if (_rightEditor == null)
                    return;

                _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll);

                using (new EditorGUILayout.VerticalScope())
                {
                    _rightEditor.DrawHeader();
                    _rightEditor.OnInspectorGUI();
                }

                EditorGUILayout.EndScrollView();
            });

            var leftContainer = root.Query<VisualElement>("left-container").First();
            leftContainer.Add(leftGuiContainer);

            var rightContainer = root.Query<VisualElement>("right-container").First();
            rightContainer.Add(rightGuiContainer);
        }

        private static T LoadAssetByGuid<T>(string guid) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
        }

        private void BindingObjectReference<T>(Expression<Func<string, T>> reference, Func<T, bool> validate, Action<T> callback) where T : Object
        {
            var variable = (reference.Body as MemberExpression)?.Member.Name;
            var property = _so.FindProperty(variable);
            var element = rootVisualElement.Q(variable);

            if (element is IBindable bindable)
                bindable.BindProperty(property);

            if (element is PropertyField prop)
                prop.RegisterCallback<ChangeEvent<Object>>(w =>
                {
                    var r = validate.Invoke((T)w.newValue);
                    if (!r)
                        property.objectReferenceValue = w.previousValue;
                    callback.Invoke((T)property.objectReferenceValue);
                });
        }

        private void BindingObject<T>(Expression<Func<string, T>> reference, Func<T, bool> validate, Action<T> callback)
        {
            var variable = (reference.Body as MemberExpression)?.Member.Name;
            var property = _so.FindProperty(variable);
            var element = rootVisualElement.Q(variable);

            if (element is IBindable bindable)
                bindable.BindProperty(property);

            if (element is INotifyValueChanged<T> changed)
                changed.RegisterValueChangedCallback(w =>
                {
                    var r = validate.Invoke(w.newValue);
                    if (!r)
                        changed.SetValueWithoutNotify(w.previousValue);
                    callback.Invoke(r ? w.newValue : w.previousValue);
                });
        }
    }
}