using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;

namespace ParsePage
{
    public partial class Form1 : Form
    {
        private string url;
        WebBrowser wb;
        public Form1()
        {
            InitializeComponent();
            //textBox_URL.Text = @"http://www.lamoda.ru/cb/479-23837/clothes-muzhskaya-verkhnyaya-odezhda-topman/?order=ORDER_BY_NEW";
            textBox_URL.Text = @" http://www.lamoda.ru/c/157/shoes-muzhskie-tufli/?genders=men&sitelink=topmenuM&l=4";
            textBox_pathToImages.Text = @"C:\All images\";
            makeTable();
            dataGridView1.DataSource = table;
            dataGridView1.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            dataGridView1.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridView1.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridView1.Columns[3].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            dataGridView1.Columns[4].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            //&page=2
            //<span class="products-catalog__head-counter">59 товаров</span>
            url = textBox_URL.Text;
            isStoped = false;
        }

        private string Win1251ToUTF8(string source)
        {
            Encoding utf8 = Encoding.GetEncoding("utf-8");
            Encoding win1251 = Encoding.GetEncoding("windows-1251");
            byte[] utf8Bytes = win1251.GetBytes(source);
            byte[] win1251Bytes = Encoding.Convert(win1251, utf8, utf8Bytes);
            source = win1251.GetString(win1251Bytes);
            return source;
        }
        private void button_download_Click(object sender, EventArgs e)
        {

            url = textBox_URL.Text;
            currPage = 1;
            pagesCount = 0;
            countImages = 0;
            progressBar1.Value = 0;
            wb = new WebBrowser();
            wb.AllowNavigation = true;
            wb.ScriptErrorsSuppressed = true;
            wb.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(wb_DocumentCompleted);
            wb.Navigate(url);
        }
        
        int pagesCount;
        int currPage=1;
        private void wb_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            WebBrowser wb = sender as WebBrowser;
            if (wb.ReadyState != WebBrowserReadyState.Complete)
                return;
            if (e.Url.AbsolutePath != (sender as WebBrowser).Url.AbsolutePath)
                return;
            HtmlDocument doc = wb.Document;
            //textBox_inner.Text = doc.Body.InnerText;


            // wb.Document is not null at this point
            if (pagesCount==0)
                foreach (HtmlElement elm in ElementsByClass(doc, "products-catalog__head-counter"))
                {//<span class="products-catalog__head-counter">59 товаров</span>
                 //19941 товар
                
                    string str = elm.InnerText;
                    str=str.Replace(" товаров", "").Replace(" товара", "").Replace(" товар", "");
                    pagesCount = (Int32.Parse(str) / 60 + ((Int32.Parse(str) % 60) != 0 ? 1 : 0)) ;
                    progressBar1.Minimum = 0;
                    progressBar1.Maximum = pagesCount;
                    break;
                }
            if (pagesCount>1 && currPage < pagesCount)
            {
                //&page=2

                if (!isStoped)
                {
                    currPage++;
                    wb.Navigate(url + "&page=" + currPage);
                }
             }
            parseProducts(doc);
            progressBar1.Increment(1);

        }
        private static string MakeValidFileName(string name)
        {
            string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");
        }
        int countImages;
        void parseProducts(HtmlDocument doc)
        {
            HtmlElementCollection elms = doc.GetElementsByTagName("div");
            DataRow row;
            List<string> srcs = new List<string>();
            //products-list-item__img
            /*
            <img class="products-list-item__img" width="236" height="341"
            src="//pi0.lmcdn.ru/img236x341/B/U/BU182AMJKR77_1_v1.jpg"
            alt="Туфли Bugatti, цвет: коричневый. Артикул: BU182AMJKR77. Мужская обувь / Туфли" 
            src-rollover="//pi2.lmcdn.ru/img236x341/B/U/BU182AMJKR77_2_v1.jpg" 
            data-src="//pi0.lmcdn.ru/img236x341/B/U/BU182AMJKR77_1_v1.jpg">
            */
            string localFolder = textBox_pathToImages.Text;
            if(!System.IO.Directory.Exists(localFolder))
                System.IO.Directory.CreateDirectory(localFolder);
            using (WebClient client = new WebClient())
            {
                int count_img = doc.GetElementsByTagName("img").Count;
                foreach (HtmlElement elm in ElementsByClass(doc, "products-list-item__img lazy"))
                {
                    //"//pi0.lmcdn.ru/img236x341/V/A/VA468AMICV22_1_v1.jpg"
                    countImages += 1;
                    string src = elm.GetAttribute("data-src");
                    src = src.Replace("//", "http://");
                    string name = localFolder + MakeValidFileName(src);
                    if (System.IO.File.Exists(name))
                        name = newFileName(name,countImages);
                    client.DownloadFile(src, name);
                }
                foreach (HtmlElement elm in ElementsByClass(doc, "products-list-item__img"))
                {
                    countImages += 1;
                    string src = elm.GetAttribute("src");
                    string name = localFolder + MakeValidFileName(src);
                    if (System.IO.File.Exists(name))
                        name = newFileName(name, countImages);
                    client.DownloadFile(src, name);
                }
            }

            foreach (HtmlElement elm in ElementsByClass(doc, "products-list-item__brand"))
            {
                row = table.NewRow();
                row["id"] = curr_id;
                row["Brand"] = elm.InnerText;
                //
                if (elm.NextSibling != null)
                {


                    row["ProductName"] = elm.NextSibling.InnerText;
                    /*
                    <span class="price">
                        <span class="price__old">7 900 руб.</span><wbr></wbr><span class="price__new" itemprop="price">7 110 руб.</span>
                        <link itemprop="availability" href="http://schema.org/InStock">
                    </span>

                    <span class="button button_s button_outline paginator__prev">Назад</span><span class="paginator__pages"></span>
                    <span class="button button_s button_outline paginator__next">Дальше</span>


                    ///itemprop="price"
        */
                    if (!(elm.NextSibling.NextSibling.GetAttribute("itemprop") == "price"))
                    {
                        foreach (HtmlElement pr in elm.NextSibling.NextSibling.Children)
                        {
                            if (pr.GetAttribute("className") == "price__old") row["OldPrice"] = ParseInt(pr.InnerText);
                            if (pr.GetAttribute("className") == "price__new") row["NewPrice"] = ParseInt(pr.InnerText);
                        }

                    }
                    else
                    {
                        row["OldPrice"] = ParseInt(elm.NextSibling.NextSibling.InnerText);
                        row["NewPrice"] = ParseInt(elm.NextSibling.NextSibling.InnerText);
                    }
                }
                table.Rows.Add(row);
                curr_id++;
            }
        }

