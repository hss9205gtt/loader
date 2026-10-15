using System;
using System.Collections;
using UnityEngine;

namespace NumberBlocks
{
    /// <summary>
    /// 숫자 블록 캐릭터 하나. 값이 n이면 큐브 n개로 몸이 만들어진다.
    /// 5개까지는 한 줄로 쌓고, 6부터는 옆에 두 번째 줄을 쌓는다 (10칸 틀 모양).
    /// </summary>
    public class NumberBlock : MonoBehaviour
    {
        public int Value { get; private set; }
        public int Columns { get; private set; }
        public int Rows { get; private set; }
        public bool Dragging { get; set; }

        /// <summary>바닥에서 차지하는 가로 폭 (블록 간 겹침 계산용)</summary>
        public float Width => Columns;

        Transform body;
        Transform[] cubes;
        Transform eyes;
        Transform[] arms;
        TextMesh label;
        Coroutine bodyAnim;
        Coroutine countAnim;
        float blinkAt;
        float phase;
        float lift;

        static readonly Color LabelColor = new Color(0.17f, 0.18f, 0.26f);
        static readonly Color Dark = new Color(0.12f, 0.12f, 0.16f);

        public static NumberBlock Create(int value, Vector3 position)
        {
            var go = new GameObject("Block " + value);
            go.transform.position = position;
            var block = go.AddComponent<NumberBlock>();
            block.Build(value);
            return block;
        }

        void Build(int value)
        {
            Value = Mathf.Clamp(value, 1, NumberInfo.MaxValue);
            Columns = (Value - 1) / 5 + 1;
            Rows = Mathf.Min(Value, 5);
            phase = UnityEngine.Random.value * 10f;
            blinkAt = Time.time + UnityEngine.Random.Range(1.5f, 4f);

            body = new GameObject("Body").transform;
            body.SetParent(transform, false);

            Color baseColor = NumberInfo.ColorOf(Value);
            Color lightColor = Color.Lerp(baseColor, Color.white, 0.18f);

            // 몸통 큐브: 한 칸씩 밝기를 번갈아 바꿔서 하나하나 세기 쉽게 한다
            cubes = new Transform[Value];
            for (int i = 0; i < Value; i++)
            {
                int col = i / 5, row = i % 5;
                float x = col - (Columns - 1) / 2f;
                var c = (i % 2 == 0) ? baseColor : lightColor;
                cubes[i] = Art.MeshObject("Cube " + (i + 1), body, Art.RoundedCube, c,
                    new Vector3(x, row + 0.5f, 0f), Vector3.one * 0.96f).transform;
            }

            BuildFace(baseColor);

            label = Art.Text("Label", transform, Value.ToString(), 0.9f, LabelColor);
            label.transform.localPosition = new Vector3(0f, Rows + 0.75f, 0f);

            var col3d = gameObject.AddComponent<BoxCollider>();
            col3d.center = new Vector3(0f, Rows / 2f, 0f);
            col3d.size = new Vector3(Columns, Rows, 1f);
        }

        void BuildFace(Color baseColor)
        {
            // 얼굴은 첫째 줄 맨 위 큐브의 앞면(카메라 쪽, -z)에 붙인다
            float x0 = -(Columns - 1) / 2f;
            var face = new GameObject("Face").transform;
            face.SetParent(body, false);
            face.localPosition = new Vector3(x0, Rows - 0.5f, -0.5f);

            eyes = new GameObject("Eyes").transform;
            eyes.SetParent(face, false);
            eyes.localPosition = new Vector3(0f, 0.1f, 0f);
            foreach (float sx in new[] { -0.19f, 0.19f })
            {
                Art.MeshObject("EyeWhite", eyes, Art.Sphere, Color.white, new Vector3(sx, 0f, -0.01f), new Vector3(0.26f, 0.3f, 0.1f));
                Art.MeshObject("Pupil", eyes, Art.Sphere, Dark, new Vector3(sx, -0.02f, -0.06f), new Vector3(0.12f, 0.15f, 0.05f));
                Art.MeshObject("Shine", eyes, Art.Sphere, Color.white, new Vector3(sx + 0.03f, 0.02f, -0.09f), new Vector3(0.04f, 0.04f, 0.02f));
            }

            // 웃는 입: 작은 구슬을 웃는 곡선으로 늘어놓는다
            for (int k = -2; k <= 2; k++)
            {
                Art.MeshObject("Mouth", face, Art.Sphere, Dark,
                    new Vector3(k * 0.065f, -0.2f + 0.012f * k * k, -0.03f), new Vector3(0.07f, 0.07f, 0.04f));
            }

            Color cheek = Color.Lerp(new Color(1f, 0.55f, 0.65f), baseColor, 0.25f);
            foreach (float sx in new[] { -0.31f, 0.31f })
                Art.MeshObject("Cheek", face, Art.Sphere, cheek, new Vector3(sx, -0.12f, -0.02f), new Vector3(0.13f, 0.08f, 0.03f));

            // 짧은 팔 (축하할 때 흔든다)
            Color armColor = Color.Lerp(baseColor, Color.black, 0.15f);
            arms = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                float ax = (side < 0 ? x0 - 0.5f : -x0 + 0.5f) + side * 0.08f;
                var pivot = new GameObject(side < 0 ? "ArmL" : "ArmR").transform;
                pivot.SetParent(body, false);
                pivot.localPosition = new Vector3(ax, Rows - 0.75f, 0f);
                pivot.localRotation = Quaternion.Euler(0f, 0f, side * 25f);
                Art.MeshObject("Arm", pivot, Art.RoundedCube, armColor, new Vector3(side * 0.08f, -0.2f, 0f), new Vector3(0.18f, 0.42f, 0.18f));
                arms[i] = pivot;
            }
        }

