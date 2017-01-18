﻿using EA4S.Db;
using System.Collections.Generic;
using UnityEngine;

namespace EA4S
{
    public enum AppScene
    {
        Home,
        Mood,
        Map,
        Book,
        Intro,
        GameSelector,
        MiniGame,
        Assessment,
        AnturaSpace,
        Rewards,
        PlaySessionResult,
        DebugPanel
    }

    /// <summary>
    /// Controls the transitions between different scenes in the application.
    /// </summary>
    public class NavigationManager : MonoBehaviour
    {
        // refactor: the singleton entry point should be through the AppManager
        public static NavigationManager I;

        public AppScene CurrentScene;
        public bool IsLoadingMinigame { get; private set; } // Daniele mod - SceneTransitioner needs it to know when a minigame is being loaded 

        void Start()
        {
            I = this;
        }

        #region Automatic navigation API

        /// <summary>
        /// Given the current context, selects the scene that should be loaded next and loads it.
        /// </summary>
        // refactor: the whole NavigationManager could work using just GoToNextScene (and similars, such as GoBack), so that it controls all scene movement
        public void GoToNextScene()
        {
            //var nextScene = GetNextScene();
            switch (CurrentScene)
            {
                case AppScene.Home:
                    break;
                case AppScene.Mood:
                    break;
                case AppScene.Map:
                    if (AppManager.I.Teacher.journeyHelper.IsAssessmentTime(AppManager.I.Player.CurrentJourneyPosition))
                        GoToGameScene(TeacherAI.I.CurrentMiniGame);
                    else
                        GoToScene(AppScene.GameSelector);
                    break;
                case AppScene.Book:
                    break;
                case AppScene.Intro:
                    break;
                case AppScene.GameSelector:
                    AppManager.I.Player.ResetPlaySessionMinigame();
                    WorldManager.I.CurrentWorld = (WorldID)(AppManager.I.Player.CurrentJourneyPosition.Stage - 1);
                    GoToGameScene(TeacherAI.I.CurrentMiniGame);
                    break;
                case AppScene.MiniGame:
                    if (AppManager.I.Teacher.journeyHelper.IsAssessmentTime(AppManager.I.Player.CurrentJourneyPosition))
                    {
                        // assessment ended!
                        AppManager.I.Player.ResetPlaySessionMinigame();
                        GoToScene(AppScene.Rewards);
                    }
                    else {
                        AppManager.I.Player.NextPlaySessionMinigame();
                        if (AppManager.I.Player.CurrentMiniGameInPlaySession >= TeacherAI.I.CurrentPlaySessionMiniGames.Count)
                        {
                            /// - Reward screen
                            /// *-- check first contact : 
                            /// 

                            // MaxJourneyPosistionProgress (with Reset CurrentMiniGameInPlaySession) is performed contestually to reward creation to avoid un-sync results.
                            GoToScene(AppScene.PlaySessionResult);
                        }
                        else {
                            // Next game
                            GoToGameScene(TeacherAI.I.CurrentMiniGame);
                        }
                    }
                    break;
                case AppScene.AnturaSpace:
                    break;
                case AppScene.Rewards:
                    MaxJourneyPositionProgress();
                    GoToScene(AppScene.Map);
                    break;
                case AppScene.PlaySessionResult:
                    GoToScene(AppScene.Map);
                    break;
                case AppScene.DebugPanel:
                    GoToGameScene(AppManager.I.CurrentMinigame);
                    break;
                default:
                    break;
            }
        }

        #endregion

        #region Direct navigation API

        // refactor: GoToScene can be separated for safety reasons in GoToMinigame (with a string code, or minigame code) and GoToAppScene (with an AppScene as the parameter)
        public void GoToScene(string sceneName)
        {
            IsLoadingMinigame = sceneName.Substring(0, 5) == "game_";
            AppManager.Instance.Modules.SceneModule.LoadSceneWithTransition(sceneName);

            if (AppConstants.UseUnityAnalytics) {
                UnityEngine.Analytics.Analytics.CustomEvent("changeScene", new Dictionary<string, object> { { "scene", sceneName } });
            }
        }

        public void GoToScene(AppScene newScene)
        {
            // Additional checks for specific scenes
            switch (newScene) {
                case AppScene.Rewards:
                    // Already rewarded this playsession?
                    if (RewardSystemManager.RewardAlreadyUnlocked(AppManager.I.Player.CurrentJourneyPosition)) {
                        GoToScene(AppScene.Map);
                        return;
                    }
                    break;
                default:
                    // Do nothing
                    break;
            }

            var nextSceneName = GetSceneName(newScene);
            GoToScene(nextSceneName);
        }

