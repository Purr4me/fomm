﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;
using System.Windows.Forms;
using System.IO;
using Fomm.Util;
using ChinhDo.Transactions;
using fomm.Transactions;
#if TRACE
using System.Diagnostics;
#endif

namespace Fomm.Games.Fallout3
{
	/// <summary>
	/// This migrates files used by the mod manager from they're old FOMM (pre 0.13.0)
	/// locations to the new locations.
	/// </summary>
	public class FOMMMigrator
	{
		private BackgroundWorkerProgressDialog m_bwdProgress = null;

		/// <summary>
		/// Starts the migration, if necessary.
		/// </summary>
		/// <returns><lang cref="false"/> if the migration failed;
		/// <lang cref="true"/> otherwise.</returns>
		public bool Migrate()
		{
			if (Properties.Settings.Default.migratedFromPre0130)
				return true;

#if TRACE
			Trace.WriteLine("Check for old FOMM to migrate from...");
			Trace.Indent();
#endif
			string strOldFOMMLocation = (Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Fallout Mod Manager_is1", "InstallLocation", "") ?? "").ToString();
#if TRACE
			Trace.WriteLine("First guess: " + strOldFOMMLocation);
#endif
			if (String.IsNullOrEmpty(strOldFOMMLocation))
			{
				strOldFOMMLocation = (Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Fallout Mod Manager_is1", "InstallLocation", "") ?? "").ToString();
#if TRACE
				Trace.WriteLine("Second guess: " + strOldFOMMLocation);
#endif
			}
			if (String.IsNullOrEmpty(strOldFOMMLocation))
			{
#if TRACE
				Trace.WriteLine("No need to migrate");
				Trace.Unindent();
#endif
				return true;
			}

			string strMessage = "An older version of the mod manager was detected. Would you like to migrate your mods into the new programme?" + Environment.NewLine +
								"If you answer \"No\", you will have to manually copy your mods into: " + Environment.NewLine +
								Program.GameMode.ModDirectory + Environment.NewLine +
								"You will also have to reinstall the mods, so make sure you deactivate them in the old FOMM first." + Environment.NewLine +
								"Clicking \"Cancel\" will close the programme so you can deactivate the mods in the old FOMM, if you so choose." + Environment.NewLine +
								"If you are confused, click \"Yes\".";
			switch (MessageBox.Show(strMessage, "Migrate", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question))
			{
				case DialogResult.Cancel:
					return false;
				case DialogResult.No:
					Properties.Settings.Default.migratedFromPre0130 = true;
					Properties.Settings.Default.Save();
					return true;
			}

			using (TransactionScope tsTransaction = new TransactionScope())
			{
				using (m_bwdProgress = new BackgroundWorkerProgressDialog(DoMigration))
				{
					m_bwdProgress.OverallProgressMaximum = 3;
					m_bwdProgress.OverallProgressStep = 1;
					m_bwdProgress.ItemProgressStep = 1;
					m_bwdProgress.OverallMessage = "Migrating...";
					m_bwdProgress.WorkMethodArguments = strOldFOMMLocation;
					if (m_bwdProgress.ShowDialog() == DialogResult.Cancel)
						return false;

				}
				tsTransaction.Complete();
			}

			Properties.Settings.Default.migratedFromPre0130 = true;
			Properties.Settings.Default.Save();

			return true;
		}

		/// <summary>
		/// This performs the mirgration.
		/// </summary>
		/// <param name="p_objArgs">The path to the old FOMM installation.</param>
		protected void DoMigration(object p_objArgs)
		{
			string strOldFOMMLocation = (string)p_objArgs;
			TxFileManager tfmFileManager = new TxFileManager();

			//copy the mods
			//do we need to copy?
#if TRACE
			Trace.Write("Copying Mods (");
#endif
			if (!Path.Combine(strOldFOMMLocation, "mods").Equals(Program.GameMode.ModDirectory, StringComparison.InvariantCultureIgnoreCase))
			{
				List<string> lstModFiles = new List<string>();
				lstModFiles.AddRange(Directory.GetFiles(Path.Combine(strOldFOMMLocation, "mods"), "*.fomod"));
				lstModFiles.AddRange(Directory.GetFiles(Path.Combine(strOldFOMMLocation, "mods"), "*.xml"));
				m_bwdProgress.ItemMessage = "Copying mods...";
#if TRACE
				Trace.WriteLine(lstModFiles.Count + "):");
				Trace.Indent();
#endif
				m_bwdProgress.ItemProgressMaximum = lstModFiles.Count;
				m_bwdProgress.ItemProgress = 0;
				string strModFileName = null;
				foreach (string strMod in lstModFiles)
				{
					strModFileName = Path.GetFileName(strMod);
					m_bwdProgress.ItemMessage = "Copying mods (" + strModFileName + ")...";
#if TRACE
					Trace.WriteLine(strMod + " => " + Path.Combine(Program.GameMode.ModDirectory, strModFileName));
#endif
					tfmFileManager.Copy(strMod, Path.Combine(Program.GameMode.ModDirectory, strModFileName), true);
					//File.Copy(strMod, Path.Combine(Program.GameMode.ModDirectory, Path.GetFileName(strMod)));
					m_bwdProgress.StepItemProgress();
					if (m_bwdProgress.Cancelled())
					{
#if TRACE
						Trace.Unindent();
						Trace.WriteLine("Cancelled copying Mods.");
#endif
						return;
					}
				}
			}
#if TRACE
			else
			{
				Trace.WriteLine("No Need).");
			}
			Trace.Unindent();
			Trace.WriteLine("Done copying Mods.");
#endif

			m_bwdProgress.StepOverallProgress();

			//copy overwrites folder
			//do we need to?
#if TRACE
			Trace.WriteLine("Copying overwrite files (");
#endif
			if (!Path.Combine(strOldFOMMLocation, "overwrites").Equals(((Fallout3GameMode)Program.GameMode).OverwriteDirectory, StringComparison.InvariantCultureIgnoreCase))
			{
				string[] strOverwriteFiles = Directory.GetFiles(Path.Combine(strOldFOMMLocation, "overwrites"), "*.*", SearchOption.AllDirectories);
				m_bwdProgress.ItemMessage = "Copying overwrites...";
				m_bwdProgress.ItemProgressMaximum = strOverwriteFiles.Length;
				m_bwdProgress.ItemProgress = 0;
#if TRACE
				Trace.WriteLine(strOverwriteFiles.Length + "):");
				Trace.Indent();
#endif
				FileUtil.Copy(tfmFileManager, Path.Combine(strOldFOMMLocation, "overwrites"), ((Fallout3GameMode)Program.GameMode).OverwriteDirectory, OverwriteFileCopied);
			}
#if TRACE
			else
			{
				Trace.WriteLine("No Need).");
			}
			Trace.Unindent();
			Trace.WriteLine("Done copying overwrite files.");
#endif

			m_bwdProgress.StepOverallProgress();

			//copy install logs
			//do we need to?
#if TRACE
			Trace.WriteLine("Copying install logs (");
#endif
			if (!Path.Combine(strOldFOMMLocation, "fomm").Equals(Program.GameMode.InstallInfoDirectory, StringComparison.InvariantCultureIgnoreCase))
			{
				string[] strMiscFiles = Directory.GetFiles(Path.Combine(strOldFOMMLocation, "fomm"), "InstallLog.xml*");
				m_bwdProgress.ItemMessage = "Copying info files...";
				m_bwdProgress.ItemProgressMaximum = strMiscFiles.Length;
				m_bwdProgress.ItemProgress = 0;
#if TRACE
				Trace.WriteLine(strMiscFiles.Length + "):");
				Trace.Indent();
#endif
				foreach (string strFile in strMiscFiles)
				{
#if TRACE
					Trace.WriteLine(strFile + " => " + Path.Combine(Program.GameMode.InstallInfoDirectory, Path.GetFileName(strFile)));
#endif
					tfmFileManager.Copy(strFile, Path.Combine(Program.GameMode.InstallInfoDirectory, Path.GetFileName(strFile)), true);
					m_bwdProgress.StepItemProgress();
					if (m_bwdProgress.Cancelled())
					{
#if TRACE
						Trace.Unindent();
						Trace.WriteLine("Cancelled copying install logs.");
#endif
						return;
					}
				}
			}
#if TRACE
			else
			{
				Trace.WriteLine("No Need).");
			}
			Trace.Unindent();
			Trace.WriteLine("Done copying install logs.");
#endif

			m_bwdProgress.StepOverallProgress();
		}

		/// <summary>
		/// Called when an overwrite file has been copied as part of the migration.
		/// </summary>
		/// <remarks>
		/// This allows the user to cancel the operation.
		/// </remarks>
		/// <param name="p_strFile">The file that was copied.</param>
		/// <returns><lang cref="true"/> if the user has cancelled;
		/// <lang cref="false"/> otherwise.</returns>
		protected bool OverwriteFileCopied(string p_strFile)
		{
#if TRACE
			if (m_bwdProgress.Cancelled())
			{
				Trace.Unindent();
				Trace.WriteLine("Cancelled copying overwrite files.");
			}
#endif
			return m_bwdProgress.Cancelled();
		}
	}
}
