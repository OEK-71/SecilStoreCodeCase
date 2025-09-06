# SecilStore Code Case – Config Library (.NET 8)

Bu proje, servislerin konfigürasyonlarını merkezi bir storage’dan okuyup **periyodik olarak yenileyen** ve storage erişilemezse **son başarılı snapshot** ile çalışmaya devam eden bir kütüphane ve basit bir yönetim UI’si içerir.

## Özellikler (Case Koşulları)
1. **.NET 8 kütüphane** – `SecilStoreCodeCase` projesi .NET 8.
2. **Fail-open** – Storage down iken `ConfigurationReader` son başarılı snapshot ile çalışır.
3. **Tip dönüşümleri kütüphane içinde** – `GetValue<T>()` (`string/int/bool/double`) parse işlemleri içeride çözülür.
4. **Periyodik yenileme** – Arka planda 5 sn’de bir delta sorgulanır ve cache güncellenir.
5. **İzolasyon** – Her servis yalnızca kendi konfiglerini görür (uygulama-kod seviyesinde) + (opsiyonel) **Row-Level Security** ile DB seviyesinde garantilenir.

---

## Yapı
- **SecilStoreCodeCase** (class library)
  - `ConfigurationReader` – arka plan refresh loop + memory snapshot + type-safe `GetValue<T>()`
  - `IConfigStore` – storage arayüzü
  - `SqlConfigStore` – Dapper ile SQL Server implementasyonu (RLS için session context set eder)
  - `ConfigurationItem`, `ConfigItemType`
- **SecilStoreConfigWeb** (ASP.NET Core MVC)
  - Basit yönetim UI’si (App seç, listele, upsert, deactivate/activate)
  - Üst banner’da `ConfigurationReader` snapshot’ını canlı (AJAX) gösterir

---

## Ön Koşullar
- .NET 8 SDK
- SQL Server (LocalDB / Express / Standart fark etmez)
- PowerShell veya SSMS ile SQL scriptlerini çalıştırabilme

---

## Kurulum – Veritabanı
`sql/` klasöründeki scriptleri sırayla çalıştırın:

```bash
1) 01_schema.sql
2) 02_indexes.sql
3) 03_rls_optional.sql   # İsteğe bağlı (prod için önerilir)
4) 04_seed.sql




Test – Case Koşulları
1) .NET 8

Projeler .NET 8 hedefler.

2) Fail-Open (storage down iken son snapshot)

UI üst banner’da snapshot görünsün (ör. MaxItemCount=50).

SQL Server servisini durdurun (veya connection string’i bozun).

Banner aynı değeri göstermeye devam eder (LastRefreshUtc artmaz).

UI’de yazma işlemleri (upsert/deactivate/activate) hata verebilir; bu beklenen davranış.

3) Tip dönüşümları içeride

Controller’da:

var s = _reader.GetValue<string>("SiteName");
var i = _reader.GetValue<int>("MaxItemCount");
var b = _reader.GetValue<bool>("IsFeatureXOpen");


Değerler doğru parse edilir (1/0/true/false, trim, invariant kültür).

4) Periyodik yenileme

DB’de MaxItemCount’ı 50 → 99 yapın.

~5 sn sonra sayfayı yenilemeden banner’daki snapshot otomatik 99 olur (AJAX polling).

Tekrar 99 → 50 yapın → ~5 sn sonra 50’ye döner.

5) İzolasyon

Kod seviyesi: SqlConfigStore her connection’da SESSION_CONTEXT('AppName') set eder; tüm sorgular app-bazlı çalışır.

DB seviyesi (opsiyonel RLS): RLS policy ile yanlış yazılmış sorgular bile diğer app’leri göremez.

SSMS’te:

EXEC sys.sp_set_session_context @key=N'AppName', @value=N'SERVICE-A';
SELECT * FROM dbo.Configurations;  -- sadece A gelir



 RLS nedeniyle "ALL" görünümü kısıtlı
AdminDB tamamen opsiyonel bırakıldı. 
AdminConfigDb connection string’i ekleyip ayrı bir AdminConfigStore’da Create() ile bağlanın (session context set etmeyin), sadece UI’nin GetAll/GetApplications çağrılarını oradan yapın.
