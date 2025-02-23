using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace RelativeImageSource.view; 
/// <summary>
/// Interaction logic for ImageContainer.xaml
/// </summary>
public partial class ImageContainer : UserControl {
	public ImageContainer() {
		InitializeComponent();
		Debug.WriteLine($"Src: {missing.Source}"); //xaml source reset to null
		missing.Source = new BitmapImage(
			new Uri("../asset/icons/PascalNameDiceX.png", UriKind.Relative));
		Debug.WriteLine($"Src 2: {missing.Source}"); //converted to "pack://application:,,,/asset/icons/PascalNameDiceX.png"
		Loaded += onLoaded;
	}

	private void onLoaded(object sender, RoutedEventArgs e) {
		Debug.WriteLine($"Src 3: {missing.Source}"); //reset again to null
	}
}
