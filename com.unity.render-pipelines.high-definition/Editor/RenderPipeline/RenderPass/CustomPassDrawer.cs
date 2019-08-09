using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.Linq;

namespace UnityEditor.Rendering.HighDefinition
{
	[CustomPropertyDrawer(typeof(CustomPass.CustomPassSettings), true)]
    class CustomPassDrawer : PropertyDrawer
    {
	    internal class Styles
	    {
		    public static float defaultLineSpace = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            public static float reorderableListHandleIndentWidth = 12;
			public static float indentSpaceInPixels = 16;
		    public static GUIContent callback = new GUIContent("Event", "Chose the Callback position for this render pass object.");
			public static GUIContent enabled = new GUIContent("Enabled", "Enable or Disable the custom pass");

		    //Headers
		    public static GUIContent filtersHeader = new GUIContent("Filters", "Filters.");
		    public static GUIContent renderHeader = new GUIContent("Overrides", "Different parts fo the rendering that you can choose to override.");
		    
		    //Filters
		    public static GUIContent renderQueueFilter = new GUIContent("Queue", "Filter the render queue range you want to render.");
		    public static GUIContent layerMask = new GUIContent("Layer Mask", "Chose the Callback position for this render pass object.");
		    public static GUIContent shaderPassFilter = new GUIContent("Shader Passes", "Chose the Callback position for this render pass object.");
		    
		    //Render Options
		    public static GUIContent overrideMaterial = new GUIContent("Material", "Chose an override material, every renderer will be rendered with this material.");
		    public static GUIContent overrideMaterialPass = new GUIContent("Pass Name", "The pass for the override material to use.");
		    public static GUIContent sortingCriteria = new GUIContent("Sorting", "Sorting settings used to render objects in a certain order.");

		    //Camera Settings
		    public static GUIContent overrideCamera = new GUIContent("Camera", "Override camera projections.");
		    public static GUIContent cameraFOV = new GUIContent("Field Of View", "Field Of View to render this pass in.");
		    public static GUIContent positionOffset = new GUIContent("Position Offset", "This Vector acts as a relative offset for the camera.");
		    public static GUIContent restoreCamera = new GUIContent("Restore", "Restore to the original camera projection before this pass.");

			public static string unlitShaderMessage = "HDRP Unlit shaders will force the shader passes to \"ForwardOnly\"";
			public static string hdrpLitShaderMessage = "HDRP Lit shaders are not supported in a custom pass";
	    }

	    //Headers and layout
	    private int m_FilterLines = 3;
	    private int m_MaterialLines = 2;
	    
	    private bool firstTime = true;

	    // Serialized Properties
		SerializedProperty      m_Name;
		SerializedProperty      m_Type;
		SerializedProperty      m_Enabled;
		SerializedProperty      m_TargetColorBuffer;
		SerializedProperty      m_TargetDepthBuffer;
		SerializedProperty      m_IsHDRPShader;
		SerializedProperty      m_ClearFlags;

		// Foldouts
		SerializedProperty      m_FilterFoldout;
		SerializedProperty      m_RendererFoldout;
		SerializedProperty      m_PassFoldout;

		// Filter
		SerializedProperty      m_RenderQueue;
		SerializedProperty      m_LayerMask;
		SerializedProperty      m_ShaderPasses;

		// Render
		SerializedProperty      m_OverrideMaterial;
		SerializedProperty      m_OverrideMaterialPass;
		SerializedProperty      m_SortingCriteria;

		// Fullscreen pass
		SerializedProperty		m_FullScreenPassMaterial;

	    ReorderableList m_ShaderPassesList;
		bool isUnlitShader;

		void FetchProperties(SerializedProperty property)
		{
			m_Name = property.FindPropertyRelative("name");
			m_Type = property.FindPropertyRelative("type");
			m_Enabled = property.FindPropertyRelative("enabled");
			m_TargetColorBuffer = property.FindPropertyRelative("targetColorBuffer");
			m_TargetDepthBuffer = property.FindPropertyRelative("targetDepthBuffer");
			m_IsHDRPShader = property.FindPropertyRelative("isHDRPShader");
			m_ClearFlags = property.FindPropertyRelative("clearFlags");

		    // Header bools
			m_FilterFoldout = property.FindPropertyRelative("filterFoldout");
			m_RendererFoldout = property.FindPropertyRelative("rendererFoldout");
			m_PassFoldout = property.FindPropertyRelative("passFoldout");

		    // Filter props
		    m_RenderQueue = property.FindPropertyRelative("renderQueueType");
		    m_LayerMask = property.FindPropertyRelative("layerMask");
		    m_ShaderPasses = property.FindPropertyRelative("passNames");

			// Render options
		    m_OverrideMaterial = property.FindPropertyRelative("overrideMaterial");
		    m_OverrideMaterialPass = property.FindPropertyRelative("overrideMaterialPassIndex");
			m_SortingCriteria = property.FindPropertyRelative("sortingCriteria");
			
			// FullScreen pass
			m_FullScreenPassMaterial = property.FindPropertyRelative("fullscreenPassMaterial");
		}

