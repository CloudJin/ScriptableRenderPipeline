using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering.Universal
{

    [ExecuteInEditMode]
    [AddComponentMenu("Rendering/2D/Light Reactor 2D (Experimental)")]
    public class LightReactor2D : ShadowCasterGroup2D
    {
        public enum ShadowModes
        {
            Default,
            CasterOnly,
            RendererOnly
        }

        [SerializeField] ShadowModes m_ShadowMode;
        [SerializeField] bool m_SelfShadows = false;
        [SerializeField] bool m_CastsShadows = true;
        [SerializeField] int[] m_ApplyToSortingLayers = new int[1];     // These are sorting layer IDs. If we need to update this at runtime make sure we add code to update global lights

        internal ShadowCasterGroup2D m_ShadowCasterGroup = null;

        [SerializeField] Vector3[] m_ShapePath;
        [SerializeField] int m_ShapePathHash = 0;
        [SerializeField] int m_PreviousPathHash = 0;
        [SerializeField] Mesh m_Mesh;

        internal Mesh mesh => m_Mesh;
        internal Vector3[] shapePath => m_ShapePath;
        internal int shapePathHash { get { return m_ShapePathHash; } set { m_ShapePathHash = value; } }

        Mesh m_ShadowMesh;



        Renderer m_Renderer;

        internal int[] applyToSortingLayers => m_ApplyToSortingLayers;

        public ShadowModes shadowMode => m_ShadowMode;
        public bool selfShadows => m_SelfShadows;
        public bool castsShadows => m_CastsShadows;


        int m_PreviousShadowGroup = 0;
        bool m_PreviousCastsShadows = true;
        Transform m_PreviousParent;


        private void Awake()
        {
            if (m_ShapePath == null || m_ShapePath.Length == 0)
                m_ShapePath = new Vector3[] { new Vector3(-0.5f, -0.5f), new Vector3(0.5f, -0.5f), new Vector3(0.5f, 0.5f), new Vector3(-0.5f, 0.5f) };

            m_PreviousParent = transform.parent;
        }

        private void OnStart()
        {
            m_Renderer = GetComponent<Renderer>();
            if (m_Renderer == null)
                m_ShadowMode = ShadowModes.CasterOnly;
        }

        protected void OnEnable()
        {
            if (m_Mesh == null)
            {
                m_Mesh = new Mesh();
                ShadowUtility.GenerateShadowMesh(ref m_Mesh, m_ShapePath);
                m_PreviousPathHash = m_ShapePathHash;
            }

            LightUtility.AddToLightReactorToGroup(this, out m_ShadowCasterGroup);
            ShadowCasterGroup2DManager.AddGroup(this);
        }

        protected void OnDisable()
        {
            LightUtility.RemoveLightReactorFromGroup(this, m_ShadowCasterGroup);
            ShadowCasterGroup2DManager.RemoveGroup(this);
        }

        public void Update()
        {
            bool rebuildMesh = false;
            rebuildMesh |= LightUtility.CheckForChange(m_ShapePathHash, ref m_PreviousPathHash);

            if (rebuildMesh)
                ShadowUtility.GenerateShadowMesh(ref m_Mesh, m_ShapePath);


            if (LightUtility.CheckForChange(transform.parent, ref m_PreviousParent))
            {
                if(m_ShadowCasterGroup != null)
                    LightUtility.RemoveLightReactorFromGroup(this, m_ShadowCasterGroup);

                LightUtility.AddToLightReactorToGroup(this, out m_ShadowCasterGroup);
            }


            if (LightUtility.CheckForChange(m_ShadowGroup, ref m_PreviousShadowGroup))
            {
                ShadowCasterGroup2DManager.RemoveGroup(this);
                ShadowCasterGroup2DManager.AddGroup(this);
            }


            if (LightUtility.CheckForChange(m_CastsShadows, ref m_PreviousCastsShadows))
            {
                if(m_CastsShadows)
                    ShadowCasterGroup2DManager.AddGroup(this);
                else
                    ShadowCasterGroup2DManager.RemoveGroup(this);
            }
        }
    }
}
