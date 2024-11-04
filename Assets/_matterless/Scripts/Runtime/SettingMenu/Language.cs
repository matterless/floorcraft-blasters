namespace Matterless.Floorcraft
{
    public enum Language
    {
        en_us = 0,
        th_th = 1,
        hr_hr = 2,
        fr_fr = 3,
        de_de = 4,
        es_es = 5
    }

    public static class LanguageExtensions
    {
        public static string GetLocalName(this Language language)
        {
            switch (language)
            {
                case Language.en_us:
                    return "English";
                 case Language.th_th:
                    return "ภาษาไทย";
                 case Language.hr_hr:
                    return "Hrvatska";
                 case Language.fr_fr:
                    return "français";
                 case Language.de_de:
                    return "Deutsch";
                 case Language.es_es:
                    return "español";
                
                default: return string.Empty;
            }
        }
    }
}