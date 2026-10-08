using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Velsi.Typecraft.Library;

namespace Velsi.Typecraft.Markup
{
    public sealed class MarkupContext
    {
        private readonly IconLibrary _icons;

        public MarkupContext(TypecraftSettings settings, IconLibrary icons, string locale, DeviceFamily device, List<MarkupDiagnostic> diagnostics)
        {
            Settings = settings;
            _icons = icons;
            Locale = locale;
            Device = device;
            Diagnostics = diagnostics;
        }

        public TypecraftSettings Settings { get; }
        public string Locale { get; }
        public DeviceFamily Device { get; }
        public List<MarkupDiagnostic> Diagnostics { get; }
        internal int Position { get; set; }

        public void Report(MarkupIssue issue, string value) =>
            Diagnostics?.Add(new MarkupDiagnostic(issue, value, Position));

        public bool TryAppendIcon(string reference, StringBuilder output)
        {
            if (!_icons.TryResolve(reference, out IconSet set, out IconEntry entry))
                return false;

            if (entry.IsPlaceholder || set.SpriteAsset == null)
            {
                Report(MarkupIssue.PlaceholderIcon, reference);
                AppendMissing(reference, output);
                return true;
            }

            AppendIcon(set, entry, output);
            return true;
        }

        public static void AppendIcon(IconSet set, IconEntry entry, StringBuilder output)
        {
            TmpAssetRegistry.Track(set.SpriteAsset);
            output.Append("<sprite=\"").Append(set.SpriteAsset.name).Append("\" name=\"").Append(entry.GlyphName).Append('"');
            if (entry.Tintable)
                output.Append(" tint=1");
            output.Append('>');
        }

        public static void AppendMissing(string reference, StringBuilder output)
        {
            if (Application.isEditor)
                output.Append("<color=#FF5A4E>[").Append(reference).Append("]</color>");
        }
    }
}
