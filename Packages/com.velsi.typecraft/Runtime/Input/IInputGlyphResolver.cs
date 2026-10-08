using System.Collections.Generic;
using System.Text;
using Velsi.Typecraft.Library;

namespace Velsi.Typecraft.Input
{
    public interface IInputGlyphResolver
    {
        IEnumerable<IconSet> IconSets { get; }
        void Activate();
        bool TryAppendGlyph(string actionName, DeviceFamily device, StringBuilder output);
    }
}
