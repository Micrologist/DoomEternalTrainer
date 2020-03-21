using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;

namespace DoomTrainer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{

		globalKeyboardHook hook = new globalKeyboardHook();
		SigScanTarget scanTarget;
		IntPtr xLocPtr;
		IntPtr yLocPtr;
		IntPtr zLocPtr;
		IntPtr zVelPtr;
		DeepPointer raxPointer = new DeepPointer("DOOMEternalx64vk.exe", 0x06121BB8, 0x38, 0x28, 0x0);
		DeepPointer eaxPointer = new DeepPointer("DOOMEternalx64vk.exe", 0x04C7CA08, 0xCB0, 0xDF8, 0x1D0, 0x88);
		DeepPointer zVelDP = new DeepPointer("DOOMEternalx64vk.exe", 0x04C7CA08, 0x1510, 0x598, 0x1D0,  0x3F48);
		float[] storedPos = new float[3] { 0f, 0f, 0f };
		Process process;

		DeepPointer restrictedCommandsDPTR = new DeepPointer("DOOMEternalx64vk.exe", 0x442741);
		DeepPointer restrictedKeyPressDPTR = new DeepPointer("DOOMEternalx64vk.exe", 0x44FCE7);
		IntPtr restrictedCommandsPtr;
		IntPtr restrictedKeyPressPtr;



		public MainWindow()
		{
			InitializeComponent();
			hook.KeyDown += Hook_KeyDown;
			hook.KeyUp += Hook_KeyUp;
			hook.HookedKeys.Add(System.Windows.Forms.Keys.F5);
			hook.HookedKeys.Add(System.Windows.Forms.Keys.F6);

			Timer timer = new Timer();
			timer.Interval = (16); // 60 Hz
			timer.Tick += new EventHandler(updateTick);
			timer.Start();

		}

		public void updateTick(object snder, EventArgs e){

			
			if (!GetPointers())
				return;
			if (process.HasExited)
			{
				process = null;
				return;
			}
			

			float curX;
			process.ReadValue<float>(xLocPtr, out curX);
			float curY;
			process.ReadValue<float>(yLocPtr, out curY);
			float curZ;
			process.ReadValue<float>(zLocPtr, out curZ);

			
			unlockButton.IsEnabled = restrictedCommandsDPTR.DerefBytes(process, 1)[0] > 0;

			TB1.Text = "Current Position\nX: " + curX.ToString("0.00") + "\nY: " + curY.ToString("0.00") + "\nZ: " + curZ.ToString("0.00") + "\n\nStored Position\nX: " + storedPos[0].ToString("0.00") + "\nY: " + storedPos[1].ToString("0.00") + "\nZ: " + storedPos[2].ToString("0.00");
		}
		public void Update(){
			Debug.WriteLine("test");
		}

		private void Hook_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			e.Handled = true;
		}

		private void Hook_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (GetPointers())
			{
				if (e.KeyCode == System.Windows.Forms.Keys.F5)
				{
					process.ReadValue<float>(xLocPtr, out storedPos[0]);
					process.ReadValue<float>(yLocPtr, out storedPos[1]);
					process.ReadValue<float>(zLocPtr, out storedPos[2]);
				}
				if (e.KeyCode == System.Windows.Forms.Keys.F6)
				{
					
					process.WriteBytes(xLocPtr, BitConverter.GetBytes(storedPos[0]));
					process.WriteBytes(yLocPtr, BitConverter.GetBytes(storedPos[1]));
					process.WriteBytes(zLocPtr, BitConverter.GetBytes(storedPos[2]+ 1f));
					process.WriteBytes(zVelPtr, BitConverter.GetBytes(1f));
				}
			}
			e.Handled = true;
		}



		private bool GetPointers()
		{
			List<Process> processList = Process.GetProcesses().ToList().FindAll(x => x.ProcessName.Contains("DOOMEternalx64vk"));
			if (processList.Count == 0)
				return false;
			process = processList[0];
			if (process.HasExited)
			{
				process = null;
				return false;
			}

			IntPtr raxIntPtr;
			raxPointer.DerefOffsets(process, out raxIntPtr);

			IntPtr eaxIntPtr;
			eaxPointer.DerefOffsets(process, out eaxIntPtr);
			int eaxValue = process.ReadValue<int>(eaxIntPtr);

			
			eaxValue &= 0xFFFFFF;
			eaxValue *= 0xB0;

			IntPtr posPointer = new IntPtr(raxIntPtr.ToInt64() + eaxValue + 0x30);


			float xFloat;
			process.ReadValue<float>(posPointer, out xFloat);

			xLocPtr = posPointer;
			yLocPtr = posPointer + 4;
			zLocPtr = posPointer + 8;
			zVelDP.DerefOffsets(process, out zVelPtr);

			restrictedCommandsDPTR.DerefOffsets(process, out restrictedCommandsPtr);
			restrictedKeyPressDPTR.DerefOffsets(process, out restrictedKeyPressPtr);
			return true;
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			if (!GetPointers())
				return;

			process.WriteBytes(restrictedCommandsPtr, BitConverter.GetBytes(0));
			process.WriteBytes(restrictedKeyPressPtr, BitConverter.GetBytes(0));
		}
	}
}
