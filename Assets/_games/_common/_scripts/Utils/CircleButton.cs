﻿using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EA4S
{
    public class CircleButton : MonoBehaviour
    {
        public UnityEngine.UI.Image button;
        public UnityEngine.UI.Image image;
        public TMPro.TextMeshProUGUI text;

        public System.Action<CircleButton> onClicked;

        bool isDestroying;
        float destroyTimer;
        System.Action onAnimationCompleted;

        public IAudioManager audioManager;

        ILivingLetterData answer;
        public ILivingLetterData Answer
        {
            get
            {
                return answer;
            }
            set
            {
                if (isDestroying)
                    return;

                answer = value;
                text.text = value.TextForLivingLetter;
                image.sprite = value.DrawForLivingLetter;
                text.gameObject.SetActive(!ImageMode || image.sprite == null);
                image.gameObject.SetActive(ImageMode && image.sprite != null);
            }

        }

        bool imageMode;
        public bool ImageMode
        {
            get
            {
                return imageMode;
            }
            set
            {
                if (isDestroying)
                    return;

                imageMode = value;
                text.gameObject.SetActive(!value || image.sprite == null);
                image.gameObject.SetActive(value && image.sprite != null);
            }

        }

        public void SetColor(Color color)
        {
            button.color = color;
        }

        public void Destroy(float delay = 0, System.Action onAnimationCompleted = null)
        {
            destroyTimer = delay;
            isDestroying = true;
            this.onAnimationCompleted = onAnimationCompleted;
        }

        public void OnClicked()
        {
            if (isDestroying)
                return;

            if (onClicked != null)
                onClicked(this);
        }

        Tween enterScaleTweener;
        Tween exitScaleTweener;

        public void DoEnterAnimation(float delay)
        {
            if (enterScaleTweener != null)
            {
                enterScaleTweener.Kill();
            }

            var oldScale = transform.localScale;
            transform.localScale = Vector3.one * 0.001f;
            enterScaleTweener = DOTween.Sequence().Append(
                transform.DOScale(oldScale, 0.2f).SetDelay(delay)
                ).Append(
                    transform.DOPunchRotation(Vector3.forward * 20, 0.3f, 10, 1)
                );
        }


        void ScaleTo(float scale, float duration, Action endCallback = null)
        {
            var endScaleCallback = endCallback;

            if (exitScaleTweener != null)
            {
                exitScaleTweener.Kill();
            }

            exitScaleTweener = transform.DOScale(scale, duration).OnComplete(delegate () {
                if (endScaleCallback != null)
                    endScaleCallback();
            });
        }


        void Disappear()
        {
            ScaleTo(0.01f, 0.1f, () =>
            {
                if (onAnimationCompleted != null)
                    onAnimationCompleted();

                Destroy(gameObject);
            });
        }

        void Update()
        {
            if (isDestroying)
            {
                if (destroyTimer >= 0)
                { 
                    destroyTimer -= Time.deltaTime;

                    if (destroyTimer < 0)
                    {
                        Disappear();
                    }
                }
            }
        }
    }
}