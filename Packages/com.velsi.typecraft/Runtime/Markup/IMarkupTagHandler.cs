using System.Text;

namespace Velsi.Typecraft.Markup
{
    public interface IMarkupTagHandler
    {
        string Prefix { get; }
        bool TryRender(string argument, MarkupContext context, StringBuilder output);
    }
}
