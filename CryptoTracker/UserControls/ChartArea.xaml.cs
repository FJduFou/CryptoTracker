﻿using CryptoTracker.Models;
using Telerik.UI.Xaml.Controls.Chart;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace CryptoTracker.UserControls {
	public sealed partial class ChartArea : UserControl {
        public ChartArea() {
            this.InitializeComponent();
        }

        public static readonly DependencyProperty ChartModelProperty =
        DependencyProperty.Register(
            nameof(ChartModel),
            typeof(ChartModel),
            typeof(ChartArea),
            null);

        public ChartModel ChartModel {
            get => (ChartModel)GetValue(ChartModelProperty);
            set => SetValue(ChartModelProperty, value);
        }
    }
}