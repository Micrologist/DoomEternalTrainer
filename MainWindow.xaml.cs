using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Diagnostics;
using System.Windows.Forms;
using System.Text;

namespace DoomTrainer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{

		globalKeyboardHook hook = new globalKeyboardHook();
		Process process;
		public bool hooked = false;

		DeepPointer raxDP;
		DeepPointer eaxDP;
		DeepPointer velDP;
		DeepPointer rotDP;
		DeepPointer yawDP;
		DeepPointer row1DP;
		DeepPointer row2DP;
		DeepPointer row3DP;
		DeepPointer row4DP;
		DeepPointer row5DP;
		DeepPointer row6DP;
		DeepPointer row7DP;
		DeepPointer row8DP;
		DeepPointer row9DP;

		IntPtr xLocPtr;
		IntPtr yLocPtr;
		IntPtr zLocPtr;
		IntPtr xVelPtr;
		IntPtr yVelPtr;
		IntPtr zVelPtr;
		IntPtr xRotPtr;
		IntPtr yRotPtr;
		IntPtr xYawPtr;
		IntPtr yYawPtr;
		IntPtr row1ptr;
		IntPtr row2ptr;
		IntPtr row3ptr;
		IntPtr row4ptr;
		IntPtr row5ptr;
		IntPtr row6ptr;
		IntPtr row7ptr;
		IntPtr row8ptr;
		IntPtr row9ptr;

		float curX;
		float curY;
		float curZ;
		float curXv;
		float curYv;
		float curZv;
		double hVel;

		float[] storedPos = new float[3] { 0f, 0f, 0f };
		float[] storedVel = new float[3] { 0f, 0f, 0f };
		float[] storedRot = new float[4] { 0f, 0f, 0f, 0f };

		bool retainVel;


		public MainWindow()
		{
			InitializeComponent();
			hook.KeyDown += Hook_KeyDown;
			hook.KeyUp += Hook_KeyUp;
			hook.HookedKeys.Add(System.Windows.Forms.Keys.F5);
			hook.HookedKeys.Add(System.Windows.Forms.Keys.F6);
			hook.HookedKeys.Add(System.Windows.Forms.Keys.F8);

			Timer timer = new Timer();
			timer.Interval = (16); // 60 Hz
			timer.Tick += new EventHandler(updateTick);
			timer.Start();

			retainVel = false;

		}

		public void updateTick(object snder, EventArgs e){

			if (process == null || process.HasExited)
			{
				process = null;
				hooked = false;
			}

			if (!hooked)
				hooked = Hook();

			if (!hooked)
				return;

			DerefPointers();

			
			process.ReadValue<float>(xLocPtr, out curX);
			process.ReadValue<float>(yLocPtr, out curY);
			process.ReadValue<float>(zLocPtr, out curZ);
			process.ReadValue<float>(xVelPtr, out curXv);
			process.ReadValue<float>(yVelPtr, out curYv);
			process.ReadValue<float>(zVelPtr, out curZv);
			hVel = Math.Sqrt(curXv * curXv + curYv * curYv);

			PosBlock.Text = "Current Position\nX: " + curX.ToString("0.00") + "\nY: " + curY.ToString("0.00") + "\nZ: " + curZ.ToString("0.00") + "\n\n\nStored Position\nX: " + storedPos[0].ToString("0.00") + "\nY: " + storedPos[1].ToString("0.00") + "\nZ: " + storedPos[2].ToString("0.00");
			VelBlock.Text = "Current Velocity\nX: " + curXv.ToString("0.00") + "\nY: " + curYv.ToString("0.00") + "\nZ: " + curZv.ToString("0.00") + "\n"+hVel.ToString("0.00")+"m/s\n\nStored Velocity\nX: " + storedVel[0].ToString("0.00") + "\nY: " + storedVel[1].ToString("0.00") + "\nZ: " + storedVel[2].ToString("0.00");

			ShowPos();
		}


