using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Management;

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
		private PerformanceCounter ProcessorTime = new PerformanceCounter("Processor Information", "% Processor Time", "_Total");
		private PerformanceCounter ProcessorPerformance = new PerformanceCounter("Processor Information", "% Processor Performance", "_Total");
		private PerformanceCounter IdleTime = new PerformanceCounter("Processor Information", "% Idle Time", "_Total");
		private ManagementClass ManagementClass = new ManagementClass("Win32_OperatingSystem");
		private PictureBox ProcessorTimeGraph = new PictureBox();
		private PictureBox ProcessorPerformanceLow = new PictureBox();
		private PictureBox ProcessorPerformanceHigh = new PictureBox();
		private PictureBox MemoryGraph = new PictureBox();
		private int ProcessorTimeGraphWidth = 0;
		private int ProcessorPerformanceLowWidth = 0;
		private int ProcessorPerformanceHighWidth = 0;
		private int MemoryGraphWidth = 0;
		private int interval = 250;
		private bool Working = true;

		private void Form1_Load(object sender, EventArgs e)
		{
			try
			{
				SuspendLayout();
				ShowInTaskbar = false;
				TopMost = true;
				Height = (GraphHeight * 3) + (GraphMargin * 2);
				Width = GraphWidth;
				Top = 0;
				Left = Screen.PrimaryScreen.Bounds.Width - Width;
				TransparencyKey = BackColor;
				Opacity = 0.6;
				notifyIcon1.Text = Text;

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
			while (Working)
			{
				DateTime start = DateTime.Now;
				//Debug.WriteLine((start - laststart).TotalMilliseconds);
				var pp = ProcessorPerformance.NextValue() * (100 - IdleTime.NextValue()) / 100;
				var pt = ProcessorTime.NextValue();
				var moc = ManagementClass.GetInstances();
				var mem = 0f;
				foreach (var mo in moc)
				{
					var total = float.Parse($"{mo["TotalVisibleMemorySize"]}");
					var free = float.Parse($"{mo["FreePhysicalMemory"]}");
					mem = (total - free) * 100 / total;
				}
				ProcessorPerformanceLowWidth = (int)Math.Min(GraphWidth, Math.Round(pp * GraphWidth / 100));
				ProcessorPerformanceHighWidth = (int)Math.Max(0, Math.Round((pp - 100) * GraphWidth / 100));
				ProcessorTimeGraphWidth = (int)Math.Round(pt * GraphWidth / 100);
				MemoryGraphWidth = (int)Math.Round(mem * GraphWidth / 100);

				Invoke((FormUpdate)GraphUpdate);
				DateTime finish = DateTime.Now;
				var delay = Math.Max(0, interval - (int)Math.Round((finish - start).TotalMilliseconds));
				Debug.WriteLine(delay);
				Thread.Sleep(delay);
				laststart = start;
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
	}
}