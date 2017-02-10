﻿using DG.DeExtensions;
using DG.DeInspektor.Attributes;
using EA4S.Core;
using EA4S.UI;
using UnityEngine;
using UnityEngine.UI;

namespace EA4S.Profile
{
    [RequireComponent(typeof(UIButton))]
    public class PlayerIcon : MonoBehaviour
    {
        string uuid;

        UIButton uiButton { get { if (fooUIButton == null) fooUIButton = this.GetComponent<UIButton>(); return fooUIButton; } }
        UIButton fooUIButton;

        #region Public

        public void Init(PlayerIconData playerIconData)
        {
            uuid = playerIconData.Uuid;
            SetAppearance(playerIconData.Gender, playerIconData.AvatarId, playerIconData.Tint, playerIconData.IsDemoUser);
        }

        public void Select(string _uuid)
        {
            uiButton.Toggle(uuid == _uuid);
        }
        #endregion

        void SetAppearance(PlayerGender gender, int avatarId, PlayerTint tint, bool isDemoUser)
        {
            Color color = isDemoUser ? new Color(0.4117647f, 0.9254903f, 1f, 1f) : PlayerTintConverter.ToColor(tint);
            uiButton.ChangeDefaultColors(color, color.SetAlpha(0.5f));
            uiButton.Ico.sprite = isDemoUser
                ? Resources.Load<Sprite>(AppConstants.AvatarsResourcesDir + "god")
                : Resources.Load<Sprite>(AppConstants.AvatarsResourcesDir + gender + avatarId);
        }

        [DeMethodButton("DEBUG: Randomize Appearance", mode = DeButtonMode.PlayModeOnly)]
        void RandomizeAppearance()
        {
            SetAppearance(
                UnityEngine.Random.value <= 0.5f ? PlayerGender.F : PlayerGender.M,
                UnityEngine.Random.Range(1, 5),
                (PlayerTint)UnityEngine.Random.Range(1, 8),
                UnityEngine.Random.value <= 0.2f
            );
        }
    }
}