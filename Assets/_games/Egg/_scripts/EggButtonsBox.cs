﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace EA4S.Egg
{
    public class EggButtonsBox : MonoBehaviour
    {
        public Transform anturaOut;
        public Transform anturaIn;

        public AnimationCurve normalAnimationCurve;
        public AnimationCurve anturaInAnimationCurve;

        public Color[] buttonColors;
        List<Color> availableButtonColors = new List<Color>();

        GameObject eggButtonPrefab;

        List<EggButton> eggButtons = new List<EggButton>();

        float buttonDistance = 20f;

        int buttonCount;

        IAudioManager audioManager;

        Action<ILivingLetterData> buttonsCallback;

        System.Random randomGenerator;

        public void Initialize(GameObject eggButtonPrefab, IAudioManager audioManager, Action<ILivingLetterData> buttonsCallback)
        {
            this.eggButtonPrefab = eggButtonPrefab;
            this.audioManager = audioManager;
            this.buttonsCallback = buttonsCallback;

            randomGenerator = new System.Random((int)Time.realtimeSinceStartup);
        }

        public void AddButton(ILivingLetterData letterData)
        {
            EggButton eggButton = CreateButton();

            eggButton.SetAnswer(letterData);

            eggButtons.Add(eggButton);
        }

        public void RemoveButtons()
        {
            for (int i = 0; i < eggButtons.Count; i++)
            {
                EggButton eggButton = eggButtons[i];

                eggButton.ScaleTo(0f, 0.1f, 0f, delegate () { Destroy(eggButton.gameObject); });
            }

            eggButtons.Clear();
        }

        EggButton CreateButton()
        {
            EggButton eggButton = Instantiate(eggButtonPrefab).GetComponent<EggButton>();
            eggButton.transform.SetParent(transform, false);
            eggButton.gameObject.SetActive(false);
            eggButton.Initialize(audioManager, buttonsCallback);
            eggButton.colorLightUp = GetButtonColor();
            eggButton.DisableInput();
            return eggButton;
        }

        public void ShowButtons()
        {
            for (int i = 0; i < eggButtons.Count; i++)
            {
                eggButtons[i].gameObject.SetActive(true);
            }
        }

        public void HideButtons()
        {
            for (int i = 0; i < eggButtons.Count; i++)
            {
                eggButtons[i].gameObject.SetActive(false);
            }
        }

        public void SetButtonsOnPressedColor()
        {
            for (int i = 0; i < eggButtons.Count; i++)
            {
                eggButtons[i].SetOnPressedColor();
            }
        }

        public void SetButtonsOnStandardColor()
        {
            for (int i = 0; i < eggButtons.Count; i++)
            {
                eggButtons[i].SetOnStandardColor();
            }
        }

        public void SetButtonsOnPosition()
        {
            buttonCount = eggButtons.Count;

            Vector3[] buttonsPosition = CalculateButtonPositions();

            List<int> buttonsIndex = new List<int>();

            for (int i = 0; i < buttonCount; i++)
            {
                buttonsIndex.Add(i);
            }

            for (int i = 0; i < buttonsPosition.Length; i++)
            {
                int index = randomGenerator.Next(0, buttonsIndex.Count);
                int currentIndex = buttonsIndex[index];
                buttonsIndex.RemoveAt(index);

                eggButtons[currentIndex].transform.localPosition = buttonsPosition[i];
                eggButtons[currentIndex].positionIndex = i;
            }
        }

        public void AnturaButtonOut(float duration, float delay, Action endCallback = null)
        {
            List<EggButton> buttons = GetButtons(true);

            float delayBetweenButton = 0.1f;

            for (int i = 0; i < buttons.Count; i++)
            {
                if (i == buttons.Count - 1)
                {
                    buttons[i].MoveTo(anturaOut.localPosition, duration, normalAnimationCurve, (i * delayBetweenButton) + delay, 0f, null, endCallback);
                }
                else
                {
                    buttons[i].MoveTo(anturaOut.localPosition, duration, normalAnimationCurve, (i * delayBetweenButton) + delay);
                }

                buttons[i].ScaleTo(0f, duration, (i * delayBetweenButton) + delay);
            }
        }

        public void AnturaButtonIn(float duration, float delay, float delayBetweenButton = 0.5f, float anturaSpitDelay = 0.3f, Action anturaSpit = null, Action endCallback = null)
        {
            buttonCount = eggButtons.Count;

            for(int i=0; i<eggButtons.Count; i++)
            {
                eggButtons[i].transform.localPosition = anturaIn.localPosition;
            }

            Vector3[] buttonsPosition = CalculateButtonPositions();

            List<int> buttonsIndex = new List<int>();

            for (int i = 0; i < buttonCount; i++)
            {
                buttonsIndex.Add(i);
            }

            for (int i = 0; i < buttonsPosition.Length; i++)
            {
                int index = randomGenerator.Next(0, buttonsIndex.Count);
                int currentIndex = buttonsIndex[index];
                buttonsIndex.RemoveAt(index);

                if (i == buttonsPosition.Length - 1)
                {
                    eggButtons[currentIndex].MoveTo(buttonsPosition[i], duration, anturaInAnimationCurve, (i * delayBetweenButton) + delay, anturaSpitDelay, anturaSpit, endCallback);
                }
                else
                {
                    eggButtons[currentIndex].MoveTo(buttonsPosition[i], duration, anturaInAnimationCurve, (i * delayBetweenButton) + delay, anturaSpitDelay, anturaSpit);
                }

                eggButtons[currentIndex].positionIndex = i;
                eggButtons[currentIndex].ScaleTo(1f, duration, (i * delayBetweenButton) + delay);
                eggButtons[currentIndex].transform.SetAsFirstSibling();
            }
        }

        Vector3[] CalculateButtonPositions()
        {
            Vector3[] buttonsPosition = new Vector3[buttonCount];

            Vector2 eggSizeDelta = ((RectTransform)eggButtons[0].transform).sizeDelta;

            Vector3 currentPosition = Vector3.zero;

            float positionUp = (eggSizeDelta.y + buttonDistance) / 2f;
            float positionDown = -positionUp;

            int upLineLength = 0;
            int downLineLength = 0;

            if (buttonCount <= 4)
            {
                upLineLength = buttonCount;
                downLineLength = 0;
            }
            else
            {
                upLineLength = (buttonCount == 5 || buttonCount == 6) ? 3 : 4;
                downLineLength = (buttonCount % 2) == 0 ? upLineLength : upLineLength - 1;
            }

            int lineIndex = 0;
            bool goDown = false;

            for (int i = 0; i < buttonCount; i++)
            {
                if (buttonCount <= 4)
                {
                    currentPosition.y = 0f;
                }
                else
                {
                    currentPosition.y = goDown ? positionDown : positionUp;
                }

                currentPosition.x = GetHorizontalPositions(eggSizeDelta.x, goDown ? downLineLength : upLineLength)[lineIndex];

                lineIndex++;
                if (lineIndex >= upLineLength)
                {
                    goDown = true;
                    lineIndex = 0;
                }

                buttonsPosition[i] = currentPosition;
            }

            return buttonsPosition;
        }

        float[] GetHorizontalPositions(float size, int number)
        {
            float[] horizontalPositions = new float[number];

            if (number == 1)
            {
                horizontalPositions[0] = 0f;
            }
            else
            {
                float currentHorizontal = (((size + buttonDistance) * (number - 1)) / 2f);

                for (int i = 0; i < number; i++)
                {
                    if (i != 0)
                    {
                        currentHorizontal -= size + buttonDistance;
                    }

                    horizontalPositions[i] = currentHorizontal;
                }
            }

            return horizontalPositions;
        }

        public List<EggButton> GetButtons(bool inPositionOrder)
        {
            if (inPositionOrder)
            {
                List<EggButton> buttons = new List<EggButton>();

                while (buttons.Count < eggButtons.Count)
                {
                    EggButton eB = null;

                    for (int i = 0; i < eggButtons.Count; i++)
                    {
                        if (eggButtons[i].positionIndex == buttons.Count)
                        {
                            eB = eggButtons[i];
                            break;
                        }
                    }

                    buttons.Add(eB);
                }

                return buttons;
            }
            else
            {
                return eggButtons;
            }
        }

        public EggButton GetEggButton(ILivingLetterData letterData)
        {
            for (int i = 0; i < eggButtons.Count; i++)
            {
                if (eggButtons[i].livingLetterData == letterData)
                {
                    return eggButtons[i];
                }
            }

            return null;
        }

        public void EnableButtonsInput()
        {
            for (int i = 0; i < eggButtons.Count; i++)
            {
                eggButtons[i].EnableInput();
            }
        }

        public void DisableButtonsInput()
        {
            for (int i = 0; i < eggButtons.Count; i++)
            {
                eggButtons[i].DisableInput();
            }
        }

        public void PlayButtonsAudio(bool lightUp, bool inPositionOrder, float delay, Action endCallback)
        {
            List<EggButton> buttons = GetButtons(inPositionOrder);

            Action callback = null;

            for (int i = 0; i < buttons.Count; i++)
            {
                if (i == buttons.Count - 1)
                {
                    callback = endCallback;
                }

                delay += buttons[i].PlayButtonAudio(lightUp, delay, callback);
            }
        }

        Color GetButtonColor()
        {
            Color newColor;

            if (availableButtonColors.Count <= 0)
            {
                for (int i = 0; i < buttonColors.Length; i++)
                {
                    availableButtonColors.Add(buttonColors[i]);
                }
            }

            newColor = availableButtonColors[0];

            availableButtonColors.RemoveAt(0);

            return newColor;
        }
    }
}