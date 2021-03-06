using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using fomm.Transactions;
#if TRACE
using System.Diagnostics;
#endif

namespace ChinhDo.Transactions
{
	/// <summary>
	/// File Resource Manager. Allows inclusion of file system operations in transactions.
	/// http://www.chinhdo.com/20080825/transactional-file-manager/
	/// </summary>
	public partial class TxFileManager : IFileManager
	{
		/// <summary>
		/// Initializes the <see cref="TxFileManager"/> class.
		/// </summary>
		static TxFileManager()
		{
			_tempFolder = Path.Combine(Path.GetTempPath(), "CdFileMgr");
#if TRACE
			Trace.WriteLine("Setting up the TX File Manager.");
			Trace.WriteLine("Temp Directory: " + _tempFolder);
			Trace.WriteLine("Temp Directory Exists: " + Directory.Exists(_tempFolder));
#endif
			if (!Directory.Exists(_tempFolder))
			{
#if TRACE
				Trace.Write("Creating Temp Directory...");
#endif
				Directory.CreateDirectory(_tempFolder);
#if TRACE
				Trace.WriteLine("Created: " + Directory.Exists(_tempFolder));
#endif
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether Transactions are enabled.
		/// </summary>
		public bool TxEnabled
		{
			get { return _txEnabled; }
			set { _txEnabled = value; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether to ignore exceptions during Rollback.
		/// </summary>
		public bool IgnoreExceptionsInRollback
		{
			get { return _ignoreExceptionsInRollback; }
			set { _ignoreExceptionsInRollback = value; }
		}

		#region IFileOperations

		/// <summary>
		/// Appends the specified string the file, creating the file if it doesn't already exist.
		/// </summary>
		/// <param name="path">The file to append the string to.</param>
		/// <param name="contents">The string to append to the file.</param>
		public void AppendAllText(string path, string contents)
		{
			GetEnlistment().AppendAllText(path, contents);
		}

		/// <summary>
		/// Copies the specified <paramref name="sourceFileName"/> to <paramref name="destFileName"/>.
		/// </summary>
		/// <param name="sourceFileName">The file to copy.</param>
		/// <param name="destFileName">The name of the destination file.</param>
		/// <param name="overwrite">true if the destination file can be overwritten, otherwise false.</param>
		public void Copy(string sourceFileName, string destFileName, bool overwrite)
		{
			GetEnlistment().Copy(sourceFileName, destFileName, true);
		}

		/// <summary>
		/// Creates all directories in the specified path.
		/// </summary>
		/// <param name="path">The directory path to create.</param>
		public void CreateDirectory(string path)
		{
			GetEnlistment().CreateDirectory(path);
		}

		/// <summary>
		/// Deletes the specified file. An exception is not thrown if the file does not exist.
		/// </summary>
		/// <param name="path">The file to be deleted.</param>
		public void Delete(string path)
		{
			GetEnlistment().Delete(path);
		}

		/// <summary>
		/// Moves the specified file to a new location.
		/// </summary>
		/// <param name="srcFileName">The name of the file to move.</param>
		/// <param name="destFileName">The new path for the file.</param>
		public void Move(string srcFileName, string destFileName)
		{
			GetEnlistment().Move(srcFileName, destFileName);
		}

		/// <summary>
		/// Take a snapshot of the specified file. The snapshot is used to rollback the file later if needed.
		/// </summary>
		/// <param name="fileName">The file to take a snapshot for.</param>
		public void Snapshot(string fileName)
		{
			GetEnlistment().Snapshot(fileName);
		}

		/// <summary>
		/// Creates a file, write the specified <paramref name="contents"/> to the file.
		/// </summary>
		/// <param name="path">The file to write to.</param>
		/// <param name="contents">The string to write to the file.</param>
		public void WriteAllText(string path, string contents)
		{
			GetEnlistment().WriteAllText(path, contents);
		}

		/// <summary>
		/// Creates a file, and writes the specified <paramref name="contents"/> to the file. If the file
		/// already exists, it is overwritten.
		/// </summary>
		/// <param name="path">The file to write to.</param>
		/// <param name="contents">The bytes to write to the file.</param>
		public void WriteAllBytes(string path, byte[] contents)
		{
			GetEnlistment().WriteAllBytes(path, contents);
		}

		#endregion

		/// <summary>
		/// Determines whether the specified path refers to a directory that exists on disk.
		/// </summary>
		/// <param name="path">The directory to check.</param>
		/// <returns>True if the directory exists.</returns>
		public bool DirectoryExists(string path)
		{
			return Directory.Exists(path);
		}

		/// <summary>
		/// Determines whether the specified file exists.
		/// </summary>
		/// <param name="path">The file to check.</param>
		/// <returns>True if the file exists.</returns>
		public bool FileExists(string path)
		{
			return File.Exists(path);
		}

		/// <summary>
		/// Gets the files in the specified directory.
		/// </summary>
		/// <param name="path">The directory to get files.</param>
		/// <param name="handler">The <see cref="FileEventHandler"/> object to call on each file found.</param>
		/// <param name="recursive">if set to <c>true</c>, include files in sub directories recursively.</param>
		public void GetFiles(string path, FileEventHandler handler, bool recursive)
		{
			foreach (string fileName in Directory.GetFiles(path))
			{
				bool cancel = false;
				handler(fileName, ref cancel);
				if (cancel)
				{
					return;
				}
			}

			// Check subdirs
			if (recursive)
			{
				foreach (string folderName in Directory.GetDirectories(path))
				{
					GetFiles(folderName, handler, recursive);
				}
			}
		}

		/// <summary>
		/// Creates a temporary file name. File is not automatically created.
		/// </summary>
		/// <param name="extension">File extension (with the dot).</param>
		public string GetTempFileName(string extension)
		{
			string retVal = null;
			do
			{
				retVal = Path.Combine(_tempFolder, (_tempFilesPrefix != null ? _tempFilesPrefix + "-" : "")
					+ Path.GetFileNameWithoutExtension(Path.GetRandomFileName())) + extension;
			} while (File.Exists(retVal));

			Snapshot(retVal);

			return retVal;
		}

		/// <summary>
		/// Creates a temporary file name. File is not automatically created.
		/// </summary>
		public string GetTempFileName()
		{
			return GetTempFileName(".tmp");
		}

		/// <summary>
		/// Gets a temporary directory.
		/// </summary>
		/// <returns>The path to the newly created temporary directory.</returns>
		public string GetTempDirectory()
		{
			return GetTempDirectory(Path.GetTempPath(), string.Empty);
		}

		/// <summary>
		/// Gets a temporary directory.
		/// </summary>
		/// <param name="parentDirectory">The parent directory.</param>
		/// <param name="prefix">The prefix of the directory name.</param>
		/// <returns>Path to the temporary directory. The temporary directory is created automatically.</returns>
		public string GetTempDirectory(string parentDirectory, string prefix)
		{
			string dirName = null;
			do
			{
				dirName = Path.Combine(parentDirectory, prefix + Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));
			} while (File.Exists(dirName));

			CreateDirectory(dirName);

			return dirName;
		}

		protected static string CreateTempFileName(string ext)
		{
			string retVal = null;
			do
			{
				retVal = Path.Combine(_tempFolder, (_tempFilesPrefix != null ? _tempFilesPrefix + "-" : "")
					+ Path.GetFileNameWithoutExtension(Path.GetRandomFileName())) + ext;
			} while (File.Exists(retVal));

			return retVal;
		}

		#region Private

		/// <summary>Dictionary of transaction enlistment objects for the current thread.</summary>
		[ThreadStatic]
		private static Dictionary<string, TxEnlistment> _enlistments;

		private static readonly object _enlistmentsLock = new object();
		private bool _txEnabled = true;
		private readonly static string _tempFolder;
		private readonly static string _tempFilesPrefix = "";
		private bool _ignoreExceptionsInRollback = false;

		private TxEnlistment GetEnlistment()
		{
			Transaction tx = Transaction.Current;
			TxEnlistment enlistment;

			if (TxEnabled && tx != null)
			{
				lock (_enlistmentsLock)
				{
					if (_enlistments == null)
					{
						_enlistments = new Dictionary<string, TxEnlistment>();
					}

					if (_enlistments.ContainsKey(tx.TransactionInformation.LocalIdentifier))
					{
						enlistment = _enlistments[tx.TransactionInformation.LocalIdentifier];
					}
					else
					{
						enlistment = new TxEnlistment(tx);
						enlistment.IgnoreExceptionsInRollback = IgnoreExceptionsInRollback;
						_enlistments.Add(tx.TransactionInformation.LocalIdentifier, enlistment);
					}
				}
			}
			else
			{
				enlistment = new TxEnlistment();
			}

			return enlistment;
		}

		#endregion
	}

	/// <summary>
	/// Delegate to call when a new found is found.
	/// </summary>
	public delegate void FileEventHandler(string fileName, ref bool cancel);
}
