using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

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
            if (!Tasks.CreateLogFile())
            {
                this.ButtonSave.IsEnabled = false;
                MessageBox.Show("Directory is not writable therefore no settings can be saved or log file can be written");
            }
            // Global.TableScripts Columns Creation
            Global.TableScripts.Columns.Add("Name");
            Global.TableScripts.Columns.Add("Script");
            // Global.TableRuntime Columns Creation
            Global.TableRuntime.Columns.Add("Servername");
            Global.TableRuntime.Columns.Add("IP");
            Global.TableRuntime.Columns.Add("Username");
            Global.TableRuntime.Columns.Add("Password");
            Global.TableRuntime.Columns.Add("Ping");
            Global.TableRuntime.Columns.Add("Uptime");
            Global.TableRuntime.Columns.Add("PID");
            // Get Grid to add more Controls
            GridMainWindow = this.Content as Grid;
            // Load Schema and Data from XML RemoteUpdateSettings.xml
            if (!Tasks.ReadXMLToTable(AppDomain.CurrentDomain.BaseDirectory + "RemoteUpdateSettings.xml", Global.TableSettings))
            {
                Global.TableSettings.Columns.Add("SMTPServer");
                Global.TableSettings.Columns.Add("SMTPPort");
                Global.TableSettings.Columns.Add("MailFrom");
                Global.TableSettings.Columns.Add("MailTo");
                Global.TableSettings.Columns.Add("PSVirtualAccountName");
                Global.TableSettings.Columns.Add("PSWUCommands");
                Global.TableSettings.Columns.Add("VerboseLog");
                Global.TableSettings.Rows.Add(Global.TableSettings.NewRow());
                Global.TableSettings.Rows[0]["PSVirtualAccountName"] = "VirtualAccount";
            }
            // Set Verbose Logging if it is true in Settings file
            bool bVerboseLog = bool.TryParse(Global.TableSettings.Rows[0]["VerboseLog"].ToString(), out bool bParse);
            if (bParse)
            {
                Global.bVerboseLog = bVerboseLog;
            }
            // Initialize Datatable for XML Load
            System.Data.DataTable LoadTable = new System.Data.DataTable();
            // Initialize Servernumber
            int ServerNumber;
            // Load Schema and Data from XML RemoteUpdateServer.xml
            bool bReadXML = Tasks.ReadXMLToTable(AppDomain.CurrentDomain.BaseDirectory + "RemoteUpdateServer.xml", LoadTable);
            // Only if Load of XML was successfull and RowCount is at least 1
            if (bReadXML == true && LoadTable.Rows.Count > 0)
            {
                // Temporary bool if Encryption Password is needed
                bool bPassword = false;
                // Check if in any Row is a saved Password, if yes, ask for the Password to encrypt
                for (int ii = 0; ii < LoadTable.Rows.Count; ii++)
                {
                    if (LoadTable.Rows[ii]["Password"].ToString().Length > 0)
                    {
                        bPassword = true;
                        break;
                    }
                }
                if (bPassword)
                {
                    GetPassword(false, out Global.strDecryptionPassword);
                }

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
                dtrow["Password"] = Tasks.Decrypt(LoadTable.Rows[0]["Password"].ToString(), LoadTable.Rows[0]["Server"].ToString());
                dtrow["Ping"] = "";
                dtrow["Uptime"] = "";
                Global.TableRuntime.Rows.Add(dtrow);
                Tasks.WriteLogFile(0, "Row 1 filled with values", true);
            }
            else
            {
                // If no XML could be read, set the Servernumber to 0
                ServerNumber = 0;
                // Create Empty Data Row
                Global.TableRuntime.Rows.Add(Global.TableRuntime.NewRow());
                Tasks.WriteLogFile(0, "Empty row 1 created", true);
            }
            // Create BackgroundWorker (Ping and Uptime)
            Worker.CreateBackgroundWorker(0);
            // Change Height of Main Window when more than 10 Servers are in the List
            if (ServerNumber > 2) { Application.Current.MainWindow.Height = 170 + ServerNumber * 30; }
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
                CreateButton("ButtonStart_" + ii, "Update", 70, 620, 30 * ((ii + 1) - 1) + 29, new RoutedEventHandler(ButtonStartClick), System.Windows.Visibility.Visible);
                // Time Button creation
                CreateButton("ButtonTime_" + ii, "12:12:12", 70, 620, 30 * ((ii + 1) - 1) + 29, new RoutedEventHandler(ButtonTime_Click), System.Windows.Visibility.Hidden);
                // ComboBox creation
                CreateComboBox("ComboBox_" + ii, 620, 30 * ((ii + 1) - 1) + 29, new SelectionChangedEventHandler(ComboBox_SelectionChanged));
                // Enabled Checkbox creation
                if (ii < ServerNumber) { tmpBool = Convert.ToBoolean(LoadTable.Rows[ii]["Enabled"], Global.cultures); }
                CreateCheckbox("CheckboxEnabled_" + ii, 740, 30 * (ii + 1), tmpBool);
                // If Servernumber is Even, create Light Grey Rectangle Background
                if ((ii + 1) % 2 == 0)
                {
                    // Light Grey Rectangle creation
                    CreateRectangle("BackgroundRectangle_" + ii, 30, double.NaN, 0, 24 + 30 * ii, new SolidColorBrush(Color.FromRgb(222, 217, 217)), 0);
                    CreateGifImage("gifImage_" + ii, 710, 30 * ((ii + 1) - 1) + 29);
                }
                else
                {
                    CreateGifImage("gifImage_" + ii, 710, 30 * ((ii + 1) - 1) + 29);
                }
                // Create new Row in TableRuntime
                System.Data.DataRow dtrow = Global.TableRuntime.NewRow();
                if (ii < LoadTable.Rows.Count)
                {
                    dtrow["Servername"] = LoadTable.Rows[ii]["Server"].ToString();
                    dtrow["IP"] = Tasks.GetIPfromHostname(LoadTable.Rows[ii]["Server"].ToString());
                    dtrow["Username"] = LoadTable.Rows[ii]["Username"].ToString();
                    dtrow["Password"] = Tasks.Decrypt(LoadTable.Rows[ii]["Password"].ToString(), LoadTable.Rows[ii]["Server"].ToString());
                    dtrow["Ping"] = "";
                    dtrow["Uptime"] = "";
                }
                Global.TableRuntime.Rows.Add(dtrow);
                // Write Verbose Log
                Tasks.WriteLogFile(0, "Row " + (ii + 1) + " filled with values", true);
                // Create BackgroundWorker (Ping and Uptime)
                Worker.CreateBackgroundWorker(ii);
            }
            LoadTable.Dispose();
            // Timer Creation for Interface Updates
            Global.TimerUpdateGrid.Interval = TimeSpan.FromSeconds(5);
            Global.TimerUpdateGrid.Tick += (sender, e) => { Worker.TimerUpdateGrid_Tick(GridMainWindow); };
            Global.TimerUpdateGrid.Start();
            Tasks.WriteLogFile(0, "Timer for Grid Update created", true);
            // Check WinRM Status and write Info Message into Label
            SetWinRMStatus();
            Worker.CreateBackgroundWorkerProcess();
            new System.Threading.Tasks.Task(Tasks.CheckLatestVersion).Start();
            // Add Items to first and last Combobox
            ComboBox_0.ItemsSource = new System.Collections.Generic.List<string> { "Update", "Pending", "Reboot", "Script" };
            ComboBox_All.ItemsSource = new System.Collections.Generic.List<string> { "Update All", "Pending All", "Reboot All", "Script All" };
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
        }
        private void SaveSettings(object sender, EventArgs e)
        {
            bool bIsCredential = false;
            using (System.Data.DataTable SaveTable = new System.Data.DataTable("RemoteUpdateServer"))
            {
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
                // Check if Credentials are there, if yes, then set bool to true
                for (int ii = 0; ii < tblist.Count(); ii++)
                {
                    string tmpPassword = Global.TableRuntime.Rows[ii]["Password"].ToString();
                    if (tmpPassword.Length > 0)
                    {
                        bIsCredential = true;
                        break;
                    }
                }
                // If bIsCredential is true, ask for password with which it should be saved
                string strEncryptionPassword = "";
                if (bIsCredential)
                {
                    if (!GetPassword(true, out strEncryptionPassword))
                    {
                        return;
                    }
                }
                // Create DataTable to write it to XML
                for (int ii = 0; ii < tblist.Count(); ii++)
                {
                    string tmpServername = GridMainWindow.Children.OfType<TextBox>().Where(tb => tb.Name == "TextBoxServer_" + ii).FirstOrDefault().Text;
                    string tmpPassword = Global.TableRuntime.Rows[ii]["Password"].ToString();
                    if (tmpServername.Length == 0) { continue; }
                    System.Data.DataRow dtrow = SaveTable.NewRow();
                    dtrow["Server"] = tmpServername;
                    dtrow["Accept"] = GridMainWindow.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxAccept_" + ii).FirstOrDefault().IsChecked;
                    dtrow["Drivers"] = GridMainWindow.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxDrivers_" + ii).FirstOrDefault().IsChecked;
                    dtrow["Reboot"] = GridMainWindow.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxReboot_" + ii).FirstOrDefault().IsChecked;
                    dtrow["GUI"] = GridMainWindow.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxGUI_" + ii).FirstOrDefault().IsChecked;
                    dtrow["Mail"] = GridMainWindow.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxMail_" + ii).FirstOrDefault().IsChecked;
                    dtrow["Username"] = Global.TableRuntime.Rows[ii]["Username"].ToString();
                    if (strEncryptionPassword.Length > 0)
                    {
                        dtrow["Password"] = Tasks.Encrypt(Global.TableRuntime.Rows[ii]["Password"].ToString(), tmpServername, strEncryptionPassword);
                    }
                    else
                    {
                        dtrow["Password"] = "";
                    }
                    dtrow["Enabled"] = GridMainWindow.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxEnabled_" + ii).FirstOrDefault().IsChecked;
                    SaveTable.Rows.Add(dtrow);
                }
                Tasks.WriteTableToXML(SaveTable, System.AppDomain.CurrentDomain.BaseDirectory + "RemoteUpdateServer.xml");
            }
            Tasks.WriteTableToXML(Global.TableSettings, System.AppDomain.CurrentDomain.BaseDirectory + "RemoteUpdateSettings.xml");
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
                TextWrapping = System.Windows.TextWrapping.NoWrap
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
            if (cbname.StartsWith("CheckboxAccept_", StringComparison.Ordinal) || cbname.StartsWith("CheckboxGUI_", StringComparison.Ordinal))
            {
                CheckBox1.Checked += CheckboxChangedGUIAccept;
                CheckBox1.Unchecked += CheckboxChangedGUIAccept;
            }
            else
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
        private void CreateGifImage(string strname, int imarginleft, int imargintop)
        {
            GifImage GifImage1 = new GifImage()
            {
                Name = strname,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                Width = 20,
                Height = 20,
                Margin = new Thickness(imarginleft, imargintop, 0, 0),
                AutoStart = false,
                Visibility = Visibility.Hidden
            };
            GridMainWindow.Children.Add(GifImage1);
        }
        private void CreateComboBox(string strname, int imarginleft, int imargintop, SelectionChangedEventHandler btnevent)
        {
            ComboBox ComboBox1 = new ComboBox()
            {
                Name = strname,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                Width = 88,
                Height = 20,
                Margin = new Thickness(imarginleft, imargintop, 0, 0)
                // Panel.ZIndex="-1"
            };
            Panel.SetZIndex(ComboBox1, -1);
            ComboBox1.ItemsSource = new System.Collections.Generic.List<string> { "Update", "Pending", "Reboot", "Script" };
            ComboBox1.SelectionChanged += btnevent;
            GridMainWindow.Children.Add(ComboBox1);
        }
        /// <summary>
        /// Event Function that calls two functions for Textbox LostFocus Handling
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextboxLostFocus(object sender, RoutedEventArgs e)
        {
            int line = Int32.Parse((sender as TextBox).Name.Split('_')[1], Global.cultures);
            (sender as TextBox).Text = (sender as TextBox).Text.Trim();
            Tasks.LockAndWriteDataTable(Global.TableRuntime, line, "IP", Tasks.GetIPfromHostname((sender as TextBox).Text), 100);
            //Global.TableRuntime.Rows[line]["IP"] = Tasks.GetIPfromHostname((sender as TextBox).Text);
            Tasks.LockAndWriteDataTable(Global.TableRuntime, line, "Servername", (sender as TextBox).Text, 100);
            //Global.TableRuntime.Rows[line]["Servername"] = (sender as TextBox).Text;
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
            if (tborigin.Name == list[list.Length - 1].Name)
            {
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
                CreateButton("ButtonStart_" + list.Length, "Update", 70, 620, 30 * ((list.Length + 1) - 1) + 29, new RoutedEventHandler(ButtonStartClick), System.Windows.Visibility.Visible);
                // Time Button creation
                CreateButton("ButtonTime_" + list.Length, "12:12:12", 70, 620, 30 * ((list.Length + 1) - 1) + 29, new RoutedEventHandler(ButtonTime_Click), System.Windows.Visibility.Hidden);
                // ComboBox creation
                CreateComboBox("ComboBox_" + list.Length, 620, 30 * ((list.Length + 1) - 1) + 29, new SelectionChangedEventHandler(ComboBox_SelectionChanged));
                // Enabled Checkbox creation
                CreateCheckbox("CheckboxEnabled_" + list.Length, 740, 30 * (list.Length + 1), false);
                if ((list.Length + 1) % 2 == 0)
                {
                    // Light Grey Rectangle creation
                    CreateRectangle("BackgroundRectangle_" + list.Length, 30, double.NaN, 0, 24 + 30 * list.Length, new SolidColorBrush(Color.FromRgb(222, 217, 217)), 0);
                    CreateGifImage("gifImage_" + list.Length, 710, 30 * ((list.Length + 1) - 1) + 29);
                }
                else
                {
                    CreateGifImage("gifImage_" + list.Length, 710, 30 * ((list.Length + 1) - 1) + 29);
                }
                Worker.CreateBackgroundWorker(list.Length);
                if (list.Length >= 3)
                {
                    Application.Current.MainWindow.Height = 170 + list.Length * 30;
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
        private bool GetPassword(bool bEncrypt, out string strCryptPassword)
        {
            Password AskPassword = new Password(bEncrypt);
            if ((bool)AskPassword.ShowDialog())
            {
                strCryptPassword = AskPassword.PasswordBoxPassword.Password.ToString(Global.cultures);
                return true;
            }
            else
            {
                strCryptPassword = "";
                return false;
            }
        }
        private void StartUpdate(int line)
        {
            if (Tasks.CheckPSConnection(line))
            {
                Tasks.OpenPowerShellUpdate(line, GridMainWindow);
            }
            else
            {
                string strFailureMessage;
                if (Tasks.CreatePSConnectionPrerequisites(line, out strFailureMessage))
                {
                    Tasks.OpenPowerShellUpdate(line, GridMainWindow);
                }
                else
                {
                    ThreadPool.QueueUserWorkItem(delegate { MessageBox.Show("Can't connect to server " + Global.TableRuntime.Rows[line]["Servername"].ToString().ToUpper(Global.cultures) + ".\nPlease check your credentials, firewall ruleset and the WinRM settings."); });
                    Tasks.LockAndWriteDataTable(Global.TableRuntime, line, "PID", "error", 100);
                    Tasks.UpdateStatusGUI(line, "error", GridMainWindow);
                }
            }
        }
        private void StartPending(int line)
        {
            if (Tasks.CheckPSConnection(line))
            {
                Tasks.AskPendingStatus(line, GridMainWindow);
            }
            else
            {
                string strFailureMessage;
                if (Tasks.CreatePSConnectionPrerequisites(line, out strFailureMessage))
                {
                    Tasks.AskPendingStatus(line, GridMainWindow);
                }
                else
                {
                    ThreadPool.QueueUserWorkItem(delegate { MessageBox.Show("Can't connect to server " + Global.TableRuntime.Rows[line]["Servername"].ToString().ToUpper(Global.cultures) + ".\nPlease check your credentials, firewall ruleset and the WinRM settings."); });
                    Tasks.UpdateStatusGUI(line, "error", GridMainWindow);
                }
            }
        }

        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            int line = Int32.Parse((sender as Button).Name.Split('_')[1], Global.cultures);
            ButtonClicked(line);
        }
        private void ButtonTime_Click(object sender, RoutedEventArgs e)
        {
            HideTime(Int32.Parse((sender as Button).Name.Split('_')[1], Global.cultures));
        }

        private void ButtonStartAll_Click(object sender, RoutedEventArgs e)
        {
            string btnContent = GridMainWindow.Children.OfType<Button>().Where(btn => btn.Name == "ButtonStart").FirstOrDefault().Content.ToString();
            btnContent = btnContent.Substring(0, btnContent.Length - 4);
            for (int ii = 0; ii < Global.TableRuntime.Rows.Count; ii++)
            {
                if ((bool)GridMainWindow.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxEnabled_" + ii.ToString(Global.cultures)).FirstOrDefault().IsChecked)
                {
                    if (Global.TableRuntime.Rows[ii]["Servername"].ToString().Length != 0 && Global.TableRuntime.Rows[ii]["IP"].ToString().Length != 0)
                    {
                        ButtonClicked(ii, btnContent);
                    }
                }
            }
        }
        private void ButtonStartClick(object sender, RoutedEventArgs e)
        {
            string strButtonLine = (sender as Button).Name.Split('_')[1];
            string strButtonFunction = (sender as Button).Content.ToString().Split(' ')[0];
            string strScriptBlock = "";
            if (strButtonFunction == "Script")
            {
                PSScript GetScript = new PSScript();
                GetScript.ShowDialog();
                if ((bool)GetScript.DialogResult)
                {
                    strScriptBlock = GetScript.TextBoxScript.Text;
                }
                else
                {
                    return;
                }
            }
            if (strButtonLine == "All")
            {
                for (int ii = 0; ii < Global.TableRuntime.Rows.Count; ii++)
                {
                    if ((bool)GridMainWindow.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxEnabled_" + ii.ToString(Global.cultures)).FirstOrDefault().IsChecked)
                    {
                        if (Global.TableRuntime.Rows[ii]["Servername"].ToString().Length != 0 && Global.TableRuntime.Rows[ii]["IP"].ToString().Length != 0)
                        {
                            ButtonClicked(ii, strButtonFunction, strScriptBlock);
                        }
                    }
                }
            }
            else
            {
                ButtonClicked(Int32.Parse(strButtonLine, Global.cultures), strButtonFunction, strScriptBlock);
            }
        }
        private void ButtonClicked(int line)
        {
            string btnContent = GridMainWindow.Children.OfType<Button>().Where(btn => btn.Name.Equals("ButtonStart_" + line.ToString(Global.cultures), StringComparison.Ordinal)).FirstOrDefault().Content.ToString();
            ButtonClicked(line, btnContent, "");
        }
        private void ButtonClicked(int line, string btnContent)
        {
            ButtonClicked(line, btnContent, "");

        }
        private void ButtonClicked(int line, string btnContent, string strScript)
        {
            string strTmpServername = Global.TableRuntime.Rows[line]["Servername"].ToString().ToUpper(Global.cultures);
            if (strTmpServername.Length == 0)
            {
                return;
            }
            switch (btnContent)
            {
                case "Update":
                    StartUpdate(line);
                    return;
                case "Pending":
                    StartPending(line);
                    return;
                case "Reboot":
                    Tasks.StartReboot(line);
                    return;
                case "Script":
                    Tasks.OpenPowerShellScript(line, GridMainWindow, strScript);
                    return;
                default:
                    return;
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
        private void ButtonFixIt_Click(object sender, RoutedEventArgs e)
        {
            Fixit ShowFixit = new Fixit();
            ShowFixit.ShowDialog();
            if ((bool)ShowFixit.DialogResult)
            {
                if (Tasks.IsAdministrator())
                {
                    if ((bool)ShowFixit.WinRMServiceStartupType.IsChecked)
                    {
                        Tasks.SetServiceStartup("winrm", "auto");
                    }
                    if ((bool)ShowFixit.WinRMServiceStart.IsChecked)
                    {
                        Tasks.StartService("winrm");
                    }
                    if ((bool)ShowFixit.WinRMTrustedHosts.IsChecked)
                    {
                        Tasks.SetTrustedHosts("*");
                    }
                    SetWinRMStatus();
                }
                else
                {
                    string strArguments = "";
                    if ((bool)ShowFixit.WinRMServiceStartupType.IsChecked)
                    {
                        strArguments += "WinRMService ";
                    }
                    if ((bool)ShowFixit.WinRMServiceStart.IsChecked)
                    {
                        strArguments += "WinRMStart ";
                    }
                    if ((bool)ShowFixit.WinRMTrustedHosts.IsChecked)
                    {
                        strArguments += "TrustedHosts";
                    }
                    if (strArguments.Length > 0)
                    {
                        Tasks.Elevate(strArguments);
                        SetWinRMStatus();
                    }
                }
            }
        }
        private void SetWinRMStatus()
        {
            if (Tasks.CheckWinRMStatus(out string strMessage))
            {
                this.TextboxInfoMessage.Text = strMessage;
                ButtonFixIt.Visibility = Visibility.Hidden;
                TextboxInfoMessage.Margin = new Thickness(24, 0, 315, 0);
            }
            else
            {
                this.TextboxInfoMessage.Text = strMessage;
            }
        }
        private void LabelUpdate_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Tasks.UpdateRemoteUpdate();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string strline = (sender as ComboBox).Name.Split('_')[1];
            if (strline == "All")
            {
                ButtonStart_All.Content = (sender as ComboBox).SelectedItem.ToString();
            }
            else
            {
                int line = Int32.Parse(strline, Global.cultures);
                GridMainWindow.Children.OfType<Button>().Where(btn => btn.Name.Equals("ButtonStart_" + line.ToString(Global.cultures), StringComparison.Ordinal)).FirstOrDefault().Content = (sender as ComboBox).SelectedItem.ToString();
                HideTime(line);
            }
        }

        private void HideTime(int line)
        {
            GridMainWindow.Children.OfType<Button>().Where(btn => btn.Name.Equals("ButtonTime_" + line.ToString(Global.cultures), StringComparison.Ordinal)).FirstOrDefault().Visibility = System.Windows.Visibility.Hidden;
            GridMainWindow.Children.OfType<Button>().Where(btn => btn.Name.Equals("ButtonStart_" + line.ToString(Global.cultures), StringComparison.Ordinal)).FirstOrDefault().Visibility = System.Windows.Visibility.Visible;
            GridMainWindow.Children.OfType<GifImage>().Where(gif => gif.Name.Equals("gifImage_" + line.ToString(Global.cultures), StringComparison.Ordinal)).FirstOrDefault().Visibility = System.Windows.Visibility.Hidden;
        }

        private void RemoteUpdate_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Check if actual table is different than original file
            using (System.Data.DataTable LoadTable = new System.Data.DataTable())
            {
                // Load Data from XML RemoteUpdateServer.xml
                bool bReadXML = Tasks.ReadXMLToTable(AppDomain.CurrentDomain.BaseDirectory + "RemoteUpdateServer.xml", LoadTable);
                // Check if XML read is true and if the rowscount is greater than 0 otherwise exit RemoteUpdate
                if (bReadXML == true && LoadTable.Rows.Count > 0)
                {
                    // temporary bool for status if the values have changed from the xml
                    bool bIsChanged = false;
                    // Check if the row counts are the same. if not something has changed for sure and therefore set the bIsChanged to true
                    if (LoadTable.Rows.Count != Global.TableRuntime.Rows.Count - 1)
                    {
                        bIsChanged = true;
                        // otherwise check each field
                    }
                    else
                    {
                        for (int ii = 0; ii < LoadTable.Rows.Count; ii++)
                        {
                            // Check Servername
                            if (LoadTable.Rows[ii]["Server"].ToString() != Global.TableRuntime.Rows[ii]["Servername"].ToString())
                            {
                                bIsChanged = true;
                                break;
                            }
                            // Check Username
                            if (LoadTable.Rows[ii]["Username"].ToString() != Global.TableRuntime.Rows[ii]["Username"].ToString())
                            {
                                bIsChanged = true;
                                break;
                            }
                            // Check Password
                            if (Tasks.Decrypt(LoadTable.Rows[ii]["Password"].ToString(), LoadTable.Rows[ii]["Server"].ToString()) != Global.TableRuntime.Rows[ii]["Password"].ToString())
                            {
                                bIsChanged = true;
                                break;
                            }
                            // Check AcceptAll Checkbox
                            if (LoadTable.Rows[ii]["Accept"].ToString() != GridMainWindow.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxAccept_" + ii).FirstOrDefault().IsChecked.ToString())
                            {
                                bIsChanged = true;
                                break;
                            }
                            // Check Drivers Checkbox
                            if (LoadTable.Rows[ii]["Drivers"].ToString() != GridMainWindow.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxDrivers_" + ii).FirstOrDefault().IsChecked.ToString())
                            {
                                bIsChanged = true;
                                break;
                            }
                            // Check Reboot Checkbox
                            if (LoadTable.Rows[ii]["Reboot"].ToString() != GridMainWindow.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxReboot_" + ii).FirstOrDefault().IsChecked.ToString())
                            {
                                bIsChanged = true;
                                break;
                            }
                            // Check GUI Checkbox
                            if (LoadTable.Rows[ii]["GUI"].ToString() != GridMainWindow.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxGUI_" + ii).FirstOrDefault().IsChecked.ToString())
                            {
                                bIsChanged = true;
                                break;
                            }
                            // Check Mail Checkbox
                            if (LoadTable.Rows[ii]["Mail"].ToString() != GridMainWindow.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxMail_" + ii).FirstOrDefault().IsChecked.ToString())
                            {
                                bIsChanged = true;
                                break;
                            }
                            // Check Enabled Checkbox
                            if (LoadTable.Rows[ii]["Enabled"].ToString() != GridMainWindow.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxEnabled_" + ii).FirstOrDefault().IsChecked.ToString())
                            {
                                bIsChanged = true;
                                break;
                            }
                        }
                    }
                    // If Result is different ask if you want to save
                    if (bIsChanged)
                    {
                        MessageBoxResult dialogResult = MessageBox.Show("There are unsaved changes. Do you want to save them?", "Unsaved changes", System.Windows.MessageBoxButton.YesNoCancel);
                        if (dialogResult == MessageBoxResult.Yes)
                        {
                            SaveSettings(sender, e);
                        }
                        else if (dialogResult == MessageBoxResult.Cancel)
                        {
                            e.Cancel = true;
                            return;
                        }
                    }
                }
                else
                {   // close RemoteUpdate
                    return;
                }
            }
        }
    }
}