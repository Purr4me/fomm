﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Fomm.PackageManager;
using System.IO;
using System.Transactions;
using ChinhDo.Transactions;
using System.Drawing;

namespace Fomm.FileManager
{
	/// <summary>
	/// This form allows for selection of which version of a file is used.
	/// </summary>
	public partial class FileManager : Form
	{
		/// <summary>
		/// The default constructor.
		/// </summary>
		public FileManager()
		{
			InitializeComponent();
			LoadFiles();
			rlvOverwrites.Columns[0].Width = rlvOverwrites.ClientSize.Width - 3;
			string strFalloutEsm = Path.Combine("data", "fallout3.esm");
			imlFolders.Images.Add("mod", System.Drawing.Icon.ExtractAssociatedIcon(strFalloutEsm));
		}

		/// <summary>
		/// Loads the installed files into the navigation tree.
		/// </summary>
		protected void LoadFiles()
		{
			tvwFolders.Nodes.Clear();
			if (radByFile.Checked)
			{
				List<string> lstInstalledFiles = InstallLog.Current.GetFileList();
				TreeNode tndCurrentDirectory = new TreeNode("Data");
				tndCurrentDirectory.Name = tndCurrentDirectory.Text;
				tvwFolders.Nodes.Add(tndCurrentDirectory);
				AddFilesToNode(tndCurrentDirectory, lstInstalledFiles);
				if (lvwFiles.SelectedItems.Count > 0)
					SelectNodeContainingFile(tndCurrentDirectory, (string)lvwFiles.SelectedItems[0].Tag);
			}
			else
			{
				List<string> lstInstalledMods = InstallLog.Current.GetModList();
				TreeNode tndCurrentDirectory = null;
				List<string> lstInstalledFiles = null;
				foreach (string strMod in lstInstalledMods)
				{
					tndCurrentDirectory = tvwFolders.Nodes.Add(strMod);
					tndCurrentDirectory.Name = tndCurrentDirectory.Text;
					tndCurrentDirectory.ImageKey = "mod";
					tndCurrentDirectory.SelectedImageKey = "mod";
					tndCurrentDirectory = tndCurrentDirectory.Nodes.Add("Data");
					tndCurrentDirectory.Name = tndCurrentDirectory.Text;
					lstInstalledFiles = InstallLog.Current.GetFileList(strMod);
					AddFilesToNode(tndCurrentDirectory, lstInstalledFiles);
				}
				if (rlvOverwrites.SelectedItems.Count > 0)
				{
					tndCurrentDirectory = tvwFolders.Nodes[rlvOverwrites.SelectedItems[0].Name];
					if (lvwFiles.SelectedItems.Count > 0)
						SelectNodeContainingFile(tndCurrentDirectory.Nodes[0], (string)lvwFiles.SelectedItems[0].Tag);
				}
			}
		}

		/// <summary>
		/// Selects the node that coreesponds to the directory that contains
		/// the specified file.
		/// </summary>
		/// <param name="p_tndNode">The node under which to find the selected node.</param>
		/// <param name="p_strFile">The file path whose containing directory node is to be selected.</param>
		protected void SelectNodeContainingFile(TreeNode p_tndNode, string p_strFile)
		{
			char[] chrDirecotrySeparators = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
			string[] strDirectories = p_strFile.Split(chrDirecotrySeparators);
			TreeNode tndCurrentDirectory = p_tndNode;
			for (Int32 i = 0; i < strDirectories.Length - 1; i++)
				tndCurrentDirectory = tndCurrentDirectory.Nodes[strDirectories[i]];
			tvwFolders.SelectedNode = tndCurrentDirectory;
		}

		/// <summary>
		/// Adds the given files to the given tree node.
		/// </summary>
		/// <param name="p_tndNode">The node under which to add the files.</param>
		/// <param name="p_strFiles">The files to add to the given node.</param>
		protected void AddFilesToNode(TreeNode p_tndNode, IList<string> p_strFiles)
		{
			TreeNode tndCurrentDirectory = p_tndNode;
			char[] chrDirecotrySeparators = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
			string strDirectory = null;
			List<string> lstDirectoryFiles = null;
			foreach (string strFile in p_strFiles)
			{
				tndCurrentDirectory = p_tndNode;
				string[] strDirectories = strFile.Split(chrDirecotrySeparators);
				for (Int32 i = 0; i < strDirectories.Length - 1; i++)
				{
					strDirectory = strDirectories[i];
					if (!tndCurrentDirectory.Nodes.ContainsKey(strDirectory))
					{
						tndCurrentDirectory = tndCurrentDirectory.Nodes.Add(strDirectory);
						tndCurrentDirectory.Name = tndCurrentDirectory.Text;
					}
					else
						tndCurrentDirectory = tndCurrentDirectory.Nodes[strDirectory];
				}
				lstDirectoryFiles = (List<string>)tndCurrentDirectory.Tag;
				if (lstDirectoryFiles == null)
				{
					lstDirectoryFiles = new List<string>();
					tndCurrentDirectory.Tag = lstDirectoryFiles;
				}
				if (!lstDirectoryFiles.Contains(strFile))
					lstDirectoryFiles.Add(strFile);
			}
		}

