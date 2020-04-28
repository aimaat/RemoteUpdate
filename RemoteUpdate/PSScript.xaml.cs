using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RemoteUpdate
{
    /// <summary>
    /// Interaction logic for PSScript.xaml
    /// </summary>
    public partial class PSScript : Window
    {
        System.Data.DataTable TableScripts = new System.Data.DataTable("Scripts");
        public PSScript()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Owner = Application.Current.MainWindow;
            //TableScripts.Columns.Add("ID", typeof(int),);
            TableScripts.Columns.Add("Name");
            TableScripts.Columns.Add("Script");
            //ComboBoxScript.ItemsSource = TableScripts.DefaultView;
        }
        private void ButtonCancel(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
        private void ButtonOk(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            string strScript = TextBoxScript.Text.Trim();
            string strName;
            int iID = ComboBoxScript.SelectedIndex;
            if(iID == -1)
            {
                strName = ComboBoxScript.Text;
            } else
            {
                strName = ComboBoxScript.SelectedItem.ToString().Trim();
            }
            if (strScript.Length > 1 && strName.Length > 1)
            {
                if(iID == -1)
                {
                    System.Data.DataRow tmpRow = TableScripts.NewRow();
                    tmpRow["Name"] = strName;
                    tmpRow["Script"] = strScript;
                    TableScripts.Rows.Add(tmpRow);
                } else
                {
                    TableScripts.Rows[iID]["Name"] = strName;
                    TableScripts.Rows[iID]["Script"] = strScript;
                }
            }
        }
    }
}
