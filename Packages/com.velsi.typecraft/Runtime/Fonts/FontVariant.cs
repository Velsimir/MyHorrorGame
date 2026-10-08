using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Velsi.Typecraft.Fonts
{
    [Serializable]
    public sealed class FontVariant
    {
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private List<FontMaterialPreset> _materials = new List<FontMaterialPreset>();

        public FontVariant()
        {
        }

        internal FontVariant(TMP_FontAsset font)
        {
            _font = font;
        }

        public TMP_FontAsset Font => _font;
        public IReadOnlyList<FontMaterialPreset> Materials => _materials;

        public Material FindMaterial(string presetId)
        {
            if (string.IsNullOrEmpty(presetId))
                return null;

            foreach (FontMaterialPreset preset in _materials)
            {
                if (preset != null && preset.Id == presetId)
                    return preset.Material;
            }

            return null;
        }
    }

    [Serializable]
    public sealed class FontMaterialPreset
    {
        [SerializeField] private string _id;
        [SerializeField] private Material _material;

        public string Id => _id;
        public Material Material => _material;
    }
}
