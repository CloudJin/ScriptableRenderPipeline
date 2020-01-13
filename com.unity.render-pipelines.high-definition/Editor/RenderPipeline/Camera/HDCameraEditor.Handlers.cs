using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Object = UnityEngine.Object;

namespace UnityEditor.Rendering.HighDefinition
{
    partial class HDCameraEditor
    {
        void OnSceneGUI()
        {
            var c = (Camera)target;

            if (!UnityEditor.Rendering.CameraEditorUtils.IsViewPortRectValidToRender(c.rect))
                return;

            SceneViewOverlay_Window(EditorGUIUtility.TrTextContent("Camera Preview"), OnOverlayGUI, -100, target);
            UnityEditor.CameraEditorUtils.HandleFrustum(c, c.GetInstanceID());

            if (RenderPipelineManager.currentPipeline == null)
                return;
            var mgr = (RenderPipelineManager.currentPipeline as HDRenderPipeline).sharedRTManager;
            var info = mgr.GetDepthBufferMipChainInfoRef();
            DrawDebugView(mgr.GetDepthTextureOC(), info.mipLevelSizes[0].x, info.mipLevelSizes[0].y);
        }


        static private GameObject m_dstObj;
        RenderTexture m_depthTexture;
        RenderTexture m_dstTexture;


        struct DebugInfo
        {
            public Vector4 mipmap;
            public Vector4 mipmapOffsetSize;
            public Vector4 minMaxXY;
        };
        DebugInfo m_debugInfo;
        public Material m_debugMaterial;
        HDCamera m_hdCamera;
        void DrawDebugView(RTHandle rtHandle, int mip0Width, int mip0Height)
        {
            RenderTexture renderTexture = rtHandle.rt;
            if (renderTexture == null || m_dstObj == null)
                return;
            if (m_debugMaterial == null)
                m_debugMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/DebuggingZBuffer.mat");
            if (m_hdCamera == null)
                m_hdCamera = HDCamera.GetOrCreate(target as Camera);

            FetchDebugInfo(m_dstObj.name);

            if (m_dstTexture == null || m_dstTexture.width != renderTexture.width || m_dstTexture.height != renderTexture.height)
            {
                if (m_dstTexture)
                    m_dstTexture.Release();
                m_dstTexture = new RenderTexture(renderTexture.width, renderTexture.height, 0, RenderTextureFormat.RFloat);
                m_dstTexture.filterMode = FilterMode.Point;
            }
            Graphics.CopyTexture(renderTexture, 0, m_dstTexture, 0);

            int y = 0, yOffset = 20;
            Handles.BeginGUI();
            var label = $"Mipmap Level: " + m_debugInfo.mipmap.x;
            Rect labelRect = new Rect(0, y, 150, 100);
            EditorGUI.DropShadowLabel(labelRect, label, EditorStyles.wordWrappedLabel);
            y += yOffset;
            var label2 = string.Format("ClipZ: {0:f4}; ", m_debugInfo.mipmap.y);
            label2 += string.Format("Depth: {0:f4}", m_debugInfo.mipmap.z);
            Rect labelRect2 = new Rect(0, y, 200, 100);
            EditorGUI.DropShadowLabel(labelRect2, label2, EditorStyles.wordWrappedLabel);

            y += yOffset;
            Vector2 cornerMin = new Vector2();
            cornerMin.x = m_debugInfo.minMaxXY.x * m_debugInfo.mipmapOffsetSize.z + m_debugInfo.mipmapOffsetSize.x;
            cornerMin.y = m_debugInfo.minMaxXY.y * m_debugInfo.mipmapOffsetSize.w + m_debugInfo.mipmapOffsetSize.y;
            Vector2 cornerMax = new Vector2();
            cornerMax.x = m_debugInfo.minMaxXY.z * m_debugInfo.mipmapOffsetSize.z + m_debugInfo.mipmapOffsetSize.x;
            cornerMax.y = m_debugInfo.minMaxXY.w * m_debugInfo.mipmapOffsetSize.w + m_debugInfo.mipmapOffsetSize.y;
            var label3 = string.Format("MinCorner: ({0:f1}, {1:f1})", cornerMin.x, cornerMin.y);
            Rect labelRect3 = new Rect(0, y, 200, 100);
            EditorGUI.DropShadowLabel(labelRect3, label3, EditorStyles.wordWrappedLabel);

            y += yOffset;
            var label4 = string.Format("MaxCorner: ({0:f1}, {1:f1})", cornerMax.x, cornerMax.y);
            Rect labelRect4 = new Rect(0, y, 200, 100);
            EditorGUI.DropShadowLabel(labelRect4, label4, EditorStyles.wordWrappedLabel);

            y += yOffset;
            int size = 128;
            Rect cameraRect = new Rect(0, y, size, size);
            
            var texCoords = new Rect(m_debugInfo.mipmapOffsetSize.x / renderTexture.width,
                (m_debugInfo.mipmapOffsetSize.y) / renderTexture.height,
                (m_debugInfo.mipmapOffsetSize.z) / renderTexture.width,
                (m_debugInfo.mipmapOffsetSize.w) / renderTexture.height);
            GUI.DrawTextureWithTexCoords(cameraRect, m_dstTexture, texCoords, false);
            if (Event.current.type.Equals(EventType.Repaint))
            {
//                 cameraRect.x += 120;
//                 Graphics.DrawTexture(cameraRect, m_dstTexture, texCoords,
//                     0, 0, 0, 0, m_debugMaterial, 3);
            }

            Vector2 scale = new Vector2(1.0f, 1.0f);
            scale.x = (float)m_hdCamera.actualWidth / mip0Width;
            scale.y = (float)m_hdCamera.actualHeight / mip0Height;
            float height = m_debugInfo.minMaxXY.w - m_debugInfo.minMaxXY.y;
            Rect position = new Rect(m_debugInfo.minMaxXY.x * size * scale.x,
                y + (1.0f - m_debugInfo.minMaxXY.y * scale.y - height) * size,
                (m_debugInfo.minMaxXY.z - m_debugInfo.minMaxXY.x) * size,
                (m_debugInfo.minMaxXY.w - m_debugInfo.minMaxXY.y) * size);

            Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            GUI.DrawTexture(position, sprite.texture);


            //GUI.DrawTexture(cameraRect, m_dstTexture, ScaleMode.ScaleAndCrop, true, 0, Color.red, 0, 0);
            //EditorGUI.DrawPreviewTexture(cameraRect, renderTexture);
            Handles.EndGUI();
        }

