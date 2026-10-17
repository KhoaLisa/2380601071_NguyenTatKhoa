using Abp.Configuration.Startup;
using Abp.Localization.Dictionaries;
using Abp.Localization.Dictionaries.Xml;
using Abp.Reflection.Extensions;

namespace NguyenTatKhoa_2380601071.Localization;

public static class NguyenTatKhoa_2380601071LocalizationConfigurer
{
    public static void Configure(ILocalizationConfiguration localizationConfiguration)
    {
        localizationConfiguration.Sources.Add(
            new DictionaryBasedLocalizationSource(NguyenTatKhoa_2380601071Consts.LocalizationSourceName,
                new XmlEmbeddedFileLocalizationDictionaryProvider(
                    typeof(NguyenTatKhoa_2380601071LocalizationConfigurer).GetAssembly(),
                    "NguyenTatKhoa_2380601071.Localization.SourceFiles"
                )
            )
        );
    }
}
