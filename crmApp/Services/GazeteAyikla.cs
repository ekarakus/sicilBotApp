using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
  public class AnnouncementSearchCriteria
{
    public string SicilNo { get; set; }        // 193621-0
    public string TicaretUnvani { get; set; }  // VESTEL ELEKTRONIK...
    public string SicilMudurlugu { get; set; } // İSTANBUL
    public string SayfaNo { get; set; }        // 290
    public string SayiNo { get; set; }         // 11501
    // İhtiyaç duyulursa yayın tarihi de eklenebilir
}
public class GazetteExtractionService
{
    public string ExtractSpecificAnnouncement(string fullText, AnnouncementSearchCriteria criteria)
    {
        if (string.IsNullOrEmpty(fullText)) return null;

        // 1. ADIM: Gazeteyi "Müdürlük" veya "Mahkeme" başlıklarına göre ilan bloklarına bölüyoruz.
        // Bu yapı her ilanın en tepesindeki standart girişi yakalar.
        string splitPattern = @"(?i)(?=T\.C\.\s+.*?(?:MÜDÜRLÜĞÜ|MAHKEMESİ|MEMURLUĞUNDAN))";
        var blocks = Regex.Split(fullText, splitPattern);

        // Sicil No için esnek Regex (193621-0, 193621 - 0, 193621/0 gibi)
        string cleanSicilRoot = criteria.SicilNo.Split('-', '/')[0];
        string sicilRegex = $@"\b{cleanSicilRoot}([\s\-/]*\d+)?\b";

        foreach (var block in blocks)
        {
            if (string.IsNullOrWhiteSpace(block)) continue;

            // 2. ADIM: Sicil No ve Müdürlük kontrolü (En güçlü eşleştirme)
            bool isCorrectSicil = Regex.IsMatch(block, sicilRegex);
            bool isCorrectCity = block.ToUpper().Contains(criteria.SicilMudurlugu.ToUpper());

            if (isCorrectSicil && isCorrectCity)
            {
                // 3. ADIM: (Opsiyonel ama önerilir) Sayfa No doğrulaması
                // Metin içinde "SAYFA : 290" gibi bir ibare geçiyor mu kontrol edelim.
                // Not: OCR bazen sayfa nosunu ilanın uzağında bırakabilir, o yüzden bunu 'yumuşak' bir kontrol yapabiliriz.

                return CleanAnnouncement(block);
            }
        }

        return "Eşleşen kayıt bulunamadı.";
    }

    private string CleanAnnouncement(string text)
    {
        // İlan sonundaki gereksiz referans numaralarını temizler
        string cleaned = Regex.Replace(text, @"\(\d{5,}\)", "");
        cleaned = Regex.Replace(cleaned, @"\(\d+/\w\)\(\d+/\d+\)", "");

        // Sayfa başlıklarını temizler (SAYFA : 290 gibi)
        cleaned = Regex.Replace(cleaned, @"(?i)SAYFA\s*:\s*\d+", "");

        return cleaned.Trim();
    }
}