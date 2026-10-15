using System.Collections.Generic;
using UnityEngine;

namespace NumberBlocks
{
    /// <summary>
    /// 3D 모델, 재질, 글꼴을 코드로 만든다. 외부 모델 파일 없이 동작하도록
    /// 둥근 큐브 메시를 절차적으로 생성한다.
    /// </summary>
    public static class Art
    {
        static Mesh roundedCube;
        static Mesh sphere;
        static Shader litShader;
        static Font font;
        static readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();

        /// <summary>한 변이 1인, 모서리가 둥근 큐브.</summary>
        public static Mesh RoundedCube
        {
            get
            {
                if (roundedCube == null) roundedCube = BuildRoundedCube(1f, 0.16f, 6);
                return roundedCube;
            }
        }

        public static Mesh Sphere
        {
            get
            {
                if (sphere == null) sphere = PrimitiveMesh(PrimitiveType.Sphere);
                return sphere;
            }
        }

        public static Material Mat(Color c)
        {
            if (materials.TryGetValue(c, out var m) && m != null) return m;
            m = new Material(LitShader) { color = c };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c); // URP
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.35f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.35f);
            materials[c] = m;
            return m;
        }

        /// <summary>프로젝트에 넣어 둔 한글 글꼴(Resources/Fonts/KoreanFont), 없으면 기본 글꼴.</summary>
        public static Font Font
        {
            get
            {
                if (font != null) return font;
                font = Resources.Load<Font>("Fonts/KoreanFont");
                if (font == null)
                {
                    try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
                    catch { font = null; }
                }
                return font;
            }
        }

        /// <summary>메시+재질을 가진 자식 오브젝트를 만든다.</summary>
        public static GameObject MeshObject(string name, Transform parent, Mesh mesh, Color color,
            Vector3 localPos, Vector3 localScale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = Mat(color);
            return go;
        }

        /// <summary>월드 공간 글자 (숫자 머리 위 표시 등).</summary>
        public static TextMesh Text(string name, Transform parent, string text, float size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tm = go.AddComponent<TextMesh>();
            tm.font = Font;
            tm.text = text;
            tm.fontSize = 96;
            tm.characterSize = size / 10f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            if (Font != null) go.GetComponent<MeshRenderer>().sharedMaterial = Font.material;
            return tm;
        }

        static Shader LitShader
        {
            get
            {
                if (litShader != null) return litShader;
                // 기본 큐브의 재질에서 셰이더를 빌려 온다.
                // (Built-in이든 URP든 현재 렌더 파이프라인의 기본 셰이더가 잡힌다)
                var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                litShader = tmp.GetComponent<MeshRenderer>().sharedMaterial.shader;
                Object.Destroy(tmp);
                return litShader;
            }
        }

        static Mesh PrimitiveMesh(PrimitiveType type)
        {
            var tmp = GameObject.CreatePrimitive(type);
            var mesh = tmp.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(tmp);
            return mesh;
        }

        /// <summary>
        /// 각 면을 grid x grid로 나눈 큐브의 꼭짓점을 안쪽 상자 쪽으로 당겨
        /// 모서리를 반지름 radius로 둥글게 만든다.
        /// </summary>
        static Mesh BuildRoundedCube(float size, float radius, int grid)
        {
            float h = size / 2f;
            float inner = h - radius;
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var tris = new List<int>();

            Vector3[] faceNormals = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
            foreach (var n in faceNormals)
            {
                // 면 위의 두 축 (u x v == n 이 되도록 골라 바깥쪽이 앞면이 되게 한다)
                Vector3 u = Mathf.Abs(n.y) > 0.5f ? Vector3.right : Vector3.up;
                Vector3 v = Vector3.Cross(n, u);
                int start = verts.Count;
                for (int y = 0; y <= grid; y++)
                {
                    for (int x = 0; x <= grid; x++)
                    {
                        float fu = (x / (float)grid - 0.5f) * size;
                        float fv = (y / (float)grid - 0.5f) * size;
                        Vector3 p = n * h + u * fu + v * fv;
                        Vector3 core = new Vector3(
                            Mathf.Clamp(p.x, -inner, inner),
                            Mathf.Clamp(p.y, -inner, inner),
                            Mathf.Clamp(p.z, -inner, inner));
                        Vector3 dir = (p - core).normalized;
                        verts.Add(core + dir * radius);
                        normals.Add(dir);
                    }
                }
                for (int y = 0; y < grid; y++)
                {
                    for (int x = 0; x < grid; x++)
                    {
                        int i = start + y * (grid + 1) + x;
                        int right = i + 1, up = i + grid + 1, upRight = up + 1;
                        // u x v = n 이므로 (i, right, up) 순서면 바깥쪽이 앞면이 된다
                        tris.Add(i); tris.Add(right); tris.Add(up);
                        tris.Add(right); tris.Add(upRight); tris.Add(up);
                    }
                }
            }

            var mesh = new Mesh { name = "RoundedCube" };
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
