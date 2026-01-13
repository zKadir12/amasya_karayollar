using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Button = System.Windows.Forms.Button;
using Label = System.Windows.Forms.Label;

namespace amasya_karayollar
{
    public partial class Form1 : Form
    {
        int node = 0;
        int baslangicNode = -1;
        int bitisNode = -1;
        int zurna = 0;
        bool zaman = false;

        Dictionary<int, List<Komsuluk>> haritaGraph = new Dictionary<int, List<Komsuluk>>();
        Dictionary<string, int> yolAdiToNodeID = new Dictionary<string, int>();
        Dictionary<int, int> nodeToLabelMap = new Dictionary<int, int>();

        List<Point> cizilecekNoktalar = new List<Point>();
        public Form1()
        {
            InitializeComponent();
        }
        private void AraclariListele()
        {
            bekleyen_konum.Text = "Bekleyen Araç Konum:";

            string adres = "Data Source=.\\SQLEXPRESS;Initial Catalog=Karayollari;Integrated Security=True";

            using (SqlConnection baglanti = new SqlConnection(adres))
            {
                try
                {
                    baglanti.Open();

                    string sql = @"
                    SELECT 
                        A.plaka AS 'Plaka', 
                        A.hat_no AS 'Hat', 
                        A.anlik_hiz AS 'Hız', 
                        ISNULL(
                            (SELECT TOP 1 Y.yol_adi 
                             FROM Dügümler D 
                             INNER JOIN Yollar Y ON D.yol_id = Y.id 
                             WHERE D.node_ilk = A.konum OR D.node_son = A.konum 
                             ORDER BY D.yol_id ASC), 
                             'Bilinmiyor'
                        ) AS 'Konum'
                    FROM Araçlar A";

                    SqlDataAdapter da = new SqlDataAdapter(sql, baglanti);
                    DataTable tablo = new DataTable();

                    da.Fill(tablo);
                    dataGridView1.DataSource = tablo;

                    SqlCommand bekleyen = new SqlCommand("select hat_no from [Araçlar] inner join Bekleyenler on Bekleyenler.bekleyen_arac_id = Araçlar.arac_id;", baglanti);

                    SqlDataReader okuyucu = bekleyen.ExecuteReader();
                    List<string> bekleyenHatlar = new List<string>();

                    while (okuyucu.Read())
                    {
                        bekleyenHatlar.Add(okuyucu["hat_no"].ToString());
                    }
                    okuyucu.Close();

                    if (bekleyenHatlar.Count > 0)
                    {
                        Bekleme_label.Text = "Bekleyen Hatlar: " + string.Join(", ", bekleyenHatlar);
                    }
                    else
                    {
                        Bekleme_label.Text = "Bekleyen Araç Yok";
                    }

                    string duraktakiler = @"SELECT 
    (SELECT TOP 1 Y.yol_adi 
     FROM Dügümler DG 
     INNER JOIN Yollar Y ON DG.yol_id = Y.id 
     WHERE DG.node_ilk = D.durak_konum OR DG.node_son = D.durak_konum
     ORDER BY Y.id ASC) AS yol_adi,
    A.hat_no
FROM Bekleyenler B
INNER JOIN Araçlar A ON B.bekleyen_arac_id = A.arac_id
INNER JOIN Duraklar D ON B.durak_id = D.durak_id";
                    SqlCommand bekleyenler = new SqlCommand(duraktakiler, baglanti);
                    SqlDataReader oku = bekleyenler.ExecuteReader();

                    string toplananVeri = "Bekleyen Araç Konum: " + "\n";
                    while (oku.Read())
                    {
                        string yol = oku["yol_adi"].ToString();
                        string hat = oku["hat_no"].ToString();

                        toplananVeri += yol + " (Hat: " + hat + ")" + "\n";
                    }
                    oku.Close();
                    bekleyen_konum.Text = toplananVeri.TrimEnd('\n');
                }
                catch (Exception hata)
                {
                    MessageBox.Show("Listeleme Hatası: " + hata.Message);
                }
            }
        }
        private void NodeSecimIslemi()
        {
            string turna = "label" + zurna;
            Control[] bulunanlar = this.Controls.Find(turna, true);

            if (baslangicNode == -1)
            {
                zaman = true;

                cizilecekNoktalar.Clear();
                panel2.Invalidate();
                pictureBox1.Invalidate();
                pictureBox2.Invalidate();
                pictureBox3.Invalidate();

                for (int i = 7; i <= 39; i++)
                {
                    string nokta = "label" + i;

                    Control[] noktalar = this.Controls.Find(nokta, true);

                    if (noktalar.Length > 0)
                    {
                        noktalar[0].Visible = false;
                    }
                }
                baslangicNode = node;
                label6.Text = "Başlangıç noktası seçildi." + Environment.NewLine + "Lütfen Varış noktasını seçiniz...";
            }
            else
            {
                bitisNode = node;

                DijkstraHesapla(baslangicNode, bitisNode);

                baslangicNode = -1;
                bitisNode = -1;
            }

            if (bulunanlar.Length > 0)
            {
                bulunanlar[0].Visible = true;
            }
        }
        // ---------------------------------------- DİJKSTRA ----------------------------------------------------------
        private void DijkstraHesapla(int baslangic, int bitis)
        {
            var mesafeler = new Dictionary<int, int>();
            var oncekiNode = new Dictionary<int, int>();
            var oncekiYolAdi = new Dictionary<int, string>();
            var ziyaretEdilmemis = new List<int>();

            foreach (var n in haritaGraph.Keys)
            {
                mesafeler[n] = int.MaxValue;
                ziyaretEdilmemis.Add(n);
            }
            mesafeler[baslangic] = 0;

            while (ziyaretEdilmemis.Count > 0)
            {
                int aktifNode = -1;
                int enKucuk = int.MaxValue;
                foreach (int n in ziyaretEdilmemis)
                {
                    if (mesafeler[n] < enKucuk) { enKucuk = mesafeler[n]; aktifNode = n; }
                }

                if (aktifNode == -1 || aktifNode == bitis) break;
                ziyaretEdilmemis.Remove(aktifNode);

                if (haritaGraph.ContainsKey(aktifNode))
                {
                    foreach (var komsuluk in haritaGraph[aktifNode])
                    {
                        int yeniMesafe = mesafeler[aktifNode] + komsuluk.Mesafe;
                        if (yeniMesafe < mesafeler[komsuluk.HedefNode])
                        {
                            mesafeler[komsuluk.HedefNode] = yeniMesafe;
                            oncekiNode[komsuluk.HedefNode] = aktifNode;
                            oncekiYolAdi[komsuluk.HedefNode] = komsuluk.YolAdi;
                        }
                    }
                }
            }

            if (mesafeler[bitis] == int.MaxValue)
            {
                label6.Text = "Bu iki nokta arasında ulaşım yolu bulunamadı.";
                return;
            }

            List<string> rota = new List<string>();
            List<int> rotaNodeIDleri = new List<int>();

            int temp = bitis;
            while (temp != baslangic)
            {
                rota.Add(oncekiYolAdi[temp]);
                rotaNodeIDleri.Add(temp);
                temp = oncekiNode[temp];
            }
            rotaNodeIDleri.Add(baslangic);
            rotaNodeIDleri.Reverse();
            rota.Reverse();

            List<string> temizRota = new List<string>();

            if (rota.Count > 0)
            {
                temizRota.Add(rota[0]);

                for (int i = 1; i < rota.Count; i++)
                {
                    if (rota[i] != rota[i - 1])
                    {
                        temizRota.Add(rota[i]);
                    }
                }
            }

            string sonucMetni = "";
            sonucMetni += "--------------------------------" + Environment.NewLine;

            sonucMetni += string.Join(Environment.NewLine, temizRota);

            label6.Text = sonucMetni;

            int scrollX = panel2.HorizontalScroll.Value;
            int scrollY = panel2.VerticalScroll.Value;

            foreach (int nodeID in rotaNodeIDleri)
            {
                if (nodeToLabelMap.ContainsKey(nodeID))
                {
                    int labelNo = nodeToLabelMap[nodeID];
                    string labelAdi = "label" + labelNo;

                    Control[] bulunan = this.Controls.Find(labelAdi, true);

                    if (bulunan.Length > 0)
                    {
                        Control lbl = bulunan[0];
                        Point nokta = new Point(0, 0);

                        if (lbl.Parent is PictureBox)
                        {
                            int gercekX = lbl.Parent.Location.X + scrollX;
                            int gercekY = lbl.Parent.Location.Y + scrollY;

                            nokta.X = gercekX + lbl.Location.X + 7;
                            nokta.Y = gercekY + lbl.Location.Y + 19;
                        }
                        else
                        {
                            nokta.X = (lbl.Location.X + scrollX) + 5;
                            nokta.Y = (lbl.Location.Y + scrollY) + 19;
                        }

                        cizilecekNoktalar.Add(nokta);
                    }
                }
            }

            panel2.Invalidate();
            pictureBox1.Invalidate();
            pictureBox2.Invalidate();
            pictureBox3.Invalidate();
        }
        // --------------------------------------------------- DİJKSTRA BİTİŞ ------------------------------------------------------
        private void HatasizParentYap(Control arac, PictureBox yeniBaba)
        {
            Point gercekYer = arac.Location;

            arac.Parent = yeniBaba;

            arac.Location = new Point(
                gercekYer.X - yeniBaba.Location.X,
                gercekYer.Y - yeniBaba.Location.Y
            );

            arac.BackColor = Color.Transparent;

            arac.BringToFront();
        }
        // ------------------------------------------- LOAD --------------------------------------------------------------
        private void Form1_Load(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Maximized;
            timer1.Start();
            timer1.Interval = 500;

            foreach (Button btn in panel2.Controls.OfType<Button>().ToList())
            {
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseDownBackColor = Color.Transparent;
                btn.FlatAppearance.MouseOverBackColor = Color.Transparent;
                btn.BackColor = Color.Transparent;

                if (btn.Bounds.IntersectsWith(pictureBox1.Bounds))
                {
                    HatasizParentYap(btn, pictureBox1);
                }
                else if (btn.Bounds.IntersectsWith(pictureBox2.Bounds))
                {
                    HatasizParentYap(btn, pictureBox2);
                }
                else if (btn.Bounds.IntersectsWith(pictureBox3.Bounds))
                {
                    HatasizParentYap(btn, pictureBox3);
                }
            }
            foreach (Label lbl in panel2.Controls.OfType<Label>().ToList())
            {
                if (lbl.Bounds.IntersectsWith(pictureBox1.Bounds))
                {
                    HatasizParentYap(lbl, pictureBox1);
                }
                else if (lbl.Bounds.IntersectsWith(pictureBox2.Bounds))
                {
                    HatasizParentYap(lbl, pictureBox2);
                }
                else if (lbl.Bounds.IntersectsWith(pictureBox3.Bounds))
                {
                    HatasizParentYap(lbl, pictureBox3);
                }
            }

            string adres = "Data Source=.\\SQLEXPRESS;Initial Catalog=Karayollari;Integrated Security=True";

            using (SqlConnection baglanti = new SqlConnection(adres))
            {
                try
                {
                    baglanti.Open();

                    string sorgu = @"
                SELECT D.node_ilk, D.node_son, D.uzunluk, Y.yol_adi 
                FROM Dügümler D 
                INNER JOIN Yollar Y ON Y.id = D.yol_id";

                    string etikersorgusu = "SELECT Label_id, Label_no FROM Etiketler";

                    SqlCommand komut = new SqlCommand(sorgu, baglanti);
                    SqlCommand cmdMap = new SqlCommand(etikersorgusu, baglanti);

                    using (SqlDataReader okuyucu = komut.ExecuteReader())
                    {
                        while (okuyucu.Read())
                        {
                            int u1 = Convert.ToInt32(okuyucu["node_ilk"]);
                            int u2 = Convert.ToInt32(okuyucu["node_son"]);
                            int dist = Convert.ToInt32(okuyucu["uzunluk"]);
                            string yolIsmi = okuyucu["yol_adi"].ToString();

                            if (!haritaGraph.ContainsKey(u1)) haritaGraph[u1] = new List<Komsuluk>();
                            if (!haritaGraph.ContainsKey(u2)) haritaGraph[u2] = new List<Komsuluk>();

                            haritaGraph[u1].Add(new Komsuluk { HedefNode = u2, Mesafe = dist, YolAdi = yolIsmi });
                            haritaGraph[u2].Add(new Komsuluk { HedefNode = u1, Mesafe = dist, YolAdi = yolIsmi });

                            if (!yolAdiToNodeID.ContainsKey(yolIsmi))
                            {
                                yolAdiToNodeID[yolIsmi] = u1;
                            }
                            else
                            {
                                if (u1 < yolAdiToNodeID[yolIsmi])
                                {
                                    yolAdiToNodeID[yolIsmi] = u1;
                                }
                            }
                        }
                    }
                    using (SqlDataReader okuyucuMap = cmdMap.ExecuteReader())
                    {
                        while (okuyucuMap.Read())
                        {
                            int nID = Convert.ToInt32(okuyucuMap["Label_id"]);
                            int lblNo = Convert.ToInt32(okuyucuMap["Label_no"]);

                            if (!nodeToLabelMap.ContainsKey(nID))
                            {
                                nodeToLabelMap.Add(nID, lblNo);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Veritabanı Hatası: " + ex.Message);
                }
            }

            foreach (var yolAdi in yolAdiToNodeID.Keys.OrderBy(k => k))
            {
                Baslangic.Items.Add(yolAdi);
                Varis.Items.Add(yolAdi);
                comboBox1.Items.Add(yolAdi);
            }

            pictureBox1.Paint += panel2_Paint;
            pictureBox2.Paint += panel2_Paint;
            pictureBox3.Paint += panel2_Paint;

            panel2.Invalidate();
            pictureBox1.Invalidate();
            pictureBox2.Invalidate();
            pictureBox3.Invalidate();

            AraclariListele();
        }
        // -------------------------------------------------------- LOAD BİTİŞ -----------------------------------------------
        private void Ara_buton_Click(object sender, EventArgs e)
        {
            zaman = true;

            cizilecekNoktalar.Clear();
            panel2.Invalidate();
            pictureBox1.Invalidate();
            pictureBox2.Invalidate();
            pictureBox3.Invalidate();

            if (Baslangic.SelectedItem == null || Varis.SelectedItem == null)
            {
                MessageBox.Show("Lütfen hem başlangıç hem de varış yolunu seçiniz.");
                return;
            }

            string secilenBaslangic = Baslangic.Text;
            string secilenVaris = Varis.Text;

            if (yolAdiToNodeID.ContainsKey(secilenBaslangic) && yolAdiToNodeID.ContainsKey(secilenVaris))
            {
                int startID = yolAdiToNodeID[secilenBaslangic];
                int endID = yolAdiToNodeID[secilenVaris];

                DijkstraHesapla(startID, endID);
            }
            else
            {
                label6.Text = "Hata: Seçilen yolların harita karşılığı bulunamadı.";
            }
        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {
            if (cizilecekNoktalar == null || cizilecekNoktalar.Count < 2) return;

            using (Pen kalem = new Pen(Color.Red, 3))
            {
                kalem.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                kalem.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                kalem.EndCap = System.Drawing.Drawing2D.LineCap.Round;

                int scrollX = panel2.HorizontalScroll.Value;
                int scrollY = panel2.VerticalScroll.Value;

                if (sender is PictureBox)
                {
                    PictureBox resimKutusu = (PictureBox)sender;

                    int resimGercekX = resimKutusu.Location.X + scrollX;
                    int resimGercekY = resimKutusu.Location.Y + scrollY;

                    List<Point> yerelNoktalar = new List<Point>();

                    foreach (Point p in cizilecekNoktalar)
                    {
                        yerelNoktalar.Add(new Point(p.X - resimGercekX, p.Y - resimGercekY));
                    }

                    e.Graphics.DrawLines(kalem, yerelNoktalar.ToArray());
                }
                else
                {
                    e.Graphics.TranslateTransform(-scrollX, -scrollY);
                    e.Graphics.DrawLines(kalem, cizilecekNoktalar.ToArray());
                    e.Graphics.ResetTransform();
                }
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            MessageBox.Show("Uygun Yol Bulunamadı.");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //34. nokta
            node = 34;
            zurna = 7;
            NodeSecimIslemi();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //16. nokta
            node = 16;
            zurna = 12;
            NodeSecimIslemi();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //33. nokta
            node = 33;
            zurna = 8;
            NodeSecimIslemi();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            //15. nokta
            node = 15;
            zurna = 9;
            NodeSecimIslemi();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            //14. nokta
            node = 14;
            zurna = 11;
            NodeSecimIslemi();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            //1. nokta
            node = 1;
            zurna = 38;
            NodeSecimIslemi();
        }

        private void button9_Click(object sender, EventArgs e)
        {
            //2. nokta
            node = 2;
            zurna = 39;
            NodeSecimIslemi();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            //3. nokta
            node = 3;
            zurna = 37;
            NodeSecimIslemi();
        }

        private void button10_Click(object sender, EventArgs e)
        {
            //4. nokta
            node = 4;
            zurna = 36;
            NodeSecimIslemi();
        }

        private void button11_Click(object sender, EventArgs e)
        {
            //5. nokta
            node = 5;
            zurna = 35;
            NodeSecimIslemi();
        }

        private void button12_Click(object sender, EventArgs e)
        {
            //6 ve 7. nokta (6 yı attık kafadan gitti)
            node = 7;
            zurna = 28;
            NodeSecimIslemi();
        }

        private void button13_Click(object sender, EventArgs e)
        {
            //20. nokta
            node = 20;
            zurna = 25;
            NodeSecimIslemi();
        }

        private void button15_Click(object sender, EventArgs e)
        {
            //21. nokta
            node = 21;
            zurna = 20;
            NodeSecimIslemi();
        }

        private void button14_Click(object sender, EventArgs e)
        {
            //22. nokta
            node = 22;
            zurna = 18;
            NodeSecimIslemi();
        }

        private void button16_Click(object sender, EventArgs e)
        {
            //31. nokta
            node = 31;
            zurna = 27;
            NodeSecimIslemi();
        }

        private void button17_Click(object sender, EventArgs e)
        {
            //32. nokta
            node = 32;
            zurna = 24;
            NodeSecimIslemi();
        }

        private void button18_Click(object sender, EventArgs e)
        {
            //28. nokta
            node = 28;
            zurna = 21;
            NodeSecimIslemi();
        }

        private void button19_Click(object sender, EventArgs e)
        {
            //29. nokta
            node = 29;
            zurna = 22;
            NodeSecimIslemi();
        }

        private void button20_Click(object sender, EventArgs e)
        {
            //30. nokta
            node = 30;
            zurna = 23;
            NodeSecimIslemi();
        }

        private void button21_Click(object sender, EventArgs e)
        {
            //23. nokta
            node = 23;
            zurna = 19;
            NodeSecimIslemi();
        }

        private void button22_Click(object sender, EventArgs e)
        {
            //24. nokta
            node = 24;
            zurna = 26;
            NodeSecimIslemi();
        }

        private void button23_Click(object sender, EventArgs e)
        {
            //25. nokta
            node = 25;
            zurna = 30;
            NodeSecimIslemi();
        }

        private void button24_Click(object sender, EventArgs e)
        {
            //26. nokta
            node = 26;
            zurna = 17;
            NodeSecimIslemi();
        }

        private void button25_Click(object sender, EventArgs e)
        {
            //27. nokta
            node = 27;
            zurna = 10;
            NodeSecimIslemi();
        }

        private void button26_Click(object sender, EventArgs e)
        {
            //19. nokta
            node = 19;
            zurna = 31;
            NodeSecimIslemi();
        }

        private void button27_Click(object sender, EventArgs e)
        {
            //18. nokta
            node = 18;
            zurna = 33;
            NodeSecimIslemi();
        }

        private void button28_Click(object sender, EventArgs e)
        {
            //17. nokta
            node = 17;
            zurna = 16;
            NodeSecimIslemi();
        }

        private void button29_Click(object sender, EventArgs e)
        {
            //8. nokta
            node = 8;
            zurna = 29;
            NodeSecimIslemi();
        }

        private void button30_Click(object sender, EventArgs e)
        {
            //9. nokta
            node = 9;
            zurna = 32;
            NodeSecimIslemi();
        }

        private void button31_Click(object sender, EventArgs e)
        {
            //10. nokta
            node = 10;
            zurna = 34;
            NodeSecimIslemi();
        }

        private void button32_Click(object sender, EventArgs e)
        {
            //11. nokta
            node = 11;
            zurna = 13;
            NodeSecimIslemi();
        }

        private void button33_Click(object sender, EventArgs e)
        {
            //12. nokta
            node = 12;
            zurna = 14;
            NodeSecimIslemi();
        }

        private void button34_Click(object sender, EventArgs e)
        {
            //13. nokta
            node = 13;
            zurna = 15;
            NodeSecimIslemi();
        }

        private void button35_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Uygun Yol Bulunamadı.");
        }

        private void button36_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Uygun Yol Bulunamadı.");
        }

        private void button37_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Uygun Yol Bulunamadı.");
        }

        private void hız_ogren_Click(object sender, EventArgs e)
        {
            string adres = "Data Source=.\\SQLEXPRESS;Initial Catalog=Karayollari;Integrated Security=True";

            using (SqlConnection baglanti = new SqlConnection(adres))
            {
                try
                {
                    baglanti.Open();

                    string sql = "SELECT maxspeed FROM Yollar WHERE yol_adi = @ad";

                    SqlCommand komut = new SqlCommand(sql, baglanti);
                    komut.Parameters.AddWithValue("@ad", comboBox1.Text);

                    object sonuc = komut.ExecuteScalar();

                    if (sonuc != null && sonuc != DBNull.Value)
                    {
                        label3.Text = sonuc.ToString() + " KM/h";
                    }
                    else
                    {
                        label3.Text = "Hız Verisi Yok";
                    }
                }
                catch (Exception hata)
                {
                    MessageBox.Show("Hata: " + hata.Message);
                }
            }
        }

        private void Guncelle_Click(object sender, EventArgs e)
        {
            for (int i = 7; i <= 39; i++)
            {
                Control[] durak = this.Controls.Find("label" + i, true);
                if (durak.Length > 0) durak[0].Visible = false;
            }

            string adres = "Data Source=.\\SQLEXPRESS;Initial Catalog=Karayollari;Integrated Security=True";

            Random rnd = new Random();

            using (SqlConnection baglanti = new SqlConnection(adres))
            {
                try
                {
                    baglanti.Open();

                    DataTable dtAraclar = new DataTable();
                    SqlDataAdapter da = new SqlDataAdapter("SELECT arac_id, anlik_hiz FROM Araçlar", baglanti);
                    da.Fill(dtAraclar);

                    foreach (DataRow satir in dtAraclar.Rows)
                    {
                        int id = Convert.ToInt32(satir["arac_id"]);
                        int mevcutHiz = Convert.ToInt32(satir["anlik_hiz"]);
                        int yeniHiz = 0;

                        if (mevcutHiz > 50)
                        {
                            yeniHiz = 0;
                        }
                        else if (mevcutHiz == 0)
                        {
                            yeniHiz = rnd.Next(0, 21);
                        }
                        else if (mevcutHiz >= 1 && mevcutHiz <= 30)
                        {
                            yeniHiz = mevcutHiz + 17;
                        }
                        else if (mevcutHiz > 30)
                        {
                            int ek = rnd.Next(-20, 41);
                            yeniHiz = mevcutHiz + ek;

                            if (yeniHiz < 0) yeniHiz = 0;
                        }

                        int yeniKonum = rnd.Next(1, 35);
                        if (yeniKonum == 6)
                        {
                            yeniKonum++;
                        }

                        string updateSql = "UPDATE Araçlar SET anlik_hiz = @yh, konum = @yk WHERE arac_id = @id";
                        SqlCommand cmdUpdate = new SqlCommand(updateSql, baglanti);
                        cmdUpdate.Parameters.AddWithValue("@yh", yeniHiz);
                        cmdUpdate.Parameters.AddWithValue("@yk", yeniKonum);
                        cmdUpdate.Parameters.AddWithValue("@id", id);
                        cmdUpdate.ExecuteNonQuery();
                    }

                    SqlCommand cmdSil = new SqlCommand("TRUNCATE TABLE Bekleyenler", baglanti);
                    cmdSil.ExecuteNonQuery();

                    string ekleSql = @"
                INSERT INTO Bekleyenler (durak_id, bekleyen_arac_id)
                SELECT D.durak_id, A.arac_id
                FROM Araçlar A
                INNER JOIN Duraklar D ON A.konum = D.durak_konum
                WHERE A.anlik_hiz = 0";

                    SqlCommand cmdEkle = new SqlCommand(ekleSql, baglanti);
                    int bekleyenSayisi = cmdEkle.ExecuteNonQuery();

                    AraclariListele();
                    MessageBox.Show("Güncelleme Tamamlandı... ");
                }
                catch (Exception hata)
                {
                    MessageBox.Show("Hata: " + hata.Message);
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            string adres = "Data Source=.\\SQLEXPRESS;Initial Catalog=Karayollari;Integrated Security=True";

            using (SqlConnection baglanti = new SqlConnection(adres))
            {
                try
                {
                    baglanti.Open();

                    string sql = @"
                SELECT DISTINCT E.Label_no 
                FROM Bekleyenler B 
                INNER JOIN Etiketler E ON B.durak_id = E.Label_id";

                    SqlCommand komut = new SqlCommand(sql, baglanti);
                    SqlDataReader okuyucu = komut.ExecuteReader();

                    while (okuyucu.Read())
                    {
                        int durakNo = Convert.ToInt32(okuyucu["Label_no"]);

                        string hedefLabel = "label" + durakNo;

                        Control[] bulunanlar = this.Controls.Find(hedefLabel, true);

                        if (bulunanlar.Length > 0)
                        {
                            bulunanlar[0].Visible = !bulunanlar[0].Visible;
                        }

                        if (zaman == true)
                        {
                            bulunanlar[0].Visible = false;
                            timer1.Stop();
                        }
                    }
                    okuyucu.Close();

                    string sqlAnaliz = @"
                    SELECT 
                        A.hat_no AS [Hat], 
                        COUNT(A.arac_id) AS [Toplam Araç], 
                        AVG(A.anlik_hiz) AS [Ort. Hız],
                        MAX(A.anlik_hiz) AS [Max Hız],
                        CASE 
                            WHEN AVG(A.anlik_hiz) < 30 THEN 'Yoğun'
                            WHEN AVG(A.anlik_hiz) BETWEEN 30 AND 60 THEN 'Akıcı'
                            ELSE 'Hızlı'
                        END AS [Durum]
                    FROM Araçlar A
                    GROUP BY A.hat_no
                    HAVING COUNT(A.arac_id) > 0
                    ORDER BY [Ort. Hız] ASC";

                    SqlDataAdapter da = new SqlDataAdapter(sqlAnaliz, baglanti);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    dataGridView2.DataSource = dt;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Bağlantı veya Sorgu Hatası: " + ex.Message);
                }
            }
        }

        private void Temizle_Click(object sender, EventArgs e)
        {
            for (int i = 7; i <= 39; i++)
            {
                Control[] durak = this.Controls.Find("label" + i, true);
                if (durak.Length > 0) durak[0].Visible = false;
            }

            cizilecekNoktalar.Clear();
            panel2.Invalidate();
            pictureBox1.Invalidate();
            pictureBox2.Invalidate();
            pictureBox3.Invalidate();

            label6.Text = "";

            zaman = false;
            timer1.Start();
        }

        private void Ekle_buton_Click(object sender, EventArgs e)
        {
            string adres = "Data Source=.\\SQLEXPRESS;Initial Catalog=Karayollari;Integrated Security=True";
            string ekle = "INSERT INTO Araçlar (plaka, hat_no, anlik_hiz, konum) VALUES (@plaka, @hat, @hiz, @konum)";

            if (string.IsNullOrWhiteSpace(Plaka_ekle.Text) || string.IsNullOrWhiteSpace(Hat_ekle.Text))
            {
                MessageBox.Show("Lütfen Gereken Kutuları Doldurun!");
                return; 
            }

            int hat;
            if (!int.TryParse(Hat_ekle.Text, out hat))
            {
                MessageBox.Show("Lütfen Hat Numarasına Sayısal Bir Değer Verin!");
                return;
            }
            string plaka = Plaka_ekle.Text;

            using (SqlConnection baglanti = new SqlConnection(adres))
            {
                using (SqlCommand komut = new SqlCommand(ekle, baglanti))
                {
                    komut.Parameters.AddWithValue("@plaka", plaka);
                    komut.Parameters.AddWithValue("@hat", hat);
                    komut.Parameters.AddWithValue("@hiz", 0);
                    komut.Parameters.AddWithValue("@konum", 1);

                    try
                    {
                        baglanti.Open();
                        komut.ExecuteNonQuery();
                        MessageBox.Show("Araç başarıyla sisteme eklendi.");
                        AraclariListele();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Hata oluştu: " + ex.Message);
                    }
                }
            }

            Plaka_ekle.Clear();
            Hat_ekle.Clear();
        }
    }
    public class Komsuluk
    {
        public int HedefNode { get; set; }
        public int Mesafe { get; set; }
        public string YolAdi { get; set; }
    }
}
