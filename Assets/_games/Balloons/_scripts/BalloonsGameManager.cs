﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using ModularFramework.Core;
using ModularFramework.Helpers;
using EA4S;
using TMPro;

namespace Balloons
{
    public class BalloonsGameManager: MiniGameBase
    {
        [Header("References")]
        public WordPromptController wordPrompt;
        public GameObject floatingLetterPrefab;
        public Transform[] floatingLetterLocations;
        public AnimationClip balloonPopAnimation;
        public GameObject runningAntura;
        public Canvas uiCanvas;
        public Canvas endGameCanvas;
        public Sprite TutorialImage;
        public TextMeshProUGUI roundNumberText;
        public TimerManager timer;
        public Animator countdownAnimator;
        public StarFlowers starFlowers;
        public GameObject FxParticlesPoof;

        [Header("Stage")]
        public float minX;
        public float maxX;
        public float minY;
        public float maxY;

        [Header("Game Parameters")] [Tooltip("e.g.: 6")]
        public int numberOfRounds;
        public int lives;
        [Range(10, 300)] [Tooltip("e.g.: 30.9")]
        public float roundTime;
        public Color[] balloonColors;

        [HideInInspector]
        public List<FloatingLetterController> floatingLetters;
        [HideInInspector]
        public float letterDropDelay;
        [HideInInspector]
        public float letterAnimationLength = 0.367f;

        public static BalloonsGameManager instance;

        private WordData wordData;
        private string word;
        private List<LetterData> wordLetters;
        private int currentRound = 0;
        private int remainingLives;
        private int correctWords = 0;
        private int _tutorialState;

        private int TutorialState
        {
            get { return _tutorialState; }
            set {
                _tutorialState = value;
                OnTutorialStateChanged();
            }
        }

        private enum Result {
            PERFECT,
            GOOD,
            CLEAR,
            FAIL
        }

        protected override void Awake()
        {
            base.Awake();
            instance = this;
        }

        protected override void Start()
        {
            base.Start();

            Random.seed = System.DateTime.Now.GetHashCode();
            remainingLives = lives;
            letterDropDelay = balloonPopAnimation.length;
            AppManager.Instance.InitDataAI();
            AppManager.Instance.CurrentGameManagerGO = gameObject;

            ResetScene();
            SceneTransitioner.Close();
            //ShowTutorial();
            Play();
        }

        private void ShowTutorial()
        {
            WidgetPopupWindow.I.ShowTutorial(TutorialNextButtonAction, TutorialImage);
            TutorialState = 3;
        }

        public void TutorialNextButtonAction()
        {
            TutorialState--;
        }

        private void OnTutorialStateChanged()
        {
            switch (TutorialState) {
                case 3:
                    WidgetSubtitles.I.DisplaySentence("game_balloons_intro1");
                    break;
                case 2:
                    WidgetSubtitles.I.DisplaySentence("game_balloons_intro2");
                    break;
                case 1:
                    WidgetSubtitles.I.DisplaySentence("game_balloons_intro3");
                    break;
                default:
                    WidgetSubtitles.I.Close();
                    WidgetPopupWindow.Close();
                    Play();
                    break;
            }
        }

        public void OnRoundStartPressed()
        {
            WidgetPopupWindow.Close();
            BeginGameplay();
        }

        public void OnRoundResultPressed()
        {
            AudioManager.I.PlaySfx(Sfx.UIButtonClick);
            WidgetPopupWindow.Close();
            Play();
        }

        public void Play()
        {
            currentRound++;
            if (currentRound <= numberOfRounds) {
                StartNewRound();
            } else {
                EndGame();
            }
        }

        public void StartNewRound()
        {
            ResetScene();
            SetNewWord();
            StartCoroutine(StartNewRound_Coroutine());
        }

        private IEnumerator StartNewRound_Coroutine()
        {
            float delay = 0.75f;
            yield return new WaitForSeconds(delay);

            AudioManager.I.PlayWord(wordData.Key);
            WidgetPopupWindow.I.ShowSentenceAndWord(OnRoundStartPressed, "game_balloons_intro2", wordData);

            uiCanvas.gameObject.SetActive(true);
        }

        private void EndRound(Result result)
        {
            AudioManager.I.PlayMusic(Music.Relax);
            DisableFloatingLetters();
            timer.StopTimer();
            ProcessRoundResult(result);

            LoggerEA4S.Log("minigame", "Balloons", "wordFinished", wordData.Key);
            LoggerEA4S.Save();
        }

