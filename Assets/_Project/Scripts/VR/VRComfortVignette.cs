using UnityEngine;

namespace Skybound
{
    /// <summary>
    /// Soft dark ring in front of the VR camera that fades in while the player
    /// moves. It narrows the peripheral view during motion, which is the usual
    /// comfort trick for third-person VR. The mesh is generated at runtime so
    /// the effect needs no texture or material asset.
    /// </summary>
    public class VRComfortVignette : MonoBehaviour
    {
        [Header("Look")]
        [SerializeField] private float distance = 0.3f;
        [SerializeField] private float innerRadius = 0.26f;
        [SerializeField] private float outerRadius = 0.48f;
        [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.88f;
        [SerializeField] private float fadeSpeed = 3.5f;

        [Header("Shape")]
        [SerializeField] private int segments = 48;

        private MeshRenderer _renderer;
        private Material _material;
        private float _target;
        private float _current;

        /// <summary>0 = fully clear, 1 = full vignette.</summary>
        public float Intensity
        {
            get => _target;
            set => _target = Mathf.Clamp01(value);
        }

        private void Awake()
        {
            Build();
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }

        private void Update()
        {
            if (_material == null) return;

            float dt = Time.unscaledDeltaTime;
            _current = Mathf.MoveTowards(_current, _target * maxAlpha, fadeSpeed * dt);

            Color color = _material.color;
            color.a = _current;
            _material.color = color;

            bool visible = _current > 0.002f;
            if (_renderer != null && _renderer.enabled != visible) _renderer.enabled = visible;
        }

        private void Build()
        {
            transform.localPosition = new Vector3(0f, 0f, distance);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            var filter = gameObject.GetComponent<MeshFilter>();
            if (filter == null) filter = gameObject.AddComponent<MeshFilter>();
            _renderer = gameObject.GetComponent<MeshRenderer>();
            if (_renderer == null) _renderer = gameObject.AddComponent<MeshRenderer>();

            filter.sharedMesh = BuildAnnulus(segments, innerRadius, outerRadius);

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("UI/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");

            _material = new Material(shader) { name = "VR Comfort Vignette" };
            _material.color = new Color(0f, 0f, 0f, 0f);
            _material.renderQueue = 3100;

            _renderer.sharedMaterial = _material;
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.enabled = false;
        }

        private static Mesh BuildAnnulus(int count, float inner, float outer)
        {
            count = Mathf.Max(8, count);

            var vertices = new Vector3[count * 2];
            var colors = new Color[count * 2];
            var triangles = new int[count * 6];

            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f;
                float sin = Mathf.Sin(angle);
                float cos = Mathf.Cos(angle);

                vertices[i * 2] = new Vector3(cos * inner, sin * inner, 0f);
                vertices[i * 2 + 1] = new Vector3(cos * outer, sin * outer, 0f);

                colors[i * 2] = new Color(0f, 0f, 0f, 0f);
                colors[i * 2 + 1] = new Color(0f, 0f, 0f, 1f);
            }

            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;
                int a = i * 2;
                int b = i * 2 + 1;
                int c = next * 2;
                int d = next * 2 + 1;

                int t = i * 6;
                triangles[t] = a;
                triangles[t + 1] = b;
                triangles[t + 2] = c;
                triangles[t + 3] = b;
                triangles[t + 4] = d;
                triangles[t + 5] = c;
            }

            var mesh = new Mesh { name = "VR Comfort Vignette" };
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
