Amasya Karayolları Uygulaması

Bu proje, Veri Tabanı Yönetimi dersi kapsamında geliştirilmiş bir **C# Windows Forms** ve **MSSQL** uygulamasıdır. Amasya içindeki ulaşım ağını, durakları, araçları ve yol güzergahlarını simüle eder ve analizler sunar.

Projenin amacı:
* Amasya şehrinde bulunan yolların bilgilerini öğrenmek.
* Toplu taşıma araçlarının anlık konum, hız ve hat bilgilerini takip etmek.
* Duraklardaki yoğunlukları analiz etmek.
* SQL sorguları ile performans raporları oluşturmak.
* Proje temsili ve hayali bir senaryoda gerçekleşmiştir!

Kullanılan Teknolojiler
* **Dil:** C# (.NET Framework)
* **Veritabanı:** Microsoft SQL Server (MSSQL)

Projeyi bilgisayarınızda çalıştırmak için şu adımları izleyin:
Veritabanını Kurun:
    * Klasördeki `veritabani.sql` dosyasını açın.
    * Tüm kodları kopyalayıp SQL Server Management Studio'da yeni bir sorgu ekranına yapıştırın ve çalıştırın.
    * Veritabanı ve tablolar otomatik oluşacaktır.
Bağlantı Ayarı:
    Visual Studio'da projeyi açın.
    Kodlardaki `connectionString` kısmını kendi bilgisayarınıza göre güncelleyin.
    (Örn: `Data Source=.;Initial Catalog=Karayollari;Integrated Security=True`)

Özellikler
* Gelişmiş SQL Analizi: Hat bazlı ortalama hız ve yoğunluk raporları.
* Araçların anlık hız ve konum verilerini öğrenme.
* Görsel Takip: Duraklardaki araçların görsel olarak simülasyonu.
* Akıllı Veri Girişi: Trigger ve Stored Procedure mantığına uygun veri yönetimi.

---
👤 **Geliştiriciler:** Kadir Can Yılmaz ve Ömürhan Meşe.
