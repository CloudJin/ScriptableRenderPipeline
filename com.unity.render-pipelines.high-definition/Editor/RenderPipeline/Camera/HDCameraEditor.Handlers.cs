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

            DrawDebugView((RenderPipelineManager.currentPipeline as HDRenderPipeline).sharedRTManager.m_CameraDepthBufferMipChainOCDebug);

            SceneViewOverlay_Window(EditorGUIUtility.TrTextContent("Camera Preview"), OnOverlayGUI, -100, target);
            UnityEditor.CameraEditorUtils.HandleFrustum(c, c.GetInstanceID());
        }


        private GameObject m_dstObj;
        RenderTexture m_depthTexture;
        RenderTexture m_dstTexture;
        ComputeBuffer m_debugBuffer;
        struct DebugInfo
        {
            Vector4 mipmap;
            Vector4 minMaxXY;
        };
       
        void DrawDebugView(RenderTexture renderTexture)
        {
            if (renderTexture == null)
                return;

            if (m_dstTexture == null)
                m_dstTexture = new RenderTexture(renderTexture.width, renderTexture.height, 0, RenderTextureFormat.RFloat);
            Graphics.CopyTexture(renderTexture, 0, m_dstTexture, 0);
            Handles.BeginGUI();
            var label = $"test.";
            //Rect cameraRect = GUILayoutUtility.GetRect(64, 64);
            Rect labelRect = new Rect(0, 0, 100, 100);
            EditorGUI.DropShadowLabel(labelRect, label, EditorStyles.wordWrappedLabel);

            Rect cameraRect = new Rect(0, 20, 128, 128);
            var texCoords = new Rect(0, 0, 1, 1);
            GUI.DrawTextureWithTexCoords(cameraRect, m_dstTexture, texCoords, false);

            
            //GUI.DrawTexture(cameraRect, m_dstTexture, ScaleMode.ScaleAndCrop, true, 0, Color.red, 0, 0);
            //EditorGUI.DrawPreviewTexture(cameraRect, renderTexture);
            Handles.EndGUI();
        }

        void FetchDebugInfo()
        {
            if (m_dstObj == null)
                return;



            HDRenderPipeline pipeline = (RenderPipelineManager.currentPipeline as HDRenderPipeline);
            HiZBufferDebugInfo info;
            pipeline.QueryHiZBufferDebugInfo(m_dstObj.name, out info);

//             cs.SetBuffer(0, "_debugBufferEx", m_debugBuffer);
            //m_debugBuffer.GetData();
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
