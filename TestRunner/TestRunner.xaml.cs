using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Reflection;
using NUnit.Framework;
using System.IO;
using System.Threading;
using System.Windows.Threading;

namespace GitSharp.TestRunner
{
    public partial class TestRunner : UserControl
    {
        #region --> Initialization


        public TestRunner()
        {
            InitializeComponent();
            m_button_run.Click += (o, args) =>
            {
                var item = m_treeview.SelectedItem as TreeViewItem;
                if (item == null)
                    item = m_treeview.Items[0] as TreeViewItem;
                m_button_run.IsEnabled = false;
                StartBackgroundRunner(item);
                //PresentSummary(item);
            };
            Init();
        }
        //ImageSource red_icon = new BitmapImage().FromFile(Directory.GetCurrentDirectory() + @"./Resources/offline.png");
        //ImageSource green_icon = new BitmapImage().FromFile(Directory.GetCurrentDirectory() + @"./Resources/online.png");

        private void Init()
        {
            var item = m_treeview.Items[0] as TreeViewItem;
            {

                item.PreviewMouseDown += (o, args) => PresentSummary(item);
                item.ContextMenu = InitContextMenu(item);
            }

            List<Assembly> assemblies = new List<Assembly>();
            assemblies.Add(Assembly.GetEntryAssembly());
            foreach (AssemblyName name in Assembly.GetEntryAssembly().GetReferencedAssemblies())
                assemblies.Add(Assembly.Load(name));
            foreach (Assembly assembly in assemblies)
                Analyze(assembly, item.Items);
            UpdateTree();
        } 


        #endregion

        /// <summary>
        /// Add assemblies containing unit tests to be run by the testrunner.
        /// </summary>
        /// <param name="assembly"></param>
        public void AddAssembly(Assembly assembly)
        {
            Analyze(assembly, (m_treeview.Items[0] as TreeViewItem).Items);
            UpdateTree();
        }

        #region --> Assembly analization


        private void Analyze(Assembly assembly, ItemCollection collection)
        {
            var item = new TreeViewItem();
            {
                item.Header = assembly.GetName().Name;
                item.Tag = assembly;
                collection.Add(item);
                item.PreviewMouseDown += (o, args) => PresentSummary(item);
            }
            foreach (Type type in assembly.GetExportedTypes())
                Analyze(type, item.Items);

            if (item.Items.Count == 0)
            {
                collection.Remove(item); // pruning assemblies without testcases
                return;
            }
            item.ContextMenu = InitContextMenu(item);
        }

        private void Analyze(Type type, ItemCollection collection)
        {
            object[] attrs = Attribute.GetCustomAttributes(type);
            //if ( attrs.Contains(NUnit.Framework.TestAttribute);
            if (type.GetCustomAttributes(typeof(TestFixtureAttribute), false).Count() == 0)
                return;
            var fixture = new TestFixtureView(type.Name, type);
            var item = new TreeViewItem();
            {
                item.Header = type.Name;
                item.Tag = fixture;
                collection.Add(item);
                item.PreviewMouseDown += (o, args) => PresentSummary(item);
            }

            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                Analyze(type, method, item.Items, fixture);

        }

        private void Analyze(Type type, MethodInfo method, ItemCollection collection, TestFixtureView fixture)
        {
            if (method.GetCustomAttributes(typeof(TestAttribute), false).Count() > 0)
            {

                var testcase = new TestcaseView(type, method);
                collection.Add(CreateItem(testcase));
                fixture.AddTest(testcase);
            }
            else if (method.GetCustomAttributes(typeof(SetUpAttribute), false).Count() > 0)
            {
                fixture.SetupMethod = method;
            }
            else if (method.GetCustomAttributes(typeof(TearDownAttribute), false).Count() > 0)
            {
                fixture.TeardownMethod = method;
            }
        }

        private TreeViewItem CreateItem(TestcaseView testcase)
        {

            var item = new TreeViewItem();
            item.Header = testcase.TestMethod.Name;
            //item.FontWeight = FontWeights.Bold;
            item.Tag = testcase;
            item.ContextMenu = InitContextMenu(item);
            item.PreviewMouseDown += (o, args) => PresentSummary(item);
#if false
            ImageSource iconSource=red_icon;
            TextBlock textBlock;
            Image icon;

            StackPanel stack = new StackPanel();
            stack.Orientation = Orientation.Horizontal;
            item.Header = stack;
            //Uncomment this code If you want to add an Image after the             Node-HeaderText
            //textBlock = new TextBlock();
            //textBlock.VerticalAlignment = VerticalAlignment.Center;
            //stack.Children.Add(textBlock);
            icon = new Image();
            icon.VerticalAlignment = VerticalAlignment.Center;
            icon.Margin = new Thickness(0, 0, 4, 0);
            icon.Source = iconSource;
            stack.Children.Add(icon);
            //Add the HeaderText After Adding the icon
            textBlock = new TextBlock();
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            stack.Children.Add(textBlock);
#endif
            return item;
        }

