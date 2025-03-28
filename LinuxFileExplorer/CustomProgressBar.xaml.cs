using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace LinuxFileExplorer
{
    public partial class CustomProgressBar : UserControl
    {
        public CustomProgressBar()
        {
            InitializeComponent();
            DataContext = this;
        }

        public double ProgressValue
        {
            get { return (double)GetValue(ProgressValueProperty); }
            set { SetValue(ProgressValueProperty, value); }
        }

        public static readonly DependencyProperty ProgressValueProperty =
            DependencyProperty.Register("ProgressValue", typeof(double), typeof(CustomProgressBar), new PropertyMetadata(0.0));

        public string ProgressText
        {
            get { return (string)GetValue(ProgressTextProperty); }
            set { SetValue(ProgressTextProperty, value); }
        }

        public static readonly DependencyProperty ProgressTextProperty =
            DependencyProperty.Register("ProgressText", typeof(string), typeof(CustomProgressBar), new PropertyMetadata("0%"));
    }
}