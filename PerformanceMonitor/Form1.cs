using System;
using System.Diagnostics;
using System.Drawing;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PerformanceMonitor
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
		}

		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams cp = base.CreateParams;
				cp.ExStyle |= 0x00000020;
				return cp;
			}
		}

		private readonly int GraphHeight = 10;
		private readonly int GraphWidth = 200;
		private readonly int GraphMargin = 1;
		private readonly PerformanceCounter ProcessorTime = new PerformanceCounter("Processor Information", "% Processor Time", "_Total");
		private readonly PerformanceCounter ProcessorPerformance = new PerformanceCounter("Processor Information", "% Processor Performance", "_Total");
		private readonly PerformanceCounter IdleTime = new PerformanceCounter("Processor Information", "% Idle Time", "_Total");
		private readonly ManagementClass ManagementClass = new ManagementClass("Win32_OperatingSystem");
		private readonly PictureBox ProcessorTimeGraph = new PictureBox();
		private readonly PictureBox ProcessorPerformanceLow = new PictureBox();
		private readonly PictureBox ProcessorPerformanceHigh = new PictureBox();
		private readonly PictureBox MemoryGraph = new PictureBox();
		private int ProcessorTimeGraphWidth = 0;
		private int ProcessorPerformanceLowWidth = 0;
		private int ProcessorPerformanceHighWidth = 0;
		private int MemoryGraphWidth = 0;
		private readonly int interval = 250;
		private bool Working = true;

		private void ResetForm()
		{
			ShowInTaskbar = false;
			TopMost = true;
			Height = (GraphHeight * 3) + (GraphMargin * 2);
			Width = GraphWidth;
			Top = 0;
			Left = Screen.PrimaryScreen.Bounds.Width - Width;
			TransparencyKey = BackColor;
			Opacity = 0.6;
			notifyIcon1.Text = Text;
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			try
			{
				SuspendLayout();
				ResetForm();

				ProcessorTimeGraph.Left = 0;
				ProcessorTimeGraph.Top = 0;
				ProcessorTimeGraph.Width = GraphWidth;
				ProcessorTimeGraph.Height = GraphHeight;
				ProcessorTimeGraph.BackColor = Color.LawnGreen;
				Controls.Add(ProcessorTimeGraph);

				ProcessorPerformanceHigh.Left = 0;
				ProcessorPerformanceHigh.Top = (GraphHeight * 1) + (GraphMargin * 1);
				ProcessorPerformanceHigh.Width = GraphWidth;
				ProcessorPerformanceHigh.Height = GraphHeight;
				ProcessorPerformanceHigh.BackColor = Color.OrangeRed;
				Controls.Add(ProcessorPerformanceHigh);

				ProcessorPerformanceLow.Location = ProcessorPerformanceHigh.Location;
				ProcessorPerformanceLow.Size = ProcessorPerformanceHigh.Size;
				ProcessorPerformanceLow.BackColor = Color.CornflowerBlue;
				Controls.Add(ProcessorPerformanceLow);

				MemoryGraph.Left = 0;
				MemoryGraph.Top = (GraphHeight * 2) + (GraphMargin * 2);
				MemoryGraph.Width = GraphWidth;
				MemoryGraph.Height = GraphHeight;
				MemoryGraph.BackColor = Color.Violet;
				Controls.Add(MemoryGraph);
				ResumeLayout();

				GC.Collect();
				Task.Run(MonitorWorker);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
				Close();
			}
		}

		private void MonitorWorker()
		{
			DateTime laststart = DateTime.Now;
			DateTime start = DateTime.Now;
			float pp = ProcessorPerformance.NextValue() * (100 - IdleTime.NextValue()) / 100;
			float pt = ProcessorTime.NextValue();
			ManagementObjectCollection moc = ManagementClass.GetInstances();
			float mem = 0f;
			float total = 0f;
			float free = 0f;

			while (Working)
			{
				laststart = start;
				start = DateTime.Now;
				Debug.WriteLine(Math.Round((start - laststart).TotalMilliseconds));
				pp = ProcessorPerformance.NextValue() * (100 - IdleTime.NextValue()) / 100;
				pt = ProcessorTime.NextValue();
				moc = ManagementClass.GetInstances();
				mem = 0f;
				foreach (ManagementBaseObject mo in moc)
				{
					total = float.Parse($"{mo["TotalVisibleMemorySize"]}");
					free = float.Parse($"{mo["FreePhysicalMemory"]}");
					mem = (total - free) * 100 / total;
				}
				ProcessorPerformanceLowWidth = (int)Math.Min(GraphWidth, Math.Round(pp * GraphWidth / 100));
				ProcessorPerformanceHighWidth = (int)Math.Max(0, Math.Round((pp - 100) * GraphWidth / 100));
				ProcessorTimeGraphWidth = (int)Math.Round(pt * GraphWidth / 100);
				MemoryGraphWidth = (int)Math.Round(mem * GraphWidth / 100);

				Invoke((FormUpdate)GraphUpdate);
				DateTime finish = DateTime.Now;
				Thread.Sleep(Math.Max(0, interval - (int)Math.Round((finish - start).TotalMilliseconds)));
			}
		}

		private delegate void FormUpdate();
		private void GraphUpdate()
		{
			ProcessorTimeGraph.Width = ProcessorTimeGraphWidth;
			ProcessorPerformanceLow.Width = ProcessorPerformanceLowWidth;
			ProcessorPerformanceHigh.Width = ProcessorPerformanceHighWidth;
			MemoryGraph.Width = MemoryGraphWidth;
		}

		private void closeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Working = false;
			Thread.Sleep(interval);
			Close();
		}

		private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				ResetForm();
			}
		}
	}
}