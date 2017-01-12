using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;


namespace apcsp
{
    class Program
    {
        //instantiating triggerKey variable. This will be used to store the key used to enable the simulated mouse click to fire
        static private int triggerKey;

        //instantiating client variable. This will be used to store the address of client.dll inside of the csgo process
        static private IntPtr client;

        //instantiating our two classes
        static private memoryFunctions mem = new memoryFunctions();
        static private windowsAPIs winAPI = new windowsAPIs();

        static bool canShoot() //this checks if there is a valid target in the crosshair
        {
            int playerBase = mem.readInt((int)client + 0x00AA5834); //IntPtr to int. 0x00AA5818 is the localplayer address in memory
            int inCrosshair = mem.readInt(playerBase + 0x0000AA70); //reads our local playerbase + the crosshair offset. 0x0000AA70 is the crosshairID address in memory

            return inCrosshair > 0 && inCrosshair < 64 ? true : false; //returns true if the crosshair value is between 0 and 64. false if otherwise
        }
        static void triggerLoop()
        {
            Console.Beep(100, 400); //beeps when started successfully

            while (true)
            {
                if (winAPI.isKeyPushedDown(triggerKey))
                {
                    if (canShoot())
                        winAPI.doMouseClick(); //simulates a mouse click in order to shoot
                }
                Thread.Sleep(1); //brings down CPU usage
            }
        }
        static void Main(string[] args)
        {
            Console.Title = "APCSP FINAL PROJECT";
            Console.WriteLine("key list at http://cherrytree.at/misc/vk.htm"); //list of decimal key codes that this program can take
            Console.Write("Please enter a decimal trigger key value: "); //Prompts the user to enter a trigger key value

            string input = Console.ReadLine(); //get decimal key value as a string 

            triggerKey = Convert.ToInt16(input);

            Console.Write("Confirm that the game is open. Press enter to start ");
            Console.ReadKey(); //waits for key press from the user

            Console.Clear();
            mem.initialize(); //starts intialize method 

            client = mem.getModuleAddress(); //sets the client variable to the result of getModuleAddress, so it's a little cleaner
            triggerLoop(); //starts the triggerLoop
        }
    }
    class memoryFunctions
    {
        private IntPtr processHandle; //for the handle to the game so we can read data from csgo, would be HANDLE processHandle in c++
        private IntPtr client; //for the client module inside of the game, would be DWORD in C++

        //have to import these api calls because C#. kernel32.dll and user32.dll refer to the api's locations
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId); //so we can get access to the game

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] buffer, int size, int lpNumberOfBytesRead); //so we can read data from the game using our handle

        public void initialize()
        {
            Process[] processes = Process.GetProcessesByName("csgo"); //gets all of the processes named csgo

            foreach (Process p in processes) //get the csgo process
            {
                processHandle = OpenProcess(0x0010, false, p.Id); //open a handle to the game using the process id with read only privileges, 0x0010

                foreach (ProcessModule module in p.Modules)
                {
                    if ((module.FileName).IndexOf("client.dll") != -1 && (module.FileName).IndexOf("steamclient.dll") == -1) //checks to make sure that we dont accidentally get the steamclient module address
                    {
                        client = module.BaseAddress; //get the address of the client.dll
                        module.Dispose(); //clean up
                    }
                }
            }
         
        }
        public IntPtr getModuleAddress() //returns the client address that we set to module.BaseAddress in the initialize method
        {
            return client;
        }
        private byte[] ReadMem(int offset, int size) //offset is the adddress to whatever we're trying to read
        {
            byte[] buffer = new byte[size];
            ReadProcessMemory((int)processHandle, offset, buffer, size, 0); //reads from the game our offset into the buffer, a byte array, and returns it. See documentation below for ReadProcessMemory
            return buffer;
        }
        public int readInt(int offset)
        {
            return BitConverter.ToInt32(ReadMem(offset, 4), 0); //converts it to int, 4 is the size that you will need 99% of the time
        }

    }
    class windowsAPIs
    {
        [DllImport("user32.dll")]
        public static extern ushort GetAsyncKeyState(int vKey); //so we can see if our triggerkey is pressed

        [DllImport("user32.dll")]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo); //so we can simulate a mouse click

        //mouse flags
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;

        public void doMouseClick()
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }
        public bool isKeyPushedDown(int vKey)
        {
            return 0 != (GetAsyncKeyState(vKey) & 0x8000); //bitwise checks to see if key is pressed down
        }
    }
}

//WINAPI DOCUMENTATION///////////////////////////////////////////////////////////////////////////////////////////////////////
// ReadProcessMemory: https://msdn.microsoft.com/en-us/library/windows/desktop/ms680553(v=vs.85).aspx   //
// OpenProcess: https://msdn.microsoft.com/en-us/library/windows/desktop/ms684320(v=vs.85).aspx                            //
// GetAsyncKeyState: https://msdn.microsoft.com/en-us/library/windows/desktop/ms646293(v=vs.85).aspx                       //
// mouse_event: https://msdn.microsoft.com/en-us/library/windows/desktop/ms646260(v=vs.85).aspx                            //
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
