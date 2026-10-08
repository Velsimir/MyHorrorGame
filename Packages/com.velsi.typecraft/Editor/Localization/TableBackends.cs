using System;
using System.Collections.Generic;
using Velsi.Typecraft.Text;
using Velsi.Typecraft.Text.Tables;
#if TYPECRAFT_LOCALIZATION
using Velsi.Typecraft.Localization;
#endif

namespace Velsi.Typecraft.Editor.Localization
{
    public static class TableBackends
    {
        private static readonly List<Func<ITextSource, ITableBackend>> Factories = new List<Func<ITextSource, ITableBackend>>();

        public static void Register(Func<ITextSource, ITableBackend> factory)
        {
            if (factory != null && !Factories.Contains(factory))
                Factories.Add(factory);
        }

        public static ITableBackend Current
        {
            get
            {
                ITextSource source = Typecraft.Settings != null ? Typecraft.Settings.TextSource : null;
                if (source == null)
                    return null;

                foreach (Func<ITextSource, ITableBackend> factory in Factories)
                {
                    ITableBackend custom = factory(source);
                    if (custom != null)
                        return custom;
                }

                if (source is TableTextSource tables)
                    return new BuiltInTableBackend(tables);
#if TYPECRAFT_LOCALIZATION
                if (source is UnityLocalizationTextSource)
                    return new UnityLocalizationBackend();
#endif
                return null;
            }
        }
    }
}
