using System;
using System.Collections.Generic;
using UnityEngine;

namespace Velsi.Typecraft.AutoTags
{
    [Serializable]
    public sealed class AutoTagRule
    {
        [SerializeField] private List<string> _words = new List<string>();
        [SerializeField] private string _template = "[style]$0[/style]";

        public AutoTagRule(IEnumerable<string> words, string template)
        {
            _words = new List<string>(words);
            _template = template;
        }

        public IReadOnlyList<string> Words => _words;
        public string Template => _template;
    }
}
