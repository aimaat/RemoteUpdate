﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Net.NetworkInformation;
using System.Windows.Threading;
using System.Security.Cryptography;
using System.IO;
using System.Management.Automation.Runspaces;
using System.ComponentModel;
using System.Diagnostics;

namespace RemoteUpdate
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        // BackgroundWorker List for Uptime Checks
        public List<BackgroundWorker> ListBackgroundWorkerUptime = new List<BackgroundWorker>();
        // BackgroundWorker List for Ping Checks
        public List<BackgroundWorker> ListBackgroundWorkerPing = new List<BackgroundWorker>();
        // MainWindow Grid
        public Grid GridMainWindow;
        // Timer for Grid Update
        public DispatcherTimer TimerUpdateGrid = new DispatcherTimer();
        // Table for Runtime Values like Servername, IP, etc.
        //public System.Data.DataTable TableRuntime = new System.Data.DataTable("RuntimeValues");
        // Table for Settings
        //public System.Data.DataTable Global.TableSettings = new System.Data.DataTable("Settings");
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
            try
            {
                // Load Schema and Data from XML RemoteUpdateServer.xml
                LoadTable.ReadXmlSchema(System.AppDomain.CurrentDomain.BaseDirectory + "RemoteUpdateServer.xml");
                LoadTable.ReadXml(System.AppDomain.CurrentDomain.BaseDirectory + "RemoteUpdateServer.xml");
                // Set Servernumber according to Rows from XML
                ServerNumber = LoadTable.Rows.Count;
                // Set Values for first Row
                this.TextBoxServer_0.Text = LoadTable.Rows[0]["Server"].ToString();
                this.CheckboxAccept_0.IsChecked = Convert.ToBoolean(LoadTable.Rows[0]["Accept"]);
                this.CheckboxDrivers_0.IsChecked = Convert.ToBoolean(LoadTable.Rows[0]["Drivers"]);
                this.CheckboxReboot_0.IsChecked = Convert.ToBoolean(LoadTable.Rows[0]["Reboot"]);
                this.CheckboxGUI_0.IsChecked = Convert.ToBoolean(LoadTable.Rows[0]["GUI"]);
                this.CheckboxMail_0.IsChecked = Convert.ToBoolean(LoadTable.Rows[0]["Mail"]);
                this.CheckboxEnabled_0.IsChecked = Convert.ToBoolean(LoadTable.Rows[0]["Enabled"]);
                // Create first DataRow in Global.TableRuntime
                System.Data.DataRow dtrow = Global.TableRuntime.NewRow();
                dtrow["Servername"] = LoadTable.Rows[0]["Server"].ToString();
                dtrow["IP"] = GetIPfromHostname(LoadTable.Rows[0]["Server"].ToString());
                dtrow["Username"] = LoadTable.Rows[0]["Username"].ToString();
                dtrow["Password"] = Decrypt(LoadTable.Rows[0]["Password"].ToString());
                dtrow["Ping"] = "";
                dtrow["Uptime"] = "";
                Global.TableRuntime.Rows.Add(dtrow);
            } catch
            {
                // If no XML could be read, set the Servernumber to 0
                ServerNumber = 0;
                // Create Empty Data Row
                Global.TableRuntime.Rows.Add(Global.TableRuntime.NewRow());
            }
            try
            {
                // Load Schema and Data from XML RemoteUpdateSettings.xml
                Global.TableSettings.ReadXmlSchema(System.AppDomain.CurrentDomain.BaseDirectory + "RemoteUpdateSettings.xml");
                Global.TableSettings.ReadXml(System.AppDomain.CurrentDomain.BaseDirectory + "RemoteUpdateSettings.xml");

            } catch
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
            // Create BackgroundWorker Uptime for Line 0
            CreateBackgroundWorkerUptime(0);
            // Create BackgroundWorker Ping for Line 1
            CreateBackgroundWorkerPing(0);
            // Change Height of Main Window when more than 10 Servers are in the List
            if (ServerNumber > 2) { Application.Current.MainWindow.Height = 130 + ServerNumber * 30; }
            // Add Controls for each Server loaded
            for (int ii = 1; ii < ServerNumber + 1; ii++)
            {
                // ServerName Textbox creation
                string tmpText = "";
                bool tmpBool = false;
                if (ii < ServerNumber) { tmpText = LoadTable.Rows[ii]["Server"].ToString(); }
                // Textbox Servername creation
                CreateTextbox("TextBoxServer_" + ii, tmpText, 18, 120, 20, 30 * (ii + 1));
                // Uptime Label creation
                CreateLabel("LabelUptime_" + ii, "", 26, 90, 150, 30 * (ii + 1) - 4, Visibility.Visible);
                // Accept Checkbox creation
                if (ii < ServerNumber) { tmpBool = Convert.ToBoolean(LoadTable.Rows[ii]["Accept"]); }
                CreateCheckbox("CheckboxAccept_" + ii, 250, 30 * (ii + 1), tmpBool);
                // Drivers Checkbox creation
                if (ii < ServerNumber) { tmpBool = Convert.ToBoolean(LoadTable.Rows[ii]["Drivers"]); }
                CreateCheckbox("CheckboxDrivers_" + ii, 310, 30 * (ii + 1), tmpBool);
                // Reboot Checkbox creation
                if (ii < ServerNumber) { tmpBool = Convert.ToBoolean(LoadTable.Rows[ii]["Reboot"]); }
                CreateCheckbox("CheckboxReboot_" + ii, 370, 30 * (ii + 1), tmpBool);
                // GUI Checkbox creation
                if (ii < ServerNumber) { tmpBool = Convert.ToBoolean(LoadTable.Rows[ii]["GUI"]); }
                CreateCheckbox("CheckboxGUI_" + ii, 430, 30 * (ii + 1), true);
                // Mail Checkbox creation
                if (ii < ServerNumber) { tmpBool = Convert.ToBoolean(LoadTable.Rows[ii]["Mail"]); }
                CreateCheckbox("CheckboxMail_" + ii, 490, 30 * (ii + 1), tmpBool);
                // Credentials Button creation
                CreateButton("ButtonCredentials_" + ii, "Credentials", 70, 530, 30 * ((ii + 1) - 1) + 29, new RoutedEventHandler(GetCredentials), System.Windows.Visibility.Visible);
                // Start Button creation
                CreateButton("ButtonStart_" + ii, "Start", 70, 620, 30 * ((ii + 1) - 1) + 29, new RoutedEventHandler(ButtonStart_Click), System.Windows.Visibility.Visible);
                // Time Button creation
                CreateButton("ButtonTime_" + ii, "12:12:12", 70, 620, 30 * ((ii + 1) - 1) + 29, new RoutedEventHandler(ButtonTime_Click), System.Windows.Visibility.Hidden);
                // Enabled Checkbox creation
                if (ii < ServerNumber) { tmpBool = Convert.ToBoolean(LoadTable.Rows[ii]["Enabled"]); }
                CreateCheckbox("CheckboxEnabled_" + ii, 710, 30 * (ii + 1), tmpBool);
                // If Servernumber is Even, create Light Grey Rectangle Background
                if ((ii + 1) % 2 == 0)
                {
                    // Light Grey Rectangle creation
                    CreateRectangle("BackgroundRectangle_" + ii, 30, double.NaN, 0, 24 + 30 * ii, new SolidColorBrush(Color.FromRgb(222, 217, 217)), 0);
                }
                // Create BackgroundWorker Uptime for Line ii
                CreateBackgroundWorkerUptime(ii);
                // Create BackgroundWorker Ping for Line ii
                CreateBackgroundWorkerPing(ii);
                System.Data.DataRow dtrow = Global.TableRuntime.NewRow();
                if (ii < LoadTable.Rows.Count)
                {
                    dtrow["Servername"] = LoadTable.Rows[ii]["Server"].ToString();
                    dtrow["IP"] = GetIPfromHostname(LoadTable.Rows[ii]["Server"].ToString());
                    dtrow["Username"] = LoadTable.Rows[ii]["Username"].ToString();
                    dtrow["Password"] = Decrypt(LoadTable.Rows[ii]["Password"].ToString());
                    dtrow["Ping"] = "";
                    dtrow["Uptime"] = "";
                }
                Global.TableRuntime.Rows.Add(dtrow);

                // Timer Creation for Interface Update
                TimerUpdateGrid.Interval = TimeSpan.FromSeconds(5);
                TimerUpdateGrid.Tick += (sender, e) => { TimerUpdateGrid_Tick(sender, e); };
                TimerUpdateGrid.Start();
            }
        }

        private void TimerUpdateGrid_Tick(object sender, EventArgs e)
        {

            for (int ii = 0; ii < Global.TableRuntime.Rows.Count; ii++)
            {
                if (Global.TableRuntime.Rows[ii]["Servername"].ToString() == "")
                {
                    GridMainWindow.Children.OfType<TextBox>().Where(tb => tb.Name == "TextBoxServer_" + ii).FirstOrDefault().Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)); // #FFFFFF
                    GridMainWindow.Children.OfType<Label>().Where(lbl => lbl.Name == "LabelUptime_" + ii).FirstOrDefault().Content = "";
                    continue;
                }
                if (Global.TableRuntime.Rows[ii]["IP"].ToString() == "")
                {
                    GridMainWindow.Children.OfType<TextBox>().Where(tb => tb.Name == "TextBoxServer_" + ii).FirstOrDefault().Background = new SolidColorBrush(Color.FromRgb(218, 97, 230)); // 
                    GridMainWindow.Children.OfType<Label>().Where(lbl => lbl.Name == "LabelUptime_" + ii).FirstOrDefault().Content = "no IP";
                    continue;
                }
                if (Global.TableRuntime.Rows[ii]["Ping"].ToString() == "true")
                {
                    GridMainWindow.Children.OfType<TextBox>().Where(tb => tb.Name == "TextBoxServer_" + ii).FirstOrDefault().Background = new SolidColorBrush(Color.FromRgb(121, 255, 164)); // #FF79FFA4
                    GridMainWindow.Children.OfType<Label>().Where(lbl => lbl.Name == "LabelUptime_" + ii).FirstOrDefault().Content = Global.TableRuntime.Rows[ii]["Uptime"].ToString();
                }
                else
                {
                    GridMainWindow.Children.OfType<TextBox>().Where(tb => tb.Name == "TextBoxServer_" + ii).FirstOrDefault().Background = new SolidColorBrush(Color.FromRgb(240, 139, 139)); // #FFF08B8B
                    GridMainWindow.Children.OfType<Label>().Where(lbl => lbl.Name == "LabelUptime_" + ii).FirstOrDefault().Content = Global.TableRuntime.Rows[ii]["Uptime"].ToString();
                }
            }
        }

        /// <summary>
        /// Function to change all Checkboxes IsChecked Status in the same name range
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBoxChanged(object sender, RoutedEventArgs e)
        {
            var ParentGrid = ((FrameworkElement)sender).Parent as Grid;
            var list = ParentGrid.Children.OfType<CheckBox>().Where(cb => cb.Name.Contains((sender as CheckBox).Name + "_")).ToArray();
            if (((CheckBox)sender).IsChecked == true)
            {
                for (int ii = 0; ii < list.Count(); ii++)
                {
                    list[ii].IsChecked = true;
                }
            }
            else
            {
                for (int ii = 0; ii < list.Count(); ii++)
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
            var ParentGrid = ((FrameworkElement)sender).Parent as Grid;
            var list = ParentGrid.Children.OfType<CheckBox>().Where(cb => cb.Name == (sender as CheckBox).Name.Split('_')[0]).FirstOrDefault();
            if (list.IsChecked == true)
            {
                list.Unchecked -= CheckBoxChanged;
                list.IsChecked = false;
                list.Unchecked += CheckBoxChanged;
            }
        }
        private string GetIPfromHostname(string Servername)
        {
            if (Servername == "") { return ""; }
            try
            {
                return System.Net.Dns.GetHostAddresses(Servername).First().ToString();
            }
            catch
            {
                return "";
            }
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
            Grid ParentGrid = ((FrameworkElement)sender).Parent as Grid;
            var tblist = ParentGrid.Children.OfType<TextBox>().Where(tb => tb.Name.Contains("TextBoxServer_"));
            for (int ii = 0; ii < tblist.Count(); ii++)
            {
                string tmpServername = ParentGrid.Children.OfType<TextBox>().Where(tb => tb.Name == "TextBoxServer_" + ii).FirstOrDefault().Text;
                if (tmpServername == "") { continue; }

                System.Data.DataRow dtrow = SaveTable.NewRow();
                dtrow["Server"] = tmpServername;
                dtrow["Accept"] = ParentGrid.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxAccept_" + ii).FirstOrDefault().IsChecked;
                dtrow["Drivers"] = ParentGrid.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxDrivers_" + ii).FirstOrDefault().IsChecked;
                dtrow["Reboot"] = ParentGrid.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxReboot_" + ii).FirstOrDefault().IsChecked;
                dtrow["GUI"] = ParentGrid.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxGUI_" + ii).FirstOrDefault().IsChecked;
                dtrow["Mail"] = ParentGrid.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxMail_" + ii).FirstOrDefault().IsChecked;
                dtrow["Username"] = Global.TableRuntime.Rows[ii]["Username"].ToString();
                dtrow["Password"] = Encrypt(Global.TableRuntime.Rows[ii]["Password"].ToString());
                dtrow["Enabled"] = ParentGrid.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxEnabled_" + ii).FirstOrDefault().IsChecked;
                SaveTable.Rows.Add(dtrow);
            }
            SaveTable.WriteXml(System.AppDomain.CurrentDomain.BaseDirectory + "RemoteUpdateServer.xml", System.Data.XmlWriteMode.WriteSchema);
            Global.TableSettings.WriteXml(System.AppDomain.CurrentDomain.BaseDirectory + "RemoteUpdateSettings.xml", System.Data.XmlWriteMode.WriteSchema);
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
            CheckBox1.Unchecked += CheckBoxChangedServer;
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
            int line = Int32.Parse((sender as TextBox).Name.Split('_')[1]);
            Global.TableRuntime.Rows[line]["IP"] = GetIPfromHostname((sender as TextBox).Text);
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
            if (tborigin.Text == "") { return; }
            var ParentGrid = ((FrameworkElement)sender).Parent as Grid;
            var list = ParentGrid.Children.OfType<TextBox>().Where(tb => tb.Name.Contains((sender as TextBox).Name.Split('_')[0])).ToArray();
            // Compare the sender and the last element if the Textbox Name is the same
            if (tborigin.Name.ToString() == list[list.Count() - 1].Name.ToString()) {
                // Textbox Servername creation
                CreateTextbox("TextBoxServer_" + list.Count(), "", 18, 120, 20, 30 * (list.Count() + 1));
                // Uptime Label creation
                CreateLabel("LabelUptime_" + list.Count(), "", 26, 90, 150, 30 * (list.Count() + 1) - 4, Visibility.Visible);
                // Accept Checkbox creation
                CreateCheckbox("CheckboxAccept_" + list.Count(), 250, 30 * (list.Count() + 1), false);
                // Drivers Checkbox creation
                CreateCheckbox("CheckboxDrivers_" + list.Count(), 310, 30 * (list.Count() + 1), false);
                // Reboot Checkbox creation
                CreateCheckbox("CheckboxReboot_" + list.Count(), 370, 30 * (list.Count() + 1), false);
                // GUI Checkbox creation
                CreateCheckbox("CheckboxGUI_" + list.Count(), 430, 30 * (list.Count() + 1), true);
                // Mail Checkbox creation
                CreateCheckbox("CheckboxMail_" + list.Count(), 490, 30 * (list.Count() + 1), false);
                // Credentials Button creation
                CreateButton("ButtonCredentials_" + list.Count(), "Credentials", 70, 530, 30 * ((list.Count() + 1) - 1) + 29, new RoutedEventHandler(GetCredentials), System.Windows.Visibility.Visible);
                // Start Button creation
                CreateButton("ButtonStart_" + list.Count(), "Start", 70, 620, 30 * ((list.Count() + 1) - 1) + 29, new RoutedEventHandler(ButtonStart_Click), System.Windows.Visibility.Visible);
                // Time Button creation
                CreateButton("ButtonTime_" + list.Count(), "12:12:12", 70, 620, 30 * ((list.Count() + 1) - 1) + 29, new RoutedEventHandler(ButtonTime_Click), System.Windows.Visibility.Hidden);
                // Enabled Checkbox creation
                CreateCheckbox("CheckboxEnabled_" + list.Count(), 710, 30 * (list.Count() + 1), false);
                if ((list.Count() + 1) % 2 == 0)
                {
                    // Light Grey Rectangle creation
                    CreateRectangle("BackgroundRectangle_" + list.Count(), 30, double.NaN, 0, 24 + 30 * list.Count(), new SolidColorBrush(Color.FromRgb(222, 217, 217)), 0);
                }
                //CreateTimerPing(list.Count());
                // Create BackgroundWorker Uptime for Line list.Count()
                CreateBackgroundWorkerUptime(list.Count());
                // Create BackgroundWorker Ping for Line list.Count()
                CreateBackgroundWorkerPing(list.Count());
                if (list.Count() >= 3)
                {
                    Application.Current.MainWindow.Height = 130 + list.Count() * 30;
                }

                System.Data.DataRow dtrow = Global.TableRuntime.NewRow();
                dtrow["Servername"] = "";
                dtrow["IP"] = "";
                dtrow["Username"] = "";
                dtrow["Password"] = "";
                Global.TableRuntime.Rows.Add(dtrow);

            }
        }
        /// <summary>
        /// Create a new BackgroundWorker for Uptime of the host xyz
        /// </summary>
        /// <param name="trline"></param>
        private void CreateBackgroundWorkerUptime(int line)
        {
            BackgroundWorker BgWUptimeTmp = new BackgroundWorker();
            BgWUptimeTmp.DoWork += new DoWorkEventHandler((sender, e) => { BackgroundWorkerUptime(sender, e, line); });
            BgWUptimeTmp.RunWorkerAsync();
            ListBackgroundWorkerUptime.Add(BgWUptimeTmp);

        }
        /// <summary>
        /// Create a new BackgroundWorker for Ping of the host xyz
        /// </summary>
        /// <param name="trline"></param>
        private void CreateBackgroundWorkerPing(int line)
        {
            BackgroundWorker BgWUptimeTmp = new BackgroundWorker();
            BgWUptimeTmp.DoWork += new DoWorkEventHandler((sender, e) => { BackgroundWorkerPing(sender, e, line); });
            BgWUptimeTmp.RunWorkerAsync();
            ListBackgroundWorkerPing.Add(BgWUptimeTmp);

        }
        private void BackgroundWorkerUptime(object sender, EventArgs e, int line)
        {
            System.Threading.Thread.Sleep(10 * 1000);
            while (true)
            {
                Uptime_Tick(sender, e, line);
                System.Threading.Thread.Sleep(30 * 1000);  // Wait 30 seconds
            }
        }
        private void BackgroundWorkerPing(object sender, EventArgs e, int line)
        {
            System.Threading.Thread.Sleep(1 * 1000);
            while (true)
            {
                Ping_Tick(sender, e, line);
                System.Threading.Thread.Sleep(5 * 1000);  // Wait 30 seconds
            }
        }
        public void Uptime_Tick(object sender, EventArgs e, int line)
        {
            if(Global.TableRuntime.Rows[line]["IP"].ToString() == "") { return; }
            TimeSpan tstmp = new TimeSpan();
            var ParentGrid = GridMainWindow;
            var sessionState = InitialSessionState.CreateDefault();
            using (var psRunspace = RunspaceFactory.CreateRunspace(sessionState))
            {
                psRunspace.Open();
                Pipeline pipeline = psRunspace.CreatePipeline();
                if (Global.TableRuntime.Rows[line]["Username"].ToString() != "" && Global.TableRuntime.Rows[line]["Username"].ToString() != "")
                {
                    pipeline.Commands.AddScript("$pass = ConvertTo-SecureString -AsPlainText '" + Global.TableRuntime.Rows[line]["Password"].ToString() + "' -Force;");
                    pipeline.Commands.AddScript("$Cred = New-Object System.Management.Automation.PSCredential -ArgumentList '" + Global.TableRuntime.Rows[line]["Username"].ToString() + "',$pass;");
                    pipeline.Commands.AddScript("Invoke-Command -Credential $Cred -ComputerName '" + Global.TableRuntime.Rows[line]["Servername"].ToString() + "' { (get-date) - (gcim Win32_OperatingSystem).LastBootUpTime };");
                } else
                {
                    pipeline.Commands.AddScript("Invoke-Command -ComputerName '" + Global.TableRuntime.Rows[line]["Servername"].ToString() + "' { (get-date) - (gcim Win32_OperatingSystem).LastBootUpTime };");
                }

                var exResults = pipeline.Invoke();
                
                if (exResults.Count != 0)
                {
                    tstmp = TimeSpan.Parse(exResults[0].BaseObject.ToString());
                    Global.TableRuntime.Rows[line]["Uptime"] = tstmp.Days + "d " + tstmp.Hours + "h " + tstmp.Minutes + "m";
                }
                else
                {
                    Global.TableRuntime.Rows[line]["Uptime"] = "no connection";
                }
                psRunspace.Close();
            }
        }
        public void Ping_Tick(object sender, EventArgs e, int line)
        {
            if (Global.TableRuntime.Rows[line]["IP"].ToString() == "" && Global.TableRuntime.Rows[line]["Servername"].ToString() == "")
            {
                return;
            }
            if (Global.TableRuntime.Rows[line]["IP"].ToString() == "") {
                Global.TableRuntime.Rows[line]["Ping"] = "noIP";
                return;
            }
            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();
            options.DontFragment = true;
            byte[] buffer = Encoding.ASCII.GetBytes("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
            PingReply reply = pingSender.Send(Global.TableRuntime.Rows[line]["IP"].ToString(), 1000, buffer, options);
            if (reply.Status == IPStatus.Success)
            {
                Global.TableRuntime.Rows[line]["Ping"] = "true";
            }
            else
            {
                Global.TableRuntime.Rows[line]["Ping"] = "false";
            }
        }
        private void GetCredentials(object sender, RoutedEventArgs e)
        {
            var ParentGrid = ((FrameworkElement)sender).Parent as Grid;
            string strLabelID = (sender as Button).Name.Split('_')[1];
            string tmpUsername = Global.TableRuntime.Rows[Int32.Parse(strLabelID)]["Username"].ToString();
            string tmpPassword = Global.TableRuntime.Rows[Int32.Parse(strLabelID)]["Password"].ToString();
            string tmpServer = Global.TableRuntime.Rows[Int32.Parse(strLabelID)]["Servername"].ToString().ToUpper();

            RemoteUpdate.Credentials AskCred = new RemoteUpdate.Credentials(sender, tmpUsername, tmpPassword);
            AskCred.Title = tmpServer + " Credentials";
            bool? result = AskCred.ShowDialog();
            if(result == true)
            {
                if (AskCred.TextboxUsername.Text != "Username")
                {
                    Global.TableRuntime.Rows[Int32.Parse(strLabelID)]["Username"] = AskCred.TextboxUsername.Text;
                }
                if (AskCred.PasswordBoxPassword.Password != "ABC")
                {
                    Global.TableRuntime.Rows[Int32.Parse(strLabelID)]["Password"] = AskCred.PasswordBoxPassword.Password;
                }
            }
        }
        public static string Encrypt(string clearText)
        {
            if(clearText == "") { return clearText; }
            string EncryptionKey = System.Net.Dns.GetHostEntry("localhost").HostName + "RemoteUpdateByAIMA" + System.Security.Principal.WindowsIdentity.GetCurrent().Owner.ToString();
            byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    clearText = Convert.ToBase64String(ms.ToArray());
                }
            }
            return clearText;
        }
        public static string Decrypt(string cipherText)
        {
            string EncryptionKey = System.Net.Dns.GetHostEntry("localhost").HostName + "RemoteUpdateByAIMA" + System.Security.Principal.WindowsIdentity.GetCurrent().Owner.ToString();
            cipherText = cipherText.Replace(" ", "+");
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    cipherText = Encoding.Unicode.GetString(ms.ToArray());
                }
            }
            return cipherText;
        }
        public void StartUpdate(int line)
        {
            OpenPowerShell(line);
        }
        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            int line = Int32.Parse((sender as Button).Name.Split('_')[1]);
            OpenPowerShell(line);
        }
        private void ButtonTime_Click(object sender, RoutedEventArgs e)
        {
            int line = Int32.Parse((sender as Button).Name.Split('_')[1]);
            GridMainWindow.Children.OfType<Button>().Where(btn => btn.Name.Equals("ButtonTime_" + line.ToString())).FirstOrDefault().Visibility = System.Windows.Visibility.Hidden;
            GridMainWindow.Children.OfType<Button>().Where(btn => btn.Name.Equals("ButtonStart_" + line.ToString())).FirstOrDefault().Visibility = System.Windows.Visibility.Visible;
        }
        private void OpenPowerShell(int line)
        {
            GridMainWindow.Children.OfType<Button>().Where(btn => btn.Name.Equals("ButtonTime_" + line.ToString())).FirstOrDefault().Visibility = System.Windows.Visibility.Visible;
            GridMainWindow.Children.OfType<Button>().Where(btn => btn.Name.Equals("ButtonTime_" + line.ToString())).FirstOrDefault().Content = DateTime.Now.ToString("HH:mm:ss");
            GridMainWindow.Children.OfType<Button>().Where(btn => btn.Name.Equals("ButtonStart_" + line.ToString())).FirstOrDefault().Visibility = System.Windows.Visibility.Hidden;

            ProcessStartInfo startInfo = new ProcessStartInfo(@"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe");
            startInfo.UseShellExecute = false;
            startInfo.EnvironmentVariables.Add("RedirectStandardOutput", "true");
            startInfo.EnvironmentVariables.Add("RedirectStandardError", "true");
            startInfo.EnvironmentVariables.Add("UseShellExecute", "false");
            startInfo.EnvironmentVariables.Add("CreateNoWindow", "true");

            startInfo.Arguments = "-noexit ";
            // https://devblogs.microsoft.com/scripting/how-can-i-expand-the-width-of-the-windows-powershell-console/
            startInfo.Arguments += "$pshost = get-host; $pswindow = $pshost.ui.rawui; $newsize = $pswindow.buffersize; $newsize.height = 10; $newsize.width = 120; $pswindow.windowsize = $newsize;";
            startInfo.Arguments += "$host.ui.RawUI.WindowTitle = '" + Global.TableRuntime.Rows[line]["Servername"].ToString().ToUpper() + "';";
            if (Global.TableRuntime.Rows[line]["Username"].ToString() != "" && Global.TableRuntime.Rows[line]["Password"].ToString() != "") 
            { 
                startInfo.Arguments += "$pass = ConvertTo-SecureString -AsPlainText '" + Global.TableRuntime.Rows[line]["Password"].ToString() + "' -Force;";
                startInfo.Arguments += "$Cred = New-Object System.Management.Automation.PSCredential -ArgumentList '" + Global.TableRuntime.Rows[line]["Username"].ToString() + "',$pass;";
                startInfo.Arguments += "$session = New-PSSession -Credential $Cred -ConfigurationName '" + Global.TableSettings.Rows[0]["PSVirtualAccountName"] + "' -ComputerName " + Global.TableRuntime.Rows[line]["Servername"].ToString() + ";";
            } else
            {
                startInfo.Arguments += "$session = New-PSSession -ConfigurationName '" + Global.TableSettings.Rows[0]["PSVirtualAccountName"] + "' -ComputerName " + Global.TableRuntime.Rows[line]["Servername"].ToString() + ";";
            }
            string WUArguments = "";
            if ((bool)GridMainWindow.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxAccept_" + line.ToString()).FirstOrDefault().IsChecked)
            {
                WUArguments += "-AcceptAll ";
            }
            if (!(bool)GridMainWindow.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxDrivers_" + line.ToString()).FirstOrDefault().IsChecked)
            {
                WUArguments += "-NotCategory Drivers ";
            }
            if ((bool)GridMainWindow.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxReboot_" + line.ToString()).FirstOrDefault().IsChecked)
            {
                WUArguments += "-AutoReboot ";
            }
            if ((bool)GridMainWindow.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxMail_" + line.ToString()).FirstOrDefault().IsChecked)
            {
                if((Global.TableSettings.Rows[0]["SMTPServer"].ToString() != "") && (Global.TableSettings.Rows[0]["SMTPPort"].ToString() != "") && (Global.TableSettings.Rows[0]["MailFrom"].ToString() != "") && (Global.TableSettings.Rows[0]["MailTo"].ToString() != ""))
                {
                    WUArguments += "-SendReport –PSWUSettings @{ SmtpServer = '" + Global.TableSettings.Rows[0]["SMTPServer"].ToString() + "'; Port = " + Global.TableSettings.Rows[0]["SMTPPort"].ToString() + "; From = '" + Global.TableSettings.Rows[0]["MailFrom"].ToString() + "'; To = '" + Global.TableSettings.Rows[0]["MailTo"].ToString() + "' }";
                }
            }
            startInfo.Arguments += "Invoke-Command $session { Install-WindowsUpdate -Verbose " + WUArguments + Global.TableSettings.Rows[0]["PSWUCommands"].ToString() + "}";
            Process.Start(startInfo);
        }
        private void ButtonStartAll_Click(object sender, RoutedEventArgs e)
        {
            for (int ii = 0; ii < Global.TableRuntime.Rows.Count; ii++) {
                if ((bool)GridMainWindow.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxEnabled_" + ii.ToString()).FirstOrDefault().IsChecked) {
                    if (Global.TableRuntime.Rows[ii]["Servername"].ToString() != "" && Global.TableRuntime.Rows[ii]["IP"].ToString() != "")
                    {
                        OpenPowerShell(ii);
                    }
                }
            }
        }
        private void ButtonSettings_Click(object sender, RoutedEventArgs e)
        {
            RemoteUpdate.Settings ShowSettings = new RemoteUpdate.Settings(Global.TableSettings.Rows[0]["SMTPServer"].ToString(), Global.TableSettings.Rows[0]["SMTPPort"].ToString(), Global.TableSettings.Rows[0]["MailFrom"].ToString(), Global.TableSettings.Rows[0]["MailTo"].ToString(), Global.TableSettings.Rows[0]["PSVirtualAccountName"].ToString(), Global.TableSettings.Rows[0]["PSWUCommands"].ToString());
            bool? result = ShowSettings.ShowDialog();
            if((bool)result)
            {
                Global.TableSettings.Rows[0]["SMTPServer"] = ShowSettings.TextboxSMTPServer.Text;
                Global.TableSettings.Rows[0]["SMTPPort"] = ShowSettings.TextboxSMTPPort.Text;
                Global.TableSettings.Rows[0]["MailFrom"] = ShowSettings.TextboxMailFrom.Text;
                Global.TableSettings.Rows[0]["MailTo"] = ShowSettings.TextboxMailTo.Text;
                Global.TableSettings.Rows[0]["PSVirtualAccountName"] = ShowSettings.TextboxVirtualAccount.Text;
                Global.TableSettings.Rows[0]["PSWUCommands"] = ShowSettings.TextboxPSWUCommands.Text;
            }
        }
        private void ButtonAbout_Click(object sender, RoutedEventArgs e)
        {
            RemoteUpdate.About ShowAbout = new RemoteUpdate.About(Global.TableSettings.Rows[0]["PSVirtualAccountName"].ToString());
            ShowAbout.ShowDialog();
        }

        private void ButtonStart_0_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MessageBox.Show("TEST");
        }
    }
}