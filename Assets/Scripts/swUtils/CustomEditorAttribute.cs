using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(MonoBehaviour), true)]
public class CustomEditorAttributeStyled : Editor
{
    private Dictionary<string, object[]> parameterValues = new Dictionary<string, object[]>();
    private Dictionary<string, (List<object> keys, List<object> values)> _dictionaryCache = new();

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
        var _fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

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

                    // // 헤더 스타일로 메소드 이름 표시
                    // EditorGUILayout.LabelField(method.Name, headerStyle);

                    // // 구분선
                    // EditorGUILayout.Space(3);
                    // var rect = EditorGUILayout.GetControlRect(false, 1);
                    // EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
                    // EditorGUILayout.Space(5);

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

        foreach (var field in _fields)
        {
            var showDictAttr = field.GetCustomAttribute<ShowDictionaryAttribute>();
            if (showDictAttr != null)
            {
                DrawDictionaryField(field, showDictAttr);
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


    private void DrawDictionaryField(FieldInfo field, ShowDictionaryAttribute attr)
    {
        var dictValue = field.GetValue(target);
        if (dictValue == null) return;

        var dictType = dictValue.GetType();
        if (!dictType.IsGenericType || dictType.GetGenericTypeDefinition() != typeof(Dictionary<,>))
            return;

        var dict = (System.Collections.IDictionary)dictValue;
        string fieldName = field.Name;
        string key = $"{target.GetInstanceID()}_{fieldName}";

        // 캐시에서 리스트 가져오기 또는 생성
        if (!_dictionaryCache.ContainsKey(key))
        {
            var keys = new List<object>();
            var values = new List<object>();
            foreach (System.Collections.DictionaryEntry item in dict)
            {
                keys.Add(item.Key);
                values.Add(item.Value);
            }
            _dictionaryCache[key] = (keys, values);
        }

        var (keyList, valueList) = _dictionaryCache[key];

        // 🎨 예쁜 Dictionary 헤더 박스
        var headerStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(8, 8, 6, 6)
        };

        EditorGUILayout.BeginVertical(headerStyle);

        // 📁 커스텀 폴드아웃 with 아이콘과 카운트 (빠른 추가 버튼 제거)
        bool isExpanded = EditorPrefs.GetBool($"Dictionary_{key}", false);

        var foldoutStyle = new GUIStyle(EditorStyles.foldout)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 12
        };

        string dictIcon = isExpanded ? "📖" : "📚";
        string displayName = ObjectNames.NicifyVariableName(fieldName);
        bool newExpanded = EditorGUILayout.Foldout(isExpanded,
            $"{dictIcon} {displayName} ({keyList.Count} items)", foldoutStyle);

        if (newExpanded != isExpanded)
        {
            EditorPrefs.SetBool($"Dictionary_{key}", newExpanded);
        }

        if (newExpanded)
        {
            // 📊 Size 필드 (예쁜 스타일)
            EditorGUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("📊 Size", GUILayout.Width(60));
            int newSize = EditorGUILayout.IntField(keyList.Count, GUILayout.Width(60));
            if (newSize != keyList.Count)
            {
                Undo.RecordObject(target, $"Resize Dictionary {fieldName}");
                while (keyList.Count < newSize)
                {
                    keyList.Add(GetDefaultValue(dictType.GetGenericArguments()[0]));
                    valueList.Add(GetDefaultValue(dictType.GetGenericArguments()[1]));
                }
                while (keyList.Count > newSize)
                {
                    keyList.RemoveAt(keyList.Count - 1);
                    valueList.RemoveAt(valueList.Count - 1);
                }
                EditorUtility.SetDirty(target);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // 🏷️ 헤더 라벨 (Key/Value 구분)
            if (keyList.Count > 0)
            {
                var labelStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"🔑 {attr.keyLabel}", labelStyle, GUILayout.ExpandWidth(true));
                EditorGUILayout.LabelField($"💎 {attr.valueLabel}", labelStyle, GUILayout.ExpandWidth(true));
                EditorGUILayout.LabelField("Del", labelStyle, GUILayout.Width(40)); // 삭제 버튼 공간 라벨
                EditorGUILayout.EndHorizontal();

                // 구분선
                var rect = GUILayoutUtility.GetRect(0, 1);
                EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
                EditorGUILayout.Space(2);
            }

            // 📋 Dictionary 항목들 (레이아웃 수정)
            for (int i = 0; i < keyList.Count; i++)
            {
                var itemStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(6, 6, 4, 4),
                    margin = new RectOffset(0, 0, 2, 2)
                };

                EditorGUILayout.BeginVertical(itemStyle);
                EditorGUILayout.BeginHorizontal();

                // Key 필드 (50% 너비)
                keyList[i] = DrawDictionaryValue(keyList[i], "", dictType.GetGenericArguments()[0], GUILayout.ExpandWidth(true));

                EditorGUILayout.Space(5);

                // Value 필드 (50% 너비) 
                valueList[i] = DrawDictionaryValue(valueList[i], "", dictType.GetGenericArguments()[1], GUILayout.ExpandWidth(true));

                EditorGUILayout.Space(5);

                // 🗑️ 삭제 버튼 (고정 너비)
                var deleteStyle = new GUIStyle(GUI.skin.button)
                {
                    normal = { textColor = Color.white },
                    hover = { textColor = Color.white },
                    fontSize = 10
                };

                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("X", deleteStyle, GUILayout.Width(35), GUILayout.Height(20)))
                {
                    Undo.RecordObject(target, $"Remove Dictionary Item {fieldName}");
                    keyList.RemoveAt(i);
                    valueList.RemoveAt(i);
                    EditorUtility.SetDirty(target);
                    GUI.backgroundColor = Color.white;
                    break;
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            // ➕ Add Element 버튼 (그라데이션 스타일)
            EditorGUILayout.Space(4);
            GUI.backgroundColor = new Color(0.6f, 0.9f, 0.6f);
            var addButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 11
            };

            if (GUILayout.Button("➕ Add New Element", addButtonStyle, GUILayout.Height(25)))
            {
                Undo.RecordObject(target, $"Add Dictionary Item {fieldName}");
                keyList.Add(GetDefaultValue(dictType.GetGenericArguments()[0]));
                valueList.Add(GetDefaultValue(dictType.GetGenericArguments()[1]));
                EditorUtility.SetDirty(target);
            }
            GUI.backgroundColor = Color.white;

            // Dictionary에 변경사항 적용
            dict.Clear();
            for (int i = 0; i < keyList.Count; i++)
            {
                if (keyList[i] != null && !dict.Contains(keyList[i]))
                {
                    dict[keyList[i]] = valueList[i];
                }
            }
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(2);
    }

