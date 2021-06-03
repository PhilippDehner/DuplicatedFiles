using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DuplicatedFiles.UserControls
{
	/// <summary>
	/// Interaktionslogik für Uc_OwnFile.xaml
	/// </summary>
	public partial class Uc_OwnFile : UserControl
	{
		public Uc_OwnFile()
		{
			InitializeComponent();
			this.DataContext = this;
		}

		public string FileName
		{
			get { return (string)GetValue(FileNameProperty); }
			set { SetValue(FileNameProperty, value); }
		}

		public static readonly DependencyProperty FileNameProperty = DependencyProperty.Register("FileName", typeof(string), typeof(Uc_OwnFile));

	}
}
