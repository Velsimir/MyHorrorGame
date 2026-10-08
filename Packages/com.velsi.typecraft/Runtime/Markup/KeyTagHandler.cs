using System.Text;
using Velsi.Typecraft.Input;

namespace Velsi.Typecraft.Markup
{
    public sealed class KeyTagHandler : IMarkupTagHandler
    {
        public string Prefix => "key";

        public bool TryRender(string argument, MarkupContext context, StringBuilder output)
        {
            IInputGlyphResolver resolver = context.Settings != null ? context.Settings.InputGlyphs : null;
            return resolver != null && resolver.TryAppendGlyph(argument, context.Device, output);
        }
    }
}
