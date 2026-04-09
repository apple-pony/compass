using System;
using System.Reflection;
using UnityEngine;

namespace Compass
{
    // Duckov 로더가 찾는 엔트리 포인트
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        protected override void OnAfterSetup()
        {
            try
            {
                GameObject go = new GameObject("CompassRoot");
                UnityEngine.Object.DontDestroyOnLoad(go);

                go.AddComponent<SimpleCompassHUD>();

                Debug.Log("[Compass] ModBehaviour.OnAfterSetup - HUD initialized");
            }
            catch (Exception ex)
            {
                Debug.Log("[Compass] Init exception: " + ex);
            }
        }
    }

    /// <summary>
    /// 화면 상단에 눈금형 나침반을 띄우는 HUD.
    /// - 가운데 붉은 세로 바 (현재 바라보는 방향)
    /// - 5°마다 눈금, 30°마다 긴 눈금 + 각도 숫자
    /// - 0/90/180/270°엔 동/서/남/북 표시 (노란색)
    /// - 가운데 작은 노란 ▲ 표시
    /// </summary>
    internal class SimpleCompassHUD : MonoBehaviour
    {
        private Transform _target;              // 방향 기준 (메인 카메라)

        private Rect _barRect;                  // 전체 바 영역
        private float _lastScreenWidth;
        private float _lastScreenHeight;

        private GUIStyle _degreeStyle;          // 각도 숫자용
        private GUIStyle _cardinalStyle;        // 동서남북/▲ 용

        private static Texture2D _bgTexture;    // 반투명 검정 배경
        private static Texture2D _lineTexture;  // 흰색/색상 선 그리기용 (1x1)

        // 설정값
        private const float BarHeight = 40f;
        private const float BarMarginTop = 20f;
        private const float MaxVisibleAngle = 90f;      // 좌우 90도까지 눈금 표시
        private const float PixelsPerDegree = 4f;       // 1도당 픽셀 수 (넓이/민감도 조절)
        private const int MinorTickStep = 5;            // 작은 눈금 간격
        private const int MajorTickStep = 30;           // 큰 눈금 간격

        // Reflection cache
        private static FieldInfo _shoulderField;
        private static object _shoulderInstance;
        private static bool _shoulderInitialized = false;

        private void Awake()
        {
            UnityEngine.Object.DontDestroyOnLoad(this.gameObject);
        }

        private void Start()
        {
            SetupTarget();
            SetupStylesAndRect();
            EnsureTextures();
        }

        private void SetupTarget()
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                _target = cam.transform;
            }
        }

        private void SetupStylesAndRect()
        {
            _degreeStyle = new GUIStyle();
            _degreeStyle.alignment = TextAnchor.UpperCenter;
            _degreeStyle.fontSize = 12;
            _degreeStyle.normal.textColor = Color.white;
            _degreeStyle.clipping = TextClipping.Overflow;

            _cardinalStyle = new GUIStyle();
            _cardinalStyle.alignment = TextAnchor.UpperCenter;
            _cardinalStyle.fontSize = 14;
            _cardinalStyle.fontStyle = FontStyle.Bold;
            _cardinalStyle.normal.textColor = Color.yellow;
            _cardinalStyle.clipping = TextClipping.Overflow;

            UpdateBarRect();
        }

        private void UpdateBarRect()
        {
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;

            float height = BarHeight;

            // 🔽 눈금이 실제로 보이는 각도 범위(±MaxVisibleAngle)에 맞게 폭 계산
            float visibleWidth = MaxVisibleAngle * 2f * PixelsPerDegree; // 예: 180 * 4 = 720
            float width = visibleWidth + 40f; // 양 끝에 살짝 여유(20px씩)

            // 화면 중앙에 배치
            float x = (Screen.width - width) * 0.5f;
            float y = BarMarginTop;

            _barRect = new Rect(x, y, width, height);
        }

        private static void EnsureTextures()
        {
            if (_bgTexture == null)
            {
                _bgTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                _bgTexture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.5f)); // 반투명 검정
                _bgTexture.Apply();
            }

            if (_lineTexture == null)
            {
                _lineTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                _lineTexture.SetPixel(0, 0, Color.white); // 나중에 GUI.color로 색 변경
                _lineTexture.Apply();
            }
        }

        private void Update()
        {
            if (_target == null)
            {
                SetupTarget();
            }

            if (_degreeStyle == null || _cardinalStyle == null)
            {
                SetupStylesAndRect();
            }

            if (!Mathf.Approximately(_lastScreenWidth, Screen.width) ||
                !Mathf.Approximately(_lastScreenHeight, Screen.height))
            {
                SetupStylesAndRect();
            }
        }

        private void OnGUI()
        {
            if (_target == null || _degreeStyle == null || _cardinalStyle == null)
            {
                return;
            }

            // Don't draw if not final pass
            if (Event.current.type != EventType.Repaint)
                return;
            // Hide when active UI
            if (Cursor.visible)
                return;
            // Hide when no camera is active
            if (Camera.main == null || !Camera.main.enabled)
                return;

            // Hide when no 3rd person view mod loaded or loaded but switched to default isometric view
            if (!IsShoulderCameraActive())
                return;

            EnsureTextures();

            float yaw = Mathf.Repeat(_target.eulerAngles.y, 360f);

            float barLeft = _barRect.x;
            float barRight = _barRect.xMax;
            float barTop = _barRect.y;
            float barBottom = _barRect.yMax;
            float barCenterX = _barRect.center.x;

            Color oldColor = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(_barRect, _bgTexture);

            float midY = barTop + BarHeight * 0.6f;
            DrawLine(barLeft, midY, barRight, midY, 1f, new Color(1f, 1f, 1f, 0.6f));

            int yawInt = Mathf.RoundToInt(yaw);

            for (int angle = 0; angle < 360; angle += MinorTickStep)
            {
                float rel = Mathf.DeltaAngle(yaw, angle); // -180~180
                if (Mathf.Abs(rel) > MaxVisibleAngle)
                    continue;

                float x = barCenterX + rel * PixelsPerDegree;
                if (x < barLeft - 2f || x > barRight + 2f)
                    continue;

                bool isMajor = (angle % MajorTickStep) == 0;
                bool isCardinal = (angle % 90) == 0;

                float tickHeight = isMajor ? 14f : 7f;
                float tickYTop = midY - tickHeight;
                float tickYBottom = midY;

                Color tickColor = isCardinal
                    ? new Color(1f, 1f, 1f, 0.9f)
                    : new Color(1f, 1f, 1f, 0.6f);

                DrawLine(x, tickYTop, x, tickYBottom, 1f, tickColor);

                if (isMajor)
                {
                    string degText = angle + "°";
                    Rect labelRect = new Rect(x - 20f, tickYBottom + 2f, 40f, 16f);
                    GUI.Label(labelRect, degText, _degreeStyle);
                }

                if (isCardinal)
                {
                    string name = GetCardinalName(angle);
                    if (!string.IsNullOrEmpty(name))
                    {
                        Rect labelRect = new Rect(x - 20f, barTop, 40f, 16f);
                        GUI.Label(labelRect, name, _cardinalStyle);
                    }
                }
            }

            // 가운데 붉은 세로바
            DrawLine(barCenterX, barTop, barCenterX, barBottom, 2f, Color.red);

            // 가운데 노란 ▲
            Rect centerArrowRect = new Rect(barCenterX - 8f, barBottom - 18f, 16f, 16f);
            GUI.Label(centerArrowRect, "▲", _cardinalStyle);

            GUI.color = oldColor;
        }

        private static string GetCardinalName(int angle)
        {
            int a = ((angle % 360) + 360) % 360;
            if (a == 0) return "N";
            if (a == 90) return "E";
            if (a == 180) return "S";
            if (a == 270) return "W";
            return null;
        }

        private static void DrawLine(float x1, float y1, float x2, float y2, float width, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;

            if (Mathf.Approximately(y1, y2))
            {
                float y = y1 - width * 0.5f;
                float w = Mathf.Abs(x2 - x1);
                float x = Mathf.Min(x1, x2);
                GUI.DrawTexture(new Rect(x, y, w, width), _lineTexture);
            }
            else if (Mathf.Approximately(x1, x2))
            {
                float x = x1 - width * 0.5f;
                float h = Mathf.Abs(y2 - y1);
                float y = Mathf.Min(y1, y2);
                GUI.DrawTexture(new Rect(x, y, width, h), _lineTexture);
            }
            else
            {
                float w = Mathf.Abs(x2 - x1);
                float h = Mathf.Abs(y2 - y1);
                float x = Mathf.Min(x1, x2);
                float y = Mathf.Min(y1, y2);
                GUI.DrawTexture(new Rect(x, y, w, h), _lineTexture);
            }

            GUI.color = old;
        }

        // -------- Reflection logic --------
        private static void InitShoulderCamera()
        {
            if (_shoulderInitialized)
                return;

            _shoulderInitialized = true;

            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if (type.Namespace == null || !type.Namespace.Contains("ShoulderSurfing"))
                            continue;

                        var field = type.GetField("shoulderCameraToggled",
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.Static |
                            BindingFlags.Instance);

                        if (field != null)
                        {
                            _shoulderField = field;

                            if (!field.IsStatic)
                                _shoulderInstance = UnityEngine.Object.FindObjectOfType(type);

                            Debug.Log("[Compass] Shoulder camera field found: " + type.FullName);
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log("[Compass] Reflection error: " + ex);
            }
        }

        private static bool IsShoulderCameraActive()
        {
            if (!_shoulderInitialized)
                InitShoulderCamera();

            if (_shoulderField == null)
                return false;

            try
            {
                if (!_shoulderField.IsStatic && _shoulderInstance == null)
                {
                    _shoulderInstance = UnityEngine.Object.FindObjectOfType(_shoulderField.DeclaringType);
                }

                return (bool)_shoulderField.GetValue(_shoulderInstance);
            }
            catch
            {
                return false;
            }
        }
    }
}
