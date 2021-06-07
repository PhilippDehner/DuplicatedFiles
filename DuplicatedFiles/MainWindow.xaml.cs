using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;

namespace DuplicatedFiles
{
	/// <summary>
	/// Interaktionslogik für MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		#region enums

		public enum AnalysisStates
		{
			[Description("unknown")]
			unknown,

			[Description("Noch keine Analyse durchgeführt")]
			NoAnalysisDone,

			[Description("Alle Dateien suchen")]
			GetAllFiles,

			[Description("Liste mit allen Dateien erstellen")]
			CreateListAllFiles,

			[Description("Search for duplicates")]
			DuplicatesSearching,

			[Description("Analyse durchgeführt")]
			AnalysisDone
		}

		#endregion

		#region types
		public class SettingsClass
		{
			#region Trash
			/// <summary>
			/// true == Delete
			/// false == Move to Trash folder
			/// </summary>
			public bool DeleteMode;

			/// <summary>
			/// true == without directory structure
			/// false == with directory structure
			/// </summary>
			public bool TrashMode = true;

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

				DeleteMode = false;
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
			public string FullName
			{
				get
				{
					return fileInfo.FullName;
				}
			}
			public string Name
			{
				get
				{
					return fileInfo.Name;
				}
			}
			public long FileSize
			{
				get
				{
					return fileInfo.Length;
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

			public OwnFile(string filePath)
			{
				this.fileInfo = new FileInfo(filePath);

				using (var sha256 = System.Security.Cryptography.SHA256.Create())
				{
					using (var stream = System.IO.File.OpenRead(this.FullName))
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

		#region properties
		public SettingsClass Settings;

		private List<OwnFile> allFiles;

		private List<List<OwnFile>> duplicates = new List<List<OwnFile>>();

		private int _currentDuplicate;
		public int CurrentDuplicate
		{
			get
			{
				return _currentDuplicate;
			}
			set
			{
				_currentDuplicate = value;
				if (duplicates == null || duplicates.Count == 0)
					return;

				if (_currentDuplicate > duplicates.Count - 1)
					_currentDuplicate = 0;
				if (_currentDuplicate < 0)
					_currentDuplicate = duplicates.Count - 1;

				SameFilesList.ItemsSource = duplicates[_currentDuplicate];
			}
		}

		private AnalysisStates _analysisStates;
		private AnalysisStates AnalysisState
		{
			set
			{
				if (value == null)
				{
					_analysisStates = AnalysisStates.unknown;
					return;
				}
				_analysisStates = value;
				SBI_Analysestatus.Content = value.ToDescription();

				switch (_analysisStates)
				{
					case AnalysisStates.NoAnalysisDone:
						But_EditDuplicates.IsEnabled = false;
						break;
					case AnalysisStates.GetAllFiles:
					case AnalysisStates.CreateListAllFiles:
						But_EditDuplicates.IsEnabled = false;
						break;
					case AnalysisStates.DuplicatesSearching:
						But_EditDuplicates.IsEnabled = true;
						break;
					case AnalysisStates.AnalysisDone:
						But_EditDuplicates.IsEnabled = true;
						Tab_DuplicatedFiles.IsEnabled = true;
						break;
					default:
						break;
				}
			}
		}
		#endregion

		public MainWindow()
		{
			InitializeComponent();
			this.DataContext = this;

			AnalysisState = AnalysisStates.NoAnalysisDone;

			Settings = ReadSettingsFromXml();
			Settings.Settingsupdater();

			SearchingFoldersList.ItemsSource = Settings.SearchingPaths;

			RB_DeleteModeTrash.IsChecked = !Settings.DeleteMode;
			RB_DeleteModeDeleting.IsChecked = Settings.DeleteMode;
			RB_SeetingsTrashWithoutStructure.IsChecked = Settings.TrashMode;
			RB_SeetingsTrashWithStructure.IsChecked = !Settings.TrashMode;

			Tab_Auswertung.IsEnabled = false;
			Tab_DuplicatedFiles.IsEnabled = false;

			SettingsTrashDirectory.Text = Settings.TrashPath;
			WriteSettingsToXml(Settings);
		}

		#region Methods
		private void UpdateSearchingPaths()
		{
			WriteSettingsToXml(Settings);
			SearchingFoldersList.Items.Refresh();
		}

		private void DuplicatesInfo()
		{
			DuplicatesInfoText.Text = (CurrentDuplicate + 1).ToString() + " / " + duplicates.Count.ToString();

			if (duplicates != null && duplicates.Count > 0 && duplicates[CurrentDuplicate] != null)
			{
				var ds = duplicates[CurrentDuplicate];

				bool sameName = true;
				bool sameSize = true;
				bool sameDate = true;

				for (int i = 1; i < ds.Count; i++)
				{
					if (!ds[i].Name.Equals(ds[i - 1].Name) && sameName)
						sameName = false;
					if (!ds[i].FileSize.Equals(ds[i - 1].FileSize) && sameSize)
						sameSize = false;
					if (!ds[i].LastWriteTime.Equals(ds[i - 1].LastWriteTime) && sameDate)
						sameDate = false;
				}

				if (sameName)
				{
					TB_DuplInfoName.Text = ds[0].Name;
					TB_DuplInfoName.Foreground = Brushes.Black;
				}
				else
				{
					TB_DuplInfoName.Text = "Unterschiedliche Dateinamen";
					TB_DuplInfoName.Foreground = Brushes.Orange;
				}

				if (sameSize)
				{
					TB_DuplInfoSize.Text = ds[0].FileSizeB;
					TB_DuplInfoSize.Foreground = Brushes.Black;
				}
				else
				{
					TB_DuplInfoSize.Text = "Unterschiedliche Dateigrößen";
					TB_DuplInfoSize.Foreground = Brushes.Orange;
				}
				if (sameDate)
				{
					TB_DuplInfoDate.Text = ds[0].LastWriteTime.ToString();
					TB_DuplInfoDate.Foreground = Brushes.Black;
				}
				else
				{
					TB_DuplInfoDate.Text = "Unterschiedliche Erstelldaten";
					TB_DuplInfoDate.Foreground = Brushes.Orange;
				}
			}
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

		private async void StartAnalysis()
		{
			var state = new Progress<AnalysisStates>(v => AnalysisState = v);
			var foundFiles = new Progress<int>(value => TB_FoundFiles.Text = value.ToString());
			var foundDuplicates = new Progress<int>(value => TB_FoundDuplicates.Text = value.ToString());
			var foundDuplicatesProgress = new Progress<int>(value => PB_FoundDuplicatesProgress.Value = value);
			var foundDuplicatesProgressMaximum = new Progress<int>(value => PB_FoundDuplicatesProgress.Maximum = value);
			var currentDuplicate = new Progress<int>(value => CurrentDuplicate = value);
			await Task.Run(() => { Analysis(state, foundFiles, foundDuplicates, foundDuplicatesProgress, foundDuplicatesProgressMaximum, currentDuplicate); });

			System.Windows.MessageBox.Show("Auswertung beendet", "Auswertung");
		}

		private void Analysis(IProgress<AnalysisStates> state, IProgress<int> foundFiles, IProgress<int> foundDuplicates, IProgress<int> foundDuplicatesProgress, IProgress<int> foundDuplicatesProgressMaximum, IProgress<int> currentDuplicate)
		{
			allFiles = new List<OwnFile>();
			int allFilesCount = 0;
			List<string[]> filePathsList = new List<string[]>();

			// Get all files
			state.Report(AnalysisStates.GetAllFiles);
			foreach (OwnPath searchingPath in Settings.SearchingPaths)
			{
				filePathsList.Add(Directory.GetFiles(searchingPath.FilePath, "*.*", SearchOption.AllDirectories));
			}

			// Search for all files
			state.Report(AnalysisStates.CreateListAllFiles);
			foreach (OwnPath searchingPath in Settings.SearchingPaths)
			{
				foreach (string[] filePaths in filePathsList)
				{
					foreach (string filePath in filePaths)
					{
						allFiles.Add(new OwnFile(filePath));
						foundFiles.Report(allFiles.Count);
					}
				}
			}
			allFilesCount = allFiles.Count;

			foundDuplicatesProgressMaximum.Report(allFilesCount);

			// Find duplicated files
			state.Report(AnalysisStates.DuplicatesSearching);
			duplicates = new List<List<OwnFile>>();
			while (allFiles.Count > 0)
			{
				OwnFile comparingFile = allFiles[0];
				allFiles.RemoveAt(0);
				List<OwnFile> sameFile = new List<OwnFile>();
				List<OwnFile> toDeleteFiles = new List<OwnFile>();
				foreach (OwnFile file in allFiles)
				{
					if (file.Equals(comparingFile))
					{
						sameFile.Add(file);
						toDeleteFiles.Add(file);
					}
				}
				foreach (OwnFile file in toDeleteFiles)
				{
					allFiles.Remove(file);
				}
				if (sameFile.Count > 0)
				{
					sameFile.Add(comparingFile);
					duplicates.Add(sameFile);

					foundDuplicates.Report(duplicates.Count);
					Thread.Sleep(10);
				}

				foundDuplicatesProgress.Report(allFilesCount - allFiles.Count);
				//foundDuplicatesProgressMaximum.Report(allFiles.Count);
			}

			state.Report(AnalysisStates.AnalysisDone);
			currentDuplicate.Report(0);
		}
		#endregion

		#region Events
		private void DuplicatesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			//ListBoxItem lbi = ((sender as System.Windows.Controls.ListBox).SelectedItem as ListBoxItem);
		}

		private void DeleteFile(object sender, RoutedEventArgs e)
		{
			FrameworkElement baseobj = sender as FrameworkElement;
			OwnFile file = baseobj.DataContext as OwnFile;

			duplicates[CurrentDuplicate].Remove(file);
			CurrentDuplicate = CurrentDuplicate;

			if (Settings.DeleteMode)
			{
				File.Delete(file.FullName);
			}
			else
			{
				if (Settings.TrashMode)
				{
					File.Move(file.FullName, Settings.TrashPath + "\\" + file.Name);
				}
				else
				{
					throw new NotImplementedException();
				}
			}

			if (duplicates[CurrentDuplicate].Count <= 1)
			{
				duplicates.RemoveAt(CurrentDuplicate);
				CurrentDuplicate = CurrentDuplicate;
			}

			DuplicatesInfo();
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
			SettingsTrashDirectory.Text = Settings.TrashPath;
			WriteSettingsToXml(Settings);
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

		private void NextDuplicateCommand(object sender, RoutedEventArgs e)
		{
			if (duplicates.Count > 0)
			{
				CurrentDuplicate++;
				DuplicatesInfo();
			}
		}

		private void PreviousDuplicateCommand(object sender, RoutedEventArgs e)
		{
			if (duplicates.Count > 0)
			{
				CurrentDuplicate--;
				DuplicatesInfo();
			}
		}

		private void SettingsDeleteFile_Checked(object sender, RoutedEventArgs e)
		{
			Settings.DeleteMode = true;
		}

		private void SettingsFileToTrash_Checked(object sender, RoutedEventArgs e)
		{
			Settings.DeleteMode = false;
		}

		private void EditDuplicates(object sender, RoutedEventArgs e)
		{
			Tab_DuplicatedFiles.IsEnabled = true;
			Tab_DuplicatedFiles.IsSelected = true;
			DuplicatesInfo();
		}

		private void StartAnalysis_Click(object sender, RoutedEventArgs e)
		{
			TB_FoundDuplicates.Text = "0";
			TB_FoundFiles.Text = "0";

			Tab_Auswertung.IsEnabled = true;
			Tab_Auswertung.IsSelected = true;
			But_EditDuplicates.IsEnabled = false;

			StartAnalysis();

			DuplicatesInfo();
			But_EditDuplicates.IsEnabled = true;
		}

		void Window_Closing(object sender, CancelEventArgs e)
		{
		}
		#endregion


		/// <summary>
		/// Finds a Child of a given item in the visual tree. 
		/// </summary>
		/// <param name="parent">A direct parent of the queried item.</param>
		/// <typeparam name="T">The type of the queried item.</typeparam>
		/// <param name="childName">x:Name or Name of child. </param>
		/// <returns>The first parent item that matches the submitted type parameter. 
		/// If not matching item can be found, a null parent is being returned.</returns>
		private static T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
		{
			// Confirm parent and childName are valid. 
			if (parent == null) return null;

			T foundChild = null;

			int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
			for (int i = 0; i < childrenCount; i++)
			{
				var child = VisualTreeHelper.GetChild(parent, i);
				// If the child is not of the request child type child
				T childType = child as T;
				if (childType == null)
				{
					// recursively drill down the tree
					foundChild = FindChild<T>(child, childName);

					// If the child is found, break so we do not overwrite the found child. 
					if (foundChild != null) break;
				}
				else if (!string.IsNullOrEmpty(childName))
				{
					var frameworkElement = child as FrameworkElement;
					// If the child's name is set for search
					if (frameworkElement != null && frameworkElement.Name == childName)
					{
						// if the child's name is of the request name
						foundChild = (T)child;
						break;
					}
				}
				else
				{
					// child element found.
					foundChild = (T)child;
					break;
				}
			}

			return foundChild;
		}
	}

	public static class AttributesHelperExtension
	{
		public static string ToDescription(this Enum value)
		{
			var da = (DescriptionAttribute[])(value.GetType().GetField(value.ToString())).GetCustomAttributes(typeof(DescriptionAttribute), false);
			return da.Length > 0 ? da[0].Description : value.ToString();
		}
	}

	public class ImageConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var path = value as string;

			if (path == null)
			{
				return DependencyProperty.UnsetValue;
			}
			//create new stream and create bitmap frame
			var bitmapImage = new BitmapImage();
			bitmapImage.BeginInit();
			try
			{
				bitmapImage.StreamSource = new FileStream(path, FileMode.Open, FileAccess.Read);
				//load the image now so we can immediately dispose of the stream
				bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				bitmapImage.EndInit();
				//clean up the stream to avoid file access exceptions when attempting to delete images
				bitmapImage.StreamSource.Dispose();
				return bitmapImage;
			}
			catch (Exception)
			{
				//do smth
			}
			return bitmapImage;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
