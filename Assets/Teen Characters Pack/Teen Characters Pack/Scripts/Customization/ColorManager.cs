using UnityEngine;
using System;
using UnityEngine.UI;

namespace Clicknext.Customization
{
    public class ColorManager : MonoBehaviour
    {
        [Serializable]
        private struct CharacterToggle
        {
            public Toggle toggle;
            public CharacterColor characterColor;
        }

        [Serializable]
        private struct ColorShade
        {
            public Button button;
            public Color baseColor;
            public Color shade1Color;
            public Color shade2Color;
            public Color highlightColor;
        }

        [SerializeField] CharacterToggle[] characterToggles;
        [SerializeField] ColorShade[] hairBtns;
        [SerializeField] ColorShade[] eyeBtns;
        [SerializeField] ColorShade[] skinBtns;

        private CharacterColor currentCharacter;

        private void Awake()
        {
            foreach (var each in characterToggles)
            {
                each.toggle.onValueChanged.AddListener(ison => 
                {
                    if (ison)
                        currentCharacter = each.characterColor;
                });
            }

            AddListenerComponentColor(hairBtns, colors => currentCharacter.SetHairColor(colors));
            AddListenerComponentColor(eyeBtns, colors => currentCharacter.SetEyeColor(colors));
            AddListenerComponentColor(skinBtns, colors => currentCharacter.SetSkinColor(colors));
        }

        private void AddListenerComponentColor(ColorShade[] btns, Action<Color[]> selected)
        {
            for (int i = 0; i < btns.Length; i++)
            {
                var index = i;
                var shade = btns[index];
                shade.button.onClick.AddListener(() => 
                {
                    if (currentCharacter)
                        selected(new Color[] {
                            shade.baseColor,
                            shade.shade1Color,
                            shade.shade2Color,
                            shade.highlightColor
                        });
                });
            }
        }

    }


}