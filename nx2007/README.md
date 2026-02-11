# Siemens NX 2007 - WCS Hizalama Kodu

Bu NX Open C# kodu şunları yapar:

1. Kullanıcıdan **yüzey (face)** veya **datum düzlemi** seçmesini ister.
2. Seçimi assembly içinde de destekler (`AnyInAssembly`).
3. WCS orijinini seçilen yüzey/düzlemin merkezine konumlandırır.
   - Face için UV orta noktası,
   - Datum plane için plane origin kullanılır.
4. **Z eksenini** seçilen geometriye dik yapar.
5. Face seçiminde **X eksenini** en uzun U/V yönüne paralel ayarlar.
6. Arayüzde (modern, sade görünüm):
   - **Sadece Z ters çevir** vardır,
   - **X/Y eksenleri Z etrafında ±90° döndürülebilir**,
   - Ayrıca kutuya değer girerek (ör. `18.35`) **istenen açıda döndürme** yapılabilir.

## Dosya

- `WcsAlignToFaceOrPlane.cs`

## Derleme notu

- NX 2007 ile gelen .NET assembly referansları gereklidir: `NXOpen.dll`, `NXOpen.UF.dll`.
- DLL olarak derleyip NX Journal/Customer Defaults üzerinden çalıştırın.