        private ContextMenu InitContextMenu(TreeViewItem item)
        {
            var menu = new ContextMenu();
            var item1 = new MenuItem();
            {
                menu.Items.Add(item1);
                item1.Header = "Run";
                item1.Click += (o, args) =>
                {
                    StartBackgroundRunner(item);
                    PresentSummary(item);
                };
            }
            var item2 = new MenuItem();
            {
                menu.Items.Add(item2);
                item2.Header = "Expand all";
                item2.Click += (o, args) => item.ExpandDownRecursive();
            } return menu;
        } 


        #endregion

        #region --> Run mechanics


        private bool m_stop_background_runner = false;

        private void StartBackgroundRunner(TreeViewItem item)
        {
            m_stop_background_runner = false;
            var cases = new List<TestcaseView>();
            UpdateTree();
            CollectAll(item, cases);
            ThreadPool.QueueUserWorkItem(new WaitCallback(x =>
            {
                RunAll(cases, item);
                this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => m_button_run.IsEnabled = true));
            }));
        }

        private void CollectAll(TreeViewItem item, List<TestcaseView> testcases)
        {
            if (item == null || testcases == null)
                return;
            if (item.Tag is TestcaseView)
            {
                var testcase = item.Tag as TestcaseView;
                testcase.Reset();
                testcases.Add(testcase);
            }
            else
                foreach (var child in item.Items)
                    CollectAll(child as TreeViewItem, testcases);
        }

        private void RunAll(List<TestcaseView> testcases, TreeViewItem toplevel_item)
        {
            foreach (var testcase in testcases)
            {
                if (m_stop_background_runner)
                    return;
                if (testcase == null)
                    continue;
                Run(testcase);
                PresentSummary(toplevel_item);
            }
        }

        private void Run(TestcaseView testcase)
        {
            if (m_stop_background_runner)
                return;
            //item.Foreground = Brushes.Black; // Colors.Black;
            //m_textbox1.Text = "Running ... " + testcase.ToString();
            //m_textbox2.Text = "";
            testcase.Run();
        } 


        #endregion

        #region --> Presentation of results


        int m_failed_count, m_ok_count, m_not_run_count;

        private void PresentSummary(TreeViewItem item)
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                m_textbox2.Text = "";
                var testcase = item.Tag as TestcaseView;
                if (testcase == null) // present summary
                {
                    ClearStats();
                    m_summary_label.Text = "Summary for " + item.Header.ToString();
                    StringBuilder s = new StringBuilder("\n");
                    var stat_pos = s.Length;
                    foreach (var i in item.IterateDown())
                    {
                        var treeitem = i as TreeViewItem;
                        if (treeitem.Tag is TestcaseView)
                        {
                            s.AppendLine(StatusMessage(treeitem.Tag as TestcaseView));
                            UpdateStats(treeitem.Tag as TestcaseView);
                        }
                        UpdateItem(treeitem);
                    }
                    var total = (m_ok_count + m_failed_count + m_not_run_count);
                    s.Insert(stat_pos, "  " + m_failed_count + " failed  |  " + m_ok_count + " passed  |  " + m_not_run_count + " not run  |  " + total + " total tests\n\n");
                    m_textbox1.Text = s.ToString();
                    m_progressbar.Maximum = (double)total;
                    m_progressbar.Value = (double)(m_failed_count + m_ok_count);
                    return;
                }
                m_textbox1.Text = StatusMessage(testcase);
                string backtrace = testcase.Exception.PrettyPrint();
                m_textbox2.Text = backtrace;
                UpdateItem(item);
                m_progressbar.Value = 0;
                Console.WriteLine(backtrace);
            }));
        }

        private void UpdateItem(TreeViewItem item)
        {
            bool is_executed = false, is_failed = false;
            if (item.Tag is TestcaseView)
            {
                var testcase = item.Tag as TestcaseView;
                is_executed = testcase.IsExecuted;
                is_failed = testcase.Failed;
            }
            else if (item.Tag is TestFixtureView)
            {
                var fixture = item.Tag as TestFixtureView;
                is_executed = fixture.AreAllExecuted;
                is_failed = fixture.HasAnyFailures;
            }
            if (is_failed)
            {
                item.ExpandUpRecursive();
                item.BringIntoView();
                //((item.Header as StackPanel).Children[0] as Image).Source = red_icon;
                //(item.Header as TextBlock).
                item.Foreground = Brushes.Red;
                //m_textbox2.Text = testcase.Exception.PrettyPrint();
                //m_textbox1.Text = testcase.ToString() + " --> FAILED:\n\n" + testcase.Exception.Message;
                //string s = testcase.Exception.PrettyPrint();
            }
            else if (is_executed)
            {
                item.Foreground = Brushes.Black;
            }
            else
                item.Foreground = Brushes.DarkGray;

        }

        private void UpdateTree()
        {
            foreach (var i in (m_treeview.Items[0] as TreeViewItem).IterateDown())
            {
                var treeitem = i as TreeViewItem;
                if (treeitem.Tag is TestcaseView)
                    (treeitem.Tag as TestcaseView).Reset();
            }
            foreach (var i in (m_treeview.Items[0] as TreeViewItem).IterateDown())
            {
                var treeitem = i as TreeViewItem;
                UpdateItem(treeitem);
            }
        }

        private void UpdateStats(TestcaseView testcaseView)
        {
            if (testcaseView.IsExecuted)
                if (testcaseView.Failed)
                    m_failed_count += 1;
                else
                    m_ok_count += 1;
            else
                m_not_run_count += 1;
        }

        private void ClearStats()
        {
            m_ok_count = 0;
            m_failed_count = 0;
            m_not_run_count = 0;
        }

        private string StatusMessage(TestcaseView testcase)
        {
            if (testcase.IsExecuted == false)
                return testcase.ToString() + " ... not yet run.";
            if (testcase.Failed)
                return "*** " + testcase.ToString() + " --> FAILED:\n\n" + testcase.Exception.Message;
            else
                return testcase.ToString() + " --> OK";
        } 


        #endregion

        #region --> Event handling


        private void OnStopButton(object sender, RoutedEventArgs e)
        {
            m_stop_background_runner = true;
            m_button_run.IsEnabled = true;
        }


        #endregion
    }

    #region Utilities


    public static class ExceptionExtension
    {
        public static string PrettyPrint(this Exception exception)
        {
            if (exception == null)
                return "";
            m_string_builder = new StringBuilder();
            PrintRecursive(exception, "");
            return m_string_builder.ToString();
        }

        private static StringBuilder m_string_builder;

        private static void PrintRecursive(Exception exception, string indent)
        {
            string stars = new string('*', 80);
            m_string_builder.AppendLine(indent + stars);
            m_string_builder.AppendFormat(indent + "{0}: \"{1}\"\n", exception.GetType().Name, exception.Message);
            m_string_builder.AppendLine(indent + new string('-', 80));
            if (exception.InnerException != null)
            {
                m_string_builder.AppendLine(indent + "InnerException:");
                PrintRecursive(exception.InnerException, indent + "   ");
            }
            foreach (string line in exception.StackTrace.Split(new string[] { " at " }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (string.IsNullOrEmpty(line.Trim())) continue;
                string[] parts;
                parts = line.Trim().Split(new string[] { " in " }, StringSplitOptions.RemoveEmptyEntries);
                string class_info = parts[0];
                if (parts.Length == 2)
                {
                    parts = parts[1].Trim().Split(new string[] { "line" }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        string src_file = parts[0];
                        int line_nr = int.Parse(parts[1]);
                        m_string_builder.AppendFormat(indent + "  {0}({1},1):   {2}\n", src_file.TrimEnd(':'), line_nr, class_info);
                    }
                    else
                        m_string_builder.AppendLine(indent + "  " + class_info);
                }
                else
                    m_string_builder.AppendLine(indent + "  " + class_info);
            }
            m_string_builder.AppendLine(indent + stars);
        }

    }

    public static class TreeViewItemExtension
    {
        public static void Expand(this TreeViewItem item)
        {
            item.IsExpanded = true;
        }

        public static void Collapse(this TreeViewItem item)
        {
            item.IsExpanded = false;
        }

        public static void ExpandUpRecursive(this TreeViewItem item)
        {
            var parent = item.Parent as TreeViewItem;
            if (parent == null)
                return;
            parent.Expand();
            parent.ExpandUpRecursive();
        }

        public static void ExpandDownRecursive(this TreeViewItem item)
        {
            item.Expand();
            foreach (var child in item.Items)
                (child as TreeViewItem).ExpandDownRecursive();
        }

        public static IEnumerable<object> IterateDown(this TreeViewItem item)
        {
            var list = new List<object>();
            IterateDown(item, list);
            return list;
        }

        private static void IterateDown(TreeViewItem item, List<object> list)
        {
            foreach (var child in item.Items)
            {
                //var child = i as TreeViewItem;
                list.Add(child);
                if (child is TreeViewItem)
                    IterateDown(child as TreeViewItem, list);
            }
        }
    }


    #endregion
}