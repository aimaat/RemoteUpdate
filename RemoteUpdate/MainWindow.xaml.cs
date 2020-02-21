using System;
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
        public Grid GridMainWindow;
        // Timer for Grid Update
        public DispatcherTimer TimerUpdateGrid = new DispatcherTimer();
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
                dtrow["Password"] = Tasks.Decrypt(LoadTable.Rows[0]["Password"].ToString());
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
            Worker.CreateBackgroundWorkerUptime(0);
            // Create BackgroundWorker Ping for Line 1
            Worker.CreateBackgroundWorkerPing(0);
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
                Worker.CreateBackgroundWorkerUptime(ii);
                // Create BackgroundWorker Ping for Line ii
                Worker.CreateBackgroundWorkerPing(ii);
                System.Data.DataRow dtrow = Global.TableRuntime.NewRow();
                if (ii < LoadTable.Rows.Count)
                {
                    dtrow["Servername"] = LoadTable.Rows[ii]["Server"].ToString();
                    dtrow["IP"] = GetIPfromHostname(LoadTable.Rows[ii]["Server"].ToString());
                    dtrow["Username"] = LoadTable.Rows[ii]["Username"].ToString();
                    dtrow["Password"] = Tasks.Decrypt(LoadTable.Rows[ii]["Password"].ToString());
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
                dtrow["Password"] = Tasks.Encrypt(Global.TableRuntime.Rows[ii]["Password"].ToString());
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
                // Create BackgroundWorker Uptime for Line list.Count()
                Worker.CreateBackgroundWorkerUptime(list.Count());
                // Create BackgroundWorker Ping for Line list.Count()
                Worker.CreateBackgroundWorkerPing(list.Count());
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
        private void GetCredentials(object sender, RoutedEventArgs e)
        {
            int iLabelID = Int32.Parse((sender as Button).Name.Split('_')[1]);
            string tmpServer = Global.TableRuntime.Rows[iLabelID]["Servername"].ToString().ToUpper();
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
                    ThreadPool.QueueUserWorkItem(delegate { MessageBox.Show("Can't create the Powershell Virtual Account on server " + Global.TableRuntime.Rows[line]["Servername"].ToString().ToUpper() + ".\nPlease check your credentials or firewall settings."); });
                    //MessageBox.Show("Can't create the Powershell Virtual Account on server " + Global.TableRuntime.Rows[line]["Servername"].ToString() + ".\nPlease check your credentials or firewall settings.");
                }
            }
        }
        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            int line = Int32.Parse((sender as Button).Name.Split('_')[1]);
            StartUpdate(line);
        }
        private void ButtonTime_Click(object sender, RoutedEventArgs e)
        {
            int line = Int32.Parse((sender as Button).Name.Split('_')[1]);
            GridMainWindow.Children.OfType<Button>().Where(btn => btn.Name.Equals("ButtonTime_" + line.ToString())).FirstOrDefault().Visibility = System.Windows.Visibility.Hidden;
            GridMainWindow.Children.OfType<Button>().Where(btn => btn.Name.Equals("ButtonStart_" + line.ToString())).FirstOrDefault().Visibility = System.Windows.Visibility.Visible;
        }
        private void ButtonStartAll_Click(object sender, RoutedEventArgs e)
        {
            for (int ii = 0; ii < Global.TableRuntime.Rows.Count; ii++) {
                if ((bool)GridMainWindow.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxEnabled_" + ii.ToString()).FirstOrDefault().IsChecked) {
                    if (Global.TableRuntime.Rows[ii]["Servername"].ToString() != "" && Global.TableRuntime.Rows[ii]["IP"].ToString() != "")
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
    }
}