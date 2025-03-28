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
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LinuxExplorer
{
    /// <summary>
    /// Interaction logic for INPInformations.xaml
    /// </summary>
    /// 
    public class Infor
    {
        public int NumInstances { get; set; }
        public int NumNodes { get; set; }
        public int NumDofs { get; set; }
    }
    public partial class INPInformations : Window
    {
        public INPInformations(Dictionary<string, Instance> instanceData)
        {
            InitializeComponent();

            InpDataGrid.Items.Clear();
            InpDataGrid.ItemsSource = instanceData;

            Infor infor = new Infor();
            foreach (var item in instanceData)
            {
                infor.NumInstances += 1;
                infor.NumNodes += item.Value.Part.NumNode;
                infor.NumDofs += item.Value.Part.NumDof;
            }
            List<Infor> infors = new List<Infor>();
            infors.Add(infor);
            SummaryGrid.Items.Clear();
            SummaryGrid.ItemsSource = infors;
        }
    }
}