    private object DrawDictionaryValue(object value, string label, Type type, params GUILayoutOption[] options)
    {
        // 안전한 타입 검사 및 기본값 설정
        if (value == null || !type.IsAssignableFrom(value.GetType()))
        {
            value = GetDefaultValue(type);
        }

        try
        {
            if (type.IsEnum)
            {
                var enumValue = value != null && type.IsAssignableFrom(value.GetType()) ?
                    (Enum)value : (Enum)Enum.GetValues(type).GetValue(0);
                return EditorGUILayout.EnumPopup(enumValue, options);
            }
            else if (type == typeof(string))
            {
                return EditorGUILayout.TextField(value?.ToString() ?? "", options);
            }
            else if (type == typeof(int))
            {
                int intValue = 0;
                if (value != null)
                {
                    if (value is int i) intValue = i;
                    else if (int.TryParse(value.ToString(), out int parsed)) intValue = parsed;
                }
                return EditorGUILayout.IntField(intValue, options);
            }
            else if (type == typeof(float))
            {
                float floatValue = 0f;
                if (value != null)
                {
                    if (value is float f) floatValue = f;
                    else if (value is double d) floatValue = (float)d;
                    else if (float.TryParse(value.ToString(), out float parsed)) floatValue = parsed;
                }
                return EditorGUILayout.FloatField(floatValue, options);
            }
            else if (type == typeof(bool))
            {
                bool boolValue = false;
                if (value != null)
                {
                    if (value is bool b) boolValue = b;
                    else if (bool.TryParse(value.ToString(), out bool parsed)) boolValue = parsed;
                }
                return EditorGUILayout.Toggle(boolValue, options);
            }
            else if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                var objValue = value as UnityEngine.Object;
                return EditorGUILayout.ObjectField(objValue, type, true, options);
            }
            else
            {
                EditorGUILayout.LabelField($"Unsupported Type: {type.Name}", options);
                return value;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error drawing dictionary value of type {type.Name}: {ex.Message}");
            EditorGUILayout.LabelField($"Error: {type.Name}", options);
            return GetDefaultValue(type);
        }
    }

    private object GetDefaultValue(Type type)
    {
        try
        {
            if (type == null) return null;

            if (type.IsEnum)
            {
                var enumValues = Enum.GetValues(type);
                return enumValues.Length > 0 ? enumValues.GetValue(0) : null;
            }
            else if (type == typeof(string))
                return "";
            else if (type == typeof(int))
                return 0;
            else if (type == typeof(float))
                return 0f;
            else if (type == typeof(bool))
                return false;
            else if (type.IsValueType)
                return Activator.CreateInstance(type);
            else
                return null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error getting default value for type {type?.Name}: {ex.Message}");
            return null;
        }
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

public class ShowDictionaryAttribute : PropertyAttribute
{
    public string keyLabel;
    public string valueLabel;

    public ShowDictionaryAttribute(string keyLabel = "Key", string valueLabel = "Value")
    {
        this.keyLabel = keyLabel;
        this.valueLabel = valueLabel;
    }
}

[System.AttributeUsage(System.AttributeTargets.Field)]
public class ReadOnlyAttribute : PropertyAttribute { }