	    private void Init(SerializedProperty property)
	    {
			FetchProperties(property);

		    m_ShaderPassesList = new ReorderableList(null, m_ShaderPasses, true, true, true, true);

		    m_ShaderPassesList.drawElementCallback =
		    (Rect rect, int index, bool isActive, bool isFocused) =>
		    {
			    var element = m_ShaderPassesList.serializedProperty.GetArrayElementAtIndex(index);
			    var propRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
			    var labelWidth = EditorGUIUtility.labelWidth;
			    EditorGUIUtility.labelWidth = 50;
			    element.stringValue = EditorGUI.TextField(propRect, "Name", element.stringValue);
			    EditorGUIUtility.labelWidth = labelWidth;
		    };
		    
		    m_ShaderPassesList.drawHeaderCallback = (Rect testHeaderRect) => {
			    EditorGUI.LabelField(testHeaderRect, Styles.shaderPassFilter);
		    };

			UpdateIsUnlitShader();
			Undo.undoRedoPerformed += UpdateIsUnlitShader;

		    firstTime = false;
	    }

	    public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
	    {
			rect.height = EditorGUIUtility.singleLineHeight;
			EditorGUI.BeginChangeCheck();
			EditorGUI.BeginProperty(rect, label, property);

			if (firstTime)
			    Init(property);

			DoHeaderGUI(ref rect);

			if (m_PassFoldout.boolValue)
				return;

			EditorGUI.BeginDisabledGroup(!m_Enabled.boolValue);
			{
				DoCommonSettingsGUI(ref rect);

				CustomPassType	passType = (CustomPassType)m_Type.enumValueIndex;

				if (passType == CustomPassType.Renderers)
					DoRenderersGUI(property, rect);
				else
					DoFullScreenGUI(rect);
			}
			EditorGUI.EndDisabledGroup();

			EditorGUI.EndProperty();
			if (EditorGUI.EndChangeCheck())
				property.serializedObject.ApplyModifiedProperties();
	    }

		void DoCommonSettingsGUI(ref Rect rect)
		{
			EditorGUI.PropertyField(rect, m_Name);
			rect.y += Styles.defaultLineSpace;
			
			EditorGUI.PropertyField(rect, m_Type);
			rect.y += Styles.defaultLineSpace;

			EditorGUI.PropertyField(rect, m_TargetColorBuffer);
			rect.y += Styles.defaultLineSpace;

			EditorGUI.PropertyField(rect, m_TargetDepthBuffer);
			rect.y += Styles.defaultLineSpace;
			
			EditorGUI.PropertyField(rect, m_ClearFlags);
			rect.y += Styles.defaultLineSpace;
		}

		void DoHeaderGUI(ref Rect rect)
		{
			var enabledSize = EditorStyles.boldLabel.CalcSize(Styles.enabled) + new Vector2(Styles.reorderableListHandleIndentWidth, 0);
			var headerRect = new Rect(rect.x + Styles.reorderableListHandleIndentWidth,
							rect.y + EditorGUIUtility.standardVerticalSpacing,
							rect.width - Styles.reorderableListHandleIndentWidth - enabledSize.x,
							EditorGUIUtility.singleLineHeight);
			rect.y += Styles.defaultLineSpace;
			var enabledRect = headerRect;
			enabledRect.x = rect.xMax - enabledSize.x;
			enabledRect.width = enabledSize.x;

			m_PassFoldout.boolValue = EditorGUI.Foldout(headerRect, m_PassFoldout.boolValue, m_Name.stringValue, true, EditorStyles.boldLabel);
			EditorGUIUtility.labelWidth = enabledRect.width - 20;
			m_Enabled.boolValue = EditorGUI.Toggle(enabledRect, Styles.enabled, m_Enabled.boolValue);
			EditorGUIUtility.labelWidth = 0;
		}

		void DoFullScreenGUI(Rect rect)
		{
			EditorGUI.PropertyField(rect, m_FullScreenPassMaterial);
		}

		void DoRenderersGUI(SerializedProperty property, Rect rect)
		{
			DoFilters(ref rect);

			m_RendererFoldout.boolValue = EditorGUI.Foldout(rect, m_RendererFoldout.boolValue, Styles.renderHeader, true);
			rect.y += Styles.defaultLineSpace;
			if (m_RendererFoldout.boolValue)
			{
				EditorGUI.indentLevel++;
				//Override material
				DoMaterialOverride(ref rect);
				rect.y += Styles.defaultLineSpace;

				DoShaderPassesList(ref rect);

				EditorGUI.PropertyField(rect, m_SortingCriteria, Styles.sortingCriteria);
				rect.y += Styles.defaultLineSpace;

				EditorGUI.indentLevel--;
			}
		}

