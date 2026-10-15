using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NumberBlocks
{
    /// <summary>
    /// 게임 전체를 만든다: 카메라, 조명, 바닥, 블록, 터치 조작, 화면 버튼, 미션.
    ///
    /// 조작 (손가락 하나로 모두 가능)
    ///  - 블록 톡 누르기: 큐브를 하나씩 반짝이며 "하나, 둘, 셋" 센다
    ///  - 블록 끌어서 다른 블록 위에 놓기: 합쳐진다 (1 + 2 = 3)
    ///  - 블록 꾹 누르고 있기: 맨 위 하나가 떨어져 나온다 (3 - 1 = 2)
    ///  - "+1" 버튼: 새 "하나" 블록이 나온다
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        const float LongPressSeconds = 0.8f;
        const int MaxCubesOnBoard = 20;

        static GameManager instance;

        Camera cam;
        Sound sound;
        readonly List<NumberBlock> blocks = new List<NumberBlock>();

        // 놀이 설정 (부모가 화면 위쪽 버튼으로 바꾼다)
        int maxValue = 5;
        bool missionMode = true;
        int target;
        bool targetDone;

        // 바닥에서 블록이 다닐 수 있는 범위
        float xMin = -5, xMax = 5, zMin = -1, zMax = 3;
        Vector2Int lastScreen;

        // 터치 상태
        NumberBlock pressed;
        Vector2 downPos;
        float downTime;
        bool dragging;
        bool longPressDone;
        Vector3 grabOffset;

        // 화면 글자
        string bigText = "", smallText = "";
        float textTime = -10f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            // 빈 장면에서도 게임이 저절로 만들어지도록 한다
            if (instance != null) return;
            new GameObject("Number Blocks Game").AddComponent<GameManager>();
        }

        void Awake()
        {
            instance = this;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        void Start()
        {
            BuildWorld();
            sound = gameObject.AddComponent<Sound>();
            FitCamera();
            ResetBoard();
        }

        // ---------- 장면 만들기 ----------

        void BuildWorld()
        {
            cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.74f, 0.9f, 1f);
            cam.fieldOfView = 45f;

            var lightGo = new GameObject("Sun");
            var sun = lightGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.1f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.5f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.58f, 0.6f, 0.68f);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            Destroy(floor.GetComponent<Collider>());
            floor.transform.localScale = new Vector3(6f, 1f, 4f);
            floor.transform.position = new Vector3(0f, 0f, 8f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = Art.Mat(new Color(0.66f, 0.9f, 0.63f));
        }

        /// <summary>화면 비율에 맞춰 카메라 거리를 정하고, 블록이 다닐 범위를 계산한다.</summary>
        void FitCamera()
        {
            lastScreen = new Vector2Int(Screen.width, Screen.height);
            float aspect = Screen.width / (float)Mathf.Max(1, Screen.height);
            float zoom = aspect < 1.5f ? 1.5f / aspect : 1f; // 화면이 좁으면 뒤로 물러난다
            var lookAt = new Vector3(0f, 1.6f, 1.5f);
            cam.transform.position = lookAt + new Vector3(0f, 5f, -10.5f) * zoom;
            cam.transform.LookAt(lookAt);

            // 아래쪽 버튼 바로 위 높이에서 화면 양 끝이 바닥에 닿는 점
            if (ViewportFloor(new Vector2(0.06f, 0.24f), out var left) &&
                ViewportFloor(new Vector2(0.94f, 0.24f), out var right))
            {
                xMin = left.x + 0.3f;
                xMax = right.x - 0.3f;
                zMin = left.z + 0.6f;
                zMax = zMin + 3.5f;
            }
        }

        bool ViewportFloor(Vector2 viewport, out Vector3 point)
        {
            return FloorPoint(cam.ViewportPointToRay(viewport), out point);
        }

        static bool FloorPoint(Ray ray, out Vector3 point)
        {
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float d))
            {
                point = ray.GetPoint(d);
                return true;
            }
            point = Vector3.zero;
            return false;
        }

        // ---------- 매 프레임 ----------

        void Update()
        {
            if (Screen.width != lastScreen.x || Screen.height != lastScreen.y) FitCamera();
            HandleTouch();
            KeepApart();
        }

        void HandleTouch()
        {
            // 휴대폰에서는 첫 번째 손가락 터치가 마우스 입력으로도 들어온다
            Vector2 pos = Input.mousePosition;

            if (Input.GetMouseButtonDown(0) && !OverButtons(pos))
            {
                pressed = Pick(pos);
                downPos = pos;
                downTime = Time.time;
                dragging = false;
                longPressDone = false;
                if (pressed != null && FloorPoint(cam.ScreenPointToRay(pos), out var p))
                {
                    grabOffset = pressed.transform.position - p;
                    grabOffset.y = 0f;
                }
            }

            if (pressed == null) return;

            if (Input.GetMouseButton(0))
            {
                if (!dragging && (pos - downPos).magnitude > Screen.height * 0.03f)
                {
                    dragging = true;
                    pressed.Dragging = true;
                    pressed.StopCount();
                    sound.Whoosh();
                }

                if (dragging && FloorPoint(cam.ScreenPointToRay(pos), out var p))
                {
                    var t = p + grabOffset;
                    pressed.transform.position = Clamp(new Vector3(t.x, pressed.transform.position.y, t.z), pressed);
                }

                if (!dragging && !longPressDone && Time.time - downTime > LongPressSeconds)
                {
                    longPressDone = true;
                    Split(pressed);
                    pressed = null;
                }
            }
            else
            {
                // 손을 뗐다
                if (dragging)
                {
                    pressed.Dragging = false;
                    OnDrop(pressed);
                }
                else if (!longPressDone)
                {
                    OnTap(pressed);
                }
                pressed = null;
                dragging = false;
            }
        }

        NumberBlock Pick(Vector2 screenPos)
        {
            if (Physics.Raycast(cam.ScreenPointToRay(screenPos), out var hit, 100f))
            {
                var b = hit.collider.GetComponent<NumberBlock>();
                if (b != null && blocks.Contains(b)) return b;
            }
            return null;
        }

        Vector3 Clamp(Vector3 p, NumberBlock b)
        {
            float half = b.Width / 2f;
            p.x = Mathf.Clamp(p.x, xMin + half, Mathf.Max(xMin + half, xMax - half));
            p.z = Mathf.Clamp(p.z, zMin, zMax);
            return p;
        }

        /// <summary>내려놓은 블록끼리 겹치면 살살 밀어서 떨어뜨린다.</summary>
        void KeepApart()
        {
            for (int i = 0; i < blocks.Count; i++)
            {
                for (int j = i + 1; j < blocks.Count; j++)
                {
                    var a = blocks[i];
                    var b = blocks[j];
                    if (a.Dragging || b.Dragging) continue;
                    var d = b.transform.position - a.transform.position;
                    float needX = (a.Width + b.Width) / 2f + 0.15f;
                    if (Mathf.Abs(d.x) >= needX || Mathf.Abs(d.z) >= 1.1f) continue;

                    float push = Mathf.Min(needX - Mathf.Abs(d.x), 4f * Time.deltaTime);
                    float dir = d.x >= 0f ? 1f : -1f;
                    if (Mathf.Abs(d.x) < 0.01f) dir = (i % 2 == 0) ? 1f : -1f;
                    a.transform.position = Clamp(a.transform.position - new Vector3(dir * push / 2f, 0f, 0f), a);
                    b.transform.position = Clamp(b.transform.position + new Vector3(dir * push / 2f, 0f, 0f), b);
                }
            }
        }

        // ---------- 놀이 규칙 ----------

        void OnTap(NumberBlock block)
        {
            // 하나, 둘, 셋 ... 세기
            block.Count(
                i =>
                {
                    sound.Number(i);
                    Say(i.ToString(), NumberInfo.Word(i));
                },
                () =>
                {
                    Say(block.Value.ToString(), "모두 " + NumberInfo.Word(block.Value) + "!");
                    block.Jump();
                });
        }

        void OnDrop(NumberBlock a)
        {
            NumberBlock best = null;
            float bestDist = float.MaxValue;
            foreach (var b in blocks)
            {
                if (b == a) continue;
                var d = b.transform.position - a.transform.position;
                float needX = (a.Width + b.Width) / 2f + 0.1f;
                if (Mathf.Abs(d.x) > needX || Mathf.Abs(d.z) > 1.1f) continue;
                float dist = new Vector2(d.x, d.z).magnitude;
                if (dist < bestDist) { bestDist = dist; best = b; }
            }

            if (best != null) Merge(a, best);
            else sound.Pop();
        }

        /// <summary>a를 b 위에 올려 합친다. 결과는 b 자리에 생긴다.</summary>
        void Merge(NumberBlock a, NumberBlock b)
        {
            int sum = a.Value + b.Value;
            if (sum > maxValue)
            {
                b.Shake();
                sound.Boing();
                Say(b.Value + " + " + a.Value + " = " + sum, maxValue + "까지만 만들 수 있어요!");
                // 옆으로 살짝 밀어낸다
                float dir = a.transform.position.x >= b.transform.position.x ? 1f : -1f;
                a.transform.position = Clamp(b.transform.position + new Vector3(dir * ((a.Width + b.Width) / 2f + 0.6f), 0f, 0f), a);
                return;
            }
            StartCoroutine(MergeRoutine(a, b, sum));
        }

        IEnumerator MergeRoutine(NumberBlock a, NumberBlock b, int sum)
        {
            blocks.Remove(a);
            blocks.Remove(b);
            a.StopCount();
            b.StopCount();

            // a가 b 머리 위로 날아가 쏙 들어간다
            Vector3 from = a.transform.position;
            Vector3 to = b.transform.position + Vector3.up * b.Rows;
            for (float t = 0; t < 0.25f; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / 0.25f);
                a.transform.position = Vector3.Lerp(from, to, k) + Vector3.up * Mathf.Sin(k * Mathf.PI);
                yield return null;
            }

            Vector3 at = b.transform.position;
            at.y = 0f;
            int first = b.Value, second = a.Value;
            Destroy(a.gameObject);
            Destroy(b.gameObject);

            var made = Spawn(sum, at);
            sound.Pop();
            sound.Number(sum);
            Say(first + " + " + second + " = " + sum,
                NumberInfo.Word(first) + " 더하기 " + NumberInfo.WordTopic(second) + " " + NumberInfo.Word(sum) + "!");
            CheckMission(made);
        }

        /// <summary>맨 위 큐브 하나를 떼어 낸다. (3 → 2 와 1)</summary>
        void Split(NumberBlock block)
        {
            block.StopCount();
            if (block.Value == 1)
            {
                block.Shake();
                sound.Boing();
                Say("1", "하나는 더 나눌 수 없어요!");
                return;
            }

            int n = block.Value;
            Vector3 at = block.transform.position;
            at.y = 0f;
            blocks.Remove(block);
            Destroy(block.gameObject);

            var rest = Spawn(n - 1, at);
            float side = at.x + rest.Width / 2f + 1.2f <= xMax ? 1f : -1f;
            Spawn(1, at + new Vector3(side * (rest.Width / 2f + 1f), 0f, 0f));
            sound.Pop();
            sound.Number(n - 1);
            Say(n + " - 1 = " + (n - 1),
                NumberInfo.Word(n) + "에서 하나를 빼면 " + NumberInfo.Word(n - 1) + "!");
            CheckMission(rest);
        }

        NumberBlock Spawn(int value, Vector3 at)
        {
            var b = NumberBlock.Create(value, at);
            b.transform.position = Clamp(at, b);
            b.Pop();
            blocks.Add(b);
            return b;
        }

        void SpawnOne()
        {
            int cubes = 0;
            foreach (var b in blocks) cubes += b.Value;
            if (cubes >= MaxCubesOnBoard)
            {
                sound.Boing();
                Say("!", "블록이 너무 많아요. 합쳐 볼까요?");
                return;
            }

            // 다른 블록과 겹치지 않는 자리를 찾는다 (못 찾으면 아무 데나)
            Vector3 best = new Vector3(Random.Range(xMin, xMax), 0f, Random.Range(zMin, zMax));
            for (int tries = 0; tries < 30; tries++)
            {
                var p = new Vector3(Random.Range(xMin + 0.5f, xMax - 0.5f), 0f, Random.Range(zMin, zMax));
                bool free = true;
                foreach (var b in blocks)
                {
                    var d = b.transform.position - p;
                    if (Mathf.Abs(d.x) < b.Width / 2f + 0.8f && Mathf.Abs(d.z) < 1.2f) { free = false; break; }
                }
                if (free) { best = p; break; }
            }

            Spawn(1, best);
            sound.Pop();
            sound.Number(1);
            Say("1", "하나!");
        }

        void ResetBoard()
        {
            StopAllCoroutines();
            // 합쳐지는 중이던 블록까지 모두 치운다
            foreach (var b in FindObjectsByType<NumberBlock>(FindObjectsSortMode.None)) Destroy(b.gameObject);
            blocks.Clear();
            pressed = null;
            dragging = false;

            Spawn(1, new Vector3(-1.5f, 0f, zMin + 1.5f));
            Spawn(1, new Vector3(1.5f, 0f, zMin + 1.5f));
            NewTarget();
        }

        // ---------- 미션: "셋을 만들어 봐요!" ----------

        void NewTarget()
        {
            targetDone = false;
            var candidates = new List<int>();
            for (int n = 2; n <= maxValue; n++)
            {
                if (n == target) continue;
                bool onBoard = false;
                foreach (var b in blocks) if (b.Value == n) onBoard = true;
                if (!onBoard) candidates.Add(n);
            }
            if (candidates.Count == 0) candidates.Add(Random.Range(2, maxValue + 1));
            target = candidates[Random.Range(0, candidates.Count)];
        }

        void CheckMission(NumberBlock made)
        {
            if (!missionMode || targetDone || made.Value != target) return;
            targetDone = true;
            StartCoroutine(MissionSuccess(made));
        }

        IEnumerator MissionSuccess(NumberBlock made)
        {
            yield return new WaitForSeconds(0.5f);
            if (made != null)
            {
                Confetti.Burst(made.transform.position + Vector3.up * (made.Rows + 0.5f));
                made.Celebrate();
            }
            sound.Success();
            Say("잘했어요!", NumberInfo.WordObject(target) + " 만들었어요!");
            yield return new WaitForSeconds(3f);
            NewTarget();
        }

        // ---------- 화면 버튼과 글자 (OnGUI) ----------

        void Say(string big, string small)
        {
            bigText = big;
            smallText = small;
            textTime = Time.time;
        }

        float U => Mathf.Min(Screen.width, Screen.height) / 10f;
        Rect PlusRect => new Rect(0.4f * U, Screen.height - 2.2f * U, 3.2f * U, 1.9f * U);
        Rect ResetRect => new Rect(Screen.width - 2.7f * U, Screen.height - 1.5f * U, 2.4f * U, 1.1f * U);
        Rect MaxRect => new Rect(0.3f * U, 0.3f * U, 2.4f * U, 1.0f * U);
        Rect ModeRect => new Rect(Screen.width - 2.9f * U, 0.3f * U, 2.6f * U, 1.0f * U);

        bool OverButtons(Vector2 mousePos)
        {
            // Input.mousePosition은 아래가 0, GUI는 위가 0
            var p = new Vector2(mousePos.x, Screen.height - mousePos.y);
            return PlusRect.Contains(p) || ResetRect.Contains(p) || MaxRect.Contains(p) || ModeRect.Contains(p);
        }

        GUIStyle bigButton, smallButton, bigLabel, smallLabel, missionLabel;
        int stylesFor = -1;

        void OnGUI()
        {
            BuildStyles();

            if (GUI.Button(PlusRect, "+ 1", bigButton)) SpawnOne();
            if (GUI.Button(ResetRect, "처음부터", smallButton)) ResetBoard();
            if (GUI.Button(MaxRect, maxValue + "까지", smallButton))
            {
                maxValue = maxValue == 5 ? NumberInfo.MaxValue : 5;
                NewTarget();
            }
            if (GUI.Button(ModeRect, missionMode ? "미션 놀이" : "자유 놀이", smallButton))
            {
                missionMode = !missionMode;
                if (missionMode) NewTarget();
            }

            float u = U;
            if (missionMode)
            {
                var r = new Rect(Screen.width / 2f - 4f * u, 0.25f * u, 8f * u, 1.3f * u);
                string text = targetDone ? "참 잘했어요!" : NumberInfo.WordObject(target) + " 만들어 봐요!  (" + target + ")";
                Shadowed(r, text, missionLabel);
            }

            // 방금 일어난 일 (예: 1 + 1 = 2) — 3초 뒤 사라진다
            float age = Time.time - textTime;
            if (age < 3f)
            {
                var old = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01((3f - age) / 0.5f));
                float top = missionMode ? 1.6f * u : 0.5f * u;
                Shadowed(new Rect(0f, top, Screen.width, 1.4f * u), bigText, bigLabel);
                Shadowed(new Rect(0f, top + 1.35f * u, Screen.width, 0.9f * u), smallText, smallLabel);
                GUI.color = old;
            }
        }

        static void Shadowed(Rect r, string text, GUIStyle style)
        {
            var c = style.normal.textColor;
            style.normal.textColor = new Color(1f, 1f, 1f, 0.9f);
            float o = Mathf.Max(2f, style.fontSize * 0.06f);
            GUI.Label(new Rect(r.x + o, r.y + o, r.width, r.height), text, style);
            style.normal.textColor = c;
            GUI.Label(r, text, style);
        }

        void BuildStyles()
        {
            if (stylesFor == Screen.height && bigButton != null) return;
            stylesFor = Screen.height;
            float u = U;
            var font = Art.Font;
            var ink = new Color(0.17f, 0.18f, 0.26f);

            bigButton = ButtonStyle(NumberInfo.ColorOf(1), Mathf.RoundToInt(u * 0.95f), font);
            smallButton = ButtonStyle(new Color(0.36f, 0.42f, 0.6f), Mathf.RoundToInt(u * 0.42f), font);

            bigLabel = LabelStyle(Mathf.RoundToInt(u * 1.0f), ink, font);
            smallLabel = LabelStyle(Mathf.RoundToInt(u * 0.55f), ink, font);
            missionLabel = LabelStyle(Mathf.RoundToInt(u * 0.62f), new Color(0.55f, 0.2f, 0.75f), font);
        }

        static GUIStyle ButtonStyle(Color color, int size, Font font)
        {
            var s = new GUIStyle(GUI.skin.button)
            {
                font = font,
                fontSize = size,
                alignment = TextAnchor.MiddleCenter,
                border = new RectOffset(24, 24, 24, 24),
            };
            s.normal.background = RoundedTexture(color);
            s.hover.background = s.normal.background;
            s.active.background = RoundedTexture(Color.Lerp(color, Color.black, 0.2f));
            s.normal.textColor = s.hover.textColor = s.active.textColor = Color.white;
            return s;
        }

        static GUIStyle LabelStyle(int size, Color color, Font font)
        {
            var s = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = size,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
            };
            s.normal.textColor = color;
            return s;
        }

        /// <summary>모서리가 둥근 단색 버튼 배경 (9-slice로 늘려 쓴다).</summary>
        static Texture2D RoundedTexture(Color color)
        {
            const int size = 64, radius = 24;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                    float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    float a = Mathf.Clamp01(radius - d + 0.5f);
                    // 아래쪽을 살짝 어둡게 해서 눌리는 버튼처럼 보이게
                    var c = y < 8 ? Color.Lerp(color, Color.black, 0.25f) : color;
                    c.a = a;
                    px[y * size + x] = c;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }
    }
}
