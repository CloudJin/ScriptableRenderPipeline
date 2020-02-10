using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    public GameObject m_prefab;
    public Material m_mat;
    MaterialPropertyBlock m_propertyBlock;
    MeshFilter m_meshFilter;
    Matrix4x4[] m_matrix;
    Vector4[] m_color;


    struct TestProperty
    {
        public Matrix4x4 matrix;
        public Vector4 color;
    }
    ComputeBuffer m_buf;
    TestProperty[] m_bufValue;
    Bounds m_bounds;
    // Start is called before the first frame update
    void Start()
    {
        m_meshFilter = m_prefab.GetComponent<MeshFilter>();
        m_propertyBlock = new MaterialPropertyBlock();
        m_matrix = new Matrix4x4[5];
        m_color = new Vector4[5];
        for (int i = 0; i != 5; ++i)
        {
            m_matrix[i] = Matrix4x4.identity;
            m_matrix[i].SetColumn(3, new Vector4(Random.RandomRange(-5, 5), 2, Random.RandomRange(10, 20), 1));
            m_color[i].x = Random.RandomRange(0.0f, 1.0f);
            m_color[i].y = Random.RandomRange(0.0f, 1.0f);
            m_color[i].z = Random.RandomRange(0.0f, 1.0f);
            m_color[i].w = 1;
        }

        m_bufValue = new TestProperty[5];
        for (int i = 0; i != 5; ++i)
        {
            m_bufValue[i].matrix = Matrix4x4.identity;
            m_bufValue[i].matrix.SetColumn(3, new Vector4(Random.RandomRange(-5, 5), 4, Random.RandomRange(20, 30), 1));
            m_bufValue[i].color.x = Random.RandomRange(0.0f, 1.0f);
            m_bufValue[i].color.y = Random.RandomRange(0.0f, 1.0f);
            m_bufValue[i].color.z = Random.RandomRange(0.0f, 1.0f);
            m_bufValue[i].color.w = 1;
        }

        m_buf = new ComputeBuffer(5, sizeof(float) * 20);
        m_buf.SetData(m_bufValue);
        m_mat.SetBuffer("_TestBuffer", m_buf);
        
        m_bounds = TransformBounds(m_meshFilter.sharedMesh.bounds, m_bufValue[0].matrix);
        for (int i = 1; i != 5; ++i)
        {
            m_bounds.Encapsulate(TransformBounds(m_meshFilter.sharedMesh.bounds, m_bufValue[i].matrix));
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        m_propertyBlock.SetVectorArray("_LitSimpleColor", m_color);
        Graphics.DrawMeshInstanced(m_meshFilter.sharedMesh, 0, m_mat, m_matrix, 5, m_propertyBlock);

        
        //Graphics.DrawMeshInstancedIndirect(m_meshFilter.sharedMesh, 0, m_mat, m_bounds, m_buf);
        //Graphics.DrawProceduralIndirect(m_mat, );
    }

    Bounds TransformBounds(Bounds bounds, Matrix4x4 matrix)
    {
        Vector3[] points = new Vector3[8];
        points[0] = bounds.center - bounds.extents;
        points[1] = bounds.center + bounds.extents;
        points[2].Set(points[0].x, points[0].y, points[1].z);
        points[3].Set(points[0].x, points[1].y, points[0].z);
        points[4].Set(points[1].x, points[0].y, points[0].z);
        points[5].Set(points[0].x, points[1].y, points[1].z);
        points[6].Set(points[1].x, points[0].y, points[1].z);
        points[7].Set(points[1].x, points[1].y, points[0].z);

        Vector3 point = matrix.MultiplyPoint(points[0]);
        Bounds result = new Bounds(point, Vector3.zero);
        for (int i = 1; i != 8; ++i)
        {
            point = matrix.MultiplyPoint(points[i]);
            result.Encapsulate(point);
        }
        return result;
    }
}
