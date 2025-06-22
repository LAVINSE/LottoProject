using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

// [CustomEditor(typeof(MonoBehaviour), true)]
// public class CustomEditorAttribute : Editor
// {
//     private Dictionary<string, object[]> parameterValues = new Dictionary<string, object[]>();

//     public override void OnInspectorGUI()
//     {
//         DrawDefaultInspector();
//         MonoBehaviour mono = (MonoBehaviour)target;
//         var type = mono.GetType();
//         var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

//         foreach (var method in methods)
//         {
//             var buttonAttributes = method.GetCustomAttributes(typeof(ButtonAttribute), false);
//             if (buttonAttributes.Length > 0)
//             {
//                 var buttonAttribute = buttonAttributes[0] as ButtonAttribute;

//                 if (buttonAttribute.Space > 0)
//                 {
//                     GUILayout.Space(buttonAttribute.Space);
//                 }

//                 string buttonName = buttonAttribute.DisplayName ?? method.Name;
//                 var parameters = method.GetParameters();

//                 // Action 매개변수가 있는 경우 처리
//                 bool hasUnsupportedParams = false;
//                 List<ParameterInfo> supportedParams = new List<ParameterInfo>();

//                 foreach (var param in parameters)
//                 {
//                     // Action 타입들은 건너뛰기 (기본값 null 사용)
//                     if (IsActionType(param.ParameterType))
//                     {
//                         continue;
//                     }
//                     // 지원하는 타입만 추가
//                     else if (IsSupportedType(param.ParameterType))
//                     {
//                         supportedParams.Add(param);
//                     }
//                     else
//                     {
//                         hasUnsupportedParams = true;
//                     }
//                 }

//                 // 지원하지 않는 매개변수가 있으면 버튼만 표시하고 넘어가기
//                 if (hasUnsupportedParams)
//                 {
//                     //EditorGUILayout.HelpBox("일부 매개변수는 기본값으로 설정됩니다.", MessageType.Info);
//                     continue;
//                 }

//                 // 박스 스타일로 전체를 감싸기
//                 EditorGUILayout.BeginVertical(GUI.skin.box);

//                 // 매개변수가 있으면 입력 필드들을 먼저 표시
//                 if (supportedParams.Count > 0)
//                 {
//                     if (!parameterValues.ContainsKey(method.Name))
//                     {
//                         parameterValues[method.Name] = new object[parameters.Length];
//                     }

//                     // 메소드 이름을 박스 상단에 표시
//                     EditorGUILayout.LabelField(method.Name, EditorStyles.boldLabel);

//                     EditorGUILayout.Space(5);

//                     // 매개변수 입력 필드들
//                     int supportedIndex = 0;
//                     for (int i = 0; i < parameters.Length; i++)
//                     {
//                         var param = parameters[i];

//                         if (IsActionType(param.ParameterType))
//                         {
//                             // Action 타입은 null로 설정
//                             parameterValues[method.Name][i] = null;
//                             continue;
//                         }

//                         if (!IsSupportedType(param.ParameterType))
//                         {
//                             parameterValues[method.Name][i] = GetDefaultValue(param.ParameterType);
//                             continue;
//                         }

//                         EditorGUILayout.BeginHorizontal();
//                         EditorGUILayout.LabelField(param.Name, GUILayout.Width(100));

//                         if (param.ParameterType == typeof(int))
//                         {
//                             parameterValues[method.Name][i] = EditorGUILayout.IntField((int)(parameterValues[method.Name][i] ?? 0));
//                         }
//                         else if (param.ParameterType == typeof(float))
//                         {
//                             parameterValues[method.Name][i] = EditorGUILayout.FloatField((float)(parameterValues[method.Name][i] ?? 0f));
//                         }
//                         else if (param.ParameterType == typeof(string))
//                         {
//                             parameterValues[method.Name][i] = EditorGUILayout.TextField((string)(parameterValues[method.Name][i] ?? ""));
//                         }
//                         else if (param.ParameterType == typeof(bool))
//                         {
//                             parameterValues[method.Name][i] = EditorGUILayout.Toggle((bool)(parameterValues[method.Name][i] ?? false));
//                         }

//                         EditorGUILayout.EndHorizontal();
//                         supportedIndex++;
//                     }

