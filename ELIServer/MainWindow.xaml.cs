using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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

namespace ELIServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MessageSocketManager messageSocketManager;

        public MainWindow()
        {
            InitializeComponent();
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
            messageSocketManager = new MessageSocketManager(this);
        }

        private void StopApplication(object sender, CancelEventArgs e)
        {            
            System.Environment.Exit(1);
            MessageSocketManager.SetRunning(false);
        }

        static void OnProcessExit(object sender, EventArgs e)
        {
            MessageSocketManager.SetRunning(false);
        }

        public void SetNumberOfConnectedClients(int amount)
        {
            // Update the connectedClientsLabel on the main thread.
            Application.Current.Dispatcher.Invoke(
                delegate ()
                {
                    connectedClientsLabel.Content = amount;
                });
        }

        public void SetNumberOfConnectedCalls(int amount)
        {
            // Update the connectedCallsLabel on the main thread.
            Application.Current.Dispatcher.Invoke(
             delegate ()
             {
                 connectedCallsLabel.Content = amount;
             });
        }

        public void SetNumberOfPendingCalls(int amount)
        {
            // Update the pendingCallsLabel on the main thread.
            Application.Current.Dispatcher.Invoke(
             delegate ()
             {
                 pendingCallsLabel.Content = amount;
             });
        }


    }
}
