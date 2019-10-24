using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp2
{
    public partial class Form1 : Form
    {
        const uint WM_SETTEXT = 0x0C;
        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hwnd, uint Msg, int wParam, [MarshalAs(UnmanagedType.LPStr)]string lParam);
        List<Process> Processes = new List<Process>();
        int Counter = 0;

        public Form1()
        {
            InitializeComponent();
            LoadAvailableAssemblies();
        }

        void LoadAvailableAssemblies()
        {
            string except = new FileInfo(Application.ExecutablePath).Name;
            except = except.Substring(0, except.IndexOf("."));
            string[] files = Directory.GetFiles(Application.StartupPath, "*.exe");
            foreach (var file in files)
            {
                string fileName = new FileInfo(file).Name;
                if (fileName.IndexOf(except) == -1)
                    listBox2.Items.Add(fileName);
            }
        }

        void RunProcess(string AssamblyName)
        {
            Process proc = Process.Start(AssamblyName);
            Processes.Add(proc);
            if (Process.GetCurrentProcess().Id == GetParentProcessId(proc.Id))
                MessageBox.Show(proc.ProcessName + " действительно дочерний процесс текущего процесса!");
            proc.EnableRaisingEvents = true;
            proc.Exited += Proc_Exited;
            SetChildWindowText(proc.MainWindowHandle, "Child process #" + (++Counter));
            if (!listBox1.Items.Contains(proc.ProcessName))
                listBox1.Items.Add(proc.ProcessName);
            listBox2.Items.Remove(listBox2.SelectedItem);
        }

        void SetChildWindowText(IntPtr Handle, string text)
        {
            SendMessage(Handle, WM_SETTEXT, 0, text);
        }

        int GetParentProcessId(int Id)
        {
            int parentId = 0;
            using (ManagementObject obj = new ManagementObject("win32_process.handle=" + Id.ToString()))
            {
                obj.Get();
                parentId = Convert.ToInt32(obj["ParentProcessId"]);
            }
            return parentId;
        }

        private void Proc_Exited(object sender, EventArgs e)
        {
            Process proc = sender as Process;
            UpdateList(proc.ProcessName);
            Processes.Remove(proc);
            Counter--;
            int index = 0;
            foreach (var p in Processes)
            {
                SetChildWindowText(p.MainWindowHandle, "Child process #" + ++index);
            }
        }

        private void UpdateList(string ProcName)
        {
            if (listBox2.InvokeRequired)
            {
                listBox2.Invoke(new Action<string>(UpdateList), ProcName);
            }
            else
            {
                listBox1.Items.Remove(ProcName);
                listBox2.Items.Add(ProcName);
            }
        }

        delegate void ProcessDelegate(Process proc);

        void ExecuteOnProcessesByName(string ProcessName, ProcessDelegate func)
        {
            Process[] processes = Process.GetProcessesByName(ProcessName);
            foreach (var process in processes)
            {
                if (Process.GetCurrentProcess().Id == GetParentProcessId(process.Id))
                    func(process);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            RunProcess(listBox2.SelectedItem.ToString());
        }

        void Kill(Process proc)
        {
            proc.Kill();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ExecuteOnProcessesByName(listBox1.SelectedItem.ToString(), Kill);
            listBox1.Items.Remove(listBox1.SelectedItem);
        }

        void CloseMainWindow(Process proc)
        {
            proc.CloseMainWindow();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ExecuteOnProcessesByName(listBox1.SelectedItem.ToString(), CloseMainWindow);
            listBox1.Items.Remove(listBox1.SelectedItem);
        }

        void Refresh(Process proc)
        {
            proc.Refresh();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            ExecuteOnProcessesByName(listBox1.SelectedItem.ToString(), Refresh);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            RunProcess("calc.exe");
        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox2.SelectedItems.Count == 0)
            {
                button1.Enabled = false;
                return;
            }
            button1.Enabled = true;
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedItems.Count == 0)
            {
                button2.Enabled = false;
                button3.Enabled = false;
                button4.Enabled = false;
                return;
            }
            button2.Enabled = true;
            button3.Enabled = true;
            button4.Enabled = true;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (var proc in Processes)
            {
                proc.Kill();
            }
        }
    }
}
