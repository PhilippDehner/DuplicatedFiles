using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Serialization;

namespace DuplicatedFiles
{
	/// <summary>
	/// Interaktionslogik für MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		#region types
		public class SettingsClass
		{
			#region Trash
			public bool DeleteMode;
			public bool TrashMode;
			public string TrashPath;
			#endregion

			[XmlIgnore]
			public string directoryPathSettings = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\DuplicatedFiles\\";
			[XmlIgnore]
			public string fileNameSettings = "settings.xml";
			[XmlIgnore]
			public string FilePathSettings
			{
				get
				{
					return directoryPathSettings + fileNameSettings;
				}
			}

			public int SettingsVersion;

			[XmlIgnore]
			private const int MaxSettingsVersion = 1;

			public List<OwnPath> SearchingPaths;


			public string SearchingPathsToOneString
			{
				get
				{
					string output = "";
					for (int i = 0; i < SearchingPaths.Count(); i++)
					{
						output += SearchingPaths[i];
						if (i < SearchingPaths.Count() - 1)
							output += "\n";
					}
					return output;
				}
			}

			public SettingsClass()
			{
				SearchingPaths = new List<OwnPath>();
				SettingsVersion = 0;
			}

			public void Settingsupdater()
			{
				if (SettingsVersion == 0)
				{
					DeleteMode = true;
					TrashMode = true;
					TrashPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\DuplicatedFilesTrash\\";
					SettingsVersion++;
				}
			}
		}

		public class OwnFile
		{
			private FileInfo fileInfo;
			public string FileName
			{
				get
				{
					return fileInfo.FullName;
				}
			}
			public string FileSizeB
			{
				get
				{
					return fileInfo.Length.ToString() + " B";
				}
			}
			public string FileSizekB
			{
				get
				{
					return (fileInfo.Length / 1024).ToString() + " kB";
				}
			}
			public DateTime FileCreated
			{
				get
				{
					return fileInfo.CreationTime;
				}
			}
			public DateTime LastWriteTime
			{
				get
				{
					return fileInfo.LastWriteTime;
				}
			}

			private byte[] Hash;
			public string HashStr
			{
				get { return BitConverter.ToString(Hash).Replace("-", ""); }
			}

			//private ICommand deleteImage;
			//public ICommand DeleteImage
			//{
			//	get
			//	{
			//		if (this.deleteImage == null)
			//		{
			//			//this.deleteImage = new ICommand<object>(this.ExecuteCloseButton);
			//		}

			//		return this.deleteImage;
			//	}
			//}

			private void ExecuteCloseButton(object err)
			{

			}



			public OwnFile(string filePath)
			{
				this.fileInfo = new FileInfo(filePath);

				using (var sha256 = System.Security.Cryptography.SHA256.Create())
				{
					using (var stream = System.IO.File.OpenRead(this.FileName))
					{
						Hash = sha256.ComputeHash(stream);
					}
				}
			}

			public bool Equals(OwnFile obj)
			{
				return this.HashStr.Equals(obj.HashStr);
				//return this.Hash.Equals(obj.Hash);
			}
		}

		public class Duplicate
		{
			public List<OwnFile> Files;
			public void Add(OwnFile ownFile)
			{
				Files.Add(ownFile);
			}
			public int Count
			{
				get
				{
					return Files.Count;
				}
			}
		}

		public class OwnPath
		{
			public string FilePath { get; set; }

			public OwnPath()
			{
			}

			public OwnPath(string path)
			{
				FilePath = path;
			}

			public override string ToString()
			{
				return this.FilePath;
			}
		}
		#endregion

		public SettingsClass Settings;

		private List<OwnFile> allFiles;

		private List<Duplicate> duplicates;

		public string DuplicatesInfo
		{
			get
			{
				object item = SameFilesList.SelectedItem;
				List<OwnFile> list = item as List<OwnFile>;
				return duplicates.IndexOf(list) + " / " + duplicates.Count.ToString();
			}
		}

