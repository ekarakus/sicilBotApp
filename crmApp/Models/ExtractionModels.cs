
    using Newtonsoft.Json;

    // LLM'den gelecek yanıtın eşleneceği sınıf
    public class ExtractionResult
    {
        [JsonProperty("firma")]
        public string CompanyName { get; set; }

        [JsonProperty("adres")]
        public string Address { get; set; }

        [JsonProperty("ilan_metni")]
        public string FullContent { get; set; }
    }

    // LLM'e gönderilecek kriter paketi
    public class GazetteQueryCriteria
    {
        public string CompanyName { get; set; }
        public string RegistrationNumber { get; set; } // Sicil No
        public string RegistryOffice { get; set; }    // Müdürlük
        public string IssueNumber { get; set; }       // Sayı No
        public string PageNumber { get; set; }
        public string PublicationDate { get; set; }
        public string AnnouncementType { get; set; }
        public string RawGazetteText { get; set; }
    }
