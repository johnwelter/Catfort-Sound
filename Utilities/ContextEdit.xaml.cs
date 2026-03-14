using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CatfortSound.Utilities
{
    /// <summary>
    /// Interaction logic for ContextEdit.xaml
    /// </summary>
    public partial class ContextEdit : Window, INotifyPropertyChanged
    {
        string tooltip;
        public string Tooltip
        {
            get { return tooltip; }
            set {
                    tooltip = value;
                    OnPropertyChanged(nameof(Tooltip));
                }
        }
        string valuetxt;

        public string Value
        {
            get { return valuetxt; }
            set
            {
                valuetxt = value;
                OnPropertyChanged(nameof(Value));
            }
        }
        public ContextEdit()
        {
            InitializeComponent();
            DataContext = this;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Init(string context, string defaultValue, ref int output)
        {
            Tooltip = context;
            Value = defaultValue;

            bool? result = this.ShowDialog();
            output = int.Parse(Value);
        }

        private void UI_Confirm_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            Close();
        }
    }
}