        private static string newFileName(string name, int count_)
        {
            string fileNameOnly = System.IO.Path.GetFileNameWithoutExtension(name);
            string extension = System.IO.Path.GetExtension(name);
            string path = System.IO.Path.GetDirectoryName(name);
            string tempFileName = string.Format("{0}({1})", fileNameOnly, count_);
            return System.IO.Path.Combine(path, tempFileName + extension);
        }

        int ParseInt(string str)
        {
            str = str.Replace(" ","");
            return Int32.Parse(str.Replace("руб.", ""));
        }
        static IEnumerable<HtmlElement> ElementsByClass(HtmlDocument doc, string className)
        {
            foreach (HtmlElement e in doc.All)
                if (e.GetAttribute("className") == className)
                    yield return e;
        }
        System.Data.DataTable table;
        int curr_id;
        private void makeTable()
        {
            // Create a new DataTable.
            table = new DataTable("ProductsTable");
            // Declare variables for DataColumn and DataRow objects.
            DataColumn column;

            // Create new DataColumn, set DataType, 
            // ColumnName and add to DataTable.    
            column = new DataColumn();
            column.DataType = System.Type.GetType("System.Int32");
            column.ColumnName = "id";
            //column.ReadOnly = true;
            column.Unique = true;
            // Add the Column to the DataColumnCollection.
            table.Columns.Add(column);

            // Create second column.
            column = new DataColumn();
            column.DataType = System.Type.GetType("System.String");
            column.ColumnName = "Brand";
            column.AutoIncrement = false;
            column.Caption = "Brand";
            column.ReadOnly = false;
            column.Unique = false;
            table.Columns.Add(column);

            column = new DataColumn();
            column.DataType = System.Type.GetType("System.String");
            column.ColumnName = "ProductName";
            column.AutoIncrement = false;
            column.Caption = "Product name(type)";
            column.ReadOnly = false;
            column.Unique = false;
            table.Columns.Add(column);

            column = new DataColumn();
            column.DataType = System.Type.GetType("System.Int32");
            column.ColumnName = "OldPrice";
            column.AutoIncrement = false;
            column.Caption = "Old price";
            column.ReadOnly = false;
            column.Unique = false;
            column.DefaultValue = 0;
            table.Columns.Add(column);

            column = new DataColumn();
            column.DataType = System.Type.GetType("System.Int32");
            column.ColumnName = "NewPrice";
            column.AutoIncrement = false;
            column.Caption = "New price";
            column.ReadOnly = false;
            column.Unique = false;
            column.DefaultValue = 0;
            // Add the column to the table.
            table.Columns.Add(column);

            // Make the ID column the primary key column.
            DataColumn[] PrimaryKeyColumns = new DataColumn[1];
            PrimaryKeyColumns[0] = table.Columns["id"];
            table.PrimaryKey = PrimaryKeyColumns;

        }

        private void скопироватьHTMLКодToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //if(doc!=null)
            // Clipboard.SetText(doc.Body.OuterText);
        }

        private void button_clear_Click(object sender, EventArgs e)
        {
            table.Clear();
            curr_id = 0;

        }

        private void сохранитьКакToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "XML-File | *.xml";
            if (sfd.ShowDialog()==DialogResult.OK)
            {
                table.WriteXml(sfd.FileName);
            }
            
        }
        bool isStoped;
        private void button_stop_Click(object sender, EventArgs e)
        {
            if (!isStoped)
            {
                isStoped = true;
                button_stop.Text = "Продолжить";
            }else
            {
                isStoped = false;
                button_stop.Text = "Остановить";
            }
            if (!isStoped && pagesCount > 1 && currPage < pagesCount)
            {
                currPage++;
                wb.Navigate(url + "&page=" + currPage);
            }
        }
    }


}