//                     EditorGUILayout.Space(8);
//                 }
//                 else if (parameters.Length > 0)
//                 {
//                     // 매개변수가 있지만 모두 Action이거나 지원하지 않는 타입인 경우
//                     if (!parameterValues.ContainsKey(method.Name))
//                     {
//                         parameterValues[method.Name] = new object[parameters.Length];
//                         for (int i = 0; i < parameters.Length; i++)
//                         {
//                             if (IsActionType(parameters[i].ParameterType))
//                             {
//                                 parameterValues[method.Name][i] = null;
//                             }
//                             else
//                             {
//                                 parameterValues[method.Name][i] = GetDefaultValue(parameters[i].ParameterType);
//                             }
//                         }
//                     }
//                 }

//                 // 버튼을 박스 하단에 표시
//                 if (GUILayout.Button(buttonName, GUILayout.Height(35f)))
//                 {
//                     if (parameters.Length > 0)
//                     {
//                         try
//                         {
//                             method.Invoke(mono, parameterValues[method.Name]);
//                         }
//                         catch (Exception e)
//                         {
//                             Debug.LogError($"Error invoking {method.Name}: {e.Message}");
//                         }
//                     }
//                     else
//                     {
//                         method.Invoke(mono, null);
//                     }
//                 }

//                 EditorGUILayout.EndVertical();

//                 // 박스 간 간격
//                 EditorGUILayout.Space(5);
//             }
//         }
//     }

//     private bool IsActionType(Type type)
//     {
//         // Action, Action<T>, Action<T1,T2> etc. 체크
//         if (type == typeof(Action))
//             return true;

//         if (type.IsGenericType)
//         {
//             var genericTypeDef = type.GetGenericTypeDefinition();
//             return genericTypeDef == typeof(Action<>) ||
//                    genericTypeDef == typeof(Action<,>) ||
//                    genericTypeDef == typeof(Action<,,>) ||
//                    genericTypeDef == typeof(Action<,,,>);
//         }

//         return false;
//     }

//     private bool IsSupportedType(Type type)
//     {
//         return type == typeof(int) ||
//                type == typeof(float) ||
//                type == typeof(string) ||
//                type == typeof(bool);
//     }

//     private object GetDefaultValue(Type type)
//     {
//         if (type.IsValueType)
//         {
//             return Activator.CreateInstance(type);
//         }
//         return null;
//     }
// }

// 더 세련된 스타일을 원한다면 아래 버전을 사용하세요
[CustomEditor(typeof(MonoBehaviour), true)]
public class CustomEditorAttributeStyled : Editor
{
    private Dictionary<string, object[]> parameterValues = new Dictionary<string, object[]>();

    // 커스텀 스타일 정의
    private GUIStyle boxStyle;
    private GUIStyle headerStyle;
    private GUIStyle buttonStyle;

