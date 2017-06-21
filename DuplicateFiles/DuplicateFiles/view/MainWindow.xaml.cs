using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
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

namespace DuplicateFiles.view
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Restricts typing into texboxes to text that can be parsed as float
        /// </summary>
        private void PreviewTextInputFloat(object sender, TextCompositionEventArgs e)
        {
            TextBox textbox = (TextBox) sender;
            e.Handled = !isTextValidFloat(textbox.Text + e.Text);
        }

        /// <summary>
        /// Restricts pasting into textboxes to text that can be parsed as float
        /// </summary>
        private void TextBoxPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(String)))
            {
                String text = (String)e.DataObject.GetData(typeof(String));
                if (!isTextValidFloat(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        /// <summary>
        /// Check if a text can be parsed as float
        /// </summary>
        private static bool isTextValidFloat(string text)
        {
            float parsed;
            bool isFloat = float.TryParse(text, out parsed);
            return isFloat && parsed >= 0 && parsed <= 1;
        }
    }
}