        private void EndGame()
        {
            ResetScene();

            uiCanvas.gameObject.SetActive(false);
            endGameCanvas.gameObject.SetActive(true);

            int numberOfStars = 0;

            if (correctWords <= 0) {
                numberOfStars = 0;
                WidgetSubtitles.I.DisplaySentence("game_result_retry");
            } else if ((float)correctWords / numberOfRounds < 0.5f) {
                numberOfStars = 1;
                WidgetSubtitles.I.DisplaySentence("game_result_fair");
            } else if (correctWords < numberOfRounds) {
                numberOfStars = 2;
                WidgetSubtitles.I.DisplaySentence("game_result_good");
            } else {
                numberOfStars = 3;
                WidgetSubtitles.I.DisplaySentence("game_result_great");
            }
                
            LoggerEA4S.Log("minigame", "Balloons", "completedWords", correctWords.ToString());
            LoggerEA4S.Log("minigame", "Balloons", "endScoreStars", numberOfStars.ToString());
            LoggerEA4S.Save();

            starFlowers.Show(numberOfStars);
        }

        private void ResetScene()
        {
            timer.StopTimer();
            timer.ResetTimer();
            timer.DisplayTime();
            roundNumberText.text = "#" + currentRound.ToString();
            wordPrompt.Reset();
            uiCanvas.gameObject.SetActive(false);
            DestroyAllBalloons();
        }

        private void BeginGameplay()
        {
            timer.DisplayTime();
            CreateBalloons(currentRound);
            runningAntura.SetActive(true);
            timer.StartTimer();
            AudioManager.I.PlayMusic(Music.MainTheme);

            LoggerEA4S.Log("minigame", "Balloons", "start", timer.time.ToString());
            LoggerEA4S.Save();
        }

        private void AnimateCountdown(string text)
        {
            countdownAnimator.gameObject.GetComponent<TextMeshProUGUI>().text = text;
            countdownAnimator.SetTrigger("Count");
        }

        private void SetNewWord()
        {
            wordData = AppManager.Instance.Teacher.GimmeAGoodWordData();
            word = wordData.Word;
            wordLetters = ArabicAlphabetHelper.LetterDataListFromWord(word, AppManager.Instance.Letters);
            wordPrompt.DisplayWord(wordLetters);

            LoggerEA4S.Log("minigame", "Balloons", "newWord", wordData.Key);
            LoggerEA4S.Save();
            Debug.Log(word + " Length: " + word.Length);
        }

        private void CreateBalloons(int numberOfExtraLetters)
        {
            var numberOfLetters = Mathf.Clamp(wordLetters.Count + numberOfExtraLetters, 0, floatingLetterLocations.Length);

            // Create Floating Letters
            for (int i = 0; i < numberOfLetters; i++) {
                var instance = Instantiate(floatingLetterPrefab);
                instance.transform.SetParent(floatingLetterLocations[i]);
                instance.transform.localPosition = Vector3.zero;

                var floatingLetter = instance.GetComponent<FloatingLetterController>();

                floatingLetter.SetActiveVariation(Random.Range(0, floatingLetter.variations.Length));

                var balloons = floatingLetter.Balloons;
                var letter = floatingLetter.Letter;

                // Set random balloon colors without repetition if possible
                var usedColorIndexes = new List<int>();
                for (int j = 0; j < balloons.Length; j++) {
                    int randomColorIndex; 

                    if (balloons.Length <= balloonColors.Length) {
                        do {
                            randomColorIndex = Random.Range(0, balloonColors.Length);
                        } while(usedColorIndexes.Contains(randomColorIndex));
                    } else {
                        randomColorIndex = Random.Range(0, balloonColors.Length);
                    }

                    usedColorIndexes.Add(randomColorIndex);
                    balloons[j].SetColor(balloonColors[randomColorIndex]);
                }

                // Get a random letter that is not a required letter
                LetterData randomLetter;
                do {
                    randomLetter = AppManager.Instance.Letters.GetRandomElement();
                } while (wordLetters.Contains(randomLetter));
                letter.Init(randomLetter);

                floatingLetters.Add(floatingLetter);
            }

            // Assign required letters
            List<int> requiredLetterIndices = new List<int>();
            for (int i = 0; i < wordLetters.Count; i++) {
                var index = Random.Range(0, floatingLetters.Count);

                if (!requiredLetterIndices.Contains(index)) {
                    requiredLetterIndices.Add(index);
                    var letter = floatingLetters[index].GetComponent<FloatingLetterController>().Letter;
                    letter.associatedPromptIndex = i;
                    letter.Init(wordLetters[i]);
                    letter.isRequired = true;
                } else {
                    i--;
                }
            }
        }

