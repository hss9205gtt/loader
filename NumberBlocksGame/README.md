# 숫자 블록 친구들 (Unity)

시아(만 3세 전후)가 휴대폰 터치로 블록 친구들을 합치고 나누면서 **"숫자 = 블록 개수"** 를 몸으로 익히는 3D 게임입니다.
넘버블록스에서 아이디어를 얻었지만, 저작권이 있는 원래 캐릭터는 쓰지 않고 **오리지널 블록 캐릭터**를 코드로 직접 만듭니다.

## 놀이 방법

| 손동작 | 일어나는 일 |
| --- | --- |
| 블록을 톡 누르기 | 큐브가 아래부터 하나씩 반짝이며 "하나, 둘, 셋" 하고 셉니다 |
| 블록을 끌어서 다른 블록 위에 놓기 | 두 블록이 합쳐집니다. 화면에 `1 + 2 = 3`, "하나 더하기 둘은 셋!" |
| 블록을 꾹 누르고 있기 | 맨 위 큐브 하나가 떨어져 나옵니다. `3 - 1 = 2` |
| `+ 1` 버튼 | 새 "하나" 블록이 나옵니다 |

- **미션 놀이**: 화면 위에 "셋을 만들어 봐요!" 가 나오고, 만들면 색종이가 터지며 칭찬해 줍니다.
- **자유 놀이**: 미션 없이 마음대로 합치고 나눕니다.
- 왼쪽 위 `5까지` / `10까지` 버튼으로 만들 수 있는 가장 큰 수를 바꿉니다. 처음에는 5까지로 시작하는 것을 권합니다.
- 6부터는 5개 줄 옆에 두 번째 줄이 생깁니다 (10칸 틀 모양). "5와 몇" 으로 보는 연습이 됩니다.

## 블록 캐릭터

- 숫자마다 색이 다릅니다 (1 빨강, 2 주황, 3 노랑, 4 초록, 5 하늘, 6 남색, 7 보라, 8 분홍, 9 청록, 10 하양).
- 모서리가 둥근 큐브, 눈, 웃는 입, 볼, 짧은 팔을 모두 코드로 만듭니다 (`Assets/Scripts/NumberBlocks/Art.cs`, `NumberBlock.cs`).
  외부 3D 모델 파일이 필요 없어서 Blender 없이 바로 열립니다.
- 숨쉬기, 눈 깜빡임, 점프, 고개 젓기, 팔 흔들며 축하하기 애니메이션이 있습니다.

## 여는 방법

1. [Unity Hub](https://unity.com/download)를 설치하고 **Unity 2022.3 LTS** (또는 Unity 6)를 설치합니다.
   설치할 때 **Android Build Support** (아이폰이면 **iOS Build Support**)를 함께 체크합니다.
2. Unity Hub에서 `Add` → `Add project from disk` → 이 `NumberBlocksGame` 폴더를 고릅니다.
3. 처음 열면 자동으로 `Assets/Scenes/Main.unity` 장면이 만들어지고 빌드 설정이 됩니다.
   (안 되면 위 메뉴 `Number Blocks > 프로젝트 설정 다시 하기`)
4. 위쪽 가운데 ▶ 버튼을 누르면 에디터에서 바로 놀아볼 수 있습니다 (마우스 = 손가락).

## 휴대폰에 설치하기

**안드로이드**

1. 휴대폰 `설정 > 휴대전화 정보 > 소프트웨어 정보` 에서 `빌드번호`를 7번 눌러 개발자 모드를 켜고, `개발자 옵션 > USB 디버깅`을 켭니다.
2. USB로 컴퓨터에 연결합니다.
3. Unity `File > Build Settings` → `Android` 선택 → `Switch Platform` → `Build And Run`.

**아이폰**

1. Mac과 Xcode가 필요합니다. `File > Build Settings` → `iOS` → `Switch Platform` → `Build`.
2. 만들어진 Xcode 프로젝트를 열고, `Signing & Capabilities`에서 본인 Apple ID를 고른 뒤 아이폰으로 실행합니다.

## 엄마 아빠 목소리 넣기

`Assets/Resources/Voices/` 폴더에 `1.wav` ("하나") ~ `10.wav` ("열"), `great.wav` ("잘했어요!") 를 녹음해 넣으면
효과음 대신 그 목소리가 나옵니다. 없으면 숫자마다 높이가 다른 "띵" 소리가 납니다.

## 파일 구성

```
Assets/
  Scripts/NumberBlocks/
    GameManager.cs   게임 전체 (카메라, 터치 조작, 합치기/나누기, 미션, 화면 버튼)
    NumberBlock.cs   블록 캐릭터 모양과 애니메이션
    Art.cs           둥근 큐브 메시, 재질, 글꼴
    Sound.cs         효과음 합성, 녹음 목소리 재생
    Confetti.cs      성공 축하 색종이
    NumberInfo.cs    숫자별 색과 한글 이름 (하나, 둘, 셋 ...)
  Editor/NumberBlocksProjectSetup.cs   처음 열 때 장면과 빌드 설정 자동 생성
  Resources/Fonts/KoreanFont.ttf       한글 글꼴 (주아체, SIL OFL)
  Resources/Voices/                    녹음 목소리를 넣는 곳
```

장면에 따로 배치할 것이 없습니다. 게임은 `GameManager`가 시작할 때 코드로 모두 만듭니다.

## 글꼴 라이선스

`KoreanFont.ttf` 는 Google Fonts의 [주아(Jua)](https://fonts.google.com/specimen/Jua) 글꼴이며 SIL Open Font License 1.1을 따릅니다 (`Assets/Licenses/Jua-Font-OFL.txt`).

## 브라우저 미리보기

`WebPreview/index.html` 은 같은 규칙을 three.js로 옮긴 미리보기입니다. 유니티 없이 브라우저에서 바로 열어 놀아볼 수 있습니다 (인터넷 연결 필요).