        void FetchDebugInfo(string objName)
        {
            if (m_dstObj == null)
                return;



            //HDRenderPipeline pipeline = (RenderPipelineManager.currentPipeline as HDRenderPipeline);
            HiZBufferDebugInfo info;
            //UnityEditor.CameraEditorUtils.QueryHiZBufferDebugInfo(m_dstObj.name, out info);
            UnityEngine.Rendering.ScriptableRenderContext.QueryHiZBufferDebugInfo(objName, out info);
            m_debugInfo.mipmap = info.mipmap;
            m_debugInfo.minMaxXY = info.minMaxXY;
            m_debugInfo.mipmapOffsetSize = info.mipmapOffsetSize;
        }

        void OnOverlayGUI(Object target, SceneView sceneView)
        {
            UnityEditor.Rendering.CameraEditorUtils.DrawCameraSceneViewOverlay(target, sceneView, InitializePreviewCamera);
        }

        Camera InitializePreviewCamera(Camera c, Vector2 previewSize)
        {
            m_PreviewCamera.CopyFrom(c);
            EditorUtility.CopySerialized(c, m_PreviewCamera);
            var cameraData = c.GetComponent<HDAdditionalCameraData>();
            EditorUtility.CopySerialized(cameraData, m_PreviewAdditionalCameraData);
            // We need to explicitly reset the camera type here
            // It is probably a CameraType.Game, because we copied the source camera's properties.
            m_PreviewCamera.cameraType = CameraType.Preview;

            var previewTexture = GetPreviewTextureWithSize((int)previewSize.x, (int)previewSize.y);
            m_PreviewCamera.targetTexture = previewTexture;
            m_PreviewCamera.pixelRect = new Rect(0, 0, previewSize.x, previewSize.y);
            return m_PreviewCamera;
        }

        static Type k_SceneViewOverlay_WindowFunction = Type.GetType("UnityEditor.SceneViewOverlay+WindowFunction,UnityEditor");
        static Type k_SceneViewOverlay_WindowDisplayOption = Type.GetType("UnityEditor.SceneViewOverlay+WindowDisplayOption,UnityEditor");
        static MethodInfo k_SceneViewOverlay_Window = Type.GetType("UnityEditor.SceneViewOverlay,UnityEditor")
            .GetMethod(
                "Window",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
                null,
                CallingConventions.Any,
                new[] { typeof(GUIContent), k_SceneViewOverlay_WindowFunction, typeof(int), typeof(Object), k_SceneViewOverlay_WindowDisplayOption, typeof(EditorWindow) },
                null);
        static void SceneViewOverlay_Window(GUIContent title, Action<Object, SceneView> sceneViewFunc, int order, Object target)
        {
            k_SceneViewOverlay_Window.Invoke(null, new[]
            {
                title, DelegateUtility.Cast(sceneViewFunc, k_SceneViewOverlay_WindowFunction),
                order,
                target,
                Enum.ToObject(k_SceneViewOverlay_WindowDisplayOption, 1),
                null
            });
        }
    }
}
