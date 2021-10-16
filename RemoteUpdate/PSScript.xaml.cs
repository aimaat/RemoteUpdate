using System.Collections.Generic;
using System.Windows;

namespace RemoteUpdate
{
    /// <summary>
    /// Interaction logic for PSScript.xaml
    /// </summary>
    public partial class PSScript : Window
    {
        //static System.Data.DataTable TableScripts = new System.Data.DataTable("Scripts");
        public PSScript()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Owner = Application.Current.MainWindow;

            UpdateComboBoxList();
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
            if (iID == -1)
            {
                strName = ComboBoxScript.Text;
            }
            else
            {
                strName = ComboBoxScript.SelectedItem.ToString().Trim();
            }
            if (strScript.Length > 1 && strName.Length > 1)
            {
                if (iID == -1)
                {
                    System.Data.DataRow tmpRow = Global.TableScripts.NewRow();
                    tmpRow["Name"] = strName;
                    tmpRow["Script"] = strScript;
                    Global.TableScripts.Rows.Add(tmpRow);
                }
                else
                {
                    Global.TableScripts.Rows[iID]["Name"] = strName;
                    Global.TableScripts.Rows[iID]["Script"] = strScript;
                }
            }
            UpdateComboBoxList();
        }

        private void UpdateComboBoxList()
        {
            List<string> ids = new List<string>(Global.TableScripts.Rows.Count);
            foreach (System.Data.DataRow row in Global.TableScripts.Rows)
            {
                ids.Add(row["Name"].ToString());
            }
            ComboBoxScript.ItemsSource = ids;
        }
    }
}