        void Update()
        {
            // 들고 있을 때는 살짝 떠오른다
            lift = Mathf.MoveTowards(lift, Dragging ? 0.6f : 0f, Time.deltaTime * 4f);
            var p = transform.position;
            transform.position = new Vector3(p.x, lift, p.z);

            if (bodyAnim == null)
            {
                // 숨쉬기
                float s = 0.025f * Mathf.Sin(Time.time * 2.2f + phase);
                body.localScale = new Vector3(1f + s, 1f - s, 1f + s);
                body.localPosition = Vector3.zero;
                body.localRotation = Quaternion.identity;
            }

            // 눈 깜빡이기
            if (Time.time > blinkAt)
            {
                float t = Time.time - blinkAt;
                eyes.localScale = new Vector3(1f, t < 0.12f ? 0.1f : 1f, 1f);
                if (t >= 0.12f) blinkAt = Time.time + UnityEngine.Random.Range(2f, 5f);
            }
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != null) label.transform.rotation = cam.transform.rotation;
        }

        // ---------- 애니메이션 ----------

        public void Pop() => PlayBody(PopRoutine());
        public void Jump() => PlayBody(JumpRoutine(1, 0.9f));
        public void Shake() => PlayBody(ShakeRoutine());
        public void Celebrate() => PlayBody(CelebrateRoutine());

        /// <summary>큐브를 아래에서부터 하나씩 반짝이며 센다. onEach(1), onEach(2) ...</summary>
        public void Count(Action<int> onEach, Action onDone = null)
        {
            StopCount();
            countAnim = StartCoroutine(CountRoutine(onEach, onDone));
        }

        public void StopCount()
        {
            if (countAnim != null) StopCoroutine(countAnim);
            countAnim = null;
            if (cubes != null)
                foreach (var c in cubes) c.localScale = Vector3.one * 0.96f;
        }

        void PlayBody(IEnumerator routine)
        {
            if (bodyAnim != null) StopCoroutine(bodyAnim);
            bodyAnim = StartCoroutine(Wrap(routine));
        }

        IEnumerator Wrap(IEnumerator routine)
        {
            // 중첩 코루틴을 직접 돌려야 StopCoroutine 한 번으로 함께 멈춘다
            while (routine.MoveNext()) yield return routine.Current;
            body.localScale = Vector3.one;
            body.localPosition = Vector3.zero;
            body.localRotation = Quaternion.identity;
            ResetArms();
            bodyAnim = null;
        }

        IEnumerator PopRoutine()
        {
            const float dur = 0.45f;
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                float k = t / dur;
                // 탄성 있게 커지기
                float s = 1f + Mathf.Sin(k * Mathf.PI * 2.5f) * (1f - k) * 0.35f;
                s *= Mathf.SmoothStep(0.3f, 1f, Mathf.Min(1f, k * 3f));
                body.localScale = new Vector3(s, s, s);
                yield return null;
            }
        }

        IEnumerator JumpRoutine(int times, float height)
        {
            for (int j = 0; j < times; j++)
            {
                // 웅크렸다가
                for (float t = 0; t < 0.1f; t += Time.deltaTime)
                {
                    float k = t / 0.1f;
                    body.localScale = new Vector3(1f + 0.15f * k, 1f - 0.2f * k, 1f + 0.15f * k);
                    yield return null;
                }
                // 뛰어오르고
                const float air = 0.4f;
                for (float t = 0; t < air; t += Time.deltaTime)
                {
                    float k = t / air;
                    body.localPosition = new Vector3(0f, 4f * height * k * (1f - k), 0f);
                    float st = 0.12f * Mathf.Sin(k * Mathf.PI);
                    body.localScale = new Vector3(1f - st, 1f + st, 1f - st);
                    yield return null;
                }
                body.localPosition = Vector3.zero;
                // 착지
                for (float t = 0; t < 0.12f; t += Time.deltaTime)
                {
                    float k = 1f - t / 0.12f;
                    body.localScale = new Vector3(1f + 0.12f * k, 1f - 0.15f * k, 1f + 0.12f * k);
                    yield return null;
                }
            }
        }

        IEnumerator ShakeRoutine()
        {
            const float dur = 0.6f;
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                float a = Mathf.Sin(t * 30f) * 18f * (1f - t / dur);
                body.localRotation = Quaternion.Euler(0f, a, 0f);
                yield return null;
            }
        }

        IEnumerator CelebrateRoutine()
        {
            var jump = JumpRoutine(2, 1.1f);
            float t = 0;
            while (jump.MoveNext())
            {
                t += Time.deltaTime;
                float wave = 60f + Mathf.Sin(t * 25f) * 40f;
                arms[0].localRotation = Quaternion.Euler(0f, 0f, -wave - 25f);
                arms[1].localRotation = Quaternion.Euler(0f, 0f, wave + 25f);
                yield return jump.Current;
            }
        }

        void ResetArms()
        {
            arms[0].localRotation = Quaternion.Euler(0f, 0f, -25f);
            arms[1].localRotation = Quaternion.Euler(0f, 0f, 25f);
        }

        IEnumerator CountRoutine(Action<int> onEach, Action onDone)
        {
            for (int i = 0; i < cubes.Length; i++)
            {
                onEach?.Invoke(i + 1);
                for (float t = 0; t < 0.45f; t += Time.deltaTime)
                {
                    float k = t / 0.45f;
                    cubes[i].localScale = Vector3.one * (0.96f + 0.3f * Mathf.Sin(k * Mathf.PI));
                    yield return null;
                }
                cubes[i].localScale = Vector3.one * 0.96f;
            }
            countAnim = null;
            onDone?.Invoke();
        }
    }
}