		private void Hook_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			e.Handled = true;
		}

		private void Hook_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (hooked)
			{
				if (e.KeyCode == System.Windows.Forms.Keys.F5)
				{
					process.ReadValue<float>(xLocPtr, out storedPos[0]);
					process.ReadValue<float>(yLocPtr, out storedPos[1]);
					process.ReadValue<float>(zLocPtr, out storedPos[2]);
					process.ReadValue<float>(xVelPtr, out storedVel[0]);
					process.ReadValue<float>(yVelPtr, out storedVel[1]);
					process.ReadValue<float>(zVelPtr, out storedVel[2]);
					process.ReadValue<float>(xRotPtr, out storedRot[0]);
					process.ReadValue<float>(yRotPtr, out storedRot[1]);
					process.ReadValue<float>(xYawPtr, out storedRot[2]);
					process.ReadValue<float>(yYawPtr, out storedRot[3]);

				}

				if (e.KeyCode == System.Windows.Forms.Keys.F6)
				{
					

					float curxYaw;
					float curxRot;
					process.ReadValue<float>(xYawPtr, out curxYaw);
					process.ReadValue<float>(xRotPtr, out curxRot);
					float oldxYaw = storedRot[2];
					float oldxRot = storedRot[0];
					float xOffset = (oldxYaw - curxYaw) - (oldxRot - curxRot);

					float curyYaw;
					float curyRot;
					process.ReadValue<float>(yYawPtr, out curyYaw);
					process.ReadValue<float>(yRotPtr, out curyRot);
					float oldyYaw = storedRot[3];
					float oldyRot = storedRot[1];
					float yOffset = ((oldyYaw+180) - (curyYaw+180)) - (oldyRot % 360 - curyRot % 360);
					Console.WriteLine("Yoldyaw: " + oldyYaw.ToString());
					Console.WriteLine("Ycuryaw: " + curyYaw.ToString());

					Console.WriteLine("xoffset: " + xOffset.ToString());
					Console.WriteLine("yoffset: " + yOffset.ToString());

					process.WriteBytes(xRotPtr, BitConverter.GetBytes(storedRot[0] + xOffset));
					process.WriteBytes(yRotPtr, BitConverter.GetBytes(storedRot[1] + yOffset));

					if (retainVel)
					{
						process.WriteBytes(xVelPtr, BitConverter.GetBytes(storedVel[0]));
						process.WriteBytes(yVelPtr, BitConverter.GetBytes(storedVel[1]));
						if (Math.Abs(storedVel[2]) < 1f)
							storedVel[2] = 1f;
						process.WriteBytes(zVelPtr, BitConverter.GetBytes(storedVel[2]));
					}
					else
					{
						process.WriteBytes(xVelPtr, BitConverter.GetBytes(0.01f));
						process.WriteBytes(yVelPtr, BitConverter.GetBytes(0.01f));
						process.WriteBytes(zVelPtr, BitConverter.GetBytes(1f));
					}
					process.WriteBytes(xLocPtr, BitConverter.GetBytes(storedPos[0]));
					process.WriteBytes(yLocPtr, BitConverter.GetBytes(storedPos[1]));
					process.WriteBytes(zLocPtr, BitConverter.GetBytes(storedPos[2]));
				}

				if (e.KeyCode == System.Windows.Forms.Keys.F8){
					Console.WriteLine("Position Pointer: " + xLocPtr.ToString("X16"));
					Console.WriteLine("Velocity Pointer: " + xVelPtr.ToString("X16"));
					Console.WriteLine("Camera Rotation Pointer: " + xRotPtr.ToString("X16"));
					Console.WriteLine("Yaw Pointer: " + xYawPtr.ToString("X16"));

				}

			}
			e.Handled = true;
		}




		private void Button_Click(object sender, RoutedEventArgs e)
		{
			SigScanTarget consoleCmdScanTarget = new SigScanTarget("4C8B0EBA01000000488BCE448BF041FF51??8D");
			SigScanTarget consoleBindScanTarget = new SigScanTarget("4C8B0FBA01000000488BCF448BF041FF51??4C6BC507");
			IntPtr restrictedCommandsPtr;
			IntPtr restrictedKeyPressPtr;

			unlockButton.IsEnabled = false;
			if (!hooked)
			{
				unlockButton.IsEnabled = true;
				return;
			}
			SignatureScanner sigScanner = new SignatureScanner(process, process.MainModule.BaseAddress, process.MainModule.ModuleMemorySize);
			restrictedCommandsPtr = sigScanner.Scan(consoleCmdScanTarget);
			restrictedKeyPressPtr = sigScanner.Scan(consoleBindScanTarget);
			if (restrictedCommandsPtr.ToInt64() != 0 && restrictedKeyPressPtr.ToInt64() != 0)
			{
				restrictedKeyPressPtr += 4;
				restrictedCommandsPtr += 4;
				Console.WriteLine(restrictedKeyPressPtr.ToInt64().ToString("X16"));
				Console.WriteLine(restrictedCommandsPtr.ToInt64().ToString("X16"));
				process.WriteBytes(restrictedCommandsPtr, BitConverter.GetBytes(0));
				process.WriteBytes(restrictedKeyPressPtr, BitConverter.GetBytes(0));
				System.Windows.Forms.MessageBox.Show("Console commands unlocked", "Success");
			}
			else
			{
				System.Windows.Forms.MessageBox.Show("Memory Address not found.\nEither console commands are already unlocked, or your game version is not supported.", "Error");
			}
			unlockButton.IsEnabled = true;
		}

		private void zeroRB_Checked(object sender, RoutedEventArgs e)
		{
			if (zeroRB == null || retainRB == null)
				return;
			zeroRB.IsChecked = true;
			retainRB.IsChecked = false;
			retainVel = false;
		}

		private void retainRB_Checked(object sender, RoutedEventArgs e)
		{
			if (zeroRB == null || retainRB == null)
				return;
			zeroRB.IsChecked = false;
			retainRB.IsChecked = true;
			retainVel = true;
		}

		public void ShowPos()
		{
			process.VirtualProtect(row1ptr, 1024, MemPageProtect.PAGE_READWRITE);
			process.WriteBytes(row1ptr, ToByteArray("%i FPS", 8));

			process.VirtualProtect(row2ptr, 1024, MemPageProtect.PAGE_READWRITE);
			process.WriteBytes(row2ptr, ToByteArray("hvel: "+hVel.ToString("0.00") + " m/s", 79));

			process.VirtualProtect(row3ptr, 1024, MemPageProtect.PAGE_READWRITE);
			process.WriteBytes(row3ptr, ToByteArray("zvel: "+curZv.ToString("0.00") + " m/s", 19));

			process.VirtualProtect(row4ptr, 1024, MemPageProtect.PAGE_READWRITE);
			process.WriteBytes(row4ptr, ToByteArray("", 7));

			process.VirtualProtect(row5ptr, 1024, MemPageProtect.PAGE_READWRITE);
			process.WriteBytes(row5ptr, ToByteArray("", 34));

			process.VirtualProtect(row9ptr, 1024, MemPageProtect.PAGE_READWRITE);
			process.WriteBytes(row9ptr, ToByteArray("", 34));

			process.VirtualProtect(row6ptr, 1024, MemPageProtect.PAGE_READWRITE);
			process.WriteBytes(row6ptr, ToByteArray("pos: "+curX.ToString("0.00")+" "+curY.ToString("0.00")+" "+curZ.ToString("0.00"), 64));

			process.VirtualProtect(row7ptr, 1024, MemPageProtect.PAGE_READWRITE);
			process.WriteBytes(row7ptr, ToByteArray("vel: " + curXv.ToString("0.00") + " " + curYv.ToString("0.00") + " " + curZv.ToString("0.00"), 64));
		}

		public byte[] ToByteArray(string text, int length)
		{
			byte[] output = new byte[length];
			byte[] textArray = Encoding.ASCII.GetBytes(text);
			for (int i = 0; i < length; i++)
			{
				if (i >= textArray.Length)
					break;
				output[i] = textArray[i];
			}
			return output;
		}

		public bool Hook()
		{
			List<Process> processList = Process.GetProcesses().ToList().FindAll(x => x.ProcessName.Contains("DOOMEternalx64vk"));
			if (processList.Count == 0)
			{
				process = null;
				return false;
			}
			process = processList[0];
			if (process.HasExited)
				return false;
			int mainModuleSize = process.MainModule.ModuleMemorySize;
			SetPointersByModuleSize(mainModuleSize);
			return true;
		}

		public void SetPointersByModuleSize(int moduleSize)
		{
			if (moduleSize == 507191296 || moduleSize == 515133440 || moduleSize == 510681088) // STEAM VERSION
			{
				Debug.WriteLine("Found Steam version");
				raxDP = new DeepPointer("DOOMEternalx64vk.exe", 0x06121BB8, 0x38, 0x28, 0x0);
				eaxDP = new DeepPointer("DOOMEternalx64vk.exe", 0x04C7CA08, 0xCB0, 0xDF8, 0x1D0, 0x88);
				velDP = new DeepPointer("DOOMEternalx64vk.exe", 0x04C7CA08, 0x1510, 0x598, 0x1D0, 0x3F40);
				rotDP = new DeepPointer("DOOMEternalx64vk.exe", 0x4C83F38);
				yawDP = new DeepPointer("DOOMEternalx64vk.exe", 0x61AC728);
				row1DP = new DeepPointer("DOOMEternalx64vk.exe", 0x25B4A80);
				row2DP = new DeepPointer("DOOMEternalx64vk.exe", 0x25B4A88);
				row3DP = new DeepPointer("DOOMEternalx64vk.exe", 0x25B4AD8);
				row4DP = new DeepPointer("DOOMEternalx64vk.exe", 0x25B4AF0);
				row5DP = new DeepPointer("DOOMEternalx64vk.exe", 0x25B4AF8);
				row6DP = new DeepPointer("DOOMEternalx64vk.exe", 0x5B83BC0);
				row7DP = new DeepPointer("DOOMEternalx64vk.exe", 0x5B83244);
				row8DP = new DeepPointer("DOOMEternalx64vk.exe", 0x25B4B18);
				row9DP = new DeepPointer("DOOMEternalx64vk.exe", 0x25B4B28);
			}
			else if (moduleSize == 450445312 || moduleSize == 444944384) // BETHESDA VERSION
			{
				raxDP = new DeepPointer("DOOMEternalx64vk.exe", 0x060E38B8, 0x38, 0x28, 0x0);
				eaxDP = new DeepPointer("DOOMEternalx64vk.exe", 0x04C3F008, 0xCB0, 0xDF8, 0x1D0, 0x88);
				velDP = new DeepPointer("DOOMEternalx64vk.exe", 0x04C3F008, 0x1510, 0x598, 0x1D0, 0x3F40);
				rotDP = new DeepPointer("DOOMEternalx64vk.exe", 0x4C46538);
				yawDP = new DeepPointer("DOOMEternalx64vk.exe", 0x616E328);
				row1DP = new DeepPointer("DOOMEternalx64vk.exe", 0x25818C0);
				row2DP = new DeepPointer("DOOMEternalx64vk.exe", 0x25818C8);
				row3DP = new DeepPointer("DOOMEternalx64vk.exe", 0x25818C0 + 0x58);
				row4DP = new DeepPointer("DOOMEternalx64vk.exe", 0x25818C0 + 0x70);
				row5DP = new DeepPointer("DOOMEternalx64vk.exe", 0x25818C0 + 0x78);
				row6DP = new DeepPointer("DOOMEternalx64vk.exe", 0x5B46140);
				row7DP = new DeepPointer("DOOMEternalx64vk.exe", 0x5B457C4);
				row8DP = new DeepPointer("DOOMEternalx64vk.exe", 0x25818C0 + 0x98);
				row9DP = new DeepPointer("DOOMEternalx64vk.exe", 0x25818C0 + 0xA8);
			}
			else //UNKNOWN GAME VERSION
			{
				System.Windows.Forms.MessageBox.Show("This game version is not supported.", "Unsupported Game Version");
				process = null;
			}
		}

		public void DerefPointers()
		{
			IntPtr raxIntPtr;
			raxDP.DerefOffsets(process, out raxIntPtr);
			IntPtr eaxIntPtr;
			eaxDP.DerefOffsets(process, out eaxIntPtr);
			int eaxValue = process.ReadValue<int>(eaxIntPtr);
			eaxValue &= 0xFFFFFF;
			eaxValue *= 0xB0;
			IntPtr posPointer = new IntPtr(raxIntPtr.ToInt64() + eaxValue + 0x30);
			xLocPtr = posPointer;
			yLocPtr = posPointer + 4;
			zLocPtr = posPointer + 8;

			velDP.DerefOffsets(process, out xVelPtr);
			yVelPtr = xVelPtr + 4;
			zVelPtr = xVelPtr + 8;

			rotDP.DerefOffsets(process, out xRotPtr);
			yRotPtr = xRotPtr + 4;

			yawDP.DerefOffsets(process, out xYawPtr);
			yYawPtr = xYawPtr + 4;

			row1DP.DerefOffsets(process, out row1ptr);
			row2DP.DerefOffsets(process, out row2ptr);
			row3DP.DerefOffsets(process, out row3ptr);
			row4DP.DerefOffsets(process, out row4ptr);
			row5DP.DerefOffsets(process, out row5ptr);
			row6DP.DerefOffsets(process, out row6ptr);
			row7DP.DerefOffsets(process, out row7ptr);
			row8DP.DerefOffsets(process, out row8ptr);
			row9DP.DerefOffsets(process, out row9ptr);
		}

	}
}