	    void DoFilters(ref Rect rect)
	    {
		    m_FilterFoldout.boolValue = EditorGUI.Foldout(rect, m_FilterFoldout.boolValue, Styles.filtersHeader, true);
		    rect.y += Styles.defaultLineSpace;
		    if (m_FilterFoldout.boolValue)
		    {
			    EditorGUI.indentLevel++;
			    //Render queue filter
			    EditorGUI.PropertyField(rect, m_RenderQueue, Styles.renderQueueFilter);
			    rect.y += Styles.defaultLineSpace;
			    //Layer mask
			    EditorGUI.PropertyField(rect, m_LayerMask, Styles.layerMask);
			    rect.y += Styles.defaultLineSpace;
			    //Shader pass list
			    EditorGUI.indentLevel--;
		    }
	    }

		GUIContent[] GetMaterialPassNames(Material mat)
		{
			GUIContent[] passNames = new GUIContent[mat.passCount];

			for (int i = 0; i < mat.passCount; i++)
			{
				string passName = mat.GetPassName(i);
				passNames[i] = new GUIContent(string.IsNullOrEmpty(passName) ? i.ToString() : passName);
			}
			
			return passNames;
		}

	    void DoMaterialOverride(ref Rect rect)
	    {
		    //Override material
			EditorGUI.BeginChangeCheck();
		    EditorGUI.PropertyField(rect, m_OverrideMaterial, Styles.overrideMaterial);
			if (EditorGUI.EndChangeCheck())
			{
				UpdateIsUnlitShader();
				var mat = m_OverrideMaterial.objectReferenceValue as Material;
				m_IsHDRPShader.boolValue = isUnlitShader || HDEditorUtils.IsHDRPShader(mat?.shader);
			}

		    if (m_OverrideMaterial.objectReferenceValue)
		    {
				var mat = m_OverrideMaterial.objectReferenceValue as Material;
			    rect.y += Styles.defaultLineSpace;
			    EditorGUI.indentLevel++;
			    EditorGUI.BeginChangeCheck();
			    // EditorGUI.PropertyField(rect, m_OverrideMaterialPass, Styles.overrideMaterialPass);
				m_OverrideMaterialPass.intValue = EditorGUI.IntPopup(rect, Styles.overrideMaterialPass, m_OverrideMaterialPass.intValue, GetMaterialPassNames(mat), Enumerable.Range(0, mat.passCount).ToArray());
			    if (EditorGUI.EndChangeCheck())
				    m_OverrideMaterialPass.intValue = Mathf.Max(0, m_OverrideMaterialPass.intValue);
			    EditorGUI.indentLevel--;
		    }
	    }

		void DoShaderPassesList(ref Rect rect)
		{
			Rect shaderPassesRect = rect;
			shaderPassesRect.x += EditorGUI.indentLevel * Styles.indentSpaceInPixels;
			shaderPassesRect.width -= EditorGUI.indentLevel * Styles.indentSpaceInPixels;

			var mat = m_OverrideMaterial.objectReferenceValue as Material;
			// We only draw the shader passes if we don't know which type of shader is used (aka user shaders)
			if (isUnlitShader)
			{
				EditorGUI.HelpBox(shaderPassesRect, Styles.unlitShaderMessage, MessageType.Info);
				rect.y += Styles.defaultLineSpace;
			}
			else if (m_IsHDRPShader.boolValue)
			{
				// Lit HDRP shader not supported
				m_IsHDRPShader.boolValue = true;
				EditorGUI.HelpBox(shaderPassesRect, Styles.hdrpLitShaderMessage, MessageType.Warning);
				rect.y += Styles.defaultLineSpace;
			}
			else
			{
				m_IsHDRPShader.boolValue = false;
				m_ShaderPassesList.DoList(shaderPassesRect);
				rect.y += m_ShaderPassesList.GetHeight();
			}
		}

		void UpdateIsUnlitShader()
		{
			var mat = m_OverrideMaterial.objectReferenceValue as Material;
			isUnlitShader = HDEditorUtils.IsUnlitHDRPShader(mat?.shader);
		}

	    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	    {
		    float height = Styles.defaultLineSpace;

			if (firstTime)
				Init(property);

			var type = (CustomPassType)m_Type.enumValueIndex;

			if (m_PassFoldout.boolValue)
				return height;
			
		    if (!firstTime)
		    {
				height += Styles.defaultLineSpace * 5; // name + type + target buffers + clearFlags
		        height += Styles.defaultLineSpace * (m_FilterFoldout.boolValue ? m_FilterLines : 1);

				if (type == CustomPassType.Renderers)
				{
					height += Styles.defaultLineSpace; // add line for overrides dropdown
					if (m_RendererFoldout.boolValue)
					{
						height += Styles.defaultLineSpace * (m_OverrideMaterial.objectReferenceValue != null ? m_MaterialLines : 1);
						var mat = m_OverrideMaterial.objectReferenceValue as Material;
						if (m_IsHDRPShader.boolValue || isUnlitShader)
							height += Styles.defaultLineSpace; // help box
						else
							height += m_ShaderPassesList.GetHeight(); // shader passes list
						height += Styles.defaultLineSpace; // sorting criteria;
					}
				}
		    }

		    return height;
	    }
    }
}