        public void OnDropped(LetterController letter = null)
        {
            bool isRequired = false;
            int promptIndex = -1;
            string letterKey = "";

            if (letter != null) {
                isRequired = letter.isRequired;
                promptIndex = letter.associatedPromptIndex;
                if (letter.LetterModel != null && letter.LetterModel.Data != null && !string.IsNullOrEmpty(letter.LetterModel.Data.Key)) {
                    letterKey = letter.LetterModel.Data.Key;
                }
            }

            if (isRequired) {
                LoggerEA4S.Log("minigame", "Balloons", "goodLetterExplode", letterKey);
                OnDroppedRequired(promptIndex);
            } else {
                LoggerEA4S.Log("minigame", "Balloons", "badLetterExplode", letterKey);
            }

            CheckRemainingBalloons();
        }

        public void OnDroppedRequired(int promptIndex)
        {
            remainingLives--;
            wordPrompt.letterPrompts[promptIndex].State = LetterPromptController.PromptState.WRONG;
            AudioManager.I.PlaySfx(Sfx.LetterSad);

            if (remainingLives <= 0) {
                EndRound(Result.FAIL);
            }
        }

        private void CheckRemainingBalloons()
        {
            int idlePromptsCount = wordPrompt.IdleLetterPrompts.Count;
            bool randomBalloonsExist = floatingLetters.Exists(balloon => balloon.Letter.isRequired == false);
            bool requiredBalloonsExist = floatingLetters.Exists(balloon => balloon.Letter.isRequired == true);

            if (!requiredBalloonsExist) {
                EndRound(Result.FAIL);
            } else if (!randomBalloonsExist) {
                Result result;
                if (idlePromptsCount == wordLetters.Count) {
                    result = Result.PERFECT;
                } else if (idlePromptsCount >= 2) {
                    result = Result.GOOD;
                } else {
                    result = Result.CLEAR;
                }
                EndRound(result);
            }
        }

        private void DisableFloatingLetters()
        {
            for (int i = 0; i < floatingLetters.Count; i++) {
                floatingLetters[i].Disable();
            }
        }

        private void DestroyAllBalloons()
        {
            for (int i = 0; i < floatingLetters.Count; i++) {
                Destroy(floatingLetters[i].gameObject);
            }
            floatingLetters.Clear();
        }

        private void DestroyUnrequiredBalloons()
        {
            for (int i = 0; i < floatingLetters.Count; i++) {
                if (!floatingLetters[i].Letter.isRequired) {
                    Destroy(floatingLetters[i]);
                }
            }
        }

        public void OnTimeUp()
        {
            bool randomBalloonsExist = floatingLetters.Exists(balloon => balloon.Letter.isRequired == false);

            if (randomBalloonsExist) {
                EndRound(Result.FAIL);
            } else {
                OnDropped();
            }
        }

        private void ProcessRoundResult(Result result)
        {
            bool win = false;

            switch (result) {
                case Result.PERFECT:
                    correctWords++;
                    win = true;
                    AudioManager.I.PlaySfx(Sfx.Win);
                    break;
                case Result.GOOD:
                    correctWords++;
                    win = true;
                    AudioManager.I.PlaySfx(Sfx.Win);
                    break;
                case Result.CLEAR:
                    correctWords++;
                    win = true;
                    AudioManager.I.PlaySfx(Sfx.Win);
                    break;
                case Result.FAIL:
                    win = false;
                    AudioManager.I.PlaySfx(Sfx.Lose);
                    break;
                default:
                    break;
            }

            DisplayRoundResult(win);
        }

        private void DisplayRoundResult(bool win)
        {
            StartCoroutine(DisplayRoundResult_Coroutine(win));
        }

        private IEnumerator DisplayRoundResult_Coroutine(bool win)
        {
            var initialDelay = 0.25f;
            yield return new WaitForSeconds(initialDelay);


            if (win) {
                AudioManager.I.PlayDialog("comment_welldone");
                var popUpDelay = 0.25f;
                yield return new WaitForSeconds(popUpDelay);

                WidgetPopupWindow.I.ShowSentenceAndWord(OnRoundResultPressed, "comment_welldone", wordData);
                var speakWordDelay = 0.75f;
                yield return new WaitForSeconds(speakWordDelay);

                AudioManager.I.PlayWord(wordData.Key);

            } else {
                var failDelay = 0.75f;
                yield return new WaitForSeconds(failDelay);

                var sentenceOptions = new[]{ "game_balloons_commentA", "game_balloons_commentB" };
                var sentence = sentenceOptions[Random.Range(0, sentenceOptions.Length)];
                WidgetPopupWindow.I.ShowSentenceWithMark(OnRoundResultPressed, sentence, false);
            }
        }
    }
}
