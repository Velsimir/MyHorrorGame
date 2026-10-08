using System.Collections.Generic;
using UnityEngine;

namespace Velsi.Typecraft.Fonts
{
    [CreateAssetMenu(fileName = "FontProfile", menuName = "Typecraft/Font Profile")]
    public sealed class FontProfile : ScriptableObject
    {
        [SerializeField] private List<FontRole> _roles = new List<FontRole>();

        public IReadOnlyList<FontRole> Roles => _roles;

        internal void Add(FontRole role) => _roles.Add(role);

        public bool TryGetRole(string roleId, out FontRole role)
        {
            if (!string.IsNullOrEmpty(roleId))
            {
                foreach (FontRole candidate in _roles)
                {
                    if (candidate != null && candidate.Id == roleId)
                    {
                        role = candidate;
                        return true;
                    }
                }
            }

            role = null;
            return false;
        }

        public FontVariant Resolve(string roleId, string locale) =>
            TryGetRole(roleId, out FontRole role) ? role.Resolve(locale) : null;
    }
}