    private void InitializeStyles()
    {
        if (boxStyle == null)
        {
            boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(5, 5, 5, 5)
            };

            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                //fontSize = 11,
                //fontStyle = FontStyle.Bold
            };
        }
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        InitializeStyles();

        MonoBehaviour mono = (MonoBehaviour)target;
        var type = mono.GetType();
        var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        foreach (var method in methods)
        {
            var buttonAttributes = method.GetCustomAttributes(typeof(ButtonAttribute), false);
            if (buttonAttributes.Length > 0)
            {
                var buttonAttribute = buttonAttributes[0] as ButtonAttribute;

                if (buttonAttribute.Space > 0)
                {
                    GUILayout.Space(buttonAttribute.Space);
                }

                string buttonName = buttonAttribute.DisplayName ?? method.Name;
                var parameters = method.GetParameters();

                // Action 매개변수가 있는 경우 처리
                bool hasUnsupportedParams = false;
                List<ParameterInfo> supportedParams = new List<ParameterInfo>();

                foreach (var param in parameters)
                {
                    if (IsActionType(param.ParameterType))
                    {
                        continue;
                    }
                    else if (IsSupportedType(param.ParameterType))
                    {
                        supportedParams.Add(param);
                    }
                    else
                    {
                        hasUnsupportedParams = true;
                    }
                }

                if (hasUnsupportedParams)
                {
                    continue;
                }

                // 커스텀 스타일 박스로 전체를 감싸기
                EditorGUILayout.BeginVertical(boxStyle);

                if (supportedParams.Count > 0)
                {
                    if (!parameterValues.ContainsKey(method.Name))
                    {
                        parameterValues[method.Name] = new object[parameters.Length];
                    }

                    // 헤더 스타일로 메소드 이름 표시
                    EditorGUILayout.LabelField(method.Name, headerStyle);

                    // 구분선
                    EditorGUILayout.Space(3);
                    var rect = EditorGUILayout.GetControlRect(false, 1);
                    EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
                    EditorGUILayout.Space(5);

                    // 매개변수 입력 필드들
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var param = parameters[i];

                        if (IsActionType(param.ParameterType))
                        {
                            parameterValues[method.Name][i] = null;
                            continue;
                        }

                        if (!IsSupportedType(param.ParameterType))
                        {
                            parameterValues[method.Name][i] = GetDefaultValue(param.ParameterType);
                            continue;
                        }

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(param.Name, GUILayout.Width(80));

                        if (param.ParameterType == typeof(int))
                        {
                            parameterValues[method.Name][i] = EditorGUILayout.IntField((int)(parameterValues[method.Name][i] ?? 0));
                        }
                        else if (param.ParameterType == typeof(float))
                        {
                            parameterValues[method.Name][i] = EditorGUILayout.FloatField((float)(parameterValues[method.Name][i] ?? 0f));
                        }
                        else if (param.ParameterType == typeof(string))
                        {
                            parameterValues[method.Name][i] = EditorGUILayout.TextField((string)(parameterValues[method.Name][i] ?? ""));
                        }
                        else if (param.ParameterType == typeof(bool))
                        {
                            parameterValues[method.Name][i] = EditorGUILayout.Toggle((bool)(parameterValues[method.Name][i] ?? false));
                        }

                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.Space(2);
                    }

                    EditorGUILayout.Space(5);
                }
                else if (parameters.Length > 0)
                {
                    if (!parameterValues.ContainsKey(method.Name))
                    {
                        parameterValues[method.Name] = new object[parameters.Length];
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            if (IsActionType(parameters[i].ParameterType))
                            {
                                parameterValues[method.Name][i] = null;
                            }
                            else
                            {
                                parameterValues[method.Name][i] = GetDefaultValue(parameters[i].ParameterType);
                            }
                        }
                    }
                }

                // 커스텀 스타일 버튼
                if (GUILayout.Button(buttonName, buttonStyle, GUILayout.Height(35f)))
                {
                    if (parameters.Length > 0)
                    {
                        try
                        {
                            method.Invoke(mono, parameterValues[method.Name]);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"Error invoking {method.Name}: {e.Message}");
                        }
                    }
                    else
                    {
                        method.Invoke(mono, null);
                    }
                }

                EditorGUILayout.EndVertical();
                //EditorGUILayout.Space(8);
            }
        }
    }

    private bool IsActionType(Type type)
    {
        if (type == typeof(Action))
            return true;

        if (type.IsGenericType)
        {
            var genericTypeDef = type.GetGenericTypeDefinition();
            return genericTypeDef == typeof(Action<>) ||
                   genericTypeDef == typeof(Action<,>) ||
                   genericTypeDef == typeof(Action<,,>) ||
                   genericTypeDef == typeof(Action<,,,>);
        }

        return false;
    }

    private bool IsSupportedType(Type type)
    {
        return type == typeof(int) ||
               type == typeof(float) ||
               type == typeof(string) ||
               type == typeof(bool);
    }

    private object GetDefaultValue(Type type)
    {
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }
        return null;
    }
}

[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = true;
    }
}
#endif // UNITY_EDITOR

[AttributeUsage(AttributeTargets.Method)]
public class ButtonAttribute : PropertyAttribute
{
    public string DisplayName { get; private set; }
    public float Space { get; private set; }

    public ButtonAttribute(string displayName = null)
    {
        DisplayName = displayName;
        Space = 0f;
    }

    public ButtonAttribute(float space)
    {
        DisplayName = null;
        Space = space;
    }

    public ButtonAttribute(string displayName, float space)
    {
        DisplayName = displayName;
        Space = space;
    }
}

[System.AttributeUsage(System.AttributeTargets.Field)]
public class ReadOnlyAttribute : PropertyAttribute { }