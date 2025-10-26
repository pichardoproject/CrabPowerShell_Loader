using System;
using System.Windows.Forms;

namespace WinFormsApp2
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Initialize application and run your main window
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm()); // 👈 This launches MainForm.cs
        }
    }
}