        public void GoToGameScene(MiniGameData _miniGame)
        {
            AppManager.I.GameLauncher.LaunchGame(_miniGame.Code);
        }

        #endregion

        #region Specific scene change methods

        public void GoHome()
        {
            GoToScene(AppScene.Home);
        }

        public void OpenPlayerBook()
        {
            GoToScene(AppScene.Book);
        }

        public void ExitAndGoHome()
        {
            if (CurrentScene == AppScene.Map)
            {
                GoToScene(AppScene.Home);
            }
            else {
                GoToScene(AppScene.Map);
            }
        }

        // obsolete: to be implemented?
        public void GoBack() { }

        // obsolete: to be implemented?
        public void ExitCurrentGame() { }

        #endregion

        // refactor: scene names should match AppScene so that this can be removed
        public string GetSceneName(AppScene scene, Db.MiniGameData minigameData = null)
        {
            switch (scene) {
                case AppScene.Home:
                    return "_Start";
                case AppScene.Mood:
                    return "app_Mood";
                case AppScene.Map:
                    return "app_Map";
                case AppScene.Book:
                    return "app_PlayerBook";
                case AppScene.Intro:
                    return "app_Intro";
                case AppScene.GameSelector:
                    return "app_GamesSelector";
                case AppScene.MiniGame:
                    return minigameData.Scene;
                case AppScene.Assessment:
                    return "app_Assessment";
                case AppScene.AnturaSpace:
                    return "app_AnturaSpace";
                case AppScene.Rewards:
                    return "app_Rewards";
                case AppScene.PlaySessionResult:
                    return "app_PlaySessionResult";
                default:
                    return "";
            }
        }

        // refactor: move this method directly to PlayerProfile 
        public void MaxJourneyPositionProgress()
        {
            AppManager.I.Player.SetMaxJourneyPosition(TeacherAI.I.journeyHelper.FindNextJourneyPosition(AppManager.I.Player.CurrentJourneyPosition));
        }

        // obsolete: unused
        /*public string GetCurrentScene()
        {
            return "";
        }*/

        // refactor: move these a more coherent manager, which handles the state of a play session between minigames
        #region temp for demo
        List<EndsessionResultData> EndSessionResults = new List<EndsessionResultData>();

        /// <summary>
        /// Called to notify end minigame with result (pushed continue button on UI).
        /// </summary>
        /// <param name="_stars">The stars.</param>
        public void EndMinigame(int _stars)
        {
            if (TeacherAI.I.CurrentMiniGame == null)
                return;
            EndsessionResultData res = new EndsessionResultData(_stars, TeacherAI.I.CurrentMiniGame.GetIconResourcePath(), TeacherAI.I.CurrentMiniGame.GetBadgeIconResourcePath());
            EndSessionResults.Add(res);

        }

        /// <summary>
        /// Uses the end session results and reset it.
        /// </summary>
        /// <returns></returns>
        public List<EndsessionResultData> UseEndSessionResults()
        {
            List<EndsessionResultData> returnResult = EndSessionResults;
            EndSessionResults = new List<EndsessionResultData>();
            return returnResult;
        }

        /// <summary>
        /// Calculates the unlock item count in accord to gameplay result information.
        /// </summary>
        /// <returns></returns>
        public int CalculateUnlockItemCount()
        {
            int totalEarnedStars = 0;
            for (int i = 0; i < EndSessionResults.Count; i++) {
                totalEarnedStars += EndSessionResults[i].Stars;
            }
            // Add bones to player
            int unlockItemsCount = 0;
            if (EndSessionResults.Count > 0) {
                float starRatio = totalEarnedStars / EndSessionResults.Count;
                // Prevent aproximation errors (0.99f must be == 1 but 0.7f must be == 0) 
                unlockItemsCount = starRatio - Mathf.CeilToInt(starRatio) < 0.0001f
                    ? Mathf.CeilToInt(starRatio)
                    : Mathf.RoundToInt(starRatio);
            }
            // decrement because the number of stars needed to unlock the first reward is 2. 
            unlockItemsCount -= 1;
            return unlockItemsCount;
        }

        /// <summary>
        /// Called to notify end of playsession (pushed continue button on UI).
        /// </summary>
        /// <param name="_stars">The star.</param>
        /// <param name="_bones">The bones.</param>
        public void EndPlaySession(int _stars, int _bones)
        {
            // Logic
            // log
            // GoToScene ...
        }
        #endregion
    }
}