		public MainWindow()
		{
			InitializeComponent();

			Settings = ReadSettingsFromXml();
			Settings.Settingsupdater();

			allFiles = new List<OwnFile>();

			// Search for all files
			foreach (OwnPath searchingPath in Settings.SearchingPaths)
			{
				string[] filePaths = Directory.GetFiles(searchingPath.FilePath, "*.*", SearchOption.AllDirectories);
				foreach (string filePath in filePaths)
				{
					allFiles.Add(new OwnFile(filePath));
				}
			}

			// Find duplicated files
			duplicates = new List<Duplicate>();
			while (allFiles.Count > 0)
			{
				OwnFile comparingFile = allFiles[0];
				allFiles.RemoveAt(0);
				Duplicate sameFile = new Duplicate();
				foreach (OwnFile file in allFiles)
				{
					if (file.Equals(comparingFile))
					{
						sameFile.Add(file);
					}
				}
				if (sameFile.Count > 0)
				{
					sameFile.Add(comparingFile);
					duplicates.Add(sameFile);
				}
			}

			// Output
			foreach (Duplicate sameFiles in duplicates)
			{
				foreach (OwnFile file in sameFiles.Files)
				{
					Console.WriteLine(file.FileName + " | ");
				}
				Console.WriteLine();
			}

			SameFilesList.ItemsSource = duplicates[0].Files;

			SearchingFoldersList.ItemsSource = Settings.SearchingPaths;

			WriteSettingsToXml(Settings);
		}

		#region Methods
		private void UpdateSearchingPaths()
		{
			WriteSettingsToXml(Settings);
			SearchingFoldersList.Items.Refresh();
		}

		private SettingsClass ReadSettingsFromXml()
		{
			XmlSerializer serializer = new XmlSerializer(typeof(SettingsClass));
			SettingsClass s = new SettingsClass();
			try
			{
				using (Stream reader = new FileStream(s.FilePathSettings, FileMode.Open))
				{
					s = (SettingsClass)serializer.Deserialize(reader);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
			return s;
		}
		private void WriteSettingsToXml(SettingsClass settings)
		{
			if (!Directory.Exists(settings.directoryPathSettings))
				Directory.CreateDirectory(settings.directoryPathSettings);
			if (!File.Exists(settings.FilePathSettings))
			{
				FileStream file = File.Create(settings.FilePathSettings);
				file.Close();
			}

			SettingsClass s = new SettingsClass();
			XmlSerializer serializer = new XmlSerializer(typeof(SettingsClass));
			TextWriter writer = new StreamWriter(s.FilePathSettings);
			serializer.Serialize(writer, settings);
			writer.Close();
		}
		#endregion

		#region Events
		private void lbTodoList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{

			ListBoxItem lbi = ((sender as System.Windows.Controls.ListBox).SelectedItem as ListBoxItem);
			//	tb.Text = "   You selected " + lbi.Content.ToString() + ".";
		}

		private void DeleteFile(object sender, RoutedEventArgs e)
		{

		}

		private void TrashLLocation_Click(object sender, RoutedEventArgs e)
		{
			using (var fbd = new System.Windows.Forms.FolderBrowserDialog())
			{
				fbd.SelectedPath = Settings.TrashPath;
				DialogResult result = fbd.ShowDialog();
				if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
				{
					Settings.TrashPath = fbd.SelectedPath;
				}
			}
		}

		private void SearchingFolderAdd(object sender, RoutedEventArgs e)
		{
			try
			{
				using (var fbd = new FolderBrowserDialog())
				{
					DialogResult result = fbd.ShowDialog();
					if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
					{
						OwnPath path = new OwnPath(fbd.SelectedPath);
						Settings.SearchingPaths.Add(path);
						WriteSettingsToXml(Settings);
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}

			UpdateSearchingPaths();
		}

		private void DeleteSearchingFolder(object sender, RoutedEventArgs e)
		{
			FrameworkElement baseobj = sender as FrameworkElement;
			OwnPath path = baseobj.DataContext as OwnPath;
			DialogResult dialogResult = System.Windows.Forms.MessageBox.Show("Möchten Sie wirklich den Pfad " + path.FilePath + " löschen?", "Deleting Searching Path", MessageBoxButtons.YesNo);
			if (dialogResult == System.Windows.Forms.DialogResult.Yes)
			{
				Settings.SearchingPaths.Remove(path);
				UpdateSearchingPaths();
			}
		}

		private void DuplFileOpenExplorer(object sender, RoutedEventArgs e)
		{
			FrameworkElement baseobj = sender as FrameworkElement;
			TextBlock textBlock = baseobj as TextBlock;
			string path = textBlock.Text;
			string args = string.Format("/e, /select, \"{0}\"", path);

			ProcessStartInfo info = new ProcessStartInfo();
			info.FileName = "explorer";
			info.Arguments = args;
			Process.Start(info);
		}

		#endregion

	}
}