		/// <summary>
		/// Loads the list of mods that installed the file selected in <see cref="lvwFiles"/>.
		/// </summary>
		protected void loadInstallingMods()
		{
			if (lvwFiles.SelectedItems.Count == 0)
				return;
			rlvOverwrites.Items.Clear();
			string strFile = (string)lvwFiles.SelectedItems[0].Tag;
			List<string> lstInstallers = InstallLog.Current.GetInstallingMods(strFile);
			ListViewItem lviMod = null;
			foreach (string strMod in lstInstallers)
			{
				lviMod = rlvOverwrites.Items.Add(strMod);
				lviMod.Name = lviMod.Text;
			}
			rlvOverwrites.Items[rlvOverwrites.Items.Count - 1].BackColor = Color.LightGreen;
			rlvOverwrites.Items[rlvOverwrites.Items.Count - 1].Selected = true;
		}

		/// <summary>
		/// Handles the <see cref="TreeView.AfterSelect"/> event of the folder tree view.
		/// </summary>
		/// <remarks>
		/// This method lists the files in the currently selected directory.
		/// </remarks>
		/// <param name="sender">The object that triggered the event</param>
		/// <param name="e">A <see cref="TreeViewEventArgs"/> describing the event arguments.</param>
		private void tvwFolders_AfterSelect(object sender, TreeViewEventArgs e)
		{
			TreeNode tndDirectory = e.Node;
			List<string> lstDirectoryFiles = (List<string>)tndDirectory.Tag;
			lvwFiles.Items.Clear();
			if ((lstDirectoryFiles == null) || (lstDirectoryFiles.Count == 0))
				return;
			foreach (string strFile in lstDirectoryFiles)
			{
				ListViewItem lviFile = new ListViewItem(Path.GetFileName(strFile));
				lviFile.Name = lviFile.Text;
				lviFile.Tag = strFile;
				FileInfo fliFile = new FileInfo(Path.GetFullPath(Path.Combine("data", strFile)));
				lviFile.SubItems.Add(fliFile.CreationTime.ToString("g"));
				lviFile.SubItems.Add(fliFile.LastWriteTime.ToString("g"));
				lviFile.SubItems.Add(((Int64)Math.Ceiling(fliFile.Length / 1024.0)) + " KB");
				imlFiles.Images.Add(fliFile.Extension, System.Drawing.Icon.ExtractAssociatedIcon(fliFile.FullName));
				lviFile.ImageKey = fliFile.Extension;
				lvwFiles.Items.Add(lviFile);
			}
			lvwFiles.Items[0].Selected = true;
		}

		/// <summary>
		/// Handles the <see cref="ListView.SelectedIndexChanged"/> event of the file list view.
		/// </summary>
		/// <remarks>
		/// This method lists the mods that installed the selected file.
		/// </remarks>
		/// <param name="sender">The object that triggered the event</param>
		/// <param name="e">A <see cref="TreeViewEventArgs"/> describing the event arguments.</param>
		private void lvwFiles_SelectedIndexChanged(object sender, EventArgs e)
		{
			loadInstallingMods();
		}

		/// <summary>
		/// Handles the <see cref="ListView.DragDrop"/> event of the installer list view.
		/// </summary>
		/// <remarks>
		/// This method changes the order of the currently selected file's installers.
		/// </remarks>
		/// <param name="sender">The object that triggered the event</param>
		/// <param name="e">A <see cref="TreeViewEventArgs"/> describing the event arguments.</param>
		private void rlvOverwrites_DragDrop(object sender, DragEventArgs e)
		{
			List<string> lstOrderedMods = new List<string>();
			foreach (ListViewItem lviMod in rlvOverwrites.Items)
				lstOrderedMods.Add(lviMod.Text);

			string strFile = (string)lvwFiles.SelectedItems[0].Tag;
			ModInstallReorderer mirReorderer = new ModInstallReorderer();
			if (!mirReorderer.ReorderFileInstallers(strFile, lstOrderedMods))
				loadInstallingMods();
			else
			{
				for (Int32 i = rlvOverwrites.Items.Count - 2; i >= 0; i--)
					rlvOverwrites.Items[i].BackColor = SystemColors.Window;
				rlvOverwrites.Items[rlvOverwrites.Items.Count - 1].BackColor = Color.LightGreen;
			}
		}

		/// <summary>
		/// Handles the <see cref="Control.SizeChanged"/> event of the installer list view.
		/// </summary>
		/// <remarks>
		/// This resizes the column so that it is full width.
		/// </remarks>
		/// <param name="sender">The object that triggered the event</param>
		/// <param name="e">A <see cref="TreeViewEventArgs"/> describing the event arguments.</param>
		private void rlvOverwrites_SizeChanged(object sender, EventArgs e)
		{
			rlvOverwrites.Columns[0].Width = rlvOverwrites.ClientSize.Width - 3;
		}

		/// <summary>
		/// Handles the <see cref="RadioButton.CheckedChanged"/> event of the order by fil radio button.
		/// </summary>
		/// <remarks>
		/// This reloads the directory tree to to be ordered as requested.
		/// </remarks>
		/// <param name="sender">The object that triggered the event</param>
		/// <param name="e">A <see cref="EventArgs"/> describing the event arguments.</param>
		private void radByFile_CheckedChanged(object sender, EventArgs e)
		{
			LoadFiles();
		}
	}
}