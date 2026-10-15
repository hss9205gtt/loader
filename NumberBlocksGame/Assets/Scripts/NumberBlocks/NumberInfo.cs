using UnityEngine;

namespace NumberBlocks
{
    /// <summary>숫자별 색, 한글 이름, 조사 같은 고정 정보.</summary>
    public static class NumberInfo
    {
        public const int MaxValue = 10;

        // 오리지널 색 팔레트 (무지개 순서로 1~10)
        static readonly Color[] Colors =
        {
            Color.gray,
            Hex(0xFF5A5F), // 1 빨강
            Hex(0xFF9F1C), // 2 주황
            Hex(0xFFD23F), // 3 노랑
            Hex(0x5BC85B), // 4 초록
            Hex(0x3FA9F5), // 5 하늘
            Hex(0x5A6FF0), // 6 남색
            Hex(0xA66CFF), // 7 보라
            Hex(0xFF6FB5), // 8 분홍
            Hex(0x2EC4B6), // 9 청록
            Hex(0xF2F2F2), // 10 하양
        };

        static readonly string[] Words =
        {
            "", "하나", "둘", "셋", "넷", "다섯", "여섯", "일곱", "여덟", "아홉", "열"
        };

        public static Color ColorOf(int n) => Colors[Mathf.Clamp(n, 0, MaxValue)];

        /// <summary>하나, 둘, 셋 ...</summary>
        public static string Word(int n) => (n >= 1 && n <= MaxValue) ? Words[n] : n.ToString();

        /// <summary>"하나는", "둘은" 처럼 은/는을 붙인다. (하나만 받침이 없다)</summary>
        public static string WordTopic(int n) => Word(n) + (n == 1 ? "는" : "은");

        /// <summary>"하나를", "셋을" 처럼 을/를을 붙인다.</summary>
        public static string WordObject(int n) => Word(n) + (n == 1 ? "를" : "을");

        static Color Hex(int rgb) => new Color(
            ((rgb >> 16) & 0xFF) / 255f,
            ((rgb >> 8) & 0xFF) / 255f,
            (rgb & 0xFF) / 255f);
    }
}
