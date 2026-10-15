using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NumberBlocks.EditorTools
{
    /// <summary>
    /// 프로젝트를 처음 열면 자동으로 장면(Assets/Scenes/Main.unity)을 만들고
    /// 빌드 목록, 앱 이름, 가로 화면 설정을 해 둔다.
    /// 메뉴 "Number Blocks > 프로젝트 설정 다시 하기"로 언제든 다시 실행할 수 있다.
    /// </summary>
    [InitializeOnLoad]
    static class NumberBlocksProjectSetup
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string DoneKey = "NumberBlocks.SetupDone";

        static NumberBlocksProjectSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool(DoneKey, false)) Setup(false);
            };
        }

        [MenuItem("Number Blocks/프로젝트 설정 다시 하기")]
        static void SetupFromMenu() => Setup(true);

        static void Setup(bool force)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            SessionState.SetBool(DoneKey, true);

            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory("Assets/Scenes");
                var active = EditorSceneManager.GetActiveScene();
                bool replaceActive = string.IsNullOrEmpty(active.path) && !active.isDirty;
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                    replaceActive ? NewSceneMode.Single : NewSceneMode.Additive);
                EditorSceneManager.SaveScene(scene, ScenePath);
                if (!replaceActive) EditorSceneManager.CloseScene(scene, true);
                AssetDatabase.Refresh();
            }

            var scenes = EditorBuildSettings.scenes;
            if (force || scenes.Length == 0 || scenes[0].path != ScenePath)
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            if (force || PlayerSettings.productName != "숫자 블록 친구들")
            {
                PlayerSettings.companyName = "Hamsia";
                PlayerSettings.productName = "숫자 블록 친구들";
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.hamsia.numberblocks");
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.hamsia.numberblocks");
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
                PlayerSettings.allowedAutorotateToPortrait = false;
                PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
                PlayerSettings.allowedAutorotateToLandscapeLeft = true;
                PlayerSettings.allowedAutorotateToLandscapeRight = true;
            }

            // 빈 새 장면이 열려 있으면 게임 장면으로 바꿔 연다
            var current = EditorSceneManager.GetActiveScene();
            if (current.path != ScenePath && string.IsNullOrEmpty(current.path) && !current.isDirty)
                EditorSceneManager.OpenScene(ScenePath);

            Debug.Log("[Number Blocks] 준비 완료: ▶ 버튼을 누르면 게임이 시작됩니다.");
        }
    }
}
