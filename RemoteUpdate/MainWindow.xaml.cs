using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Threading;

namespace RemoteUpdate
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        // MainWindow Grid
        internal Grid GridMainWindow;
        public MainWindow()
        {
            InitializeComponent();
            // Global.TableRuntime Columns Creation
            Global.TableRuntime.Columns.Add("Servername");
            Global.TableRuntime.Columns.Add("IP");
            Global.TableRuntime.Columns.Add("Username");
            Global.TableRuntime.Columns.Add("Password");
            Global.TableRuntime.Columns.Add("Ping");
            Global.TableRuntime.Columns.Add("Uptime");
            // Get Grid to add more Controls
            GridMainWindow = this.Content as Grid;
            // Initialize Datatable for XML Load
            System.Data.DataTable LoadTable = new System.Data.DataTable();
            // Initialize Servernumber
            int ServerNumber;
            // Load Schema and Data from XML RemoteUpdateServer.xml
            if (Tasks.ReadXMLToTable(AppDomain.CurrentDomain.BaseDirectory + "RemoteUpdateServer.xml", LoadTable))
            {
                // Set Servernumber according to Rows from XML
                ServerNumber = LoadTable.Rows.Count;
                // Set Values for first Row
                this.TextBoxServer_0.Text = LoadTable.Rows[0]["Server"].ToString();
                this.CheckboxAccept_0.IsChecked = Convert.ToBoolean(LoadTable.Rows[0]["Accept"], Global.cultures);
                this.CheckboxDrivers_0.IsChecked = Convert.ToBoolean(LoadTable.Rows[0]["Drivers"], Global.cultures);
                this.CheckboxReboot_0.IsChecked = Convert.ToBoolean(LoadTable.Rows[0]["Reboot"], Global.cultures);
                this.CheckboxGUI_0.IsChecked = Convert.ToBoolean(LoadTable.Rows[0]["GUI"], Global.cultures);
                this.CheckboxMail_0.IsChecked = Convert.ToBoolean(LoadTable.Rows[0]["Mail"], Global.cultures);
                this.CheckboxEnabled_0.IsChecked = Convert.ToBoolean(LoadTable.Rows[0]["Enabled"], Global.cultures);
                // Create first DataRow in Global.TableRuntime
                System.Data.DataRow dtrow = Global.TableRuntime.NewRow();
                dtrow["Servername"] = LoadTable.Rows[0]["Server"].ToString();
                dtrow["IP"] = Tasks.GetIPfromHostname(LoadTable.Rows[0]["Server"].ToString());
                dtrow["Username"] = LoadTable.Rows[0]["Username"].ToString();
                dtrow["Password"] = Tasks.Decrypt(LoadTable.Rows[0]["Password"].ToString());
                dtrow["Ping"] = "";
                dtrow["Uptime"] = "";
                Global.TableRuntime.Rows.Add(dtrow);
            }
            else
            {
                // If no XML could be read, set the Servernumber to 0
                ServerNumber = 0;
                // Create Empty Data Row
                Global.TableRuntime.Rows.Add(Global.TableRuntime.NewRow());
            }
            // Load Schema and Data from XML RemoteUpdateSettings.xml
            if (!Tasks.ReadXMLToTable(AppDomain.CurrentDomain.BaseDirectory + "RemoteUpdateSettings.xml", Global.TableSettings)) 
            { 
                Global.TableSettings.Columns.Add("SMTPServer");
                Global.TableSettings.Columns.Add("SMTPPort");
                Global.TableSettings.Columns.Add("MailFrom");
                Global.TableSettings.Columns.Add("MailTo");
                Global.TableSettings.Columns.Add("PSVirtualAccountName");
                Global.TableSettings.Columns.Add("PSWUCommands");
                Global.TableSettings.Rows.Add(Global.TableSettings.NewRow());
                Global.TableSettings.Rows[0]["PSVirtualAccountName"] = "VirtualAccount";
            }
            // Create BackgroundWorker (Ping and Uptime)
            Worker.CreateBackgroundWorker(0);
            // Change Height of Main Window when more than 10 Servers are in the List
            if (ServerNumber > 2) { Application.Current.MainWindow.Height = 130 + ServerNumber * 30; }
            // Add Controls for each Server loaded
            for (int ii = 1; ii < ServerNumber + 1; ii++)
            {
                // ServerName Textbox creation
                string tmpText = "";
                bool tmpBool = false;
                bool tmpBoolGUI = true;
                if (ii < ServerNumber) { tmpText = LoadTable.Rows[ii]["Server"].ToString(); }
                // Textbox Servername creation
                CreateTextbox("TextBoxServer_" + ii, tmpText, 18, 120, 20, 30 * (ii + 1));
                // Uptime Label creation
                CreateLabel("LabelUptime_" + ii, "", 26, 90, 150, 30 * (ii + 1) - 4, Visibility.Visible);
                // Accept Checkbox creation
                if (ii < ServerNumber) { tmpBool = Convert.ToBoolean(LoadTable.Rows[ii]["Accept"], Global.cultures); }
                CreateCheckbox("CheckboxAccept_" + ii, 250, 30 * (ii + 1), tmpBool);
                // Drivers Checkbox creation
                if (ii < ServerNumber) { tmpBool = Convert.ToBoolean(LoadTable.Rows[ii]["Drivers"], Global.cultures); }
                CreateCheckbox("CheckboxDrivers_" + ii, 310, 30 * (ii + 1), tmpBool);
                // Reboot Checkbox creation
                if (ii < ServerNumber) { tmpBool = Convert.ToBoolean(LoadTable.Rows[ii]["Reboot"], Global.cultures); }
                CreateCheckbox("CheckboxReboot_" + ii, 370, 30 * (ii + 1), tmpBool);
                // GUI Checkbox creation
                if (ii < ServerNumber) { tmpBoolGUI = Convert.ToBoolean(LoadTable.Rows[ii]["GUI"], Global.cultures); }
                CreateCheckbox("CheckboxGUI_" + ii, 430, 30 * (ii + 1), tmpBoolGUI);
                // Mail Checkbox creation
                if (ii < ServerNumber) { tmpBool = Convert.ToBoolean(LoadTable.Rows[ii]["Mail"], Global.cultures); }
                CreateCheckbox("CheckboxMail_" + ii, 490, 30 * (ii + 1), tmpBool);
                // Credentials Button creation
                CreateButton("ButtonCredentials_" + ii, "Credentials", 70, 530, 30 * ((ii + 1) - 1) + 29, new RoutedEventHandler(GetCredentials), System.Windows.Visibility.Visible);
                // Start Button creation
                CreateButton("ButtonStart_" + ii, "Start", 70, 620, 30 * ((ii + 1) - 1) + 29, new RoutedEventHandler(ButtonStart_Click), System.Windows.Visibility.Visible);
                // Time Button creation
                CreateButton("ButtonTime_" + ii, "12:12:12", 70, 620, 30 * ((ii + 1) - 1) + 29, new RoutedEventHandler(ButtonTime_Click), System.Windows.Visibility.Hidden);
                // Enabled Checkbox creation
                if (ii < ServerNumber) { tmpBool = Convert.ToBoolean(LoadTable.Rows[ii]["Enabled"], Global.cultures); }
                CreateCheckbox("CheckboxEnabled_" + ii, 710, 30 * (ii + 1), tmpBool);
                // If Servernumber is Even, create Light Grey Rectangle Background
                if ((ii + 1) % 2 == 0)
                {
                    // Light Grey Rectangle creation
                    CreateRectangle("BackgroundRectangle_" + ii, 30, double.NaN, 0, 24 + 30 * ii, new SolidColorBrush(Color.FromRgb(222, 217, 217)), 0);
                }
                // Create BackgroundWorker (Ping and Uptime)
                Worker.CreateBackgroundWorker(ii);
                // Create new Row in TableRuntime
                System.Data.DataRow dtrow = Global.TableRuntime.NewRow();
                if (ii < LoadTable.Rows.Count)
                {
                    dtrow["Servername"] = LoadTable.Rows[ii]["Server"].ToString();
                    dtrow["IP"] = Tasks.GetIPfromHostname(LoadTable.Rows[ii]["Server"].ToString());
                    dtrow["Username"] = LoadTable.Rows[ii]["Username"].ToString();
                    dtrow["Password"] = Tasks.Decrypt(LoadTable.Rows[ii]["Password"].ToString());
                    dtrow["Ping"] = "";
                    dtrow["Uptime"] = "";
                }
                Global.TableRuntime.Rows.Add(dtrow);
                // Timer Creation for Interface Updates
                Global.TimerUpdateGrid.Interval = TimeSpan.FromSeconds(5);
                Global.TimerUpdateGrid.Tick += (sender, e) => { Worker.TimerUpdateGrid_Tick(GridMainWindow); };
                Global.TimerUpdateGrid.Start();
            }
            LoadTable.Dispose();
        }
        /// <summary>
        /// Function to change all Checkboxes IsChecked Status in the same name range
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBoxChanged(object sender, RoutedEventArgs e)
        {
            var list = GridMainWindow.Children.OfType<CheckBox>().Where(cb => cb.Name.Contains((sender as CheckBox).Name + "_")).ToArray();
            if (((CheckBox)sender).IsChecked == true)
            {
                for (int ii = 0; ii < list.Length; ii++)
                {
                    list[ii].IsChecked = true;
                }
            }
            else
            {
                for (int ii = 0; ii < list.Length; ii++)
                {
                    list[ii].IsChecked = false;
                }
            }
        }
        /// <summary>
        /// Function to Uncheck CheckboxAll if one Checkbox_? is unchecked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBoxChangedServer(object sender, RoutedEventArgs e)
        {
            string strCheckBoxName = (sender as CheckBox).Name.Split('_')[0];
            var list = GridMainWindow.Children.OfType<CheckBox>().Where(cb => cb.Name == strCheckBoxName).FirstOrDefault();
            if (list.IsChecked == true)
            {
                list.Unchecked -= CheckBoxChanged;
                list.IsChecked = false;
                list.Unchecked += CheckBoxChanged;
            }
            //if()
        }
        private void SaveSettings(object sender, EventArgs e)
        {
            System.Data.DataTable SaveTable = new System.Data.DataTable("RemoteUpdateServer");
            SaveTable.Columns.Add("Server");
            SaveTable.Columns.Add("Accept");
            SaveTable.Columns.Add("Drivers");
            SaveTable.Columns.Add("Reboot");
            SaveTable.Columns.Add("GUI");
            SaveTable.Columns.Add("Mail");
            SaveTable.Columns.Add("Username");
            SaveTable.Columns.Add("Password");
            SaveTable.Columns.Add("Enabled");
            var tblist = GridMainWindow.Children.OfType<TextBox>().Where(tb => tb.Name.Contains("TextBoxServer_"));
            for (int ii = 0; ii < tblist.Count(); ii++)
            {
                string tmpServername = GridMainWindow.Children.OfType<TextBox>().Where(tb => tb.Name == "TextBoxServer_" + ii).FirstOrDefault().Text;
                if (tmpServername.Length == 0) { continue; }

                System.Data.DataRow dtrow = SaveTable.NewRow();
                dtrow["Server"] = tmpServername;
                dtrow["Accept"] = GridMainWindow.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxAccept_" + ii).FirstOrDefault().IsChecked;
                dtrow["Drivers"] = GridMainWindow.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxDrivers_" + ii).FirstOrDefault().IsChecked;
                dtrow["Reboot"] = GridMainWindow.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxReboot_" + ii).FirstOrDefault().IsChecked;
                dtrow["GUI"] = GridMainWindow.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxGUI_" + ii).FirstOrDefault().IsChecked;
                dtrow["Mail"] = GridMainWindow.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxMail_" + ii).FirstOrDefault().IsChecked;
                dtrow["Username"] = Global.TableRuntime.Rows[ii]["Username"].ToString();
                dtrow["Password"] = Tasks.Encrypt(Global.TableRuntime.Rows[ii]["Password"].ToString());
                dtrow["Enabled"] = GridMainWindow.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxEnabled_" + ii).FirstOrDefault().IsChecked;
                SaveTable.Rows.Add(dtrow);
            }
            Tasks.WriteTableToXML(SaveTable, System.AppDomain.CurrentDomain.BaseDirectory + "RemoteUpdateServer.xml");
            Tasks.WriteTableToXML(Global.TableSettings, System.AppDomain.CurrentDomain.BaseDirectory + "RemoteUpdateSettings.xml");
            SaveTable.Dispose();
        }
        private void CreateTextbox(string tbname, string tbtext, int tbheight, int tbwidth, int tbmarginleft, int tbmargintop)
        {
            TextBox Textbox1 = new TextBox()
            {
                Height = tbheight,
                Width = tbwidth,
                Name = tbname,
                Text = tbtext,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                Margin = new Thickness(tbmarginleft, tbmargintop, 0, 0),
                TextWrapping = System.Windows.TextWrapping.Wrap
            };
            Textbox1.LostFocus += TextboxLostFocus;
            GridMainWindow.Children.Add(Textbox1);
        }
        private void CreateLabel(string lblname, string lblContent, int lblheight, int lblwidth, int lblmarginleft, int lblmargintop, Visibility lblvisibility)
        {
            Label Label1 = new Label()
            {
                Height = lblheight,
                Width = lblwidth,
                Name = lblname,
                Content = lblContent,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                Margin = new Thickness(lblmarginleft, lblmargintop, 0, 0),
                Visibility = lblvisibility,
            };
            GridMainWindow.Children.Add(Label1);
        }
        private void CreateCheckbox(string cbname, int cbmarginleft, int cbmargintop, bool cbchecked)
        {
            CheckBox CheckBox1 = new CheckBox()
            {
                Name = cbname,
                IsChecked = cbchecked,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                Margin = new Thickness(cbmarginleft, cbmargintop, 0, 0),
            };
            if(cbname.StartsWith("CheckboxAccept_", StringComparison.Ordinal) || cbname.StartsWith("CheckboxGUI_", StringComparison.Ordinal))
            {
                CheckBox1.Checked += CheckboxChangedGUIAccept;
                CheckBox1.Unchecked += CheckboxChangedGUIAccept;
            } else
            {
                CheckBox1.Unchecked += CheckBoxChangedServer;
            }
            GridMainWindow.Children.Add(CheckBox1);
        }
        private void CreateButton(string btnname, string btntext, int btnwidth, int btnmarginleft, int rtmargintop, RoutedEventHandler btnevent, System.Windows.Visibility btnvisibility)
        {
            Button Button1 = new Button()
            {
                Name = btnname,
                Width = btnwidth,
                Content = btntext,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                Margin = new Thickness(btnmarginleft, rtmargintop, 0, 0),
                Visibility = btnvisibility
            };
            Button1.Click += btnevent;
            GridMainWindow.Children.Add(Button1);
        }
        private void CreateRectangle(string rtname, int rtheight, double rtwidth, int rtmarginleft, int rtmargintop, SolidColorBrush rtfill, int rtstroke)
        {
            Rectangle Rectangle1 = new Rectangle()
            {
                Name = rtname,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                Height = rtheight,
                Width = rtwidth,
                Margin = new Thickness(rtmarginleft, rtmargintop, 0, 0),
                Fill = rtfill,
                StrokeThickness = rtstroke,
            };
            Panel.SetZIndex(Rectangle1, -3);
            GridMainWindow.Children.Add(Rectangle1);
        }
        /// <summary>
        /// Event Function that calls two functions for Textbox LostFocus Handling
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextboxLostFocus(object sender, RoutedEventArgs e)
        {
            int line = Int32.Parse((sender as TextBox).Name.Split('_')[1], Global.cultures);
            Global.TableRuntime.Rows[line]["IP"] = Tasks.GetIPfromHostname((sender as TextBox).Text);
            Global.TableRuntime.Rows[line]["Servername"] = (sender as TextBox).Text;
            CreateNewLine(sender, e);
        }
        /// <summary>
        /// Creates a new Line of Controls when it is the last one
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreateNewLine(object sender, RoutedEventArgs e)
        {
            TextBox tborigin = sender as TextBox;
            if (tborigin.Text.Length == 0) { return; }
            var list = GridMainWindow.Children.OfType<TextBox>().Where(tb => tb.Name.Contains((sender as TextBox).Name.Split('_')[0])).ToArray();
            // Compare the sender and the last element if the Textbox Name is the same
            if (tborigin.Name == list[list.Length - 1].Name) {
                // Textbox Servername creation
                CreateTextbox("TextBoxServer_" + list.Length, "", 18, 120, 20, 30 * (list.Length + 1));
                // Uptime Label creation
                CreateLabel("LabelUptime_" + list.Length, "", 26, 90, 150, 30 * (list.Length + 1) - 4, Visibility.Visible);
                // Accept Checkbox creation
                CreateCheckbox("CheckboxAccept_" + list.Length, 250, 30 * (list.Length + 1), false);
                // Drivers Checkbox creation
                CreateCheckbox("CheckboxDrivers_" + list.Length, 310, 30 * (list.Length + 1), false);
                // Reboot Checkbox creation
                CreateCheckbox("CheckboxReboot_" + list.Length, 370, 30 * (list.Length + 1), false);
                // GUI Checkbox creation
                CreateCheckbox("CheckboxGUI_" + list.Length, 430, 30 * (list.Length + 1), true);
                // Mail Checkbox creation
                CreateCheckbox("CheckboxMail_" + list.Length, 490, 30 * (list.Length + 1), false);
                // Credentials Button creation
                CreateButton("ButtonCredentials_" + list.Length, "Credentials", 70, 530, 30 * ((list.Length + 1) - 1) + 29, new RoutedEventHandler(GetCredentials), System.Windows.Visibility.Visible);
                // Start Button creation
                CreateButton("ButtonStart_" + list.Length, "Start", 70, 620, 30 * ((list.Length + 1) - 1) + 29, new RoutedEventHandler(ButtonStart_Click), System.Windows.Visibility.Visible);
                // Time Button creation
                CreateButton("ButtonTime_" + list.Length, "12:12:12", 70, 620, 30 * ((list.Length + 1) - 1) + 29, new RoutedEventHandler(ButtonTime_Click), System.Windows.Visibility.Hidden);
                // Enabled Checkbox creation
                CreateCheckbox("CheckboxEnabled_" + list.Length, 710, 30 * (list.Length + 1), false);
                if ((list.Length + 1) % 2 == 0)
                {
                    // Light Grey Rectangle creation
                    CreateRectangle("BackgroundRectangle_" + list.Length, 30, double.NaN, 0, 24 + 30 * list.Length, new SolidColorBrush(Color.FromRgb(222, 217, 217)), 0);
                }
                // Create BackgroundWorker (Ping and Uptime)
                Worker.CreateBackgroundWorker(list.Length);
                if (list.Length >= 3)
                {
                    Application.Current.MainWindow.Height = 130 + list.Length * 30;
                }

                System.Data.DataRow dtrow = Global.TableRuntime.NewRow();
                dtrow["Servername"] = "";
                dtrow["IP"] = "";
                dtrow["Username"] = "";
                dtrow["Password"] = "";
                Global.TableRuntime.Rows.Add(dtrow);

            }
        }
        private void GetCredentials(object sender, RoutedEventArgs e)
        {
            int iLabelID = Int32.Parse((sender as Button).Name.Split('_')[1], Global.cultures);
            string tmpServer = Global.TableRuntime.Rows[iLabelID]["Servername"].ToString().ToUpper(Global.cultures);
            Credentials AskCred = new Credentials(iLabelID);
            AskCred.Title = tmpServer + " Credentials";
            AskCred.ShowDialog();
        }
        private void StartUpdate(int line)
        {
            if (Tasks.CheckPSConnection(line))
            {
                Tasks.OpenPowerShell(line, GridMainWindow);
            }
            else
            {
                if (Tasks.CreatePSVirtualAccount(line))
                {
                    Tasks.OpenPowerShell(line, GridMainWindow);
                }
                else
                {
                    ThreadPool.QueueUserWorkItem(delegate { MessageBox.Show("Can't create the Powershell Virtual Account on server " + Global.TableRuntime.Rows[line]["Servername"].ToString().ToUpper(Global.cultures) + ".\nPlease check your credentials or firewall settings."); });
                    //MessageBox.Show("Can't create the Powershell Virtual Account on server " + Global.TableRuntime.Rows[line]["Servername"].ToString() + ".\nPlease check your credentials or firewall settings.");
                }
            }
        }
        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            int line = Int32.Parse((sender as Button).Name.Split('_')[1], Global.cultures);
            StartUpdate(line);
        }
        private void ButtonTime_Click(object sender, RoutedEventArgs e)
        {
            int line = Int32.Parse((sender as Button).Name.Split('_')[1], Global.cultures);
            GridMainWindow.Children.OfType<Button>().Where(btn => btn.Name.Equals("ButtonTime_" + line.ToString(Global.cultures), StringComparison.Ordinal)).FirstOrDefault().Visibility = System.Windows.Visibility.Hidden;
            GridMainWindow.Children.OfType<Button>().Where(btn => btn.Name.Equals("ButtonStart_" + line.ToString(Global.cultures), StringComparison.Ordinal)).FirstOrDefault().Visibility = System.Windows.Visibility.Visible;
        }
        private void ButtonStartAll_Click(object sender, RoutedEventArgs e)
        {
            for (int ii = 0; ii < Global.TableRuntime.Rows.Count; ii++) {
                if ((bool)GridMainWindow.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxEnabled_" + ii.ToString(Global.cultures)).FirstOrDefault().IsChecked) {
                    if (Global.TableRuntime.Rows[ii]["Servername"].ToString().Length != 0 && Global.TableRuntime.Rows[ii]["IP"].ToString().Length != 0)
                    {
                        StartUpdate(ii);
                    }
                }
            }
        }
        private void ButtonSettings_Click(object sender, RoutedEventArgs e)
        {
            RemoteUpdate.Settings ShowSettings = new RemoteUpdate.Settings();
            ShowSettings.ShowDialog();
        }
        private void ButtonAbout_Click(object sender, RoutedEventArgs e)
        {
            RemoteUpdate.About ShowAbout = new RemoteUpdate.About(Global.TableSettings.Rows[0]["PSVirtualAccountName"].ToString());
            ShowAbout.ShowDialog();
        }

        private void CheckboxChangedGUIAccept(object sender, RoutedEventArgs e)
        {
            string strCheckBoxName = (sender as CheckBox).Name.Split('_')[0];
            string line = (sender as CheckBox).Name.Split('_')[1];
            bool bCheckBoxState = (bool)(sender as CheckBox).IsChecked;
            // If CheckboxAccept and Unchecked
            if (strCheckBoxName == "CheckboxAccept" && !bCheckBoxState)
            {
                GridMainWindow.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxGUI_" + line).FirstOrDefault().IsChecked = true;
                CheckBoxChangedServer(sender, e);
            }
            // If CheckboxGUI and Unchecked
            else if (strCheckBoxName == "CheckboxGUI" && !bCheckBoxState)
            {
                GridMainWindow.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxAccept_" + line).FirstOrDefault().IsChecked = true;
                CheckBoxChangedServer(sender, e);
                // If CheckboxGUI and Checked
            }
        }
    }
}