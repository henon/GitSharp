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
using GitSharp;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using System.Reflection;
using System.Diagnostics;

namespace GitSharp.Demo
{

    public partial class Browser : Window
    {
        public Browser()
        {
            InitializeComponent();
            m_commits.SelectionChanged += (o, args) => SelectCommit(m_commits.SelectedItem as Commit);
            //m_branches.SelectionChanged += (o, args) => SelectBranch(m_branches.SelectedItem as Branch);
            m_refs.SelectionChanged += (o, args) => SelectRef(m_refs.SelectedItem as Ref);
            m_tree.SelectedItemChanged += (o, args) => SelectObject(m_tree.SelectedValue as TreeEntry);
            //m_config_tree.SelectedItemChanged += (o, args) => SelectConfiguration(m_config_tree.SelectedItem);
        }


        Repository m_repository;

        // load
        private void OnLoadRepository(object sender, RoutedEventArgs e)
        {
            var url = m_url_textbox.Text;
            var repo = Repository.Open(url);
            var head = repo.OpenCommit(repo.Head.ObjectId) as Commit;
            m_repository = repo;
            var tags = repo.Tags.Values.Select(@ref => repo.MapTag(@ref.Name, @ref.ObjectId));
            //var branches = repo.Branches.Values.Select(@ref => repo.MapCommit(@ref.ObjectId));
            m_refs.ItemsSource = repo.Refs.Values;
            DisplayCommit(head, "HEAD");
            ReloadConfiguration();
        }

        private void SelectObject(TreeEntry node)
        {
            if (node.IsBlob)
            {
                //var blob = node as Blob;
                var text = Encoding.UTF8.GetString(m_repository.OpenBlob(node.Id).getBytes()); // TODO: better interface for blobs
                m_object.Document.Blocks.Clear();
                var p = new Paragraph();
                p.Inlines.Add(text);
                m_object.Document.Blocks.Add(p);
                m_object_title.Text = "Content of " + node.FullName;
            }
            else
            {
                m_object.Document.Blocks.Clear();
            }
        }

        private void SelectBranch(object branch)
        {
            if (branch == null)
                return;
            //DisplayCommit(branch.Commit, "Branch "+branch.Name);
        }

        private void SelectRef(Ref r)
        {
            if (r == null)
                return;
            var obj = m_repository.OpenObject(r.ObjectId);
            if (obj.getType() == Constants.OBJ_COMMIT)
            {
                DisplayCommit(m_repository.MapCommit(r.ObjectId), "Commit history of " + r.Name);
                return;
            }
            else if (obj.getType() == Constants.OBJ_TAG)
            {
                var tag = m_repository.MapTag(r.Name, r.ObjectId);
                if (tag.TagId == r.ObjectId) // it sometimes happens to have self referencing tags
                {
                    return;
                }
                var tagged_commit = m_repository.MapCommit(tag.TagId);
                DisplayCommit(tagged_commit, "Commit history of " + tag.TagName);
                return;
            }
            else if (obj.getType() == Constants.OBJ_TREE)
            {
                // hmm, display somehow
            }
            else if (obj.getType() == Constants.OBJ_BLOB)
            {
                // hmm, display somehow
            }
            else
            {
                Debug.Fail("don't know how to display this object: "+obj.ToString());
            }
        }

        private void SelectTag(Tag tag)
        {
            if (tag == null)
                return;
            //if (tag.Object is Commit)
            //    DisplayCommit(tag.Object as Commit, "Tag "+tag.Name);
        }

        private void OnSelectRepository(object sender, RoutedEventArgs e)
        {
            var dlg = new System.Windows.Forms.FolderBrowserDialog();
            //dlg.CheckPathExists = true;
            if (dlg.ShowDialog() ==  System.Windows.Forms.DialogResult.OK)
            {
                m_url_textbox.Text = dlg.SelectedPath;
            }
        }

        //private void SelectConfiguration(object obj)
        //{
        //    if (obj is Entry)
        //    {
        //        var entry = obj as dotGit.Config.Entry;
        //        m_config_name.Content = entry.FullName;
        //        if (entry.Value != null)
        //            m_config_value.Text = entry.Value;
        //    }
        //}

        private void DisplayCommit(Commit commit, string info)
        {
            if (commit == null)
                return;
            var list = commit.Ancestors.ToList();
            list.Insert(0, commit);
            m_commits.ItemsSource = list;
            m_commits.SelectedIndex = 0;
            m_commit_title.Text = "Commit history for " + info;
        }

        private void SelectCommit(Commit commit)
        {
            if (commit == null)
                return;
            m_tree.ItemsSource = (commit.TreeEntry as Tree).Members;
            m_tree_title.Text = "Repository tree of Commit " + commit.CommitId;
            //(m_tree.ItemContainerGenerator.ContainerFromIndex(0) as TreeViewItem).IsExpanded = true;
        }

        private void OnLoadConfiguration(object sender, RoutedEventArgs e)
        {
            ReloadConfiguration();
        }

        private void ReloadConfiguration()
        {
            m_repository.Config.Load();
            m_config_tree.ItemsSource = null;
            //m_config_tree.ItemsSource = m_repository.Config.Sections;
        }

        private void SaveConfiguration()
        {
            m_repository.Config.Save();
            ReloadConfiguration();
        }

        private void OnSaveConfiguration(object sender, RoutedEventArgs e)
        {
            SaveConfiguration();
        }
    }
}
